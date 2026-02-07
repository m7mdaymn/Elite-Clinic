# UPDATE_V2.md — Phase 2: Clinic Setup & Users

> **Phase:** 2  
> **Status:** 🔲 PLANNING — Awaiting Approval  
> **Date Created:** 2026-02-07  
> **Source of Truth:** spec-kit/ (PLAN.md §2, §3, §4, §9, §13, PERMISSIONS_MATRIX.md, FRONTEND_CONTRACT.md)

---

## 1. What Is IN SCOPE for Phase 2

Per **PLAN.md §18 Phase 2** — *"Clinic settings, staff/employee management, doctor management, patient registration, tenant-scoped authentication"*

### 1.1 Clinic Settings (ClinicOwner Only)

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Get clinic settings | PLAN.md §9, PERMISSIONS_MATRIX §Clinic Settings | `GET /api/clinic/settings` — Returns tenant's clinic configuration |
| Update clinic settings | PLAN.md §9, PERMISSIONS_MATRIX §Clinic Settings | `PUT /api/clinic/settings` — Name, phone, WhatsApp numbers, address, working hours |
| Configure working hours | PLAN.md §6.1, §9.2 | Part of clinic settings — array of day/start/end entries |

**New Entity: `ClinicSettings`**
```
ClinicSettings (extends TenantBaseEntity):
  - TenantId: Guid (FK, unique — one-to-one)
  - ClinicName: string (display name, can differ from Tenant.Name)
  - Phone: string?
  - WhatsAppSenderNumber: string? (for automated messages)
  - SupportWhatsAppNumber: string? (for manual support chat)
  - SupportPhoneNumber: string? (for voice calls)
  - Address: string?
  - City: string?
  - LogoUrl: string?
  - BookingEnabled: bool (default false — mirrors feature flag)
  - CancellationWindowHours: int (default 2 — how far before appointment patient can cancel)
  - WorkingHours: ICollection<WorkingHour>
```

**New Entity: `WorkingHour`**
```
WorkingHour (extends TenantBaseEntity):
  - ClinicSettingsId: Guid (FK)
  - DayOfWeek: DayOfWeek (0=Sunday..6=Saturday)
  - StartTime: TimeSpan
  - EndTime: TimeSpan
  - IsActive: bool (default true)
```

### 1.2 Staff/Employee Management (ClinicOwner Only)

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Create staff | PERMISSIONS_MATRIX §Staff | `POST /api/clinic/staff` — Creates ApplicationUser with role ClinicManager, assigns to tenant |
| List staff | PERMISSIONS_MATRIX §Staff | `GET /api/clinic/staff` — Paginated list of tenant staff |
| Get staff details | PERMISSIONS_MATRIX §Staff | `GET /api/clinic/staff/{id}` — Full profile |
| Update staff | PERMISSIONS_MATRIX §Staff | `PUT /api/clinic/staff/{id}` — Edit profile, role |
| Enable/disable staff | PERMISSIONS_MATRIX §Staff | `POST /api/clinic/staff/{id}/enable`, `POST /api/clinic/staff/{id}/disable` |

**New Entity: `Employee`**
```
Employee (extends TenantBaseEntity):
  - UserId: Guid (FK → ApplicationUser, unique)
  - Name: string
  - Phone: string?
  - Role: string (ClinicManager, Reception — derived from ApplicationUser role)
  - Salary: decimal?
  - HireDate: DateTime?
  - Notes: string?
  - IsEnabled: bool (default true)
  - User: ApplicationUser (navigation)
```

**ApplicationUser changes:**
- Add navigation property: `Tenant Tenant` on `ApplicationUser`
- Configure FK relationship in `OnModelCreating`: `ApplicationUser.TenantId → Tenant.Id`

### 1.3 Doctor Management (ClinicOwner Only)

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Create doctor | PERMISSIONS_MATRIX §Doctor | `POST /api/clinic/doctors` — Creates ApplicationUser with Doctor role + Doctor entity |
| List doctors | PERMISSIONS_MATRIX §Doctor | `GET /api/clinic/doctors` — Paginated list |
| Get doctor details | PERMISSIONS_MATRIX §Doctor | `GET /api/clinic/doctors/{id}` — Full profile with services |
| Update doctor | PERMISSIONS_MATRIX §Doctor | `PUT /api/clinic/doctors/{id}` — Edit profile |
| Enable/disable doctor | PERMISSIONS_MATRIX §Doctor | `POST /api/clinic/doctors/{id}/enable`, `POST /api/clinic/doctors/{id}/disable` |
| Configure services | PLAN.md §3.3 | `PUT /api/clinic/doctors/{id}/services` — Services & pricing |
| Configure visit fields | PLAN.md §7.1 | `PUT /api/clinic/doctors/{id}/visit-fields` — Toggle vitals fields per doctor |
| Configure urgent mode | PLAN.md §5.3 | Part of doctor profile (urgentCaseMode field) |

**New Entity: `Doctor`**
```
Doctor (extends TenantBaseEntity):
  - UserId: Guid (FK → ApplicationUser, unique)
  - Name: string
  - Specialty: string?
  - Phone: string?
  - Bio: string?
  - PhotoUrl: string?
  - IsEnabled: bool (default true)
  - UrgentCaseMode: UrgentCaseMode (enum: UrgentNext=0, UrgentBucket=1, UrgentFront=2)
  - AvgVisitDurationMinutes: int (default 15 — used for wait time estimation)
  - User: ApplicationUser (navigation)
  - Services: ICollection<DoctorService> (navigation)
  - VisitFieldConfig: DoctorVisitFieldConfig? (navigation)
```

**New Entity: `DoctorService`**
```
DoctorService (extends TenantBaseEntity):
  - DoctorId: Guid (FK → Doctor)
  - ServiceName: string (e.g., "Consultation", "Root Canal")
  - Price: decimal
  - DurationMinutes: int?
  - IsActive: bool (default true)
```

**New Entity: `DoctorVisitFieldConfig`**
```
DoctorVisitFieldConfig (extends TenantBaseEntity):
  - DoctorId: Guid (FK → Doctor, unique — one-to-one)
  - BloodPressure: bool (default false)
  - HeartRate: bool (default false)
  - Temperature: bool (default true)
  - Weight: bool (default true)
  - Height: bool (default false)
  - BMI: bool (default false)
  - BloodSugar: bool (default false)
  - OxygenSaturation: bool (default false)
  - RespiratoryRate: bool (default false)
```

**New Enum: `UrgentCaseMode`**
```
UrgentCaseMode:
  - UrgentNext = 0      (goes right after current patient)
  - UrgentBucket = 1    (accumulates; doctor pulls when ready)
  - UrgentFront = 2     (goes to front of queue)
```

### 1.4 Patient Registration (ClinicOwner + ClinicManager)

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Create patient | PLAN.md §4.1, PERMISSIONS_MATRIX §Patient | `POST /api/clinic/patients` — Walk-in registration |
| List patients | PERMISSIONS_MATRIX §Patient | `GET /api/clinic/patients` — Paginated, searchable |
| Get patient details | PERMISSIONS_MATRIX §Patient | `GET /api/clinic/patients/{id}` — Full profile with sub-profiles |
| Update patient | PERMISSIONS_MATRIX §Patient | `PUT /api/clinic/patients/{id}` — Edit profile |
| Add sub-profile | PLAN.md §4.1 | `POST /api/clinic/patients/{id}/profiles` — Child/dependent profile |
| Reset password | PLAN.md §4.3 | `POST /api/clinic/patients/{id}/reset-password` — Staff-initiated reset |
| Soft-delete patient | PERMISSIONS_MATRIX §Patient | `DELETE /api/clinic/patients/{id}` — ClinicOwner only |

**New Entity: `Patient`**
```
Patient (extends TenantBaseEntity):
  - UserId: Guid (FK → ApplicationUser, unique)
  - Name: string
  - Phone: string
  - DateOfBirth: DateTime?
  - Gender: Gender (enum: Male=0, Female=1)
  - Address: string?
  - Notes: string?
  - IsDefault: bool (default true — primary profile under this account)
  - ParentPatientId: Guid? (FK → Patient, self-referencing for sub-profiles)
  - User: ApplicationUser (navigation)
  - SubProfiles: ICollection<Patient> (navigation — children under same phone/account)
```

**New Enum: `Gender`**
```
Gender:
  - Male = 0
  - Female = 1
```

**Auto-generated credentials on patient creation:**
- Username: auto-generated (e.g., `patient_{sequentialNumber}`)
- Password: auto-generated (e.g., random 8-char with uppercase, digit, special)
- Returned in response body (for reception to see)
- Credentials WhatsApp message NOT sent in Phase 2 (WhatsApp is Phase 4)
- Staff can call `POST .../reset-password` which generates a new password

### 1.5 Tenant-Scoped Authentication

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Staff/Doctor login | PLAN.md §3.2-3.4, SWAGGER_DOCUMENTATION §Auth | `POST /api/auth/login` with `X-Tenant` header — existing endpoint, enhanced |
| Patient login | PLAN.md §4.3, SWAGGER_DOCUMENTATION §Auth | `POST /api/auth/patient/login` — new endpoint |
| /me for tenant users | SWAGGER_DOCUMENTATION §Auth | `GET /api/auth/me` — existing endpoint, enhanced with tenant context |

**Changes to existing AuthService:**
- `POST /api/auth/login` with `X-Tenant` header: resolve tenant → validate user belongs to tenant → return JWT with `tenantId` claim
- New `POST /api/auth/patient/login`: Patient-specific login with long-lived token (365 days) and `profiles[]` in response
- `GET /api/auth/me`: Return `tenantSlug` and `permissions` for tenant-scoped users

### 1.6 Phase 1 Bug Fixes

| Issue | Fix |
|-------|-----|
| SV03: DateTime value type defaults to 0001-01-01 | Make `StartDate` and `EndDate` nullable (`DateTime?`) in `CreateSubscriptionRequest`, add custom validation |
| RF06: ProblemDetails format for model validation | Add `InvalidModelStateResponseFactory` in `Program.cs` to wrap all model validation errors in `ApiResponse` format |

### 1.7 Infrastructure Changes

| Change | Details |
|--------|---------|
| `TenantBaseEntity` usage | All new tenant-scoped entities extend `TenantBaseEntity` (not `BaseEntity`) |
| Global query filter | Add `builder.HasQueryFilter(e => e.TenantId == currentTenantId)` for all `TenantBaseEntity` types |
| ApplicationUser FK | Configure `ApplicationUser.TenantId → Tenant.Id` FK in `OnModelCreating` |
| Auto-create ClinicSettings | When tenant is created, auto-create `ClinicSettings` (similar to feature flags) |

---

## 2. What Is OUT OF SCOPE for Phase 2

| Feature | Phase | Reason |
|---------|-------|--------|
| Queue system (sessions, tickets) | Phase 3 | Depends on doctors being created first |
| Visits, prescriptions, labs | Phase 3 | Depends on queue + patients |
| Payments, expenses, finance | Phase 3 | Depends on visits |
| WhatsApp messaging | Phase 4 | Independent communication module |
| Online booking | Phase 4 | Depends on queue system |
| Public SEO endpoints | Phase 4 | Independent public module |
| Reporting & analytics | Phase 5 | Depends on all operational data |
| Platform audit viewer | Phase 5 | Enhancement over existing logging |
| Login history / tracking | Phase 5 | Enhancement |
| SignalR real-time | Phase 5 | Performance optimization |
| Credentials WhatsApp message on patient creation | Phase 4 | WhatsApp integration not in Phase 2 |

---

## 3. Assumptions

| # | Assumption | Based On |
|---|-----------|----------|
| A1 | ClinicOwner is the only role that can manage staff, doctors, and clinic settings | PERMISSIONS_MATRIX §Staff, §Doctor, §Clinic Settings |
| A2 | ClinicManager can create/edit patients but NOT staff or doctors | PERMISSIONS_MATRIX §Patient, §Staff |
| A3 | Patient credentials are auto-generated, not user-chosen | PLAN.md §4.1 — "System auto-generates Username + Password" |
| A4 | Sub-profiles (children) share the same `ApplicationUser` account as the parent | PLAN.md §4.1 — "multiple profiles under same phone" |
| A5 | Doctor is a separate entity from Employee — doctors have services, visit config, urgent mode; staff do not | PLAN.md §3.3 vs §3.4 — distinct responsibilities |
| A6 | ClinicSettings is auto-created when tenant is created, similar to FeatureFlags | Inferred from Phase 1 pattern |
| A7 | Staff login uses same `POST /api/auth/login` endpoint with `X-Tenant` header | SWAGGER_DOCUMENTATION §Auth |
| A8 | Patient login is a separate endpoint `POST /api/auth/patient/login` with long-lived token | PLAN.md §4.3, SWAGGER_DOCUMENTATION §Auth |

---

## 4. Risks

| # | Risk | Impact | Mitigation |
|---|------|--------|-----------|
| R1 | Global query filter for TenantId may conflict with platform routes | Platform endpoints may inadvertently filter by tenant | Use `IgnoreQueryFilters()` on platform service queries. Test both paths. |
| R2 | ApplicationUser is shared between platform and tenant users | Schema changes must not break SuperAdmin auth | Add FK with no cascade. Null TenantId for SuperAdmin preserved. |
| R3 | Patient sub-profile self-referencing FK may complicate queries | Parent/child relationships need careful handling | Limit to 1 level of nesting (no grandchild profiles) |
| R4 | Credential generation must be unique per tenant | Username collision between tenants | Username format: `patient_{tenantSlug}_{sequential}` or GUID-based |
| R5 | Large number of new entities may produce complex migration | Migration conflicts with existing data | Generate single migration, test against existing seed data |

---

## 5. Dependencies on Phase 1

| Dependency | Status | Notes |
|------------|--------|-------|
| Tenant entity | ✅ Built | Name, Slug, Status, Phone, Address, LogoUrl |
| TenantStatus enum | ✅ Built | Active, Suspended, Blocked, Inactive |
| TenantMiddleware | ✅ Built | Needs enhancement: tenant-scoped routes now exist |
| ApplicationUser with TenantId | ✅ Built | Needs FK config + navigation |
| Roles seeded (5 roles) | ✅ Built | SuperAdmin, ClinicOwner, ClinicManager, Doctor, Patient |
| JWT authentication | ✅ Built | Needs `tenantId` claim for tenant users |
| ApiResponse<T> / PagedResult<T> | ✅ Built | Ready to use |
| BaseEntity / TenantBaseEntity | ✅ Built | TenantBaseEntity now used by all new entities |
| AuditLog auto-capture | ✅ Built | All new entities inherit from TenantBaseEntity → BaseEntity |

---

## 6. Spec Gaps Discovered

### SPEC GAP #6 — ClinicSettings vs Tenant Entity Overlap

**Issue:** `Tenant` already has `Name`, `ContactPhone`, `Address`, `LogoUrl`. `ClinicSettings` would overlap these fields.  
**Proposed resolution:** `ClinicSettings` holds clinic-specific operational config (WhatsApp numbers, working hours, booking rules). Display-facing fields stay on `Tenant`. If the clinic wants a different display name, `ClinicSettings.ClinicName` can override `Tenant.Name` on public-facing routes.  
**Action required:** Approve or modify.

### SPEC GAP #7 — Employee vs ApplicationUser Relationship

**Issue:** Should a "staff member" be just an `ApplicationUser` with role `ClinicManager`, or a separate `Employee` entity linked to a user?  
**Proposed resolution:** Separate `Employee` entity — stores salary, hire date, notes, schedule metadata. `ApplicationUser` handles auth only. One-to-one link via `UserId`.  
**Action required:** Approve or simplify to ApplicationUser-only.

### SPEC GAP #8 — Patient Username Format

**Issue:** PLAN.md §4.1 says "auto-generates Username + Password" but doesn't define the format.  
**Proposed resolution:** Username format: `patient_{sequence}` (e.g., `patient_20001`). Unique per tenant (patient table scoped by TenantId). Password: random 8-char (`Admin@{4-digit-random}`).  
**Action required:** Approve or define format.

### SPEC GAP #9 — Sub-Profile Auth Model

**Issue:** PLAN.md says "multiple profiles under same phone." Do sub-profiles (children) share the parent's `ApplicationUser` login, or get their own?  
**Proposed resolution:** Sub-profiles share the parent's login. Patient login response includes `profiles[]` array. Frontend lets the user switch active profile. No separate user account for children.  
**Action required:** Approve.

---

## 7. Deliverables Summary

| # | Deliverable | Type |
|---|------------|------|
| D1 | `ClinicSettings` entity + `WorkingHour` entity | New entities |
| D2 | `Employee` entity | New entity |
| D3 | `Doctor` entity + `DoctorService` + `DoctorVisitFieldConfig` | New entities |
| D4 | `Patient` entity (with self-referencing sub-profiles) | New entity |
| D5 | `Gender` enum + `UrgentCaseMode` enum | New enums |
| D6 | `ApplicationUser` FK to `Tenant` configured | Entity enhancement |
| D7 | ClinicSettingsController (GET/PUT) | New controller |
| D8 | StaffController (CRUD + enable/disable) | New controller |
| D9 | DoctorsController (CRUD + services + visit fields + enable/disable) | New controller |
| D10 | PatientsController (CRUD + sub-profiles + reset-password) | New controller |
| D11 | `POST /api/auth/patient/login` endpoint | New endpoint |
| D12 | Enhanced `POST /api/auth/login` with X-Tenant for tenant users | Enhancement |
| D13 | Enhanced `GET /api/auth/me` with tenant context | Enhancement |
| D14 | Global query filter for TenantId | Infrastructure |
| D15 | Auto-create ClinicSettings on tenant creation | Enhancement |
| D16 | EF Migration for all new entities | Migration |
| D17 | Seed data: ClinicOwner, 2 staff, 2 doctors (with services), 5+ patients | Seed |
| D18 | Phase 1 bug fixes (SV03, RF06) | Bug fix |
| D19 | Tests (real HTTP, runnable) | Tests |
| D20 | SWAGGER_DOCUMENTATION.md + FRONTEND_CONTRACT.md updated | Docs |

---

## 8. Endpoint Summary

### Clinic Settings (2 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| GET | `/api/clinic/settings` | Bearer + X-Tenant | ClinicOwner, ClinicManager (read-only) |
| PUT | `/api/clinic/settings` | Bearer + X-Tenant | ClinicOwner |

### Staff Management (6 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/staff` | Bearer + X-Tenant | ClinicOwner |
| GET | `/api/clinic/staff` | Bearer + X-Tenant | ClinicOwner, ClinicManager (read-only) |
| GET | `/api/clinic/staff/{id}` | Bearer + X-Tenant | ClinicOwner |
| PUT | `/api/clinic/staff/{id}` | Bearer + X-Tenant | ClinicOwner |
| POST | `/api/clinic/staff/{id}/enable` | Bearer + X-Tenant | ClinicOwner |
| POST | `/api/clinic/staff/{id}/disable` | Bearer + X-Tenant | ClinicOwner |

### Doctor Management (8 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/doctors` | Bearer + X-Tenant | ClinicOwner |
| GET | `/api/clinic/doctors` | Bearer + X-Tenant | ClinicOwner, ClinicManager (read-only) |
| GET | `/api/clinic/doctors/{id}` | Bearer + X-Tenant | ClinicOwner |
| PUT | `/api/clinic/doctors/{id}` | Bearer + X-Tenant | ClinicOwner |
| POST | `/api/clinic/doctors/{id}/enable` | Bearer + X-Tenant | ClinicOwner |
| POST | `/api/clinic/doctors/{id}/disable` | Bearer + X-Tenant | ClinicOwner |
| PUT | `/api/clinic/doctors/{id}/services` | Bearer + X-Tenant | ClinicOwner |
| PUT | `/api/clinic/doctors/{id}/visit-fields` | Bearer + X-Tenant | ClinicOwner |

### Patient Management (7 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/patients` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| GET | `/api/clinic/patients` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| GET | `/api/clinic/patients/{id}` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| PUT | `/api/clinic/patients/{id}` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| POST | `/api/clinic/patients/{id}/profiles` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| POST | `/api/clinic/patients/{id}/reset-password` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| DELETE | `/api/clinic/patients/{id}` | Bearer + X-Tenant | ClinicOwner |

### Auth Enhancements (1 new + 2 enhanced)
| Method | Route | Change |
|--------|-------|--------|
| POST | `/api/auth/patient/login` | **NEW** — Patient login with long-lived token + profiles[] |
| POST | `/api/auth/login` | **ENHANCED** — Support X-Tenant header for tenant user login |
| GET | `/api/auth/me` | **ENHANCED** — Return tenantSlug, permissions for tenant users |

**Total new endpoints: 23 + 2 enhanced = 25 endpoint changes**

---

## 9. Test Coverage Expectations

### Category Breakdown

| Category | Test Count (est.) | Notes |
|----------|------------------|-------|
| Clinic Settings CRUD | 8-10 | GET/PUT, validation, ClinicManager read-only |
| Staff CRUD + Enable/Disable | 12-15 | Create, list, update, enable, disable, validation, auth |
| Doctor CRUD + Config | 15-20 | Create, list, update, enable, disable, services, visit fields, validation |
| Patient CRUD + Profiles | 15-20 | Create, list, update, sub-profiles, reset-password, credentials, validation |
| Patient Login | 5-8 | Login, profiles in response, long-lived token, invalid credentials |
| Tenant-Scoped Auth | 8-10 | Staff login with X-Tenant, wrong tenant, disabled user, /me enrichment |
| Middleware Enforcement | 8-10 | Tenant-scoped routes now testable (TM04-TM09 from Phase 1 deferred) |
| Cross-Tenant Isolation | 5-8 | TX02-TX03 from Phase 1 deferred, new cross-tenant tests |
| Phase 1 Bug Fixes | 3-4 | SV03 DateTime fix, RF06 ApiResponse format fix |
| Phase 1 Regression | 10 | All Phase 1 endpoints still work |
| **Total** | **~90-115** | |

### Exit Criteria (What "Phase 2 Complete" Means)

- [ ] All 25 endpoint changes implemented and reachable
- [ ] 0 build errors
- [ ] All runnable tests pass (excluding justified deferrals)
- [ ] ClinicOwner can: log in, configure clinic, create staff, create doctors
- [ ] ClinicManager can: log in, create patients, view staff/doctors (read-only)
- [ ] Doctor can: log in, view own profile
- [ ] Patient can: log in via patient/login, see profiles in response
- [ ] TenantMiddleware enforces isolation on all `/api/clinic/*` routes
- [ ] Cross-tenant access prevention works (user from Tenant A cannot access Tenant B)
- [ ] Phase 1 bug fixes verified (SV03, RF06)
- [ ] Seed data: 1 ClinicOwner, 2 staff, 2 doctors (with services), 5+ patients for demo-clinic
- [ ] SWAGGER_DOCUMENTATION.md updated with Phase 2 endpoints
- [ ] FRONTEND_CONTRACT.md updated with Phase 2 contracts
- [ ] TESTS_V2.md with real HTTP evidence filed
- [ ] COMPLETION_V2.md runbook created

---

*This document is the Phase 2 scope definition. No implementation until explicitly approved.*
