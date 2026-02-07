###############################################################################
# Phase 4 - Comprehensive HTTP Tests
# Elite Clinic API - Communication, Booking, Public SEO, Notifications
###############################################################################

$ErrorActionPreference = "Continue"
$base = "http://localhost:5094"
$pass = 0; $fail = 0; $total = 0

function Test($name, $scriptBlock) {
    $script:total++
    try {
        $result = & $scriptBlock
        if ($result -eq $true) {
            [Console]::WriteLine("  PASS  $name")
            $script:pass++
        } else {
            [Console]::WriteLine("  FAIL  $name (returned: $result)")
            $script:fail++
        }
    } catch {
        [Console]::WriteLine("  FAIL  $name ($_)")
        $script:fail++
    }
}

function MakeHeaders($token, $tenant) {
    $hdrs = @{}
    if ($token) { $hdrs["Authorization"] = "Bearer $token" }
    if ($tenant) { $hdrs["X-Tenant"] = $tenant }
    return $hdrs
}

function Api($method, $path, $headers, $body) {
    $params = @{
        Uri = "$base$path"
        Method = $method
        Headers = $headers
        ContentType = "application/json"
        UseBasicParsing = $true
    }
    if ($body) { $params["Body"] = ($body | ConvertTo-Json -Depth 10 -Compress) }
    try {
        $response = Invoke-WebRequest @params
        return $response.Content | ConvertFrom-Json
    } catch {
        $sr = $_.Exception.Response
        if ($sr) {
            $statusCode = [int]$sr.StatusCode
            $errText = $null
            try {
                $stream = $sr.GetResponseStream()
                if ($stream -and $stream.CanRead) {
                    if ($stream.CanSeek) { $stream.Position = 0 }
                    $reader = New-Object System.IO.StreamReader($stream)
                    $errText = $reader.ReadToEnd()
                    $reader.Close()
                }
            } catch {}
            if ($errText) {
                try { return $errText | ConvertFrom-Json } catch {}
            }
            return [PSCustomObject]@{ success = $false; message = "HTTP $statusCode"; statusCode = $statusCode }
        }
        throw
    }
}

[Console]::WriteLine("")
[Console]::WriteLine("========== PHASE 4 TESTS ==========")

###############################################################################
# AUTHENTICATION & SETUP
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- Authenticating --")

$ownerLogin = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="owner_demo";password="Owner@123456"}
$ownerToken = $ownerLogin.data.token

$staffLogin = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="staff_sara";password="Staff@123456"}
$staffToken = $staffLogin.data.token

$doc1Login  = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="dr_khaled";password="Doctor@123456"}
$doc1Token  = $doc1Login.data.token

$doc2Login  = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="dr_mona";password="Doctor@123456"}
$doc2Token  = $doc2Login.data.token

$pat1Login  = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="patient_demo-clinic_1";password="Patient@1234"}
$pat1Token  = $pat1Login.data.token

$pat3Login  = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="patient_demo-clinic_3";password="Patient@1234"}
$pat3Token  = $pat3Login.data.token

$superLogin = Api POST "/api/auth/login" @{"Content-Type"="application/json"} @{username="superadmin";password="Admin@123456"}
$superToken = $superLogin.data.token

if (-not $ownerToken -or -not $doc1Token -or -not $pat1Token) {
    [Console]::WriteLine("FATAL: Cannot get tokens")
    exit 1
}
[Console]::WriteLine("  Tokens acquired: Owner, Staff, Doctor1, Doctor2, Patient1, Patient3, SuperAdmin")

$ownerH = MakeHeaders $ownerToken "demo-clinic"
$staffH = MakeHeaders $staffToken "demo-clinic"
$doc1H  = MakeHeaders $doc1Token  "demo-clinic"
$doc2H  = MakeHeaders $doc2Token  "demo-clinic"
$pat1H  = MakeHeaders $pat1Token  "demo-clinic"
$pat3H  = MakeHeaders $pat3Token  "demo-clinic"
$superH = MakeHeaders $superToken $null

# Get doctor IDs from paginated list
$doctorsList = (Api GET "/api/clinic/doctors" $ownerH).data.items
$doctor1Id = ($doctorsList | Where-Object { $_.name -match "Khaled" }).id
$doctor2Id = ($doctorsList | Where-Object { $_.name -match "Mona" }).id

if (-not $doctor1Id -or -not $doctor2Id) {
    [Console]::WriteLine("FATAL: Cannot find doctor IDs")
    exit 1
}
[Console]::WriteLine("  Doctor1 ID: $doctor1Id")
[Console]::WriteLine("  Doctor2 ID: $doctor2Id")

# Get doctor services from single doctor GET
$doc1Detail = (Api GET "/api/clinic/doctors/$doctor1Id" $ownerH).data
$service1Id = $doc1Detail.services[0].id
[Console]::WriteLine("  Service1 ID: $service1Id")

# Get patient1 userId and staff userId
$pat1Me = (Api GET "/api/auth/me" $pat1H).data
$pat1UserId = $pat1Me.id
$staffMe = (Api GET "/api/auth/me" $staffH).data
$staffUserId = $staffMe.id

# Get tenantId for feature-flag endpoint
$tenantsList = (Api GET "/api/platform/tenants" $superH).data.items
$tenantId = ($tenantsList | Where-Object { $_.slug -eq "demo-clinic" }).id
[Console]::WriteLine("  TenantId: $tenantId")

# Make sure flags are enabled before tests
$flagH = MakeHeaders $superToken $null
Api PUT "/api/platform/feature-flags/$tenantId" $flagH @{onlineBooking=$true;pwaNotifications=$true} | Out-Null

###############################################################################
# SECTION 1: PUBLIC SEO ENDPOINTS (no auth, no X-Tenant)
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S1: Public SEO Endpoints --")

Test "PB01 GET /api/public/slug/clinic returns 200" {
    $r = Api GET "/api/public/demo-clinic/clinic" @{}
    $r.success -eq $true -and $r.data.clinicName -ne $null -and $r.data.tenantSlug -eq "demo-clinic"
}

Test "PB02 Public clinic shows isActive=true" {
    $r = Api GET "/api/public/demo-clinic/clinic" @{}
    $r.data.isActive -eq $true
}

Test "PB03 Public clinic shows bookingEnabled" {
    $r = Api GET "/api/public/demo-clinic/clinic" @{}
    $r.data.bookingEnabled -eq $true
}

Test "PB04 GET /api/public/slug/doctors returns list" {
    $r = Api GET "/api/public/demo-clinic/doctors" @{}
    $r.success -eq $true -and $r.data.Count -ge 2
}

Test "PB05 Public doctors include name and specialty" {
    $r = Api GET "/api/public/demo-clinic/doctors" @{}
    $d = $r.data[0]
    $d.name -ne $null -and $d.specialty -ne $null
}

Test "PB06 Public doctors include services" {
    $r = Api GET "/api/public/demo-clinic/doctors" @{}
    $d = $r.data | Where-Object { $_.services.Count -gt 0 }
    $d.Count -ge 1
}

Test "PB07 GET /api/public/slug/services returns list" {
    $r = Api GET "/api/public/demo-clinic/services" @{}
    $r.success -eq $true -and $r.data.Count -ge 1
}

Test "PB08 Public services include price" {
    $r = Api GET "/api/public/demo-clinic/services" @{}
    $r.data[0].price -gt 0
}

Test "PB09 GET /api/public/slug/working-hours returns list" {
    $r = Api GET "/api/public/demo-clinic/working-hours" @{}
    $r.success -eq $true -and $r.data.Count -ge 0
}

Test "PB10 Non-existent slug returns 404" {
    $r = Api GET "/api/public/non-existent-clinic/clinic" @{}
    $r.success -eq $false -and $r.message -eq "Clinic not found"
}

Test "PB11 Non-existent slug doctors returns 404" {
    $r = Api GET "/api/public/non-existent-clinic/doctors" @{}
    $r.success -eq $false -and $r.message -eq "Clinic not found"
}

Test "PB12 Non-existent slug services returns 404" {
    $r = Api GET "/api/public/non-existent-clinic/services" @{}
    $r.success -eq $false -and $r.message -eq "Clinic not found"
}

###############################################################################
# SECTION 2: WHATSAPP MESSAGE QUEUE
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S2: WhatsApp Message Queue --")

Test "MQ01 Send WhatsApp message (patient_credentials)" {
    $body = @{
        templateName = "patient_credentials"
        recipientPhone = "+966500100001"
        channel = "WhatsApp"
        variables = @{ patientName = "test patient"; clinicName = "Demo Clinic" }
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $r.success -eq $true -and $r.data.templateName -eq "patient_credentials" -and $r.data.status -eq "Sent"
}

Test "MQ02 Send WhatsApp message (queue_ticket_issued)" {
    $body = @{
        templateName = "queue_ticket_issued"
        recipientPhone = "+966500100002"
        channel = "WhatsApp"
        variables = @{ patientName = "Ahmad"; ticketNumber = "5"; doctorName = "Dr. Khaled" }
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $r.success -eq $true -and $r.data.channel -eq "WhatsApp"
}

Test "MQ03 Send WhatsApp message (your_turn)" {
    $body = @{
        templateName = "your_turn"
        recipientPhone = "+966500100003"
        channel = "WhatsApp"
        variables = @{ patientName = "Sara"; doctorName = "Dr. Mona" }
    }
    $r = Api POST "/api/clinic/messages/send" $staffH $body
    $r.success -eq $true -and $r.data.templateName -eq "your_turn"
}

Test "MQ04 Send WhatsApp message (visit_summary)" {
    $body = @{
        templateName = "visit_summary"
        recipientPhone = "+966500100004"
        channel = "WhatsApp"
        variables = @{ patientName = "Ali"; doctorName = "Dr. Khaled"; diagnosis = "Routine checkup" }
    }
    $r = Api POST "/api/clinic/messages/send" $doc1H $body
    $r.success -eq $true -and $r.data.templateName -eq "visit_summary"
}

Test "MQ05 Send WhatsApp message (followup_reminder)" {
    $body = @{
        templateName = "followup_reminder"
        recipientPhone = "+966500100005"
        channel = "WhatsApp"
        variables = @{ patientName = "Layla"; doctorName = "Dr. Mona"; followUpDate = "2026-02-15" }
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $r.success -eq $true
}

Test "MQ06 Send WhatsApp message (password_reset)" {
    $body = @{
        templateName = "password_reset"
        recipientPhone = "+966500100006"
        channel = "WhatsApp"
        variables = @{ patientName = "Omar"; username = "patient_test"; newPassword = "Pass123" }
    }
    $r = Api POST "/api/clinic/messages/send" $staffH $body
    $r.success -eq $true
}

Test "MQ07 Send WhatsApp message (booking_confirmation)" {
    $body = @{
        templateName = "booking_confirmation"
        recipientPhone = "+966500100007"
        channel = "WhatsApp"
        variables = @{ patientName = "Khalid"; doctorName = "Dr. Khaled"; date = "2026-02-10"; time = "10:00" }
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $r.success -eq $true
}

Test "MQ08 Invalid template name returns error" {
    $body = @{
        templateName = "invalid_template"
        recipientPhone = "+966500100008"
        channel = "WhatsApp"
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $r.success -eq $false
}

Test "MQ09 WhatsApp without phone returns error" {
    $body = @{
        templateName = "patient_credentials"
        channel = "WhatsApp"
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $r.success -eq $false
}

Test "MQ10 Message status is Sent after sending" {
    $body = @{
        templateName = "patient_credentials"
        recipientPhone = "+966500100009"
        channel = "WhatsApp"
        variables = @{ patientName = "Test" }
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $r.data.status -eq "Sent" -and $r.data.attemptCount -eq 1
}

Test "MQ11 Get message by ID" {
    $body = @{
        templateName = "patient_credentials"
        recipientPhone = "+966500100010"
        channel = "WhatsApp"
        variables = @{ patientName = "Lookup Test" }
    }
    $r = Api POST "/api/clinic/messages/send" $ownerH $body
    $mid = $r.data.id
    $r2 = Api GET "/api/clinic/messages/$mid" $ownerH
    $r2.success -eq $true -and $r2.data.id -eq $mid
}

Test "MQ12 List all messages (paginated)" {
    $r = Api GET '/api/clinic/messages?pageNumber=1&pageSize=5' $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "MQ13 Filter messages by template name" {
    $r = Api GET '/api/clinic/messages?templateName=patient_credentials' $ownerH
    $r.success -eq $true -and ($r.data.items | ForEach-Object { $_.templateName }) -contains "patient_credentials"
}

Test "MQ14 Filter messages by channel" {
    $r = Api GET '/api/clinic/messages?channel=WhatsApp' $ownerH
    $r.success -eq $true -and ($r.data.items | Where-Object { $_.channel -ne "WhatsApp" }).Count -eq 0
}

Test "MQ15 Patient cannot send messages" {
    $body = @{
        templateName = "patient_credentials"
        recipientPhone = "+966500100011"
        channel = "WhatsApp"
    }
    $r = Api POST "/api/clinic/messages/send" $pat1H $body
    $r.success -eq $false -or $r.statusCode -eq 403
}

Test "MQ16 Multiple messages exist after sends" {
    $r = Api GET '/api/clinic/messages?pageSize=50' $ownerH
    $r.data.items.Count -ge 5
}

###############################################################################
# SECTION 3: ONLINE BOOKING
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S3: Online Booking --")

# Use a unique future date range with random minute offsets for idempotency
$rand = Get-Random -Minimum 50 -Maximum 90
$bkDate1 = (Get-Date).AddDays($rand).ToString("yyyy-MM-dd")
$bkDate2 = (Get-Date).AddDays($rand + 1).ToString("yyyy-MM-dd")
$bkDate3 = (Get-Date).AddDays($rand + 2).ToString("yyyy-MM-dd")
$randMin = Get-Random -Minimum 10 -Maximum 50
$bkTime1 = "09:" + $randMin.ToString("00")
$bkTime2 = "14:" + $randMin.ToString("00")
$bkTime3 = "16:" + $randMin.ToString("00")

Test "BK01 Patient creates a booking" {
    $body = @{
        doctorId = $doctor1Id
        doctorServiceId = $service1Id
        bookingDate = $bkDate1
        bookingTime = $bkTime1
        notes = "Phase4 test booking 1"
    }
    $r = Api POST "/api/clinic/bookings" $pat1H $body
    $script:booking1Id = $r.data.id
    $r.success -eq $true -and $r.data.status -eq "Confirmed"
}

Test "BK02 Booking includes patient and doctor names" {
    $r = Api GET "/api/clinic/bookings/$($script:booking1Id)" $ownerH
    $r.data.patientName -ne $null -and $r.data.doctorName -ne $null
}

Test "BK03 Booking includes service name" {
    $r = Api GET "/api/clinic/bookings/$($script:booking1Id)" $ownerH
    $r.data.serviceName -ne $null
}

Test "BK04 Patient can get their bookings (GET /my)" {
    $r = Api GET "/api/clinic/bookings/my" $pat1H
    $r.success -eq $true -and $r.data.Count -ge 1
}

Test "BK05 Staff can list all bookings (paginated)" {
    $r = Api GET "/api/clinic/bookings" $staffH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "BK06 Filter bookings by doctorId" {
    $r = Api GET "/api/clinic/bookings?doctorId=$doctor1Id" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "BK07 Filter bookings by status" {
    $r = Api GET '/api/clinic/bookings?status=Confirmed' $ownerH
    $r.success -eq $true
}

Test "BK08 Duplicate booking returns error" {
    $body = @{
        doctorId = $doctor1Id
        doctorServiceId = $service1Id
        bookingDate = $bkDate1
        bookingTime = $bkTime1
    }
    $r = Api POST "/api/clinic/bookings" $pat1H $body
    $r.success -eq $false
}

Test "BK09 Booking with past date returns error" {
    $body = @{
        doctorId = $doctor1Id
        bookingDate = "2020-01-01"
        bookingTime = $bkTime1
    }
    $r = Api POST "/api/clinic/bookings" $pat1H $body
    $r.success -eq $false
}

Test "BK10 Patient creates second booking (different date/time)" {
    $body = @{
        doctorId = $doctor1Id
        bookingDate = $bkDate2
        bookingTime = $bkTime2
        notes = "Phase4 afternoon booking"
    }
    $r = Api POST "/api/clinic/bookings" $pat1H $body
    $script:booking2Id = $r.data.id
    $r.success -eq $true -and $r.data.status -eq "Confirmed"
}

Test "BK11 Reschedule a booking" {
    $body = @{
        bookingDate = $bkDate3
        bookingTime = "11:00"
    }
    $r = Api POST "/api/clinic/bookings/$($script:booking2Id)/reschedule" $ownerH $body
    $r.success -eq $true -and $r.data.bookingTime -eq "11:00"
}

Test "BK12 Cancel a booking" {
    $body = @{ cancellationReason = "Personal reasons" }
    $r = Api POST "/api/clinic/bookings/$($script:booking2Id)/cancel" $ownerH $body
    $r.success -eq $true -and $r.data.status -eq "Cancelled"
}

Test "BK13 Cannot cancel already cancelled booking" {
    $body = @{ cancellationReason = "Trying again" }
    $r = Api POST "/api/clinic/bookings/$($script:booking2Id)/cancel" $ownerH $body
    $r.success -eq $false
}

Test "BK14 Another patient books (pat3)" {
    $body = @{
        doctorId = $doctor2Id
        bookingDate = $bkDate1
        bookingTime = $bkTime3
        notes = "Patient 3 booking"
    }
    $r = Api POST "/api/clinic/bookings" $pat3H $body
    $script:booking3Id = $r.data.id
    $r.success -eq $true
}

Test "BK15 Booking with non-existent doctor fails" {
    $fakeId = [Guid]::NewGuid().ToString()
    $body = @{
        doctorId = $fakeId
        bookingDate = $bkDate1
        bookingTime = $bkTime1
    }
    $r = Api POST "/api/clinic/bookings" $pat1H $body
    $r.success -eq $false
}

Test "BK16 Owner can list all bookings" {
    $r = Api GET "/api/clinic/bookings" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 2
}

###############################################################################
# SECTION 4: DOCTOR-TO-RECEPTION NOTES
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S4: Doctor-to-Reception Notes --")

Test "DN01 Doctor sends a note" {
    $body = @{ message = "Phase4 test: prepare exam room A" }
    $r = Api POST "/api/clinic/doctor-notes" $doc1H $body
    $script:note1Id = $r.data.id
    $r.success -eq $true -and $r.data.isRead -eq $false
}

Test "DN02 Doctor note includes doctor name" {
    $r = Api GET '/api/clinic/doctor-notes?pageSize=50' $ownerH
    $note = $r.data.items | Where-Object { $_.id -eq $script:note1Id }
    $note.doctorName -ne $null
}

Test "DN03 Second doctor sends a note" {
    $body = @{ message = "Phase4 test: follow-up for Room B" }
    $r = Api POST "/api/clinic/doctor-notes" $doc2H $body
    $script:note2Id = $r.data.id
    $r.success -eq $true
}

Test "DN04 Staff gets unread notes" {
    $r = Api GET "/api/clinic/doctor-notes/unread" $staffH
    $r.success -eq $true -and $r.data.Count -ge 1
}

Test "DN05 Staff gets all notes (paginated)" {
    $r = Api GET '/api/clinic/doctor-notes?pageSize=10' $staffH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "DN06 Filter unread only" {
    $r = Api GET '/api/clinic/doctor-notes?unreadOnly=true' $staffH
    $r.success -eq $true -and ($r.data.items | Where-Object { $_.isRead -eq $true }).Count -eq 0
}

Test "DN07 Mark note as read" {
    $r = Api POST "/api/clinic/doctor-notes/$($script:note1Id)/read" $staffH
    $r.success -eq $true -and $r.data.isRead -eq $true -and $r.data.readAt -ne $null
}

Test "DN08 Cannot mark already-read note again" {
    $r = Api POST "/api/clinic/doctor-notes/$($script:note1Id)/read" $staffH
    $r.success -eq $false
}

Test "DN09 Empty message returns error" {
    $body = @{ message = "" }
    $r = Api POST "/api/clinic/doctor-notes" $doc1H $body
    $r.success -eq $false
}

Test "DN10 Patient cannot send doctor notes" {
    $body = @{ message = "This should fail" }
    $r = Api POST "/api/clinic/doctor-notes" $pat1H $body
    $r.success -eq $false -or $r.statusCode -eq 403
}

Test "DN11 Note lifecycle: unread then read" {
    $body = @{ message = "Phase4 lifecycle test note" }
    $cr = Api POST "/api/clinic/doctor-notes" $doc1H $body
    $nid = $cr.data.id
    $unread = Api GET "/api/clinic/doctor-notes/unread" $staffH
    $foundUnread = @($unread.data | Where-Object { $_.id -eq $nid }).Count -ge 1
    $markRead = Api POST "/api/clinic/doctor-notes/$nid/read" $staffH
    ($foundUnread -eq $true) -and ($markRead.data.isRead -eq $true)
}

Test "DN12 Multiple notes from same doctor" {
    $r = Api GET '/api/clinic/doctor-notes?pageSize=50' $ownerH
    $doc1Notes = $r.data.items | Where-Object { $_.doctorName -match "Khaled" }
    $doc1Notes.Count -ge 2
}

###############################################################################
# SECTION 5: PWA NOTIFICATION INFRASTRUCTURE
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S5: PWA Notification Infrastructure --")

Test "NF01 Subscribe to push notifications" {
    $body = @{
        endpoint = "https://fcm.googleapis.com/fcm/send/p4-test-endpoint-001"
        p256dh = "p4-test-p256dh-key-001"
        auth = "p4-test-auth-key-001"
    }
    $r = Api POST "/api/clinic/notifications/subscribe" $pat1H $body
    $script:sub1Id = $r.data.id
    $r.success -eq $true -and $r.data.isActive -eq $true
}

Test "NF02 Get my subscriptions" {
    $r = Api GET "/api/clinic/notifications/my" $pat1H
    $r.success -eq $true -and $r.data.Count -ge 1
}

Test "NF03 Duplicate subscription reactivates" {
    $body = @{
        endpoint = "https://fcm.googleapis.com/fcm/send/p4-test-endpoint-001"
        p256dh = "updated-p256dh"
        auth = "updated-auth"
    }
    $r = Api POST "/api/clinic/notifications/subscribe" $pat1H $body
    $r.success -eq $true
}

Test "NF04 Another user subscribes" {
    $body = @{
        endpoint = "https://fcm.googleapis.com/fcm/send/p4-test-endpoint-002"
        p256dh = "p4-test-p256dh-key-002"
        auth = "p4-test-auth-key-002"
    }
    $r = Api POST "/api/clinic/notifications/subscribe" $pat3H $body
    $script:sub2Id = $r.data.id
    $r.success -eq $true
}

Test "NF05 Send PWA notification to patient" {
    $body = @{
        userId = $pat1UserId
        title = "Medication Reminder"
        body = "Time for Amoxicillin 500mg"
        templateName = "medication_reminder"
    }
    $r = Api POST "/api/clinic/notifications/send" $ownerH $body
    $r.success -eq $true -and $r.data.channel -eq "PWA" -and $r.data.status -eq "Sent"
}

Test "NF06 Send notification logs to message log" {
    $r = Api GET '/api/clinic/messages?channel=PWA' $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "NF07 Send notification to user without subscription fails" {
    $body = @{
        userId = $staffUserId
        title = "Test"
        body = "Should fail"
    }
    $r = Api POST "/api/clinic/notifications/send" $ownerH $body
    $r.success -eq $false
}

Test "NF08 Unsubscribe" {
    $r = Api DELETE "/api/clinic/notifications/$($script:sub1Id)" $pat1H
    $r.success -eq $true
}

Test "NF09 After unsubscribe, subscription deactivated" {
    $r = Api GET "/api/clinic/notifications/my" $pat1H
    $active = @($r.data | Where-Object { $_.endpoint -eq "https://fcm.googleapis.com/fcm/send/p4-test-endpoint-001" -and $_.isActive -eq $true })
    $r.success -eq $true -and $active.Count -eq 0
}

Test "NF10 Re-subscribe after unsubscribe" {
    $body = @{
        endpoint = "https://fcm.googleapis.com/fcm/send/p4-test-endpoint-003"
        p256dh = "new-p256dh"
        auth = "new-auth"
    }
    $r = Api POST "/api/clinic/notifications/subscribe" $pat1H $body
    $r.success -eq $true -and $r.data.isActive -eq $true
}

###############################################################################
# SECTION 6: MESSAGE RETRY LOGIC
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S6: Message Retry Logic --")

Test "MR01 Cannot retry a non-failed message" {
    $msgs = (Api GET '/api/clinic/messages?status=Sent&pageSize=1' $ownerH).data.items
    if ($msgs.Count -ge 1) {
        $r = Api POST "/api/clinic/messages/$($msgs[0].id)/retry" $ownerH
        $r.success -eq $false
    } else { $true }
}

Test "MR02 Non-existent message retry returns error" {
    $fakeId = [Guid]::NewGuid().ToString()
    $r = Api POST "/api/clinic/messages/$fakeId/retry" $ownerH
    $r.success -eq $false
}

###############################################################################
# SECTION 7: FEATURE FLAG GATING
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S7: Feature Flag Gating --")

Test "FF01 Disable OnlineBooking flag" {
    $body = @{ onlineBooking = $false }
    $r = Api PUT "/api/platform/feature-flags/$tenantId" $flagH $body
    $r.success -eq $true -and $r.data.onlineBooking -eq $false
}

Test "FF02 Booking fails when OnlineBooking disabled" {
    $ffDate = (Get-Date).AddDays(99).ToString("yyyy-MM-dd")
    $body = @{
        doctorId = $doctor1Id
        bookingDate = $ffDate
        bookingTime = "09:00"
    }
    $r = Api POST "/api/clinic/bookings" $pat1H $body
    $r.success -eq $false -and $r.message -match "not enabled"
}

Test "FF03 Re-enable OnlineBooking flag" {
    $body = @{ onlineBooking = $true }
    $r = Api PUT "/api/platform/feature-flags/$tenantId" $flagH $body
    $r.success -eq $true -and $r.data.onlineBooking -eq $true
}

Test "FF04 Disable PwaNotifications flag" {
    $body = @{ pwaNotifications = $false }
    $r = Api PUT "/api/platform/feature-flags/$tenantId" $flagH $body
    $r.success -eq $true -and $r.data.pwaNotifications -eq $false
}

Test "FF05 Notification subscribe fails when PWA disabled" {
    $body = @{
        endpoint = "https://test-endpoint-disabled-flag"
        p256dh = "test"
        auth = "test"
    }
    $r = Api POST "/api/clinic/notifications/subscribe" $pat1H $body
    $r.success -eq $false -and $r.message -match "not enabled"
}

Test "FF06 Re-enable PwaNotifications flag" {
    $body = @{ pwaNotifications = $true }
    $r = Api PUT "/api/platform/feature-flags/$tenantId" $flagH $body
    $r.success -eq $true -and $r.data.pwaNotifications -eq $true
}

###############################################################################
# SECTION 8: AUTHORIZATION AND SECURITY
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S8: Authorization and Security --")

Test "AU01 Public endpoints work without any auth" {
    $r = Api GET "/api/public/demo-clinic/clinic" @{}
    $r.success -eq $true
}

Test "AU02 Public endpoints work without X-Tenant" {
    $r = Api GET "/api/public/demo-clinic/doctors" @{}
    $r.success -eq $true
}

Test "AU03 Messages require auth" {
    $r = Api GET "/api/clinic/messages" (MakeHeaders $null "demo-clinic")
    $r.success -eq $false -or $r.statusCode -eq 401
}

Test "AU04 Bookings require auth" {
    $r = Api GET "/api/clinic/bookings/my" (MakeHeaders $null "demo-clinic")
    $r.success -eq $false -or $r.statusCode -eq 401
}

Test "AU05 Doctor notes require auth" {
    $r = Api GET "/api/clinic/doctor-notes/unread" (MakeHeaders $null "demo-clinic")
    $r.success -eq $false -or $r.statusCode -eq 401
}

Test "AU06 Notifications require auth" {
    $r = Api GET "/api/clinic/notifications/my" (MakeHeaders $null "demo-clinic")
    $r.success -eq $false -or $r.statusCode -eq 401
}

Test "AU07 Patient cannot list all messages" {
    $r = Api GET "/api/clinic/messages" $pat1H
    $r.success -eq $false -or $r.statusCode -eq 403
}

Test "AU08 Patient cannot list all bookings" {
    $r = Api GET "/api/clinic/bookings" $pat1H
    $r.success -eq $false -or $r.statusCode -eq 403
}

###############################################################################
# SECTION 9: ENUM SERIALIZATION
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S9: Enum Serialization --")

Test "ES01 BookingStatus serializes as string" {
    if (-not $script:booking1Id) {
        $allBk = (Api GET '/api/clinic/bookings?status=Confirmed&pageSize=1' $ownerH).data.items
        $script:booking1Id = $allBk[0].id
    }
    $r = Api GET "/api/clinic/bookings/$($script:booking1Id)" $ownerH
    $r.data.status -is [string] -and $r.data.status -eq "Confirmed"
}

Test "ES02 MessageChannel serializes as string" {
    $msgs = (Api GET '/api/clinic/messages?pageSize=1' $ownerH).data.items
    $msgs[0].channel -is [string]
}

Test "ES03 MessageStatus serializes as string" {
    $msgs = (Api GET '/api/clinic/messages?pageSize=1' $ownerH).data.items
    $msgs[0].status -is [string]
}

###############################################################################
# SECTION 10: CROSS-MODULE INTEGRATION
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("-- S10: Cross-Module Integration --")

Test "CI01 PWA channel messages include template name" {
    $r = Api GET '/api/clinic/messages?channel=PWA' $ownerH
    if ($r.data.items.Count -ge 1) {
        $r.data.items[0].templateName -ne $null
    } else { $true }
}

Test "CI02 Public endpoint shows correct doctor count" {
    $publicDocs = (Api GET "/api/public/demo-clinic/doctors" @{}).data.Count
    $privateDocs = (Api GET "/api/clinic/doctors" $ownerH).data.items.Count
    $publicDocs -eq $privateDocs
}

Test "CI03 Health endpoint still works" {
    $r = Api GET "/api/health" @{}
    $r.success -eq $true -and $r.data.status -eq "Healthy"
}

Test "CI04 Phase 2 login still works" {
    $r = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="owner_demo";password="Owner@123456"}
    $r.success -eq $true -and $r.data.token -ne $null
}

###############################################################################
# RESULTS
###############################################################################
[Console]::WriteLine("")
[Console]::WriteLine("========================================")
[Console]::WriteLine("  Phase 4 Results: $pass/$total PASS, $fail FAIL")
[Console]::WriteLine("========================================")
[Console]::WriteLine("")
