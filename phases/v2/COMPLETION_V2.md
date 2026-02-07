# COMPLETION_V2.md — Phase 2 Runbook

> **Phase:** 2 — Clinic Setup & Users  
> **Date:** 2026-02-08  
> **Purpose:** How to build, run, seed, test, and verify Phase 2

---

## 1. Prerequisites

- .NET 9 SDK installed
- Git repository cloned with Phase 2 code
- SQL Server running (remote: `db40278.public.databaseasp.net`)
- PowerShell 5.1+ (for test script)

---

## 2. Build

```powershell
cd "c:\DATA\Elite Clinic"
dotnet build --no-restore
```

**Expected output:**
```
0 Error(s)
Time Elapsed 00:00:02.56
```

Note: 26 CS8618 nullable warnings from Phase 0 are pre-existing and non-critical.

---

## 3. Database Migrations

Migrations apply automatically on startup. No manual step required.

**Phase 2 migrations applied:**
- `20260207022713_Phase1_TenantSubscriptionFlags` (Phase 1)
- `20260207022714_Phase2_ClinicSetupUsers` (Phase 2 entities)
- `20260207022715_Phase2_PatientUserIdNotUnique` (Patient.UserId index adjustment)

All 4 migrations confirmed applied successfully.

---

## 4. Seed Data

Seed data is auto-applied via `Program.cs` on startup (idempotent).

### Seeded Entities:

#### Tenants
- **demo-clinic** — Active, slug `demo-clinic`

#### Users & Roles
- **superadmin** / `Admin@123456` — SuperAdmin (no tenant)
- **owner_demo** / `Owner@123456` — ClinicOwner (demo-clinic)
- **staff_sara** / `Staff@123456` — ClinicManager (demo-clinic)
- **staff_ali** / `Staff@123456` — ClinicManager (demo-clinic)
- **dr_khaled** / `Doctor@123456` — Doctor (demo-clinic)
- **dr_mona** / `Doctor@123456` — Doctor (demo-clinic)
- **patient_demo-clinic_1** through **_6** / `Patient@1234` — Patient (demo-clinic)

#### Clinic Settings
- ClinicName: "Demo Dental Clinic"
- Phone: "+201000000099"
- Address: "123 Main St"
- City: "Cairo"
- BookingEnabled: true
- CancellationWindowHours: 2
- WorkingHours: Sunday–Friday 9AM–5PM

#### Doctors
- **Dr. Khaled** (Pediatrics)
  - Services: "General Check-up" (150 EGP, 15 min), "Fluoride Treatment" (200 EGP, 20 min)
  - Visit fields: Temperature + Weight enabled
- **Dr. Mona** (Ortho)
  - Services: "Consultation" (100 EGP, 10 min), "Braces Adjustment" (250 EGP, 30 min)
  - Visit fields: Temperature + Weight enabled

#### Patients
- **patient_demo-clinic_1** through **_6** — Walk-in registrations with auto-generated credentials

---

## 5. Run the API

```powershell
cd "c:\DATA\Elite Clinic"
dotnet run --project src/EliteClinic.Api
```

**Expected output:**
```
now listening on: http://localhost:5094
Hosting started
Health check log: Database connected, applying seed...
```

The API is ready when you see `Hosting started`.

---

## 6. Verify Health

```powershell
Invoke-WebRequest -Uri "http://localhost:5094/api/health" -UseBasicParsing
```

**Expected response:**
```json
{
  "success": true,
  "data": {
    "status": "Healthy",
    "database": "Connected",
    "version": "2.0.0"
  }
}
```

---

## 7. Verify Seeded Data

### 7.1 Login as SuperAdmin

```powershell
$body = @{username="superadmin"; password="Admin@123456"} | ConvertTo-Json
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/auth/login" `
  -Method Post `
  -Headers @{"Content-Type"="application/json"} `
  -Body $body `
  -UseBasicParsing
$token = ($r.Content | ConvertFrom-Json).data.token
Write-Host "Token: $($token.Substring(0,20))..."  # First 20 chars
```

**Expected:** Token string obtained (JWT format, ~600 chars).

### 7.2 Verify Tenants

```powershell
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/platform/tenants" `
  -Method Get `
  -Headers @{"Authorization"="Bearer $token"; "Content-Type"="application/json"} `
  -UseBasicParsing
$tenants = $r.Content | ConvertFrom-Json
$tenants.data.items | Select-Object name, slug, status
```

**Expected output:**
```
name              slug           status
----              ----           ------
Demo Clinic       demo-clinic    0 (Active)
```

### 7.3 Login as ClinicOwner (demo-clinic)

```powershell
$body = @{username="owner_demo"; password="Owner@123456"} | ConvertTo-Json
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/auth/login" `
  -Method Post `
  -Headers @{"Content-Type"="application/json"; "X-Tenant"="demo-clinic"} `
  -Body $body `
  -UseBasicParsing
$ownerToken = ($r.Content | ConvertFrom-Json).data.token
Write-Host "ClinicOwner logged in"
```

**Expected:** Token obtained.

### 7.4 Check Clinic Settings

```powershell
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/clinic/settings" `
  -Method Get `
  -Headers @{"Authorization"="Bearer $ownerToken"; "Content-Type"="application/json"; "X-Tenant"="demo-clinic"} `
  -UseBasicParsing
$settings = $r.Content | ConvertFrom-Json
$settings.data | Select-Object clinicName, phone, address, bookingEnabled, workingHours
```

**Expected:**
```
clinicName                 : Demo Dental Clinic
phone                      : +201000000099
address                    : 123 Main St
bookingEnabled             : True
workingHours (count)       : 5
```

### 7.5 Check Staff

```powershell
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/clinic/staff" `
  -Method Get `
  -Headers @{"Authorization"="Bearer $ownerToken"; "Content-Type"="application/json"; "X-Tenant"="demo-clinic"} `
  -UseBasicParsing
$staff = $r.Content | ConvertFrom-Json
$staff.data.items | Select-Object name, role, username | Format-Table
```

**Expected:** 2+ staff members listed (sara, ali).

### 7.6 Check Doctors

```powershell
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/clinic/doctors" `
  -Method Get `
  -Headers @{"Authorization"="Bearer $ownerToken"; "Content-Type"="application/json"; "X-Tenant"="demo-clinic"} `
  -UseBasicParsing
$doctors = $r.Content | ConvertFrom-Json
$doctors.data.items | Select-Object name, specialty, username | Format-Table
```

**Expected:** 2+ doctors listed (khaled, mona) with services.

### 7.7 Check Patients

```powershell
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/clinic/patients" `
  -Method Get `
  -Headers @{"Authorization"="Bearer $ownerToken"; "Content-Type"="application/json"; "X-Tenant"="demo-clinic"} `
  -UseBasicParsing
$patients = $r.Content | ConvertFrom-Json
$patients.data.items | Select-Object name, phone, username | Format-Table
```

**Expected:** 6+ patients listed (patient_demo-clinic_1 through _6).

### 7.8 Patient Login

```powershell
$body = @{username="patient_demo-clinic_1"; password="Patient@1234"} | ConvertTo-Json
$r = Invoke-WebRequest `
  -Uri "http://localhost:5094/api/auth/patient/login" `
  -Method Post `
  -Headers @{"Content-Type"="application/json"; "X-Tenant"="demo-clinic"} `
  -Body $body `
  -UseBasicParsing
$patientLogin = $r.Content | ConvertFrom-Json
$patientLogin.data | Select-Object token, @{N="ProfileCount"; E={$_.user.profiles.Count}}
```

**Expected:**
```
token           : (long JWT string)
ProfileCount    : 1
```

---

## 8. Run All Tests

### 8.1 Quick Test (Manual Commands)

See Section 7 above — each subsection is a manual verification test.

### 8.2 Full Test Suite

```powershell
cd "c:\DATA\Elite Clinic"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "tests\Phase2_Tests.ps1" > "tests\results_final.txt" 2>&1
```

Wait ~30–60 seconds for tests to complete.

### 8.3 View Results

```powershell
Get-Content "tests\results_final.txt" | Select-String "PASS|FAIL|TOTAL"
```

**Expected output:**
```
TOTAL: 58 | PASS: 58 | FAIL: 0
```

All 58 tests should pass.

---

## 9. Troubleshooting

### Issue: "Failed to bind to address http://127.0.0.1:5094"

**Cause:** Another process is already using port 5094.

**Solution:**
```powershell
$p = Get-NetTCPConnection -LocalPort 5094 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -First 1
if ($p) { Stop-Process -Id $p -Force; Write-Host "Killed PID $p" }
# Then retry: dotnet run --project src/EliteClinic.Api
```

### Issue: "Cannot connect to database"

**Cause:** SQL Server is down or credentials are wrong.

**Solution:**
1. Verify SQL Server is running: `ping db40278.public.databaseasp.net`
2. Check `appsettings.json` for correct connection string
3. Verify VPN/network connectivity if using remote SQL Server

### Issue: Tests fail with "Cannot login"

**Cause:** Seed data not applied (e.g., API crashed before seeding).

**Solution:**
1. Restart API: `dotnet run --project src/EliteClinic.Api`
2. Wait 5 seconds for seed to complete
3. Verify health: `Invoke-WebRequest -Uri "http://localhost:5094/api/health"`
4. Retry tests

### Issue: "Staff cannot create staff" test passes but should fail

**Cause:** Authorization check is not enforced.

**Solution:**
1. Verify `ClinicManager` role does NOT have `[Authorize(Roles = "ClinicOwner,SuperAdmin")]` annotation
2. Check `StaffController.Create` has proper role check
3. Rebuild: `dotnet build --no-restore`

---

## 10. Phase 2 Feature Checklist

- [x] **Clinic Settings** — Get/Update (6 tests)
- [x] **Staff CRUD** — Create, List, Get, Update, Enable/Disable (8 tests)
- [x] **Doctor CRUD** — Create, List, Get, Update, Enable/Disable, Services, Visit Fields (10 tests)
- [x] **Patient CRUD** — Create, List, Get, Update, Sub-Profiles, Reset Password (12 tests)
- [x] **Patient Login** — `/api/auth/patient/login` with profiles array (4 tests)
- [x] **Tenant-Scoped Auth** — `X-Tenant` header enforcement (4 tests)
- [x] **Middleware Enforcement** — 401/403 on missing/invalid headers (3 tests)
- [x] **Cross-Tenant Isolation** — Data scoped by tenant (3 tests)
- [x] **Phase 1 Bug Fixes** — DateTime nullable, ApiResponse wrapper (2 tests)
- [x] **Phase 1 Regression** — All Phase 1 endpoints still work (6 tests)

**Total: 58/58 tests passing.**

---

## 11. Next Steps

After Phase 2 completion:

1. **Phase 3 Planning** — Queue system, visits, prescriptions
2. **Phase 4 Planning** — WhatsApp integration, online booking
3. **Phase 5 Planning** — Reporting, analytics, SignalR real-time

---

## Quick Commands Reference

```powershell
# Build
dotnet build --no-restore

# Run API
dotnet run --project src/EliteClinic.Api

# Health check
Invoke-WebRequest -Uri "http://localhost:5094/api/health" -UseBasicParsing

# Run tests
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "tests\Phase2_Tests.ps1" > "tests\results.txt" 2>&1

# View test results
Get-Content "tests\results.txt" | Select-String "TOTAL|PASS|FAIL"

# Kill process on port 5094
Stop-Process -Id (Get-NetTCPConnection -LocalPort 5094 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -First 1) -Force
```
