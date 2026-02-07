# Elite Clinic — Multi-Tenant Clinic Management API

> **.NET 9 Web API** | **EF Core 9** | **SQL Server** | **JWT Auth** | **Multi-Tenant**

---

## Current Status

| Phase | Name | Status | Tests |
|-------|------|--------|-------|
| Phase 0 | Identity & Auth | ✅ Complete | Included in Phase 1 |
| Phase 1 | Platform Admin (Tenants, Subscriptions, Feature Flags) | ✅ Complete | Included in Phase 2 |
| Phase 2 | Clinic Setup & Users (Settings, Staff, Doctors, Patients) | ✅ Complete | 58/58 PASS |
| Phase 3 | Queue & Clinical Workflow (Queue, Visits, Prescriptions, Labs, Invoices, Expenses, Finance) | ✅ Complete | 99/99 PASS |
| Phase 4 | Communication & Booking | 🔲 Planned | — |
| Phase 5 | Analytics, Audit & Final | 🔲 Planned | — |

**Combined Tests: 157/157 PASS, 0 FAIL**

---

## Quick Start

```powershell
# Build
dotnet build

# Run (migrations apply automatically, seed data is idempotent)
dotnet run --project src/EliteClinic.Api

# Server starts at http://localhost:5094

# Run Phase 3 Tests
powershell -ExecutionPolicy Bypass -File tests/Phase3_Tests.ps1

# Run Phase 2 Tests
powershell -ExecutionPolicy Bypass -File tests/Phase2_Tests.ps1
```

---

## API Endpoints (83 total)

| Module | Endpoints |
|--------|-----------|
| Health | 1 |
| Auth | 5 |
| Tenants | 6 |
| Subscriptions | 5 |
| Feature Flags | 5 |
| Clinic Settings | 2 |
| Staff | 6 |
| Doctors | 9 |
| Patients | 10 |
| Queue Sessions | 5 |
| Queue Tickets | 7 |
| Queue Board | 3 |
| Visits | 6 |
| Prescriptions | 4 |
| Lab Requests | 4 |
| Invoices & Payments | 6 |
| Expenses | 4 |
| Finance Reports | 5 |

---

## Documentation

| File | Description |
|------|-------------|
| `spec-kit/SWAGGER_DOCUMENTATION.md` | Complete API reference (v3.0) |
| `spec-kit/FRONTEND_CONTRACT.md` | Frontend integration contract (v3.0) |
| `spec-kit/PERMISSIONS_MATRIX.md` | Role/action access control (v3.0) |
| `spec-kit/PLAN.md` | Full project plan |
| `phases/v3/COMPLETION_V3.md` | Phase 3 runbook |
| `phases/v3/TESTS_V3.md` | Phase 3 test evidence (99/99) |
| `phases/v2/COMPLETION_V2.md` | Phase 2 runbook |
| `phases/v2/TESTS_V2.md` | Phase 2 test evidence (58/58) |

---

## Seeded Test Credentials (demo-clinic tenant)

| Role | Username | Password |
|------|----------|----------|
| SuperAdmin | `superadmin` | `Admin@123456` |
| ClinicOwner | `owner_demo` | `Owner@123456` |
| Staff | `staff_sara` | `Staff@123456` |
| Doctor | `dr_khaled` | `Doctor@123456` |
| Patient | `patient_demo-clinic_1` | `Patient@1234` |