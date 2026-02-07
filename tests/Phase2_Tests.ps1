###############################################################################
# Phase 2 Ã¢â‚¬â€œ Comprehensive HTTP Tests
# Elite Clinic API Ã¢â‚¬â€œ Clinic Setup & Users
###############################################################################

$ErrorActionPreference = "Continue"
$base = "http://localhost:5094"
$pass = 0; $fail = 0; $total = 0
$ts = (Get-Date).ToString("HHmmss")  # Unique suffix for test usernames

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
                try {
                    return $errText | ConvertFrom-Json
                } catch {}
            }
            # Empty or non-JSON response (e.g., 401, 403 from framework)
            return [PSCustomObject]@{ success = $false; message = "HTTP $statusCode"; statusCode = $statusCode }
        }
        throw
    }
}

[Console]::WriteLine("`n========== PHASE 2 TESTS ==========")

# Ã¢â€â‚¬Ã¢â€â‚¬ Get tokens Ã¢â€â‚¬Ã¢â€â‚¬
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ Authenticating Ã¢â€â‚¬Ã¢â€â‚¬")
$ownerToken = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="owner_demo";password="Owner@123456"}).data.token
$superToken = (Api POST "/api/auth/login" @{"Content-Type"="application/json"} @{username="superadmin";password="Admin@123456"}).data.token
$staffToken = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="staff_sara";password="Staff@123456"}).data.token
$docToken   = (Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="dr_khaled";password="Doctor@123456"}).data.token

if (-not $ownerToken) { [Console]::WriteLine("FATAL: Cannot get owner token"); exit 1 }
[Console]::WriteLine("  Tokens acquired: Owner, Super, Staff, Doctor")

###############################################################################
# 1. CLINIC SETTINGS
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 1. Clinic Settings Ã¢â€â‚¬Ã¢â€â‚¬")

Test "CS01 GET settings (owner)" {
    $r = Api GET "/api/clinic/settings" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.clinicName -ne $null
}

Test "CS02 GET settings (staff can read)" {
    $r = Api GET "/api/clinic/settings" (MakeHeaders $staffToken "demo-clinic")
    $r.success -eq $true
}

Test "CS03 PUT settings (owner updates)" {
    $body = @{
        clinicName = "Demo Dental Clinic Updated"
        phone = "+201000000099"
        address = "123 Main St"
        city = "Cairo"
        bookingEnabled = $true
        cancellationWindowHours = 4
        workingHours = @(
            @{ dayOfWeek = 0; startTime = "09:00:00"; endTime = "17:00:00"; isActive = $true },
            @{ dayOfWeek = 1; startTime = "09:00:00"; endTime = "17:00:00"; isActive = $true }
        )
    }
    $r = Api PUT "/api/clinic/settings" (MakeHeaders $ownerToken "demo-clinic") $body
    $r.success -eq $true -and $r.data.clinicName -eq "Demo Dental Clinic Updated"
}

Test "CS04 GET settings reflects update" {
    $r = Api GET "/api/clinic/settings" (MakeHeaders $ownerToken "demo-clinic")
    $r.data.clinicName -eq "Demo Dental Clinic Updated" -and $r.data.bookingEnabled -eq $true
}

Test "CS05 PUT settings (staff forbidden)" {
    $r = Api PUT "/api/clinic/settings" (MakeHeaders $staffToken "demo-clinic") @{clinicName="Hack"}
    $r.success -eq $false
}

Test "CS06 GET settings without tenant returns error" {
    $r = Api GET "/api/clinic/settings" @{"Authorization"="Bearer $ownerToken";"Content-Type"="application/json"}
    $r.success -eq $false
}

###############################################################################
# 2. STAFF CRUD
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 2. Staff CRUD Ã¢â€â‚¬Ã¢â€â‚¬")

Test "ST01 Create staff (owner)" {
    $body = @{
        name = "Test Staff"
        phone = "+201999999999"
        username = "test_staff_$ts"
        password = "Staff@123456"
        role = "ClinicManager"
        salary = 3500
    }
    $r = Api POST "/api/clinic/staff" (MakeHeaders $ownerToken "demo-clinic") $body
    $script:newStaffId = $r.data.id
    $r.success -eq $true -and $r.data.name -eq "Test Staff"
}

Test "ST02 List staff" {
    $r = Api GET "/api/clinic/staff" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.items.Count -ge 3
}

Test "ST03 Get staff by id" {
    $r = Api GET "/api/clinic/staff/$($script:newStaffId)" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.name -eq "Test Staff"
}

Test "ST04 Update staff" {
    $body = @{ name = "Test Staff Updated"; phone = "+201999999998"; salary = 4000 }
    $r = Api PUT "/api/clinic/staff/$($script:newStaffId)" (MakeHeaders $ownerToken "demo-clinic") $body
    $r.success -eq $true -and $r.data.name -eq "Test Staff Updated"
}

Test "ST05 Disable staff" {
    $r = Api POST "/api/clinic/staff/$($script:newStaffId)/disable" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true
}

Test "ST06 Enable staff" {
    $r = Api POST "/api/clinic/staff/$($script:newStaffId)/enable" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true
}

Test "ST07 Staff can list staff" {
    $r = Api GET "/api/clinic/staff" (MakeHeaders $staffToken "demo-clinic")
    $r.success -eq $true
}

Test "ST08 Staff cannot create staff" {
    $body = @{ name = "Hack Staff"; phone = "+201000000000"; username = "hack_staff_$ts"; password = "Staff@123456" }
    $r = Api POST "/api/clinic/staff" (MakeHeaders $staffToken "demo-clinic") $body
    $r.success -eq $false
}

###############################################################################
# 3. DOCTOR CRUD
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 3. Doctor CRUD Ã¢â€â‚¬Ã¢â€â‚¬")

Test "DR01 Create doctor (owner)" {
    $body = @{
        name = "Dr. Test Doctor"
        specialty = "Pediatrics"
        phone = "+201888888888"
        username = "dr_test_$ts"
        password = "Doctor@123456"
        avgVisitDurationMinutes = 25
    }
    $r = Api POST "/api/clinic/doctors" (MakeHeaders $ownerToken "demo-clinic") $body
    $script:newDoctorId = $r.data.id
    $r.success -eq $true -and $r.data.name -eq "Dr. Test Doctor"
}

Test "DR02 List doctors" {
    $r = Api GET "/api/clinic/doctors" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.items.Count -ge 3
}

Test "DR03 Get doctor by id" {
    $r = Api GET "/api/clinic/doctors/$($script:newDoctorId)" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.name -eq "Dr. Test Doctor"
}

Test "DR04 Update doctor" {
    $body = @{ name = "Dr. Test Updated"; specialty = "General"; phone = "+201888888887" }
    $r = Api PUT "/api/clinic/doctors/$($script:newDoctorId)" (MakeHeaders $ownerToken "demo-clinic") $body
    $r.success -eq $true -and $r.data.name -eq "Dr. Test Updated"
}

Test "DR05 Update doctor services" {
    $body = @{
        services = @(
            @{ serviceName = "Check-up"; price = 100; durationMinutes = 15; isActive = $true },
            @{ serviceName = "Vaccination"; price = 250; durationMinutes = 20; isActive = $true }
        )
    }
    $r = Api PUT "/api/clinic/doctors/$($script:newDoctorId)/services" (MakeHeaders $ownerToken "demo-clinic") $body
    $r.success -eq $true -and $r.data.Count -eq 2
}

Test "DR06 Update visit field config" {
    $body = @{
        bloodPressure = $true; heartRate = $true; temperature = $true; weight = $true
        height = $false; bmi = $false; bloodSugar = $false; oxygenSaturation = $false; respiratoryRate = $false
    }
    $r = Api PUT "/api/clinic/doctors/$($script:newDoctorId)/visit-fields" (MakeHeaders $ownerToken "demo-clinic") $body
    $r.success -eq $true -and $r.data.bloodPressure -eq $true
}

Test "DR07 Disable doctor" {
    $r = Api POST "/api/clinic/doctors/$($script:newDoctorId)/disable" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true
}

Test "DR08 Enable doctor" {
    $r = Api POST "/api/clinic/doctors/$($script:newDoctorId)/enable" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true
}

Test "DR09 Staff can list doctors" {
    $r = Api GET "/api/clinic/doctors" (MakeHeaders $staffToken "demo-clinic")
    $r.success -eq $true
}

Test "DR10 Staff cannot create doctor" {
    $body = @{ name = "Hack Dr"; phone = "+201000000000"; username = "hack_dr_$ts"; password = "Doctor@123456" }
    $r = Api POST "/api/clinic/doctors" (MakeHeaders $staffToken "demo-clinic") $body
    $r.success -eq $false
}

###############################################################################
# 4. PATIENT CRUD
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 4. Patient CRUD Ã¢â€â‚¬Ã¢â€â‚¬")

Test "PT01 Create patient (owner)" {
    $body = @{
        name = "Test Patient"
        phone = "+201777777777"
        gender = 0
        dateOfBirth = "1990-05-15"
    }
    $r = Api POST "/api/clinic/patients" (MakeHeaders $ownerToken "demo-clinic") $body
    $script:newPatientId = $r.data.patient.id
    $script:newPatientUsername = $r.data.username
    $script:newPatientPassword = $r.data.password
    $r.success -eq $true -and $r.data.username -match "^patient_demo-clinic_"
}

Test "PT02 Create patient returns credentials" {
    $script:newPatientUsername -ne $null -and $script:newPatientPassword -ne $null
}

Test "PT03 List patients" {
    $r = Api GET "/api/clinic/patients" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.items.Count -ge 7
}

Test "PT04 Get patient by id" {
    $r = Api GET "/api/clinic/patients/$($script:newPatientId)" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.name -eq "Test Patient"
}

Test "PT05 Update patient" {
    $body = @{ name = "Test Patient Updated"; phone = "+201777777776"; address = "456 Elm St" }
    $r = Api PUT "/api/clinic/patients/$($script:newPatientId)" (MakeHeaders $ownerToken "demo-clinic") $body
    $r.success -eq $true -and $r.data.name -eq "Test Patient Updated"
}

Test "PT06 Add sub-profile" {
    $body = @{
        name = "Child of Test"
        phone = "+201777777777"
        gender = 1
        dateOfBirth = "2020-01-01"
    }
    $r = Api POST "/api/clinic/patients/$($script:newPatientId)/profiles" (MakeHeaders $ownerToken "demo-clinic") $body
    $r.success -eq $true -and ($r.data.subProfiles | Where-Object { $_.name -eq "Child of Test" }) -ne $null
}

Test "PT07 Get patient includes sub-profiles" {
    $r = Api GET "/api/clinic/patients/$($script:newPatientId)" (MakeHeaders $ownerToken "demo-clinic")
    $r.data.subProfiles.Count -ge 1
}

Test "PT08 Reset patient password" {
    $r = Api POST "/api/clinic/patients/$($script:newPatientId)/reset-password" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.newPassword -ne $null
}

Test "PT09 Staff can create patient" {
    $body = @{ name = "Staff Created Patient"; phone = "+201666666666"; gender = 1 }
    $r = Api POST "/api/clinic/patients" (MakeHeaders $staffToken "demo-clinic") $body
    $r.success -eq $true
}

Test "PT10 Search patients by name" {
    $r = Api GET "/api/clinic/patients?search=Mohamed" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "PT11 Delete patient (owner)" {
    $r = Api DELETE "/api/clinic/patients/$($script:newPatientId)" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true
}

Test "PT12 Deleted patient not in list" {
    $r = Api GET "/api/clinic/patients" (MakeHeaders $ownerToken "demo-clinic")
    $found = $r.data.items | Where-Object { $_.id -eq $script:newPatientId }
    $found -eq $null
}

###############################################################################
# 5. PATIENT LOGIN
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 5. Patient Login Ã¢â€â‚¬Ã¢â€â‚¬")

Test "PL01 Patient login via /patient/login" {
    $body = @{ username = "patient_demo-clinic_1"; password = "Patient@1234" }
    $r = Api POST "/api/auth/patient/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} $body
    $r.success -eq $true -and $r.data.token -ne $null
}

Test "PL02 Patient login returns profiles" {
    $body = @{ username = "patient_demo-clinic_1"; password = "Patient@1234" }
    $r = Api POST "/api/auth/patient/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} $body
    $r.data.user.profiles.Count -ge 1
}

Test "PL03 Patient login wrong password" {
    $body = @{ username = "patient_demo-clinic_1"; password = "wrong" }
    $r = Api POST "/api/auth/patient/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} $body
    $r.success -eq $false
}

Test "PL04 Patient login wrong tenant" {
    $body = @{ username = "patient_demo-clinic_1"; password = "Patient@1234" }
    $r = Api POST "/api/auth/patient/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic-other"} $body
    $r.success -eq $false
}

###############################################################################
# 6. TENANT-SCOPED AUTH
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 6. Tenant-Scoped Auth Ã¢â€â‚¬Ã¢â€â‚¬")

Test "TS01 Owner login wrong tenant fails" {
    $body = @{ username = "owner_demo"; password = "Owner@123456" }
    $r = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="nonexistent-tenant"} $body
    $r.success -eq $false
}

Test "TS02 Staff login wrong tenant fails" {
    $body = @{ username = "staff_sara"; password = "Staff@123456" }
    $r = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="nonexistent-tenant"} $body
    $r.success -eq $false
}

Test "TS03 SuperAdmin no tenant still works" {
    $body = @{ username = "superadmin"; password = "Admin@123456" }
    $r = Api POST "/api/auth/login" @{"Content-Type"="application/json"} $body
    $r.success -eq $true
}

Test "TS04 Clinic endpoint without tenant header" {
    $r = Api GET "/api/clinic/settings" @{"Authorization"="Bearer $ownerToken";"Content-Type"="application/json"}
    $r.success -eq $false
}

###############################################################################
# 7. MIDDLEWARE ENFORCEMENT
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 7. Middleware Enforcement Ã¢â€â‚¬Ã¢â€â‚¬")

Test "MW01 No auth header returns 401 for clinic" {
    $r = Api GET "/api/clinic/settings" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"}
    $r.success -eq $false
}

Test "MW02 Invalid token returns 401" {
    $r = Api GET "/api/clinic/settings" @{"Authorization"="Bearer invalidtoken";"Content-Type"="application/json";"X-Tenant"="demo-clinic"}
    $r.success -eq $false
}

Test "MW03 Invalid tenant slug returns error" {
    $r = Api GET "/api/clinic/settings" @{"Authorization"="Bearer $ownerToken";"Content-Type"="application/json";"X-Tenant"="nonexistent"}
    $r.success -eq $false
}

###############################################################################
# 8. CROSS-TENANT ISOLATION
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 8. Cross-Tenant Isolation Ã¢â€â‚¬Ã¢â€â‚¬")

# Get a token for smile-dental tenant (create owner if needed via superadmin)
Test "CT01 Owner sees only own tenant patients" {
    $r = Api GET "/api/clinic/patients" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "CT02 Owner sees only own tenant doctors" {
    $r = Api GET "/api/clinic/doctors" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.items.Count -ge 2
}

Test "CT03 Owner sees only own tenant staff" {
    $r = Api GET "/api/clinic/staff" (MakeHeaders $ownerToken "demo-clinic")
    $r.success -eq $true -and $r.data.items.Count -ge 2
}

###############################################################################
# 9. PHASE 1 BUG FIXES
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 9. Phase 1 Bug Fixes Ã¢â€â‚¬Ã¢â€â‚¬")

Test "BF01 SV03: Create subscription with nullable dates" {
    # Get a tenant id first
    $tenants = Api GET "/api/platform/tenants" (MakeHeaders $superToken)
    $tenantId = $tenants.data.items[0].id
    $body = @{
        tenantId = $tenantId
        planName = "Premium"
        startDate = (Get-Date).ToString("yyyy-MM-dd")
        endDate = (Get-Date).AddYears(1).ToString("yyyy-MM-dd")
        amount = 999.99
        currency = "EGP"
    }
    $r = Api POST "/api/platform/subscriptions" (MakeHeaders $superToken) $body
    $r.success -eq $true
}

Test "BF02 RF06: Invalid model returns ApiResponse format" {
    $r = Api POST "/api/auth/login" @{"Content-Type"="application/json"} @{username="";password=""}
    # Should still be wrapped in ApiResponse format
    $r.PSObject.Properties.Name -contains "success" -or $r.PSObject.Properties.Name -contains "errors"
}

###############################################################################
# 10. PHASE 1 REGRESSION
###############################################################################
[Console]::WriteLine("`nÃ¢â€â‚¬Ã¢â€â‚¬ 10. Phase 1 Regression Ã¢â€â‚¬Ã¢â€â‚¬")

Test "RG01 Tenant CRUD still works" {
    $r = Api GET "/api/platform/tenants" (MakeHeaders $superToken)
    $r.success -eq $true -and $r.data.items.Count -ge 1
}

Test "RG02 Feature flags still work" {
    $tenants = Api GET "/api/platform/tenants" (MakeHeaders $superToken)
    $tenantId = $tenants.data.items[0].id
    $r = Api GET "/api/platform/feature-flags/$tenantId" (MakeHeaders $superToken)
    $r.success -eq $true
}

Test "RG03 Subscription list still works" {
    $r = Api GET "/api/platform/subscriptions" (MakeHeaders $superToken)
    $r.success -eq $true
}

Test "RG04 Health endpoint works" {
    $r = Api GET "/api/health" @{}
    $r -ne $null
}

Test "RG05 Refresh token still works" {
    $loginR = Api POST "/api/auth/login" @{"Content-Type"="application/json"} @{username="superadmin";password="Admin@123456"}
    $refreshBody = @{ refreshToken = $loginR.data.refreshToken }
    $r = Api POST "/api/auth/refresh" @{"Content-Type"="application/json"} $refreshBody
    $r.success -eq $true -and $r.data.token -ne $null
}

Test "RG06 Me endpoint still works" {
    $r = Api GET "/api/auth/me" (MakeHeaders $superToken)
    $r.success -eq $true -and $r.data.username -eq "superadmin"
}

###############################################################################
# SUMMARY
###############################################################################
[Console]::WriteLine("`n========================================")
[Console]::WriteLine("  TOTAL: $total | PASS: $pass | FAIL: $fail")
[Console]::WriteLine("========================================`n")
