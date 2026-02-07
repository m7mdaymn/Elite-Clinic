$base = "http://localhost:5094"

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
            try {
                $reader = New-Object System.IO.StreamReader($sr.GetResponseStream())
                $errBody = $reader.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop
                return $errBody
            } catch {
                return @{ success = $false; message = "HTTP $statusCode"; statusCode = $statusCode }
            }
        }
        throw
    }
}

# Login
$ownerLogin = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="owner_demo";password="Owner@123456"}
$ownerToken = $ownerLogin.data.token
Write-Host "Token: $($ownerToken.Substring(0,20))..."

# CS03 test
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
$h = @{"Authorization"="Bearer $ownerToken";"X-Tenant"="demo-clinic"}
$r = Api PUT "/api/clinic/settings" $h $body
Write-Host "CS03 success=$($r.success) clinicName=$($r.data.clinicName)"
Write-Host "CS03 result: $($r.success -eq $true -and $r.data.clinicName -eq 'Demo Dental Clinic Updated')"

# Staff test - 403 response
$staffLogin = Api POST "/api/auth/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="staff_sara";password="Staff@123456"}
$staffToken = $staffLogin.data.token
$r2 = Api PUT "/api/clinic/settings" @{"Authorization"="Bearer $staffToken";"X-Tenant"="demo-clinic"} @{clinicName="Hack"}
Write-Host "CS05 response type=$($r2.GetType().Name) success=$($r2.success) keys=$($r2.PSObject.Properties.Name -join ',')"
Write-Host "CS05 result: $($r2.success -eq $false)"

# Patient login test
$r3 = Api POST "/api/auth/patient/login" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"} @{username="patient_demo-clinic_1";password="Patient@1234"}
Write-Host "PL01 success=$($r3.success) token=$($r3.data.token -ne $null)"
Write-Host "PL01 profiles_path=$($r3.data.user.profiles.Count)"

# MW01 test - no auth
$r4 = Api GET "/api/clinic/settings" @{"Content-Type"="application/json";"X-Tenant"="demo-clinic"}
Write-Host "MW01 response type=$($r4.GetType().Name) success=$($r4.success) keys=$($r4.PSObject.Properties.Name -join ',')"
