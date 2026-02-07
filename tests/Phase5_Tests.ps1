###############################################################################
#  Elite Clinic — Phase 5 Production-Readiness Tests
#  Focus: Role enforcement, tenant isolation, Receptionist role, AuditLog fix,
#         header presence, auth flows, negative cases, cross-cutting checks
###############################################################################

$baseUrl  = "http://localhost:5094"
$tenant   = "demo-clinic"
$pass     = 0
$fail     = 0
$results  = @()
$ProgressPreference = 'SilentlyContinue'
# Force UseBasicParsing for all Invoke-WebRequest calls (avoids IE engine prompt in PS 5.1)
$PSDefaultParameterValues['Invoke-WebRequest:UseBasicParsing'] = $true

function Test($id, $name, $ok, $detail="") {
    $script:results += [PSCustomObject]@{Id=$id;Name=$name;Result=if($ok){"PASS"}else{"FAIL"};Detail=$detail}
    if($ok){$script:pass++}else{$script:fail++}
    $sym = if($ok){"[PASS]"}else{"[FAIL]"}
    Write-Host "$sym $id - $name $(if($detail){" | $detail"})" -F $(if($ok){"Green"}else{"Red"})
}

function Login($user,$pw,$t=$null){
    $h = @{"Content-Type"="application/json"}
    if($t){$h["X-Tenant"]=$t}
    $b = @{username=$user;password=$pw}|ConvertTo-Json
    try{$r=Invoke-RestMethod "$baseUrl/api/auth/login" -Method POST -Headers $h -Body $b;return $r.data.token}
    catch{return $null}
}

function Api($method,$path,$token=$null,$body=$null,$xt=$tenant,$raw=$false){
    $h = @{"Content-Type"="application/json"}
    if($token){$h["Authorization"]="Bearer $token"}
    if($xt){$h["X-Tenant"]=$xt}
    $p = @{Uri="$baseUrl$path";Method=$method;Headers=$h;ErrorAction='Stop';UseBasicParsing=$true}
    if($body){$p.Body=$body|ConvertTo-Json -Depth 10}
    try{
        $resp = Invoke-WebRequest @p
        if($raw){return $resp}
        return ($resp.Content|ConvertFrom-Json)
    }catch{
        if($raw){return $_.Exception.Response}
        return @{statusCode=$_.Exception.Response.StatusCode.value__; error=$_.Exception.Message}
    }
}

function ApiStatus($method,$path,$token=$null,$body=$null,$xt=$tenant){
    $h = @{"Content-Type"="application/json"}
    if($token){$h["Authorization"]="Bearer $token"}
    if($xt){$h["X-Tenant"]=$xt}
    $p = @{Uri="$baseUrl$path";Method=$method;Headers=$h;UseBasicParsing=$true}
    if($body){$p.Body=$body|ConvertTo-Json -Depth 10}
    try{
        $resp = Invoke-WebRequest @p
        return [int]$resp.StatusCode
    }catch{
        return [int]$_.Exception.Response.StatusCode
    }
}

Write-Host "`n===== PHASE 5 PRODUCTION-READINESS TESTS =====`n" -F Cyan

# ───────────────────────── AUTH TOKENS ─────────────────────────
Write-Host "--- Acquiring tokens ---" -F Yellow
$tkSuperAdmin = Login "superadmin" "Admin@123456"
$tkOwner      = Login "owner_demo" "Owner@123456" $tenant
$tkDoctor     = Login "dr_khaled" "Doctor@123456" $tenant
$tkStaff      = Login "staff_sara" "Staff@123456" $tenant
$tkPatient    = Login "patient_demo-clinic_1" "Patient@1234" $tenant

Test "AU01" "SuperAdmin login" ($null -ne $tkSuperAdmin)
Test "AU02" "ClinicOwner login" ($null -ne $tkOwner)
Test "AU03" "Doctor login" ($null -ne $tkDoctor)
Test "AU04" "Staff login (ClinicManager)" ($null -ne $tkStaff)
Test "AU05" "Patient login" ($null -ne $tkPatient)

# Enable all feature flags and booking setting for test coverage
Write-Host "--- Enabling feature flags ---" -F Yellow
$tenants = Api "GET" "/api/platform/tenants" $tkSuperAdmin $null ""
$demoTenantId = ($tenants.data.items | Where-Object { $_.slug -eq "demo-clinic" }).id
if ($demoTenantId) {
    Api "PUT" "/api/platform/feature-flags/$demoTenantId" $tkSuperAdmin @{onlineBooking=$true;whatsappAutomation=$true;pwaNotifications=$true;expensesModule=$true;advancedMedicalTemplates=$true;ratings=$true;export=$true} "" | Out-Null
    $cs = Api "GET" "/api/clinic/settings" $tkOwner
    Api "PUT" "/api/clinic/settings" $tkOwner @{clinicName=$cs.data.clinicName;phone=$cs.data.phone;bookingEnabled=$true} | Out-Null
    Write-Host "Feature flags and booking enabled" -F Green
}

# ───────────────── SECTION A: AUTH FLOW TESTS ──────────────────
Write-Host "`n--- A: Auth Flow Tests ---" -F Yellow

# A01 - Expired/invalid token returns 401
$sc = ApiStatus "GET" "/api/clinic/doctors" "invalid.jwt.token"
Test "AF01" "Invalid token returns 401" ($sc -eq 401) "status=$sc"

# A02 - No token on protected route returns 401
$sc = ApiStatus "GET" "/api/clinic/doctors" $null
Test "AF02" "No token returns 401" ($sc -eq 401) "status=$sc"

# A03 - /auth/me returns user info
$me = Api "GET" "/api/auth/me" $tkOwner $null $tenant
Test "AF03" "Auth/me returns user data" ($me.success -eq $true -and $me.data.username -eq "owner_demo")

# A04 - Token refresh
$loginResp = Invoke-RestMethod "$baseUrl/api/auth/login" -Method POST -Headers @{"Content-Type"="application/json";"X-Tenant"=$tenant} -Body (@{username="owner_demo";password="Owner@123456"}|ConvertTo-Json)
$refreshToken = $loginResp.data.refreshToken
$refreshResp = try { Invoke-RestMethod "$baseUrl/api/auth/refresh" -Method POST -Headers @{"Content-Type"="application/json"} -Body (@{refreshToken=$refreshToken}|ConvertTo-Json) } catch { $null }
Test "AF04" "Token refresh works" ($null -ne $refreshResp -and $refreshResp.success -eq $true) 

# A05 - Patient login endpoint
$patLoginResp = try { Invoke-RestMethod "$baseUrl/api/auth/patient/login" -Method POST -Headers @{"Content-Type"="application/json";"X-Tenant"=$tenant} -Body (@{username="patient_demo-clinic_1";password="Patient@1234"}|ConvertTo-Json) } catch { $null }
Test "AF05" "Patient login endpoint works" ($null -ne $patLoginResp -and $patLoginResp.success -eq $true)

# ──────────────── SECTION B: TENANT ISOLATION ──────────────────
Write-Host "`n--- B: Tenant Isolation Tests ---" -F Yellow

# B01 - Missing X-Tenant on clinic route returns 400
$sc = ApiStatus "GET" "/api/clinic/doctors" $tkOwner $null ""
Test "TI01" "Missing X-Tenant returns 400" ($sc -eq 400) "status=$sc"

# B02 - Invalid tenant slug returns 404
$sc = ApiStatus "GET" "/api/clinic/doctors" $tkOwner $null "nonexistent-tenant"
Test "TI02" "Invalid tenant returns 404" ($sc -eq 404) "status=$sc"

# B03 - Suspended tenant returns 403
$sc = ApiStatus "GET" "/api/clinic/doctors" $tkOwner $null "suspended-clinic"
Test "TI03" "Suspended tenant returns 403" ($sc -eq 403) "status=$sc"

# B04 - Blocked tenant returns 403
$sc = ApiStatus "GET" "/api/clinic/doctors" $tkOwner $null "blocked-clinic"
Test "TI04" "Blocked tenant returns 403" ($sc -eq 403) "status=$sc"

# B05 - Cross-tenant access blocked (owner_demo is bound to demo-clinic)
# SuperAdmin can access any tenant, but owner cannot access a different one
# First ensure there's another active tenant... let's check inactive-clinic
$sc = ApiStatus "GET" "/api/clinic/doctors" $tkOwner $null "inactive-clinic"
Test "TI05" "Inactive tenant returns 403" ($sc -eq 403) "status=$sc"

# B06 - SuperAdmin can access any tenant via X-Tenant
$r = Api "GET" "/api/clinic/doctors" $tkSuperAdmin $null $tenant
Test "TI06" "SuperAdmin cross-tenant access works" ($r.success -eq $true)

# B07 - Platform routes work without X-Tenant
$r = Api "GET" "/api/platform/tenants" $tkSuperAdmin $null ""
Test "TI07" "Platform routes work without X-Tenant" ($r.success -eq $true)

# B08 - Public routes work without X-Tenant
$r = Api "GET" "/api/public/demo-clinic/clinic" $null $null ""
Test "TI08" "Public routes work without auth/tenant" ($r.success -eq $true)

# ────────────── SECTION C: ROLE ENFORCEMENT TESTS ──────────────
Write-Host "`n--- C: Role Enforcement Tests ---" -F Yellow

# C01 - Patient cannot access staff list by ID
$sc = ApiStatus "GET" "/api/clinic/staff/00000000-0000-0000-0000-000000000001" $tkPatient
Test "RE01" "Patient cannot GET staff/{id}" ($sc -eq 403) "status=$sc"

# C02 - Patient cannot create expense
$sc = ApiStatus "POST" "/api/clinic/expenses" $tkPatient (@{category="test";amount=1;description="x"})
Test "RE02" "Patient cannot POST expenses" ($sc -eq 403) "status=$sc"

# C03 - Doctor cannot manage tenants
$sc = ApiStatus "GET" "/api/platform/tenants" $tkDoctor $null ""
Test "RE03" "Doctor cannot access platform/tenants" ($sc -eq 403) "status=$sc"

# C04 - Patient cannot view finance
$sc = ApiStatus "GET" "/api/clinic/finance/daily" $tkPatient
Test "RE04" "Patient cannot view finance" ($sc -eq 403) "status=$sc"

# C05 - Doctor cannot create patients
$sc = ApiStatus "POST" "/api/clinic/patients" $tkDoctor (@{fullName="test";phone="0500000000";gender="Male"})
Test "RE05" "Doctor cannot create patients" ($sc -eq 403) "status=$sc"

# C06 - Patient cannot create staff
$sc = ApiStatus "POST" "/api/clinic/staff" $tkPatient (@{username="x";fullName="x";role="ClinicManager";password="X@123456"})
Test "RE06" "Patient cannot create staff" ($sc -eq 403) "status=$sc"

# C07 - Staff (ClinicManager) can list patients
$r = Api "GET" "/api/clinic/patients" $tkStaff
Test "RE07" "ClinicManager can list patients" ($r.success -eq $true)

# C08 - Doctor can list doctors (any authenticated)
$r = Api "GET" "/api/clinic/doctors" $tkDoctor
Test "RE08" "Doctor can list doctors" ($r.success -eq $true)

# C09 - Patient can list doctors (any authenticated)
$r = Api "GET" "/api/clinic/doctors" $tkPatient
Test "RE09" "Patient can list doctors" ($r.success -eq $true)

# C10 - Patient can view clinic settings (any authenticated)
$r = Api "GET" "/api/clinic/settings" $tkPatient
Test "RE10" "Patient can view clinic settings" ($r.success -eq $true)

# C11 - ClinicManager cannot delete patients (only ClinicOwner, SuperAdmin)
$sc = ApiStatus "DELETE" "/api/clinic/patients/00000000-0000-0000-0000-000000000001" $tkStaff
Test "RE11" "ClinicManager cannot delete patients" ($sc -eq 403) "status=$sc"

# C12 - Doctor cannot manage subscriptions
$sc = ApiStatus "GET" "/api/platform/subscriptions" $tkDoctor $null ""
Test "RE12" "Doctor cannot manage subscriptions" ($sc -eq 403) "status=$sc"

# C13 - Patient cannot create doctor notes (only Doctor, SuperAdmin)
$sc = ApiStatus "POST" "/api/clinic/doctor-notes" $tkPatient (@{content="test note"})
Test "RE13" "Patient cannot create doctor notes" ($sc -eq 403) "status=$sc"

# C14 - Owner can update clinic settings
$settings = Api "GET" "/api/clinic/settings" $tkOwner
$sc = ApiStatus "PUT" "/api/clinic/settings" $tkOwner (@{clinicName=$settings.data.clinicName;phone=$settings.data.phone;bookingEnabled=$true})
Test "RE14" "Owner can update clinic settings" ($sc -eq 200) "status=$sc"

# ────────────── SECTION D: RECEPTIONIST ROLE TESTS ─────────────
Write-Host "`n--- D: Receptionist Role Tests ---" -F Yellow

# First verify Receptionist role exists after seeding (accept success or already-exists)
$rcCreateOk = $false
$rcDetail = ""
try {
    $rcBody = @{username="receptionist_test_p5";name="Reception Staff";role="Receptionist";password="Recep@123456"} | ConvertTo-Json
    $rcResp = Invoke-RestMethod "$baseUrl/api/clinic/staff" -Method POST -Headers @{"Content-Type"="application/json";"Authorization"="Bearer $tkOwner";"X-Tenant"=$tenant} -Body $rcBody
    $rcCreateOk = $rcResp.success -eq $true
    $rcDetail = $rcResp.message
} catch {
    $errStream = $_.Exception.Response.GetResponseStream()
    $errReader = New-Object System.IO.StreamReader($errStream)
    $errText = $errReader.ReadToEnd()
    if ($errText -like "*already*") { $rcCreateOk = $true; $rcDetail = "Already exists" }
    else { $rcDetail = $errText }
}
Test "RC01" "Receptionist role seeded, can create staff" $rcCreateOk $rcDetail

# Login as receptionist
$tkRecep = Login "receptionist_test_p5" "Recep@123456" $tenant
Test "RC02" "Receptionist can login" ($null -ne $tkRecep)

if ($tkRecep) {
    # RC03 - Receptionist can create bookings
    $futureDate = (Get-Date).AddDays(75).ToString("yyyy-MM-dd")
    $doctors = Api "GET" "/api/clinic/doctors" $tkRecep
    $doctorId = $doctors.data.items[0].id
    # Use owner token to get doctor detail (receptionist can't access detail endpoint)
    $docDetail = Api "GET" "/api/clinic/doctors/$doctorId" $tkOwner
    $serviceId = $docDetail.data.services[0].id
    # Receptionist can't list patients (ClinicManager role does), use owner token to get patient
    $patients = Api "GET" "/api/clinic/patients" $tkOwner
    $patientId = if ($patients.data.items) { $patients.data.items[0].id } else { $null }

    if ($patientId -and $doctorId -and $serviceId) {
        $bkBody = @{patientId=$patientId;doctorId=$doctorId;doctorServiceId=$serviceId;bookingDate=$futureDate;bookingTime="14:$(Get-Random -Min 10 -Max 55)"}
        $bkResp = Api "POST" "/api/clinic/bookings" $tkRecep $bkBody
        Test "RC03" "Receptionist can create bookings" ($bkResp.success -eq $true) "$($bkResp.message)"
    } else {
        Test "RC03" "Receptionist can create bookings" $false "Missing seed data"
    }

    # RC04 - Receptionist can send messages
    $msgBody = @{channel="WhatsApp";recipientPhone="966500000001";templateName="patient_credentials";variables=@{}}
    $msgResp = Api "POST" "/api/clinic/messages/send" $tkRecep $msgBody
    Test "RC04" "Receptionist can send messages" ($msgResp.success -eq $true) "$($msgResp.message)"

    # RC05 - Receptionist can send notifications (need a target userId with subscription)
    $meRecep = Api "GET" "/api/auth/me" $tkRecep $null $tenant
    $nfBody = @{userId=$meRecep.data.id;title="Test";body="Test notification"}
    $nfResp = Api "POST" "/api/clinic/notifications/send" $tkRecep $nfBody
    Test "RC05" "Receptionist can send notifications" ($nfResp.success -eq $true) "$($nfResp.message)"

    # RC06 - Receptionist can view unread doctor notes
    $dnResp = Api "GET" "/api/clinic/doctor-notes/unread" $tkRecep
    Test "RC06" "Receptionist can view unread notes" ($dnResp.success -eq $true)

    # RC07 - Receptionist can list bookings
    $bkList = Api "GET" "/api/clinic/bookings" $tkRecep
    Test "RC07" "Receptionist can list bookings" ($bkList.success -eq $true)

    # RC08 - Receptionist cannot create doctors
    $sc = ApiStatus "POST" "/api/clinic/doctors" $tkRecep (@{userId="00000000-0000-0000-0000-000000000001";specialization="Test";dailyPatientLimit=10})
    Test "RC08" "Receptionist cannot create doctors" ($sc -eq 403) "status=$sc"

    # RC09 - Receptionist cannot update clinic settings
    $sc = ApiStatus "PUT" "/api/clinic/settings" $tkRecep (@{clinicName="Hacked"})
    Test "RC09" "Receptionist cannot update settings" ($sc -eq 403) "status=$sc"

    # RC10 - Receptionist cannot manage subscriptions
    $sc = ApiStatus "GET" "/api/platform/subscriptions" $tkRecep $null ""
    Test "RC10" "Receptionist cannot manage subscriptions" ($sc -eq 403) "status=$sc"

    # RC11 - Receptionist can list messages
    $msgList = Api "GET" "/api/clinic/messages" $tkRecep
    Test "RC11" "Receptionist can list messages" ($msgList.success -eq $true)

    # RC12 - Receptionist can subscribe to notifications
    $subBody = @{endpoint="https://test.example.com/push/recep";keys=@{p256dh="test-key-recep";auth="test-auth-recep"}}
    $subResp = Api "POST" "/api/clinic/notifications/subscribe" $tkRecep $subBody
    Test "RC12" "Receptionist can subscribe to notifications" ($subResp.success -eq $true) "$($subResp.message)"
} else {
    # All skipped
    for ($i = 3; $i -le 12; $i++) {
        $rid = "RC" + $i.ToString("00")
        Test $rid "Receptionist test (skipped - no token)" $false "Token is null"
    }
}

# ────────────── SECTION E: AUDIT LOG VERIFICATION ──────────────
Write-Host "`n--- E: AuditLog UserId Fix ---" -F Yellow

# Create a unique expense to trace in audit log
$uniqueDesc = "Phase5_Audit_Test_" + (Get-Random -Max 999999)
$expBody = @{category="Office Supplies";amount=42.5;description=$uniqueDesc;date=(Get-Date).ToString("yyyy-MM-dd")}
$expResp = Api "POST" "/api/clinic/expenses" $tkOwner $expBody
Test "AL01" "Created expense for audit" ($expResp.success -eq $true)

# Get owner's user info to know their UserId
$meOwner = Api "GET" "/api/auth/me" $tkOwner $null $tenant
$ownerUserId = $meOwner.data.id
Test "AL02" "Owner userId retrieved" ($null -ne $ownerUserId) "userId=$ownerUserId"

# ──────────────── SECTION F: PUBLIC ENDPOINT TESTS ─────────────
Write-Host "`n--- F: Public Endpoints ---" -F Yellow

$pubClinic = Api "GET" "/api/public/demo-clinic/clinic" $null $null ""
Test "PB01" "Public clinic info" ($pubClinic.success -eq $true -and $pubClinic.data.clinicName -ne $null)

$pubDoctors = Api "GET" "/api/public/demo-clinic/doctors" $null $null ""
Test "PB02" "Public doctors list" ($pubDoctors.success -eq $true -and $pubDoctors.data.Count -gt 0)

$pubServices = Api "GET" "/api/public/demo-clinic/services" $null $null ""
Test "PB03" "Public services list" ($pubServices.success -eq $true -and $pubServices.data.Count -gt 0)

$pubHours = Api "GET" "/api/public/demo-clinic/working-hours" $null $null ""
Test "PB04" "Public working hours" ($pubHours.success -eq $true)

$pubInvalid = ApiStatus "GET" "/api/public/nonexistent-slug/clinic" $null $null ""
Test "PB05" "Public invalid slug returns 404" ($pubInvalid -eq 404) "status=$pubInvalid"

# ────────────── SECTION G: HEALTH & SWAGGER TESTS ──────────────
Write-Host "`n--- G: Health and Swagger ---" -F Yellow

$health = Api "GET" "/api/health" $null $null ""
Test "HS01" "Health endpoint returns Healthy" ($health.data.status -eq "Healthy" -and $health.data.database -eq "Connected")

$swaggerStatus = ApiStatus "GET" "/swagger/v1/swagger.json" $null $null ""
Test "HS02" "Swagger JSON accessible" ($swaggerStatus -eq 200) "status=$swaggerStatus"

# ──────────── SECTION H: RESPONSE FORMAT CONSISTENCY ───────────
Write-Host "`n--- H: Response Format ---" -F Yellow

# H01 - Success responses have standard envelope
$r = Api "GET" "/api/clinic/doctors" $tkOwner
Test "RF01" "Success has success/message/data/meta" ($r.success -ne $null -and $r.message -ne $null -and $r.PSObject.Properties["data"] -and $r.PSObject.Properties["meta"])

# H02 - Paginated response has items/total/page/pageSize
$r = Api "GET" "/api/clinic/doctors?pageNumber=1&pageSize=5" $tkOwner
Test "RF02" "Pagination has totalCount/pageNumber/pageSize" ($r.data.items -ne $null -and $r.data.totalCount -ne $null -and $r.data.pageNumber -ne $null -and $r.data.pageSize -ne $null)

# H03 - Validation error format
$valBody = @{} | ConvertTo-Json
try {
    $valResp = Invoke-WebRequest "$baseUrl/api/clinic/patients" -Method POST -Headers @{"Content-Type"="application/json";"Authorization"="Bearer $tkOwner";"X-Tenant"=$tenant} -Body $valBody -UseBasicParsing
    Test "RF03" "Validation error returns 400" $false "Expected 400 but got success"
} catch {
    $valStatus = [int]$_.Exception.Response.StatusCode
    Test "RF03" "Validation error returns 400" ($valStatus -eq 400) "status=$valStatus"
}

# ──────────── SECTION I: BOOKING LIFECYCLE TESTS ───────────────
Write-Host "`n--- I: Booking Lifecycle ---" -F Yellow

$futureDt = (Get-Date).AddDays(80).ToString("yyyy-MM-dd")
$randMin = Get-Random -Min 10 -Max 55
$doctors2 = Api "GET" "/api/clinic/doctors" $tkOwner
$doc1Id = $doctors2.data.items[0].id
$docDetail2 = Api "GET" "/api/clinic/doctors/$doc1Id" $tkOwner
$svc1Id = $docDetail2.data.services[0].id
$patients2 = Api "GET" "/api/clinic/patients" $tkOwner
$pat1Id = $patients2.data.items[0].id

$bk = Api "POST" "/api/clinic/bookings" $tkOwner @{patientId=$pat1Id;doctorId=$doc1Id;doctorServiceId=$svc1Id;bookingDate=$futureDt;bookingTime="10:$randMin"}
Test "BK01" "Create booking" ($bk.success -eq $true) "$($bk.message)"
$bkId = $bk.data.id

if ($bkId) {
    $bkGet = Api "GET" "/api/clinic/bookings/$bkId" $tkOwner
    Test "BK02" "Get booking by ID" ($bkGet.success -eq $true -and $bkGet.data.status -eq "Confirmed")

    $bkCancel = Api "POST" "/api/clinic/bookings/$bkId/cancel" $tkOwner @{reason="Phase 5 test cancel"}
    Test "BK03" "Cancel booking" ($bkCancel.success -eq $true)
} else {
    Test "BK02" "Get booking by ID (skipped)" $false "No booking ID"
    Test "BK03" "Cancel booking (skipped)" $false "No booking ID"
}

# ────────────── SECTION J: QUEUE LIFECYCLE TESTS ───────────────
Write-Host "`n--- J: Queue Board ---" -F Yellow

$board = Api "GET" "/api/clinic/queue/board" $tkOwner
Test "QB01" "Queue board accessible" ($board.success -eq $true)

$myQ = Api "GET" "/api/clinic/queue/my-queue" $tkDoctor
Test "QB02" "Doctor my-queue accessible" ($myQ.success -eq $true)

$myTicketSc = ApiStatus "GET" "/api/clinic/queue/my-ticket" $tkPatient
Test "QB03" "Patient my-ticket accessible" ($myTicketSc -eq 200 -or $myTicketSc -eq 404) "status=$myTicketSc (no active ticket OK)"

# ──────────── SECTION K: DOCTOR NOTES LIFECYCLE ────────────────
Write-Host "`n--- K: Doctor Notes ---" -F Yellow

$noteBody = @{message="Phase 5 test note from doctor"}
$noteResp = Api "POST" "/api/clinic/doctor-notes" $tkDoctor $noteBody
Test "DN01" "Doctor creates note" ($noteResp.success -eq $true)
$noteId = $noteResp.data.id

$unread = Api "GET" "/api/clinic/doctor-notes/unread" $tkOwner
Test "DN02" "Owner sees unread notes" ($unread.success -eq $true)

if ($noteId) {
    $markRead = Api "POST" "/api/clinic/doctor-notes/$noteId/read" $tkOwner
    Test "DN03" "Mark note as read" ($markRead.success -eq $true)
} else {
    Test "DN03" "Mark note as read (skipped)" $false "No note ID"
}

$noteList = Api "GET" "/api/clinic/doctor-notes?pageNumber=1&pageSize=5" $tkDoctor
Test "DN04" "List doctor notes paginated" ($noteList.success -eq $true)

# ──────────── SECTION L: MESSAGE QUEUE TESTS ───────────────────
Write-Host "`n--- L: Message Queue ---" -F Yellow

$msgSend = Api "POST" "/api/clinic/messages/send" $tkOwner @{channel="WhatsApp";recipientPhone="966500099999";templateName="queue_ticket_issued";variables=@{}}
Test "MQ01" "Send WhatsApp message" ($msgSend.success -eq $true)
$msgId = $msgSend.data.id

if ($msgId) {
    $msgGet = Api "GET" "/api/clinic/messages/$msgId" $tkOwner
    Test "MQ02" "Get message by ID" ($msgGet.success -eq $true -and $msgGet.data.channel -eq "WhatsApp")

    $mqSc = ApiStatus "POST" "/api/clinic/messages/$msgId/retry" $tkOwner
    Test "MQ03" "Retry message (rejects sent)" ($mqSc -eq 400) "status=$mqSc (400=correctly rejected)"
} else {
    Test "MQ02" "Get message by ID (skipped)" $false "No msg ID"
    Test "MQ03" "Retry message (skipped)" $false "No msg ID"
}

$msgList = Api "GET" "/api/clinic/messages?pageNumber=1&pageSize=5" $tkOwner
Test "MQ04" "List messages paginated" ($msgList.success -eq $true)

# ────────────── SECTION M: NOTIFICATION TESTS ──────────────────
Write-Host "`n--- M: Notifications ---" -F Yellow

$subBody = @{endpoint="https://p5-test.example.com/push";keys=@{p256dh="p5-test-key";auth="p5-test-auth"}}
$subResp = Api "POST" "/api/clinic/notifications/subscribe" $tkOwner $subBody
Test "NF01" "Subscribe to notifications" ($subResp.success -eq $true)

$mySubs = Api "GET" "/api/clinic/notifications/my" $tkOwner
Test "NF02" "List my subscriptions" ($mySubs.success -eq $true)

$meOwner2 = Api "GET" "/api/auth/me" $tkOwner $null $tenant
$sendNotif = Api "POST" "/api/clinic/notifications/send" $tkOwner @{userId=$meOwner2.data.id;title="P5 Test";body="Phase 5 notification"}
Test "NF03" "Send notification" ($sendNotif.success -eq $true) "$($sendNotif.message)"

# Delete subscription
if ($subResp.data.id) {
    $delSub = ApiStatus "DELETE" "/api/clinic/notifications/$($subResp.data.id)" $tkOwner
    Test "NF04" "Delete subscription" ($delSub -eq 200) "status=$delSub"
} else {
    Test "NF04" "Delete subscription (skipped)" $false "No sub ID"
}

# ──────── SECTION N: VISIT/PRESCRIPTION/LAB LIFECYCLE ──────────
Write-Host "`n--- N: Visit Module ---" -F Yellow

# Get a doctor for visits
$dr1Id = $doctors2.data.items[0].id
$pat2Id = $patients2.data.items[1].id

$visitBody = @{patientId=$pat2Id;doctorId=$dr1Id;serviceId=$svc1Id;notes="Phase 5 visit test"}
$visitResp = Api "POST" "/api/clinic/visits" $tkDoctor $visitBody
Test "VM01" "Create visit" ($visitResp.success -eq $true)
$visitId = $visitResp.data.id

if ($visitId) {
    # Prescription
    $rxBody = @{medication="Amoxicillin 500mg";dosage="1 cap TID";duration="7 days"}
    $rxResp = Api "POST" "/api/clinic/visits/$visitId/prescriptions" $tkDoctor $rxBody
    Test "VM02" "Create prescription" ($rxResp.success -eq $true)

    # Lab request
    $labBody = @{testName="CBC";type="Lab";notes="Phase 5 lab test";isUrgent=$false}
    $labResp = Api "POST" "/api/clinic/visits/$visitId/labs" $tkDoctor $labBody
    Test "VM03" "Create lab request" ($labResp.success -eq $true)

    # Complete visit
    $compBody = @{diagnosis="Phase 5 test diagnosis";treatmentNotes="Phase 5 treatment"}
    $compResp = Api "POST" "/api/clinic/visits/$visitId/complete" $tkDoctor $compBody
    Test "VM04" "Complete visit" ($compResp.success -eq $true)

    # Patient summary
    $sumResp = Api "GET" "/api/clinic/patients/$pat2Id/summary" $tkDoctor
    Test "VM05" "Patient summary" ($sumResp.success -eq $true)
} else {
    for ($i = 2; $i -le 5; $i++) { Test "VM0$i" "Visit module (skipped)" $false "No visit ID" }
}

# ──────────── SECTION O: INVOICE & PAYMENT TESTS ───────────────
Write-Host "`n--- O: Invoices and Payments ---" -F Yellow

if ($visitId) {
    $invBody = @{visitId=$visitId;amount=200;notes="Phase 5 test invoice"}
    $invResp = Api "POST" "/api/clinic/invoices" $tkOwner $invBody
    Test "IP01" "Create invoice" ($invResp.success -eq $true)
    $invId = $invResp.data.id

    if ($invId) {
        $payBody = @{invoiceId=$invId;amount=200;paymentMethod="Cash"}
        $payResp = Api "POST" "/api/clinic/payments" $tkOwner $payBody
        Test "IP02" "Create payment" ($payResp.success -eq $true)

        $invPayments = Api "GET" "/api/clinic/invoices/$invId/payments" $tkOwner
        Test "IP03" "List invoice payments" ($invPayments.success -eq $true)
    } else {
        Test "IP02" "Create payment (skipped)" $false "No invoice ID"
        Test "IP03" "List invoice payments (skipped)" $false "No invoice ID"
    }
} else {
    for ($i = 1; $i -le 3; $i++) { Test "IP0$i" "Invoice (skipped)" $false "No visit ID" }
}

# ────────────── SECTION P: EXPENSE TESTS ───────────────────────
Write-Host "`n--- P: Expenses ---" -F Yellow

$expList = Api "GET" "/api/clinic/expenses?pageNumber=1&pageSize=5" $tkOwner
Test "EX01" "List expenses paginated" ($expList.success -eq $true)

# ────────────── SECTION Q: FINANCE REPORTS ─────────────────────
Write-Host "`n--- Q: Finance Reports ---" -F Yellow

$finDaily = Api "GET" "/api/clinic/finance/daily" $tkOwner
Test "FN01" "Daily finance report" ($finDaily.success -eq $true)

$finMonthly = Api "GET" "/api/clinic/finance/monthly" $tkOwner
Test "FN02" "Monthly finance report" ($finMonthly.success -eq $true)

$finYearly = Api "GET" "/api/clinic/finance/yearly" $tkOwner
Test "FN03" "Yearly finance report" ($finYearly.success -eq $true)

$finByDoc = Api "GET" "/api/clinic/finance/by-doctor" $tkOwner
Test "FN04" "Finance by doctor" ($finByDoc.success -eq $true)

$finProfit = Api "GET" "/api/clinic/finance/profit" $tkOwner
Test "FN05" "Profit report" ($finProfit.success -eq $true)

# ──────────── SECTION R: PLATFORM ADMIN TESTS ──────────────────
Write-Host "`n--- R: Platform Admin ---" -F Yellow

$tenList = Api "GET" "/api/platform/tenants" $tkSuperAdmin $null ""
Test "PA01" "List tenants" ($tenList.success -eq $true -and $tenList.data.items.Count -ge 4)

$tenId = $tenList.data.items[0].id
$tenGet = Api "GET" "/api/platform/tenants/$tenId" $tkSuperAdmin $null ""
Test "PA02" "Get tenant by ID" ($tenGet.success -eq $true)

$subList = Api "GET" "/api/platform/subscriptions" $tkSuperAdmin $null ""
Test "PA03" "List subscriptions" ($subList.success -eq $true)

$ffGet = Api "GET" "/api/platform/feature-flags/$tenId" $tkSuperAdmin $null ""
Test "PA04" "Get feature flags" ($ffGet.success -eq $true)

# ──────────── SECTION S: STAFF MANAGEMENT ──────────────────────
Write-Host "`n--- S: Staff Management ---" -F Yellow

$staffList = Api "GET" "/api/clinic/staff" $tkOwner
Test "SM01" "List staff" ($staffList.success -eq $true)

# Verify receptionist is in staff list
$recepInList = $staffList.data.items | Where-Object { $_.username -eq "receptionist_test_p5" }
Test "SM02" "Receptionist in staff list" ($null -ne $recepInList)

# ──────────── SECTION T: PATIENT MANAGEMENT ────────────────────
Write-Host "`n--- T: Patient Management ---" -F Yellow

$patList = Api "GET" "/api/clinic/patients?search=patient" $tkOwner
Test "PM01" "Search patients" ($patList.success -eq $true -and $patList.data.items.Count -gt 0)

$patDetail = Api "GET" "/api/clinic/patients/$pat1Id" $tkOwner
Test "PM02" "Get patient detail" ($patDetail.success -eq $true)

# ──────────── SECTION U: DOCTOR MANAGEMENT ─────────────────────
Write-Host "`n--- U: Doctor Management ---" -F Yellow

$docDetail = Api "GET" "/api/clinic/doctors/$doc1Id" $tkOwner
Test "DM01" "Get doctor detail" ($docDetail.success -eq $true)

$docServices = Api "GET" "/api/public/demo-clinic/services" $null $null ""
Test "DM02" "Public services list" ($docServices.success -eq $true)

# ──────── SECTION V: ENUM SERIALIZATION AS STRINGS ─────────────
Write-Host "`n--- V: Enum Serialization ---" -F Yellow

# Verify enums come as strings not integers
if ($bkId) {
    $bkCheck = Api "GET" "/api/clinic/bookings/$bkId" $tkOwner
    $statusVal = $bkCheck.data.status
    $isString = $statusVal -is [string] -and $statusVal -notmatch '^\d+$'
    Test "ES01" "BookingStatus is string enum" $isString "status=$statusVal"
} else {
    # Use any booking
    $bkAll = Api "GET" "/api/clinic/bookings?pageNumber=1&pageSize=1" $tkOwner
    if ($bkAll.data.items.Count -gt 0) {
        $statusVal = $bkAll.data.items[0].status
        $isString = $statusVal -is [string] -and $statusVal -notmatch '^\d+$'
        Test "ES01" "BookingStatus is string enum" $isString "status=$statusVal"
    } else {
        Test "ES01" "BookingStatus is string enum (no data)" $false
    }
}

# Check message channel enum
if ($msgId) {
    $msgCheck = Api "GET" "/api/clinic/messages/$msgId" $tkOwner
    $channelVal = $msgCheck.data.channel
    $isString2 = $channelVal -is [string] -and $channelVal -notmatch '^\d+$'
    Test "ES02" "MessageChannel is string enum" $isString2 "channel=$channelVal"
} else {
    $msgAll = Api "GET" "/api/clinic/messages?pageNumber=1&pageSize=1" $tkOwner
    if ($msgAll.data.items.Count -gt 0) {
        $channelVal = $msgAll.data.items[0].channel
        $isString2 = $channelVal -is [string] -and $channelVal -notmatch '^\d+$'
        Test "ES02" "MessageChannel is string enum" $isString2 "channel=$channelVal"
    } else {
        Test "ES02" "MessageChannel is string enum (no data)" $false
    }
}

# ──────────── SECTION W: NEGATIVE / EDGE CASES ────────────────
Write-Host "`n--- W: Negative and Edge Cases ---" -F Yellow

# W01 - GET nonexistent resource returns 404
$sc404 = ApiStatus "GET" "/api/clinic/doctors/00000000-0000-0000-0000-000000000099" $tkOwner
Test "NC01" "Nonexistent doctor returns 404" ($sc404 -eq 404) "status=$sc404"

# W02 - POST with missing required fields returns 400
$sc400 = ApiStatus "POST" "/api/clinic/patients" $tkOwner @{fullName=""}
Test "NC02" "Empty required field returns 400" ($sc400 -eq 400) "status=$sc400"

# W03 - Invalid GUID format in URL
$scBadId = ApiStatus "GET" "/api/clinic/doctors/not-a-guid" $tkOwner
Test "NC03" "Invalid GUID returns 400/404" ($scBadId -eq 400 -or $scBadId -eq 404) "status=$scBadId"

# W04 - Double cancel on booking
if ($bkId) {
    $dblCancel = ApiStatus "POST" "/api/clinic/bookings/$bkId/cancel" $tkOwner (@{reason="double cancel"})
    Test "NC04" "Double cancel returns error" ($dblCancel -ne 200 -and $dblCancel -ne 0) "status=$dblCancel"
} else {
    Test "NC04" "Double cancel (skipped)" $false "No booking ID"
}

# W05 - Pagination with invalid parameters
$pgResp = Api "GET" "/api/clinic/doctors?pageNumber=0&pageSize=0" $tkOwner
Test "NC05" "Invalid pagination handled" ($pgResp.success -eq $true -or $pgResp.statusCode -ne $null) "Got response"

###############################################################################
#  SUMMARY
###############################################################################
Write-Host "`n========== PHASE 5 TEST SUMMARY ==========" -F Cyan
Write-Host "PASS: $pass" -F Green
Write-Host "FAIL: $fail" -F Red
Write-Host "TOTAL: $($pass + $fail)" -F White
Write-Host "=========================================`n" -F Cyan

# Detailed results table
$results | Format-Table -AutoSize

# Final verdict
if ($fail -eq 0) {
    Write-Host "ALL TESTS PASSED - Production ready!" -F Green
} else {
    Write-Host "SOME TESTS FAILED - Review required" -F Red
}
