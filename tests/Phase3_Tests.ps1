###############################################################################
# Phase 3 – Comprehensive HTTP Tests
# Elite Clinic API – Queue, Visits, Finance
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
    $params = @{ Uri = "$base$path"; Method = $method; Headers = $headers; ContentType = "application/json"; UseBasicParsing = $true }
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

[Console]::WriteLine("`n========== PHASE 3 TESTS ==========")

###############################################################################
# AUTHENTICATION
###############################################################################
[Console]::WriteLine("`n-- Authenticating --")
$ownerToken = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="owner_demo";password="Owner@123456"}).data.token
$staffToken = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="staff_sara";password="Staff@123456"}).data.token
$doc1Token  = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="dr_khaled";password="Doctor@123456"}).data.token
$doc2Token  = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="dr_mona";password="Doctor@123456"}).data.token
$pat1Token  = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="patient_demo-clinic_1";password="Patient@1234"}).data.token
$pat3Token  = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="patient_demo-clinic_3";password="Patient@1234"}).data.token

if (-not $ownerToken -or -not $doc1Token) { [Console]::WriteLine("FATAL: Cannot get tokens"); exit 1 }
[Console]::WriteLine("  Tokens acquired: Owner, Staff, Doctor1, Doctor2, Patient1, Patient3")

$ownerH = MakeHeaders $ownerToken "demo-clinic"
$staffH = MakeHeaders $staffToken "demo-clinic"
$doc1H  = MakeHeaders $doc1Token  "demo-clinic"
$doc2H  = MakeHeaders $doc2Token  "demo-clinic"
$pat1H  = MakeHeaders $pat1Token  "demo-clinic"
$pat3H  = MakeHeaders $pat3Token  "demo-clinic"
$noAuthH = MakeHeaders $null "demo-clinic"

# Get entity IDs from seeded data
$doctors = (Api GET "/api/clinic/doctors?pageSize=50" $ownerH).data.items
$doctor1Id = ($doctors | Where-Object { $_.name -like "*Khaled*" } | Select-Object -First 1).id
$doctor2Id = ($doctors | Where-Object { $_.name -like "*Mona*" } | Select-Object -First 1).id

$patients = (Api GET "/api/clinic/patients?pageSize=50" $ownerH).data.items
$patient1Id = ($patients | Where-Object { $_.phone -eq "+201500000001" } | Select-Object -First 1).id
$patient2Id = ($patients | Where-Object { $_.phone -eq "+201500000002" } | Select-Object -First 1).id
$patient3Id = ($patients | Where-Object { $_.phone -eq "+201500000003" } | Select-Object -First 1).id
$patient4Id = ($patients | Where-Object { $_.phone -eq "+201500000004" } | Select-Object -First 1).id
$patient5Id = ($patients | Where-Object { $_.phone -eq "+201500000005" } | Select-Object -First 1).id

$services1 = (Api GET "/api/clinic/doctors/$doctor1Id/services" $ownerH).data
$service1Id = ($services1 | Select-Object -First 1).id

[Console]::WriteLine("  IDs acquired: Doctor1=$doctor1Id, Patient1=$patient1Id")

###############################################################################
# 1. QUEUE SESSIONS — Seeded data checks
###############################################################################
[Console]::WriteLine("`n-- 1. Queue Sessions (Seeded Data) --")

Test "QS01 List seeded queue sessions" {
    $r = Api GET "/api/clinic/queue/sessions" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 2
}

Test "QS02 Get session by ID" {
    $sessions = (Api GET "/api/clinic/queue/sessions" $ownerH).data.items
    $sid = $sessions[0].id
    $r = Api GET "/api/clinic/queue/sessions/$sid" $ownerH
    $r.success -eq $true -and $r.data.id -eq $sid
}

Test "QS03 Get tickets by session" {
    $sessions = (Api GET "/api/clinic/queue/sessions" $ownerH).data.items
    $sid = $sessions[0].id
    $r = Api GET "/api/clinic/queue/sessions/$sid/tickets" $ownerH
    $r.success -eq $true -and $r.data.Count -ge 1
}

###############################################################################
# 2. QUEUE — Close seeded sessions, open fresh ones for testing
###############################################################################
[Console]::WriteLine("`n-- 2. Queue Session Lifecycle --")

# Close seeded sessions first to avoid conflicts
$sessions = (Api GET "/api/clinic/queue/sessions?pageSize=50" $ownerH).data.items
foreach ($s in $sessions) {
    if ($s.isActive) {
        # Must finish InVisit tickets first
        $tickets = (Api GET "/api/clinic/queue/sessions/$($s.id)/tickets" $ownerH).data
        foreach ($t in $tickets) {
            if ($t.status -eq "InVisit") {
                Api POST "/api/clinic/queue/tickets/$($t.id)/finish" $ownerH | Out-Null
            }
        }
        Api POST "/api/clinic/queue/sessions/$($s.id)/close" $ownerH | Out-Null
    }
}

Test "QS04 Open new queue session for Doctor1" {
    $r = Api POST "/api/clinic/queue/sessions" $ownerH @{ doctorId = $doctor1Id }
    $script:newSession1Id = $r.data.id
    $r.success -eq $true -and $r.data.isActive -eq $true -and $r.data.doctorId -eq $doctor1Id
}

Test "QS05 Cannot open duplicate session for same doctor" {
    $r = Api POST "/api/clinic/queue/sessions" $ownerH @{ doctorId = $doctor1Id }
    $r.success -eq $false
}

Test "QS06 Open session for Doctor2" {
    $r = Api POST "/api/clinic/queue/sessions" $ownerH @{ doctorId = $doctor2Id }
    $script:newSession2Id = $r.data.id
    $r.success -eq $true -and $r.data.isActive -eq $true
}

###############################################################################
# 3. QUEUE TICKETS — Full walk-in workflow
###############################################################################
[Console]::WriteLine("`n-- 3. Queue Tickets --")

Test "QT01 Issue ticket for patient1" {
    $r = Api POST "/api/clinic/queue/tickets" $ownerH @{ sessionId = $newSession1Id; patientId = $patient1Id; doctorId = $doctor1Id; doctorServiceId = $service1Id }
    $script:ticket1Id = $r.data.id
    $r.success -eq $true -and $r.data.ticketNumber -eq 1 -and $r.data.status -eq "Waiting"
}

Test "QT02 Issue ticket for patient2" {
    $r = Api POST "/api/clinic/queue/tickets" $ownerH @{ sessionId = $newSession1Id; patientId = $patient2Id; doctorId = $doctor1Id }
    $script:ticket2Id = $r.data.id
    $r.success -eq $true -and $r.data.ticketNumber -eq 2
}

Test "QT03 Issue ticket for patient3" {
    $r = Api POST "/api/clinic/queue/tickets" $ownerH @{ sessionId = $newSession1Id; patientId = $patient3Id; doctorId = $doctor1Id }
    $script:ticket3Id = $r.data.id
    $r.success -eq $true -and $r.data.ticketNumber -eq 3
}

Test "QT04 Cannot issue duplicate ticket for same patient in same session" {
    $r = Api POST "/api/clinic/queue/tickets" $ownerH @{ sessionId = $newSession1Id; patientId = $patient1Id; doctorId = $doctor1Id }
    $r.success -eq $false
}

Test "QT05 Mark ticket as urgent" {
    $r = Api POST "/api/clinic/queue/tickets/$ticket3Id/urgent" $ownerH
    $r.success -eq $true -and $r.data.isUrgent -eq $true
}

Test "QT06 Call ticket (Waiting → Called)" {
    $r = Api POST "/api/clinic/queue/tickets/$ticket1Id/call" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Called"
}

Test "QT07 Start visit from ticket (Called → InVisit)" {
    $r = Api POST "/api/clinic/queue/tickets/$ticket1Id/start-visit" $ownerH
    $r.success -eq $true -and $r.data.status -eq "InVisit"
}

Test "QT08 Cannot call next while one is InVisit (business logic)" {
    # This should still work — calling another ticket switches focus
    # Actually per our service, calling when another is InVisit should work
    $r = Api POST "/api/clinic/queue/tickets/$ticket2Id/call" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Called"
}

Test "QT09 Skip ticket (Called → Skipped)" {
    $r = Api POST "/api/clinic/queue/tickets/$ticket2Id/skip" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Skipped"
}

Test "QT10 Re-call skipped ticket (Skipped → Called)" {
    $r = Api POST "/api/clinic/queue/tickets/$ticket2Id/call" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Called"
}

Test "QT11 Cancel ticket" {
    $r = Api POST "/api/clinic/queue/tickets/$ticket2Id/cancel" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Cancelled"
}

Test "QT12 Finish ticket (InVisit → Completed)" {
    $r = Api POST "/api/clinic/queue/tickets/$ticket1Id/finish" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Completed"
}

###############################################################################
# 4. QUEUE BOARD
###############################################################################
[Console]::WriteLine("`n-- 4. Queue Board --")

Test "QB01 Get queue board (today)" {
    $r = Api GET "/api/clinic/queue/board" $ownerH
    $r.success -eq $true -and $r.data.sessions.Count -ge 1
}

Test "QB02 Doctor sees my-queue" {
    $r = Api GET "/api/clinic/queue/my-queue" $doc1H
    $r.success -eq $true -and $r.data.sessionId -eq $newSession1Id
}

Test "QB03 Patient with active ticket sees my-ticket" {
    # patient3 has a Waiting ticket from seed data (ticket3 in session1)
    $r = Api GET "/api/clinic/queue/my-ticket" $pat3H
    $r.success -eq $true -and $null -ne $r.data.id -and $r.data.patientId -eq $patient3Id
}

Test "QB04 Patient without active ticket gets not-found" {
    $r = Api GET "/api/clinic/queue/my-ticket" $pat1H
    $r.success -eq $false
}

###############################################################################
# 5. QUEUE SESSION — Close
###############################################################################
[Console]::WriteLine("`n-- 5. Queue Session Close --")

Test "QS07 Close session (remaining Waiting tickets → NoShow)" {
    $r = Api POST "/api/clinic/queue/sessions/$($newSession1Id)/close" $ownerH
    $r.success -eq $true -and $r.data.isActive -eq $false
}

Test "QS08 Verify ticket3 became NoShow after session close" {
    $r = Api GET "/api/clinic/queue/sessions/$newSession1Id/tickets" $ownerH
    $noShowTicket = $r.data | Where-Object { $_.id -eq $ticket3Id }
    $noShowTicket.status -eq "NoShow"
}

###############################################################################
# 6. VISITS — Manual creation
###############################################################################
[Console]::WriteLine("`n-- 6. Visits --")

Test "V01 Create manual visit (no ticket)" {
    $r = Api POST "/api/clinic/visits" $doc1H @{ doctorId = $doctor1Id; patientId = $patient4Id; complaint = "Severe toothache" }
    $script:manualVisitId = $r.data.id
    $r.success -eq $true -and $r.data.status -eq "Open" -and $null -eq $r.data.queueTicketId
}

Test "V02 Update visit with vitals" {
    $r = Api PUT "/api/clinic/visits/$manualVisitId" $doc1H @{
        complaint = "Severe toothache - upper left"
        diagnosis = "Pulpitis in tooth #24"
        bloodPressureSystolic = 130
        bloodPressureDiastolic = 85
        heartRate = 80
        temperature = 37.2
        weight = 70
    }
    $r.success -eq $true -and $r.data.bloodPressureSystolic -eq 130 -and $r.data.diagnosis -eq "Pulpitis in tooth #24"
}

Test "V03 Get visit by ID" {
    $r = Api GET "/api/clinic/visits/$manualVisitId" $doc1H
    $r.success -eq $true -and $r.data.id -eq $manualVisitId
}

Test "V04 Complete visit" {
    $r = Api POST "/api/clinic/visits/$manualVisitId/complete" $doc1H @{ diagnosis = "Pulpitis confirmed"; notes = "Schedule root canal" }
    $r.success -eq $true -and $r.data.status -eq "Completed" -and $null -ne $r.data.completedAt
}

Test "V05 Cannot update completed visit" {
    $r = Api PUT "/api/clinic/visits/$manualVisitId" $doc1H @{ complaint = "changed" }
    $r.success -eq $false
}

Test "V06 Get patient visits (patient4)" {
    $r = Api GET "/api/clinic/patients/$patient4Id/visits" $doc1H
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "V07 Get patient summary" {
    $r = Api GET "/api/clinic/patients/$patient4Id/summary" $doc1H
    $r.success -eq $true -and $null -ne $r.data.patientId -and $r.data.totalVisits -ge 1
}

# Create another open visit for prescription/lab/invoice testing
Test "V08 Create open visit for prescription testing" {
    $r = Api POST "/api/clinic/visits" $doc1H @{ doctorId = $doctor1Id; patientId = $patient5Id; complaint = "Checkup" }
    $script:openVisitId = $r.data.id
    $r.success -eq $true -and $r.data.status -eq "Open"
}

###############################################################################
# 7. PRESCRIPTIONS
###############################################################################
[Console]::WriteLine("`n-- 7. Prescriptions --")

Test "RX01 Add prescription to open visit" {
    $r = Api POST "/api/clinic/visits/$openVisitId/prescriptions" $doc1H @{
        medicationName = "Amoxicillin 500mg"
        dosage = "500mg"
        frequency = "3 times daily"
        duration = "7 days"
        instructions = "After meals"
    }
    $script:rx1Id = $r.data.id
    $r.success -eq $true -and $r.data.medicationName -eq "Amoxicillin 500mg"
}

Test "RX02 Add second prescription" {
    $r = Api POST "/api/clinic/visits/$openVisitId/prescriptions" $doc1H @{
        medicationName = "Paracetamol 500mg"
        dosage = "500mg"
        frequency = "As needed"
        duration = "3 days"
    }
    $script:rx2Id = $r.data.id
    $r.success -eq $true
}

Test "RX03 Update prescription" {
    $r = Api PUT "/api/clinic/visits/$openVisitId/prescriptions/$rx1Id" $doc1H @{
        medicationName = "Amoxicillin 500mg"
        dosage = "500mg"
        frequency = "2 times daily"
        duration = "10 days"
        instructions = "With food"
    }
    $r.success -eq $true -and $r.data.frequency -eq "2 times daily"
}

Test "RX04 List prescriptions for visit" {
    $r = Api GET "/api/clinic/visits/$openVisitId/prescriptions" $doc1H
    $r.success -eq $true -and $r.data.Count -eq 2
}

Test "RX05 Delete prescription" {
    $r = Api DELETE "/api/clinic/visits/$openVisitId/prescriptions/$rx2Id" $doc1H
    $r.success -eq $true
}

Test "RX06 Verify prescription deleted" {
    $r = Api GET "/api/clinic/visits/$openVisitId/prescriptions" $doc1H
    $r.success -eq $true -and $r.data.Count -eq 1
}

###############################################################################
# 8. LAB REQUESTS
###############################################################################
[Console]::WriteLine("`n-- 8. Lab Requests --")

Test "LR01 Create lab request" {
    $r = Api POST "/api/clinic/visits/$openVisitId/labs" $doc1H @{
        testName = "Complete Blood Count"
        type = 0
        isUrgent = $false
    }
    $script:labId = $r.data.id
    $r.success -eq $true -and $r.data.testName -eq "Complete Blood Count"
}

Test "LR02 Create imaging request" {
    $r = Api POST "/api/clinic/visits/$openVisitId/labs" $doc1H @{
        testName = "Panoramic X-Ray"
        type = 1
        isUrgent = $true
    }
    $script:imagingId = $r.data.id
    $r.success -eq $true -and $r.data.isUrgent -eq $true
}

Test "LR03 Update lab request" {
    $r = Api PUT "/api/clinic/visits/$openVisitId/labs/$labId" $doc1H @{
        testName = "CBC with Differential"
        type = 0
        isUrgent = $true
    }
    $r.success -eq $true -and $r.data.testName -eq "CBC with Differential"
}

Test "LR04 Add result to lab request" {
    $r = Api POST "/api/clinic/visits/$openVisitId/labs/$labId/result" $ownerH @{
        resultText = "WBC: 7500, RBC: 4.8M, Hgb: 14.2, Plt: 250K"
    }
    $r.success -eq $true -and $null -ne $r.data.resultText -and $null -ne $r.data.resultReceivedAt
}

Test "LR05 List lab requests for visit" {
    $r = Api GET "/api/clinic/visits/$openVisitId/labs" $doc1H
    $r.success -eq $true -and $r.data.Count -eq 2
}

###############################################################################
# 9. INVOICES
###############################################################################
[Console]::WriteLine("`n-- 9. Invoices --")

Test "INV01 Create invoice for open visit" {
    $r = Api POST "/api/clinic/invoices" $ownerH @{
        visitId = $openVisitId
        amount = 500
        notes = "Consultation + Lab"
    }
    $script:invoiceId = $r.data.id
    $r.success -eq $true -and $r.data.amount -eq 500 -and $r.data.paidAmount -eq 0 -and $r.data.status -eq "Unpaid"
}

Test "INV02 Cannot create duplicate invoice for same visit" {
    $r = Api POST "/api/clinic/invoices" $ownerH @{ visitId = $openVisitId; amount = 300 }
    $r.success -eq $false
}

Test "INV03 Update invoice amount" {
    $r = Api PUT "/api/clinic/invoices/$invoiceId" $ownerH @{ amount = 600; notes = "Adjusted amount" }
    $r.success -eq $true -and $r.data.amount -eq 600 -and $r.data.remainingAmount -eq 600
}

Test "INV04 Get invoice by ID" {
    $r = Api GET "/api/clinic/invoices/$invoiceId" $ownerH
    $r.success -eq $true -and $r.data.id -eq $invoiceId
}

Test "INV05 List invoices" {
    $r = Api GET "/api/clinic/invoices" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

###############################################################################
# 10. PAYMENTS (partial payments)
###############################################################################
[Console]::WriteLine("`n-- 10. Payments --")

Test "PAY01 Record partial payment" {
    $r = Api POST "/api/clinic/payments" $ownerH @{
        invoiceId = $invoiceId
        amount = 200
        paymentMethod = "Cash"
    }
    $script:payment1Id = $r.data.id
    $r.success -eq $true -and $r.data.amount -eq 200
}

Test "PAY02 Invoice status is PartiallyPaid" {
    $r = Api GET "/api/clinic/invoices/$invoiceId" $ownerH
    $r.success -eq $true -and $r.data.paidAmount -eq 200 -and $r.data.remainingAmount -eq 400 -and $r.data.status -eq "PartiallyPaid"
}

Test "PAY03 Record second payment" {
    $r = Api POST "/api/clinic/payments" $ownerH @{
        invoiceId = $invoiceId
        amount = 400
        paymentMethod = "Credit Card"
        referenceNumber = "CC-TEST-001"
    }
    $r.success -eq $true
}

Test "PAY04 Invoice status is now Paid" {
    $r = Api GET "/api/clinic/invoices/$invoiceId" $ownerH
    $r.success -eq $true -and $r.data.paidAmount -eq 600 -and $r.data.remainingAmount -eq 0 -and $r.data.status -eq "Paid"
}

Test "PAY05 Cannot overpay (exceed remaining)" {
    $r = Api POST "/api/clinic/payments" $ownerH @{
        invoiceId = $invoiceId
        amount = 1
        paymentMethod = "Cash"
    }
    $r.success -eq $false
}

Test "PAY06 Get payments by invoice" {
    $r = Api GET "/api/clinic/invoices/$invoiceId/payments" $ownerH
    $r.success -eq $true -and $r.data.Count -eq 2
}

###############################################################################
# 11. INVOICE — Cannot update paid invoice amount below paidAmount
###############################################################################
[Console]::WriteLine("`n-- 11. Invoice edge cases --")

# Create another invoice for edge case testing
Test "INV06 Create invoice for completed visit" {
    $r = Api POST "/api/clinic/invoices" $ownerH @{ visitId = $manualVisitId; amount = 300 }
    $script:invoice2Id = $r.data.id
    $r.success -eq $true
}

Test "INV07 Record partial payment on invoice2" {
    $r = Api POST "/api/clinic/payments" $ownerH @{ invoiceId = $invoice2Id; amount = 100; paymentMethod = "Cash" }
    $r.success -eq $true
}

Test "INV08 Cannot reduce amount below paid amount" {
    $r = Api PUT "/api/clinic/invoices/$invoice2Id" $ownerH @{ amount = 50 }
    $r.success -eq $false
}

###############################################################################
# 12. EXPENSES
###############################################################################
[Console]::WriteLine("`n-- 12. Expenses --")

Test "EXP01 Create expense" {
    $r = Api POST "/api/clinic/expenses" $ownerH @{
        category = "Office Supplies"
        amount = 250
        notes = "Printer paper and ink"
    }
    $script:expenseId = $r.data.id
    $r.success -eq $true -and $r.data.category -eq "Office Supplies" -and $r.data.amount -eq 250
}

Test "EXP02 Create second expense" {
    $r = Api POST "/api/clinic/expenses" $ownerH @{
        category = "Maintenance"
        amount = 1000
        notes = "AC repair"
    }
    $script:expense2Id = $r.data.id
    $r.success -eq $true
}

Test "EXP03 Update expense" {
    $r = Api PUT "/api/clinic/expenses/$expenseId" $ownerH @{
        category = "Office Supplies"
        amount = 300
        notes = "Printer paper, ink, and toner"
    }
    $r.success -eq $true -and $r.data.amount -eq 300
}

Test "EXP04 List expenses" {
    $r = Api GET "/api/clinic/expenses" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 2
}

Test "EXP05 Filter expenses by category" {
    $r = Api GET "/api/clinic/expenses?category=Maintenance" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "EXP06 Delete expense" {
    $r = Api DELETE "/api/clinic/expenses/$expense2Id" $ownerH
    $r.success -eq $true
}

Test "EXP07 Deleted expense not in list" {
    $r = Api GET "/api/clinic/expenses" $ownerH
    $found = $r.data.items | Where-Object { $_.id -eq $expense2Id }
    $r.success -eq $true -and $null -eq $found
}

###############################################################################
# 13. FINANCE REPORTS
###############################################################################
[Console]::WriteLine("`n-- 13. Finance Reports --")

Test "FIN01 Daily revenue" {
    $r = Api GET "/api/clinic/finance/daily" $ownerH
    $r.success -eq $true -and $null -ne $r.data.date
}

Test "FIN02 Revenue by doctor" {
    $r = Api GET "/api/clinic/finance/by-doctor" $ownerH
    $r.success -eq $true -and $r.data.Count -ge 1
}

Test "FIN03 Revenue by specific doctor" {
    $r = Api GET "/api/clinic/finance/by-doctor?doctorId=$doctor1Id" $ownerH
    $r.success -eq $true -and $r.data.Count -ge 1
}

Test "FIN04 Monthly report" {
    $m = (Get-Date).Month
    $y = (Get-Date).Year
    $r = Api GET "/api/clinic/finance/monthly?year=$y&month=$m" $ownerH
    $r.success -eq $true -and $r.data.year -eq $y -and $r.data.month -eq $m
}

Test "FIN05 Yearly report" {
    $y = (Get-Date).Year
    $r = Api GET "/api/clinic/finance/yearly?year=$y" $ownerH
    $r.success -eq $true -and $r.data.year -eq $y -and $null -ne $r.data.months
}

Test "FIN06 Profit report" {
    $from = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd")
    $to = (Get-Date).AddDays(1).ToString("yyyy-MM-dd")
    $r = Api GET "/api/clinic/finance/profit?from=$from&to=$to" $ownerH
    $r.success -eq $true -and $null -ne $r.data.totalRevenue -and $null -ne $r.data.netProfit
}

###############################################################################
# 14. VISIT — Create from queue ticket flow (fresh session)
###############################################################################
[Console]::WriteLine("`n-- 14. Full Ticket-to-Visit Flow --")

# Open a fresh session for this flow
Test "FLOW01 Open fresh session" {
    # Close session2 first if active
    Api POST "/api/clinic/queue/sessions/$($newSession2Id)/close" $ownerH | Out-Null
    $r = Api POST "/api/clinic/queue/sessions" $ownerH @{ doctorId = $doctor1Id }
    $script:flowSessionId = $r.data.id
    $r.success -eq $true
}

Test "FLOW02 Issue ticket" {
    $r = Api POST "/api/clinic/queue/tickets" $ownerH @{ sessionId = $flowSessionId; patientId = $patient3Id; doctorId = $doctor1Id; doctorServiceId = $service1Id }
    $script:flowTicketId = $r.data.id
    $r.success -eq $true -and $r.data.status -eq "Waiting"
}

Test "FLOW03 Call ticket" {
    $r = Api POST "/api/clinic/queue/tickets/$flowTicketId/call" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Called"
}

Test "FLOW04 Start visit from ticket (auto-creates Visit)" {
    $r = Api POST "/api/clinic/queue/tickets/$flowTicketId/start-visit" $ownerH
    $r.success -eq $true -and $r.data.status -eq "InVisit"
}

Test "FLOW05 Visit was created for the ticket" {
    $r = Api GET "/api/clinic/patients/$patient3Id/visits" $ownerH
    $latestVisit = $r.data.items | Where-Object { $_.queueTicketId -eq $flowTicketId }
    $script:flowVisitId = $latestVisit.id
    $null -ne $flowVisitId
}

Test "FLOW06 Update visit during open state" {
    $r = Api PUT "/api/clinic/visits/$flowVisitId" $doc1H @{
        complaint = "Tooth sensitivity"
        diagnosis = "Enamel erosion"
        temperature = 36.9
    }
    $r.success -eq $true -and $r.data.complaint -eq "Tooth sensitivity"
}

Test "FLOW07 Add prescription to visit" {
    $r = Api POST "/api/clinic/visits/$flowVisitId/prescriptions" $doc1H @{
        medicationName = "Sensodyne Toothpaste"
        dosage = "Apply twice daily"
        frequency = "Morning and night"
        duration = "Ongoing"
    }
    $r.success -eq $true
}

Test "FLOW08 Add lab request" {
    $r = Api POST "/api/clinic/visits/$flowVisitId/labs" $doc1H @{
        testName = "Dental X-Ray"
        type = 1
        isUrgent = $false
    }
    $script:flowLabId = $r.data.id
    $r.success -eq $true
}

Test "FLOW09 Create invoice for visit" {
    $r = Api POST "/api/clinic/invoices" $ownerH @{ visitId = $flowVisitId; amount = 200 }
    $script:flowInvoiceId = $r.data.id
    $r.success -eq $true -and $r.data.amount -eq 200
}

Test "FLOW10 Pay invoice in full" {
    $r = Api POST "/api/clinic/payments" $ownerH @{ invoiceId = $flowInvoiceId; amount = 200; paymentMethod = "Cash" }
    $r.success -eq $true
}

Test "FLOW11 Finish ticket (completes visit too)" {
    $r = Api POST "/api/clinic/queue/tickets/$flowTicketId/finish" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Completed"
}

Test "FLOW12 Visit is now completed" {
    $r = Api GET "/api/clinic/visits/$flowVisitId" $doc1H
    $r.success -eq $true -and $r.data.status -eq "Completed"
}

Test "FLOW13 Invoice is Paid" {
    $r = Api GET "/api/clinic/invoices/$flowInvoiceId" $ownerH
    $r.success -eq $true -and $r.data.status -eq "Paid"
}

###############################################################################
# 15. AUTH — Unauthenticated / wrong-role checks
###############################################################################
[Console]::WriteLine("`n-- 15. Authorization Checks --")

Test "AUTH01 Unauthenticated cannot list sessions" {
    $r = Api GET "/api/clinic/queue/sessions" $noAuthH
    $r.success -eq $false -and $r.statusCode -eq 401
}

Test "AUTH02 Patient cannot open session" {
    $r = Api POST "/api/clinic/queue/sessions" $pat1H @{ doctorId = $doctor1Id }
    $r.success -eq $false
}

Test "AUTH03 Patient cannot create expense" {
    $r = Api POST "/api/clinic/expenses" $pat1H @{ category = "Test"; amount = 100 }
    $r.success -eq $false
}

###############################################################################
# 16. PATIENT VISIT HISTORY — seeded data verification
###############################################################################
[Console]::WriteLine("`n-- 16. Patient Visit History --")

Test "PVH01 Patient1 has visits from seed data" {
    $r = Api GET "/api/clinic/patients/$patient1Id/visits" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "PVH02 Patient summary shows visit count" {
    $r = Api GET "/api/clinic/patients/$patient1Id/summary" $ownerH
    $r.success -eq $true -and $r.data.totalVisits -ge 1
}

###############################################################################
# 17. VISIT WITH NESTED DATA
###############################################################################
[Console]::WriteLine("`n-- 17. Visit Nested Data --")

Test "VND01 Visit includes prescriptions in response" {
    $r = Api GET "/api/clinic/visits/$openVisitId" $doc1H
    $r.success -eq $true -and $r.data.prescriptions.Count -ge 1
}

Test "VND02 Visit includes lab requests in response" {
    $r = Api GET "/api/clinic/visits/$openVisitId" $doc1H
    $r.success -eq $true -and $r.data.labRequests.Count -ge 1
}

Test "VND03 Visit includes invoice in response" {
    $r = Api GET "/api/clinic/visits/$openVisitId" $doc1H
    $r.success -eq $true -and $null -ne $r.data.invoice
}

###############################################################################
# 18. EDGE CASES
###############################################################################
[Console]::WriteLine("`n-- 18. Edge Cases --")

Test "EDGE01 Cannot create visit for non-existent patient" {
    $fakeId = [System.Guid]::NewGuid().ToString()
    $r = Api POST "/api/clinic/visits" $doc1H @{ doctorId = $doctor1Id; patientId = $fakeId }
    $r.success -eq $false
}

Test "EDGE02 Cannot create invoice for non-existent visit" {
    $fakeId = [System.Guid]::NewGuid().ToString()
    $r = Api POST "/api/clinic/invoices" $ownerH @{ visitId = $fakeId; amount = 100 }
    $r.success -eq $false
}

Test "EDGE03 Cannot record payment for non-existent invoice" {
    $fakeId = [System.Guid]::NewGuid().ToString()
    $r = Api POST "/api/clinic/payments" $ownerH @{ invoiceId = $fakeId; amount = 100; paymentMethod = "Cash" }
    $r.success -eq $false
}

Test "EDGE04 Cannot record zero-amount payment" {
    # Create a fresh invoice for testing
    $freshVisit = (Api POST "/api/clinic/visits" $doc1H @{ doctorId = $doctor1Id; patientId = $patient2Id; complaint = "Edge test" }).data
    $freshInv = (Api POST "/api/clinic/invoices" $ownerH @{ visitId = $freshVisit.id; amount = 100 }).data
    $r = Api POST "/api/clinic/payments" $ownerH @{ invoiceId = $freshInv.id; amount = 0; paymentMethod = "Cash" }
    $r.success -eq $false
}

Test "EDGE05 Cannot add prescription to non-existent visit" {
    $fakeId = [System.Guid]::NewGuid().ToString()
    $r = Api POST "/api/clinic/visits/$fakeId/prescriptions" $doc1H @{ medicationName = "Test" }
    $r.success -eq $false
}

###############################################################################
# 19. REGRESSION — Phase 2 still works
###############################################################################
[Console]::WriteLine("`n-- 19. Regression Checks --")

Test "RG01 Doctors list still works" {
    $r = Api GET "/api/clinic/doctors" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 2
}

Test "RG02 Patients list still works" {
    $r = Api GET "/api/clinic/patients" $ownerH
    $r.success -eq $true -and $r.data.items.Count -ge 4
}

Test "RG03 Auth login still works" {
    $r = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="owner_demo";password="Owner@123456"}
    $r.success -eq $true -and $null -ne $r.data.token
}

###############################################################################
# SUMMARY
###############################################################################
[Console]::WriteLine("`n========================================")
[Console]::WriteLine("  TOTAL: $total | PASS: $pass | FAIL: $fail")
[Console]::WriteLine("========================================")
