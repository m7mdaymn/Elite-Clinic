# Elite Clinic — Multi-Tenant Clinic Management API

> **.NET 9 Web API** | **EF Core 9** | **SQL Server** | **JWT Auth** | **Multi-Tenant** | **109 Endpoints**

A production-ready RESTful API for managing multi-tenant dental/medical clinics. Supports full clinical workflow from patient registration through queue management, medical visits, prescriptions, invoicing, WhatsApp messaging, and online booking.

---

## Current Status

| Phase | Name | Status | Tests |
|-------|------|--------|-------|
| Phase 1 | Platform Admin (Tenants, Subscriptions, Feature Flags) | ✅ Complete | Included in Phase 2 |
| Phase 2 | Clinic Setup & Users (Settings, Staff, Doctors, Patients) | ✅ Complete | 58/58 PASS |
| Phase 3 | Queue & Clinical Workflow (Queue, Visits, Prescriptions, Labs, Invoices, Expenses, Finance) | ✅ Complete | 99/99 PASS |
| Phase 4 | Communication & Booking (Public SEO, Booking, Messages, Notes, Notifications) | ✅ Complete | 89/89 PASS |
| Phase 5 | Production Readiness & Final Quality | ✅ Complete | 105/105 PASS |

**Combined Tests: 351/351 PASS, 0 FAIL**

---

## Architecture

```
EliteClinic/
├── src/
│   ├── EliteClinic.Api/            # Controllers, middleware, Program.cs
│   ├── EliteClinic.Application/    # Services, DTOs, business logic
│   ├── EliteClinic.Domain/         # Entities, enums
│   └── EliteClinic.Infrastructure/ # DbContext, migrations, tenant context
├── spec-kit/                       # API specification & documentation
│   ├── SWAGGER_DOCUMENTATION.md    # Complete 109-endpoint API reference
│   ├── FRONTEND_CONTRACT.md        # Frontend integration contract
│   ├── PERMISSIONS_MATRIX.md       # Role/action access matrix (6 roles)
│   ├── PLAN.md                     # Full project plan
│   └── MESSAGE_SPEC.md            # WhatsApp & PWA message templates
├── tests/                          # PowerShell test suites
├── docs/                           # Deployment & operational guides
└── phases/                         # Phase completion records
```

### Multi-Tenancy Model

Every tenant-scoped request requires an `X-Tenant` header containing the clinic's slug. The `TenantMiddleware` resolves the tenant, validates status (Active/Suspended/Blocked), and injects `TenantContext` for the request. EF Core global query filters ensure complete data isolation — no tenant can access another tenant's data.

### Authentication

JWT Bearer tokens with HMAC-SHA256 signing. Tokens include `tenantId` claim for tenant-scoped users. SuperAdmin tokens have no tenant binding and can access all tenants. Token refresh is supported.

---

## Quick Start

```powershell
# Build
dotnet build

# Run (migrations apply automatically, seed data is idempotent)
dotnet run --project src/EliteClinic.Api

# Server starts at http://localhost:5094
# Swagger UI: http://localhost:5094/swagger
```

### Run Tests

```powershell
# All phases
powershell -ExecutionPolicy Bypass -File tests/Phase2_Tests.ps1
powershell -ExecutionPolicy Bypass -File tests/Phase3_Tests.ps1
powershell -ExecutionPolicy Bypass -File tests/Phase4_Tests.ps1
powershell -ExecutionPolicy Bypass -File tests/Phase5_Tests.ps1
```

---

## API Endpoints (109 total)

| Module | Endpoints | Controller |
|--------|-----------|------------|
| Health | 1 | HealthController |
| Auth | 4 | AuthController |
| Tenants | 8 | TenantsController |
| Subscriptions | 5 | SubscriptionsController |
| Feature Flags | 2 | FeatureFlagsController |
| Clinic Settings | 2 | ClinicSettingsController |
| Staff | 6 | StaffController |
| Doctors | 8 | DoctorsController |
| Patients | 7 | PatientsController |
| Queue Sessions | 5 | QueueSessionsController |
| Queue Tickets | 7 | QueueTicketsController |
| Queue Board | 3 | QueueBoardController |
| Visits | 6 | VisitsController |
| Prescriptions | 4 | PrescriptionsController |
| Lab Requests | 4 | LabRequestsController |
| Invoices & Payments | 6 | InvoicesController |
| Expenses | 4 | ExpensesController |
| Finance Reports | 5 | FinanceController |
| Public SEO | 4 | PublicController |
| Online Booking | 6 | BookingsController |
| WhatsApp Messages | 4 | MessagesController |
| Doctor Notes | 4 | DoctorNotesController |
| PWA Notifications | 4 | NotificationsController |

---

## Roles (6 Seeded Roles)

| Role | Scope | Description |
|------|-------|-------------|
| **SuperAdmin** | Platform | Manages tenants, subscriptions, feature flags. Cross-tenant access. |
| **ClinicOwner** | Tenant | Full clinic control — staff, doctors, settings, reports, all workflows. |
| **ClinicManager** | Tenant | Daily operations — patients, queue, payments, expenses. |
| **Receptionist** | Tenant | Front desk — bookings, messages, notifications, doctor notes. Cannot create doctors or change settings. |
| **Doctor** | Tenant | Clinical — own queue, visits, prescriptions, labs, notes. |
| **Patient** | Tenant | Self-service — view own data, book appointments, check queue status. |

---

## API Usage Examples

### Authentication

```bash
# Staff/Admin login
curl -X POST http://localhost:5094/api/auth/login \
  -H "Content-Type: application/json" \
  -H "X-Tenant: demo-clinic" \
  -d '{"username":"owner_demo","password":"Owner@123456"}'

# Patient login
curl -X POST http://localhost:5094/api/auth/patient/login \
  -H "Content-Type: application/json" \
  -H "X-Tenant: demo-clinic" \
  -d '{"username":"patient_demo-clinic_1","password":"Patient@1234"}'

# Get current user info
curl http://localhost:5094/api/auth/me \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic"
```

### Platform Administration (SuperAdmin)

```bash
# List tenants
curl http://localhost:5094/api/platform/tenants \
  -H "Authorization: Bearer {superadmin_token}"

# Create tenant
curl -X POST http://localhost:5094/api/platform/tenants \
  -H "Authorization: Bearer {superadmin_token}" \
  -H "Content-Type: application/json" \
  -d '{"name":"New Clinic","slug":"new-clinic","ownerEmail":"owner@clinic.com"}'

# Update feature flags
curl -X PUT http://localhost:5094/api/platform/feature-flags/{tenantId} \
  -H "Authorization: Bearer {superadmin_token}" \
  -H "Content-Type: application/json" \
  -d '{"onlineBooking":true,"whatsappAutomation":true,"pwaNotifications":true}'
```

### Clinic Management

```bash
# Get clinic settings
curl http://localhost:5094/api/clinic/settings \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic"

# Create staff (ClinicManager or Receptionist)
curl -X POST http://localhost:5094/api/clinic/staff \
  -H "Authorization: Bearer {owner_token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"username":"new_staff","name":"Staff Name","password":"Staff@123456","role":"Receptionist"}'

# Create doctor
curl -X POST http://localhost:5094/api/clinic/doctors \
  -H "Authorization: Bearer {owner_token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"username":"dr_new","name":"Dr. Name","password":"Doctor@123456","specialty":"Dentist"}'

# Create patient
curl -X POST http://localhost:5094/api/clinic/patients \
  -H "Authorization: Bearer {owner_token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"name":"Patient Name","phone":"0500000001","dateOfBirth":"1990-01-01","gender":"Male"}'
```

### Queue Workflow

```bash
# Open queue session for a doctor
curl -X POST http://localhost:5094/api/clinic/queue-sessions/open \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"doctorId":"{doctorId}"}'

# Issue queue ticket (walk-in)
curl -X POST http://localhost:5094/api/clinic/queue-tickets/issue \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"patientId":"{patientId}","queueSessionId":"{sessionId}"}'

# View reception board
curl http://localhost:5094/api/clinic/queue-board \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic"
```

### Medical Records

```bash
# Create visit
curl -X POST http://localhost:5094/api/clinic/visits \
  -H "Authorization: Bearer {doctor_token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"patientId":"{patientId}","complaint":"Toothache","diagnosis":"Cavity","notes":"Needs filling"}'

# Add prescription
curl -X POST http://localhost:5094/api/clinic/prescriptions \
  -H "Authorization: Bearer {doctor_token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"visitId":"{visitId}","medicationName":"Amoxicillin","dosage":"500mg","frequency":"3x daily","duration":"7 days"}'
```

### Online Booking

```bash
# Create booking (staff on behalf of patient)
curl -X POST http://localhost:5094/api/clinic/bookings \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"patientId":"{patientId}","doctorId":"{doctorId}","doctorServiceId":"{serviceId}","bookingDate":"2026-03-01","bookingTime":"09:00"}'

# Cancel booking
curl -X POST http://localhost:5094/api/clinic/bookings/{id}/cancel \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{"cancellationReason":"Schedule conflict"}'
```

### Public SEO Endpoints (No Auth Required)

```bash
# Public clinic profile
curl http://localhost:5094/api/public/demo-clinic/clinic

# Public doctors list
curl http://localhost:5094/api/public/demo-clinic/doctors

# Public services
curl http://localhost:5094/api/public/demo-clinic/services

# Public working hours
curl http://localhost:5094/api/public/demo-clinic/working-hours
```

### Finance

```bash
# Daily report
curl "http://localhost:5094/api/clinic/finance/daily?date=2026-02-07" \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic"

# Profit report
curl "http://localhost:5094/api/clinic/finance/profit?from=2026-01-01&to=2026-12-31" \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic"
```

---

## Response Envelope

All endpoints return a consistent response format:

```json
{
  "success": true,
  "message": "Operation successful",
  "data": { },
  "errors": [],
  "meta": {
    "timestamp": "2026-02-07T10:00:00Z",
    "requestId": "guid"
  }
}
```

Paginated responses include:
```json
{
  "data": {
    "items": [],
    "totalCount": 50,
    "pageNumber": 1,
    "pageSize": 10
  }
}
```

---

## Seeded Test Data (demo-clinic tenant)

| Role | Username | Password |
|------|----------|----------|
| SuperAdmin | `superadmin` | `Admin@123456` |
| ClinicOwner | `owner_demo` | `Owner@123456` |
| ClinicManager | `staff_sara` | `Staff@123456` |
| ClinicManager | `staff_ali` | `Staff@123456` |
| Doctor | `dr_khaled` | `Doctor@123456` |
| Doctor | `dr_mona` | `Doctor@123456` |
| Patient | `patient_demo-clinic_1` through `_6` | `Patient@1234` |

**Seeded Tenants:** demo-clinic, care-center, smile-dental, health-plus

---

## Documentation

| File | Description |
|------|-------------|
| `spec-kit/SWAGGER_DOCUMENTATION.md` | Complete 109-endpoint API reference (v5.0) |
| `spec-kit/FRONTEND_CONTRACT.md` | Frontend integration contract (v5.0) |
| `spec-kit/PERMISSIONS_MATRIX.md` | Role/action access matrix with 6 roles (v5.0) |
| `spec-kit/PLAN.md` | Full project plan — all 5 phases complete |
| `spec-kit/MESSAGE_SPEC.md` | WhatsApp & PWA message templates (v5.0) |
| `docs/ROLES_GUIDE.md` | Comprehensive role & permission guide |
| `docs/DEPLOYMENT.md` | Production deployment guide |

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | Embedded in appsettings |
| `JwtSettings__SecretKey` | JWT signing key (min 32 chars) | Embedded fallback |
| `JwtSettings__Issuer` | JWT token issuer | EliteClinic |
| `JwtSettings__Audience` | JWT token audience | EliteClinicApp |
| `JwtSettings__AccessTokenExpirationMinutes` | Token lifetime | 1440 |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | Development |

---

## Technology Stack

- **.NET 9** — Web API framework
- **EF Core 9** — ORM with code-first migrations
- **SQL Server** — Primary database
- **ASP.NET Identity** — User management & role-based auth
- **JWT (HMAC-SHA256)** — Stateless authentication
- **Serilog** — Structured logging
- **Swagger/OpenAPI** — API documentation (always enabled)
