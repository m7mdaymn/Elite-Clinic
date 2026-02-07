# TESTS_V2.md — Phase 2 Test Results (Real Execution Evidence)

> **Phase:** 2 — Clinic Setup & Users  
> **Status:** ✅ EXECUTED — 58/58 PASS, 0 FAIL  
> **Execution Date:** 2026-02-08  
> **Test Runner:** PowerShell `tests/Phase2_Tests.ps1` against live API on `http://localhost:5094`  
> **Total Tests:** 58

---

## EXECUTION METHOD

Tests were executed via `tests/Phase2_Tests.ps1` PowerShell script making real HTTP requests to the live API.
- Login: Multiple roles tested — ClinicOwner (`owner_demo`), SuperAdmin (`superadmin`), Staff (`staff_sara`), Doctor (`dr_khaled`)
- Patient login: `POST /api/auth/patient/login` tested separately
- All endpoints tested with real HTTP calls, real status codes and response bodies validated
- Seed data: `demo-clinic` tenant with 1 owner, 2 staff, 2 doctors (with services), 6 patients

---

## 1. CLINIC SETTINGS (6 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| CS01 | `GET /api/clinic/settings` (owner) | 200, returns settings with clinicName | ✅ PASS | `success=true`, `clinicName` present |
| CS02 | `GET /api/clinic/settings` (staff can read) | 200, staff has read access | ✅ PASS | `success=true` |
| CS03 | `PUT /api/clinic/settings` (owner updates) | 200, settings updated with working hours | ✅ PASS | `clinicName="Demo Dental Clinic Updated"`, 2 working hours applied |
| CS04 | `GET /api/clinic/settings` reflects update | Settings persisted | ✅ PASS | `clinicName="Demo Dental Clinic Updated"`, `bookingEnabled=true` |
| CS05 | `PUT /api/clinic/settings` (staff forbidden) | 403 Forbidden | ✅ PASS | `success=false` |
| CS06 | `GET /api/clinic/settings` without tenant header | Error returned | ✅ PASS | `success=false` |

---

## 2. STAFF CRUD (8 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| ST01 | `POST /api/clinic/staff` (owner) | 201, staff created with user account | ✅ PASS | `success=true`, `name="Test Staff"` |
| ST02 | `GET /api/clinic/staff` | 200, paginated list ≥ 3 items | ✅ PASS | `items.Count ≥ 3` (2 seeded + 1 created) |
| ST03 | `GET /api/clinic/staff/{id}` | 200, correct staff returned | ✅ PASS | `name="Test Staff"` |
| ST04 | `PUT /api/clinic/staff/{id}` | 200, fields updated | ✅ PASS | `name="Test Staff Updated"` |
| ST05 | `POST /api/clinic/staff/{id}/disable` | 200, staff disabled | ✅ PASS | `success=true` |
| ST06 | `POST /api/clinic/staff/{id}/enable` | 200, staff re-enabled | ✅ PASS | `success=true` |
| ST07 | Staff can list staff (read-only) | 200 | ✅ PASS | `success=true` |
| ST08 | Staff cannot create staff | 403 Forbidden | ✅ PASS | `success=false` |

---

## 3. DOCTOR CRUD (10 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| DR01 | `POST /api/clinic/doctors` (owner) | 201, doctor created with user account | ✅ PASS | `success=true`, `name="Dr. Test Doctor"` |
| DR02 | `GET /api/clinic/doctors` | 200, paginated list ≥ 3 items | ✅ PASS | `items.Count ≥ 3` (2 seeded + 1 created) |
| DR03 | `GET /api/clinic/doctors/{id}` | 200, correct doctor returned | ✅ PASS | `name="Dr. Test Doctor"` |
| DR04 | `PUT /api/clinic/doctors/{id}` | 200, fields updated | ✅ PASS | `name="Dr. Test Updated"` |
| DR05 | `PUT /api/clinic/doctors/{id}/services` | 200, 2 services assigned | ✅ PASS | `data.Count=2` (Check-up, Vaccination) |
| DR06 | `PUT /api/clinic/doctors/{id}/visit-fields` | 200, vitals config saved | ✅ PASS | `bloodPressure=true` |
| DR07 | `POST /api/clinic/doctors/{id}/disable` | 200, doctor disabled | ✅ PASS | `success=true` |
| DR08 | `POST /api/clinic/doctors/{id}/enable` | 200, doctor re-enabled | ✅ PASS | `success=true` |
| DR09 | Staff can list doctors (read-only) | 200 | ✅ PASS | `success=true` |
| DR10 | Staff cannot create doctor | 403 Forbidden | ✅ PASS | `success=false` |

---

## 4. PATIENT CRUD (12 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| PT01 | `POST /api/clinic/patients` (owner) | 201, patient + auto-generated credentials | ✅ PASS | `success=true`, `username` matches `patient_demo-clinic_*` |
| PT02 | Create patient returns credentials | Username + password in response | ✅ PASS | Both non-null |
| PT03 | `GET /api/clinic/patients` | 200, list ≥ 7 items | ✅ PASS | `items.Count ≥ 7` (6 seeded + 1 created) |
| PT04 | `GET /api/clinic/patients/{id}` | 200, correct patient | ✅ PASS | `name="Test Patient"` |
| PT05 | `PUT /api/clinic/patients/{id}` | 200, fields updated | ✅ PASS | `name="Test Patient Updated"` |
| PT06 | `POST /api/clinic/patients/{id}/profiles` | 201, sub-profile added | ✅ PASS | `subProfiles` contains entry with `name="Child of Test"` |
| PT07 | `GET /api/clinic/patients/{id}` includes sub-profiles | subProfiles ≥ 1 | ✅ PASS | `subProfiles.Count ≥ 1` |
| PT08 | `POST /api/clinic/patients/{id}/reset-password` | 200, new password returned | ✅ PASS | `newPassword` non-null |
| PT09 | Staff can create patient | 201 | ✅ PASS | `success=true` |
| PT10 | `GET /api/clinic/patients?search=Mohamed` | 200, filtered results | ✅ PASS | `items.Count ≥ 1` |
| PT11 | `DELETE /api/clinic/patients/{id}` (owner) | 200, soft-deleted | ✅ PASS | `success=true` |
| PT12 | Deleted patient not in list | Filtered out | ✅ PASS | Patient not found in items list |

---

## 5. PATIENT LOGIN (4 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| PL01 | `POST /api/auth/patient/login` valid credentials | 200, token returned | ✅ PASS | `success=true`, `token` non-null |
| PL02 | Patient login returns profiles | profiles array ≥ 1 | ✅ PASS | `user.profiles.Count ≥ 1` |
| PL03 | Patient login wrong password | 401/error | ✅ PASS | `success=false` |
| PL04 | Patient login wrong tenant | 401/error | ✅ PASS | `success=false` |

---

## 6. TENANT-SCOPED AUTH (4 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| TS01 | Owner login wrong tenant | Error | ✅ PASS | `success=false` |
| TS02 | Staff login wrong tenant | Error | ✅ PASS | `success=false` |
| TS03 | SuperAdmin login without tenant | 200, success | ✅ PASS | `success=true` |
| TS04 | Clinic endpoint without tenant header | Error | ✅ PASS | `success=false` |

---

## 7. MIDDLEWARE ENFORCEMENT (3 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| MW01 | No auth header → 401 for clinic endpoint | `success=false` | ✅ PASS | Unauthenticated request blocked |
| MW02 | Invalid token → 401 | `success=false` | ✅ PASS | Invalid JWT rejected |
| MW03 | Invalid tenant slug → error | `success=false` | ✅ PASS | Non-existent tenant returns error |

---

## 8. CROSS-TENANT ISOLATION (3 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| CT01 | Owner sees only own tenant patients | 200, items scoped | ✅ PASS | `items.Count ≥ 1` — only demo-clinic patients returned |
| CT02 | Owner sees only own tenant doctors | 200, items scoped | ✅ PASS | `items.Count ≥ 2` — only demo-clinic doctors returned |
| CT03 | Owner sees only own tenant staff | 200, items scoped | ✅ PASS | `items.Count ≥ 2` — only demo-clinic staff returned |

---

## 9. PHASE 1 BUG FIXES (2 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| BF01 | SV03: Create subscription with nullable dates | 201, subscription created | ✅ PASS | `success=true` |
| BF02 | RF06: Invalid model returns ApiResponse format | Response has `success` or `errors` property | ✅ PASS | ApiResponse wrapper confirmed |

---

## 10. PHASE 1 REGRESSION (6 tests)

| # | Test | Expected | Status | Evidence |
|---|------|----------|--------|----------|
| RG01 | Tenant CRUD still works | 200, list tenants | ✅ PASS | `success=true`, `items.Count ≥ 1` |
| RG02 | Feature flags still work | 200, flags returned | ✅ PASS | `success=true` |
| RG03 | Subscription list still works | 200, list returned | ✅ PASS | `success=true` |
| RG04 | Health endpoint works | Response returned | ✅ PASS | Non-null response |
| RG05 | Refresh token still works | 200, new token | ✅ PASS | `success=true`, new `token` returned |
| RG06 | Me endpoint still works | 200, user info | ✅ PASS | `username="superadmin"` |

---

## SUMMARY

| Category | Tests | Pass | Fail |
|----------|-------|------|------|
| 1. Clinic Settings | 6 | 6 | 0 |
| 2. Staff CRUD | 8 | 8 | 0 |
| 3. Doctor CRUD | 10 | 10 | 0 |
| 4. Patient CRUD | 12 | 12 | 0 |
| 5. Patient Login | 4 | 4 | 0 |
| 6. Tenant-Scoped Auth | 4 | 4 | 0 |
| 7. Middleware Enforcement | 3 | 3 | 0 |
| 8. Cross-Tenant Isolation | 3 | 3 | 0 |
| 9. Phase 1 Bug Fixes | 2 | 2 | 0 |
| 10. Phase 1 Regression | 6 | 6 | 0 |
| **TOTAL** | **58** | **58** | **0** |

---

## RAW TEST OUTPUT

```
========== PHASE 2 TESTS ==========
  Tokens acquired: Owner, Super, Staff, Doctor
  PASS  CS01 GET settings (owner)
  PASS  CS02 GET settings (staff can read)
  PASS  CS03 PUT settings (owner updates)
  PASS  CS04 GET settings reflects update
  PASS  CS05 PUT settings (staff forbidden)
  PASS  CS06 GET settings without tenant returns error
  PASS  ST01 Create staff (owner)
  PASS  ST02 List staff
  PASS  ST03 Get staff by id
  PASS  ST04 Update staff
  PASS  ST05 Disable staff
  PASS  ST06 Enable staff
  PASS  ST07 Staff can list staff
  PASS  ST08 Staff cannot create staff
  PASS  DR01 Create doctor (owner)
  PASS  DR02 List doctors
  PASS  DR03 Get doctor by id
  PASS  DR04 Update doctor
  PASS  DR05 Update doctor services
  PASS  DR06 Update visit field config
  PASS  DR07 Disable doctor
  PASS  DR08 Enable doctor
  PASS  DR09 Staff can list doctors
  PASS  DR10 Staff cannot create doctor
  PASS  PT01 Create patient (owner)
  PASS  PT02 Create patient returns credentials
  PASS  PT03 List patients
  PASS  PT04 Get patient by id
  PASS  PT05 Update patient
  PASS  PT06 Add sub-profile
  PASS  PT07 Get patient includes sub-profiles
  PASS  PT08 Reset patient password
  PASS  PT09 Staff can create patient
  PASS  PT10 Search patients by name
  PASS  PT11 Delete patient (owner)
  PASS  PT12 Deleted patient not in list
  PASS  PL01 Patient login via /patient/login
  PASS  PL02 Patient login returns profiles
  PASS  PL03 Patient login wrong password
  PASS  PL04 Patient login wrong tenant
  PASS  TS01 Owner login wrong tenant fails
  PASS  TS02 Staff login wrong tenant fails
  PASS  TS03 SuperAdmin no tenant still works
  PASS  TS04 Clinic endpoint without tenant header
  PASS  MW01 No auth header returns 401 for clinic
  PASS  MW02 Invalid token returns 401
  PASS  MW03 Invalid tenant slug returns error
  PASS  CT01 Owner sees only own tenant patients
  PASS  CT02 Owner sees only own tenant doctors
  PASS  CT03 Owner sees only own tenant staff
  PASS  BF01 SV03: Create subscription with nullable dates
  PASS  BF02 RF06: Invalid model returns ApiResponse format
  PASS  RG01 Tenant CRUD still works
  PASS  RG02 Feature flags still work
  PASS  RG03 Subscription list still works
  PASS  RG04 Health endpoint works
  PASS  RG05 Refresh token still works
  PASS  RG06 Me endpoint still works
========================================
  TOTAL: 58 | PASS: 58 | FAIL: 0
========================================
```
