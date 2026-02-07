# COMPLETION_V1.md — Phase 1 Runbook

> **Phase:** 1 — Tenant Management, Subscriptions & Feature Flags  
> **Date:** 2026-02-07  
> **Purpose:** How to build, run, seed, test, and verify Phase 1

---

## 1. Prerequisites

- .NET 9 SDK installed
- Git repository cloned
- No external services required (SQLite for dev, SQL Server for production)

---

## 2. Build

```powershell
cd "c:\DATA\Elite Clinic"
dotnet build EliteClinic.sln
```

**Expected:** `Build succeeded. 0 Error(s)`. 26 CS8618 nullable warnings are pre-existing from Phase 0 (non-critical).

---

## 3. Run the API

```powershell
dotnet run --project src/EliteClinic.Api
```

**Expected:** API starts on `http://localhost:5094`. Console shows:
- `Now listening on: http://localhost:5094`
- EF Core migration auto-applied
- Seed data auto-applied (idempotent)

---

## 4. Verify Health

```powershell
Invoke-RestMethod -Uri "http://localhost:5094/api/health"
```

**Expected:**
```json
{
  "success": true,
  "data": {
    "status": "Healthy",
    "database": "Connected",
    "version": "0.0.1"
  }
}
```

---

## 5. Login as SuperAdmin

```powershell
$login = Invoke-RestMethod -Uri "http://localhost:5094/api/auth/login" `
  -Method POST -ContentType "application/json" `
  -Body '{"username":"superadmin","password":"Admin@123456"}'
$token = $login.data.token
$headers = @{ "Authorization" = "Bearer $token" }
```

**Expected:** HTTP 200, JWT token returned.

---

## 6. Verify Seed Data

### 6.1 Tenants (4 seeded)
```powershell
$tenants = Invoke-RestMethod -Uri "http://localhost:5094/api/platform/tenants?pageSize=100" -Headers $headers
$tenants.data.items | Select-Object slug, status | Format-Table
```

**Expected:** `demo-clinic` (0/Active), `suspended-clinic` (1), `blocked-clinic` (2), `inactive-clinic` (3)

### 6.2 Feature Flags (auto-created)
```powershell
# Get demo-clinic tenant ID first
$demoId = ($tenants.data.items | Where-Object { $_.slug -eq "demo-clinic" }).id
$flags = Invoke-RestMethod -Uri "http://localhost:5094/api/platform/feature-flags/$demoId" -Headers $headers
$flags.data | Format-List
```

**Expected:** `onlineBooking=False, whatsappAutomation=True, pwaNotifications=False, expensesModule=True, advancedMedicalTemplates=False, ratings=False, export=False`

### 6.3 Subscriptions (4 seeded for demo-clinic)
```powershell
$subs = Invoke-RestMethod -Uri "http://localhost:5094/api/platform/subscriptions?tenantId=$demoId" -Headers $headers
$subs.data.items | Select-Object planName, status, isPaid | Format-Table
```

**Expected:** 4 subscriptions: Basic Monthly (Active/unpaid), Premium Quarterly (Active/paid), Trial Plan (Expired/paid), Annual Plan (Cancelled/unpaid)

---

## 7. Run Automated Tests

```powershell
cd "c:\DATA\Elite Clinic"
powershell -ExecutionPolicy Bypass -File "run-tests.ps1"
```

**Expected:** 73+ PASS, 1 FAIL (RF06 — model validation format), 12 DEFERRED

**Additional manual tests:**
```powershell
# SV03 - Missing StartDate (known failure)
# SV08 - Subscription for deleted tenant
# FF05 - No-op flag update
# RG04 - Refresh token
```

---

## 8. Verify Swagger UI

Open browser: `http://localhost:5094/swagger`

**Expected:** Swagger UI loads with all Phase 1 endpoints visible:
- Platform > Tenants (8 endpoints)
- Platform > Subscriptions (5 endpoints)
- Platform > FeatureFlags (2 endpoints)

---

## 9. Database Schema Check

### Tables Created in Phase 1
- `Subscriptions` (19 columns, FK to Tenants)
- `TenantFeatureFlags` (12 columns, FK to Tenants, unique TenantId index)

### Migration Applied
- `20260207022713_Phase1_TenantSubscriptionFlags`

---

## 10. Phase 1 Endpoint Reference

| # | Method | Route | Purpose |
|---|--------|-------|---------|
| 1 | POST | `/api/platform/tenants` | Create tenant |
| 2 | GET | `/api/platform/tenants` | List tenants (paginated) |
| 3 | GET | `/api/platform/tenants/{id}` | Get tenant details |
| 4 | PUT | `/api/platform/tenants/{id}` | Update tenant |
| 5 | POST | `/api/platform/tenants/{id}/activate` | Activate |
| 6 | POST | `/api/platform/tenants/{id}/suspend` | Suspend |
| 7 | POST | `/api/platform/tenants/{id}/block` | Block |
| 8 | DELETE | `/api/platform/tenants/{id}` | Soft-delete |
| 9 | POST | `/api/platform/subscriptions` | Create subscription |
| 10 | GET | `/api/platform/subscriptions` | List subscriptions |
| 11 | POST | `/api/platform/subscriptions/{id}/extend` | Extend |
| 12 | POST | `/api/platform/subscriptions/{id}/cancel` | Cancel |
| 13 | POST | `/api/platform/subscriptions/{id}/mark-paid` | Mark paid |
| 14 | GET | `/api/platform/feature-flags/{tenantId}` | Get flags |
| 15 | PUT | `/api/platform/feature-flags/{tenantId}` | Update flags |

---

## 11. Known Issues

| Issue | Impact | Planned Fix |
|-------|--------|-------------|
| SV03: DateTime defaults | Low — omitting StartDate creates subscription with 0001-01-01 | Phase 2: Make DateTime nullable |
| RF06: ProblemDetails format | Medium — model validation errors use different format than service errors | Phase 2: Add InvalidModelStateResponseFactory |
| 26 CS8618 warnings | None — runtime correct | Phase 2: Add `required` modifier |

---

*Last verified: 2026-02-07 against live API on http://localhost:5094*
