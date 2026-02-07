# COMPLETION_V3.md — Phase 3 Runbook

> **Phase:** 3 — Queue & Clinical Workflow  
> **Date:** 2026-02-08  
> **Purpose:** How to build, run, seed, test, and verify Phase 3

---

## 1. Prerequisites

- .NET 9 SDK installed
- Git repository cloned with Phase 3 code
- SQL Server (LocalDB or remote instance)
- PowerShell 5.1+ (for test scripts)

---

## 2. Build

```powershell
cd "c:\DATA\Elite Clinic"
dotnet build
```

**Expected output:**
```
0 Error(s)
```

Note: 26 CS8618 nullable warnings from Phase 0 are pre-existing and non-critical.

---

## 3. Database Migrations

Migrations apply automatically on startup. No manual step required.

**All migrations applied (5 total):**
- `InitialCreate` (Phase 0 — Identity, Tenant, FeatureFlag, Subscription)
- `Phase1_TenantSubscriptionFlags` (Phase 1)
- `Phase2_ClinicSetupUsers` (Phase 2 — ClinicSettings, Doctor, Staff, Patient, etc.)
- `Phase2_PatientUserIdNotUnique` (Phase 2 — Patient.UserId index fix)
- `Phase3_QueueVisitFinance` (Phase 3 — QueueSession, QueueTicket, Visit, Prescription, LabRequest, Invoice, Payment, Expense)

---

## 4. Seed Data

Seed data is auto-applied via `Program.cs` on startup (idempotent).

### Phase 3 Seed Data:

| Entity | Count | Details |
|--------|-------|---------|
| Queue Sessions | 2 | Dr. Khaled (active, 4 tickets), Dr. Mona (active, 1 ticket) |
| Queue Tickets | 5 | Completed, InVisit, Waiting, Waiting+Urgent, Waiting |
| Visits | 3 | 1 completed (Dr. Khaled), 1 open (Dr. Khaled), 1 manual completed (Dr. Mona) |
| Prescriptions | 2 | Amoxicillin, Ibuprofen (on completed visit) |
| Lab Requests | 2 | CBC completed (Dr. Khaled), X-Ray pending (Dr. Mona) |
| Invoices | 2 | 1 fully paid ($500), 1 partially paid ($300, $100 paid) |
| Payments | 2 | Cash $500, Credit Card $100 |
| Expenses | 3 | Supplies $1500, Utilities $800, Rent $5000 |

### Seeded Credentials:
| Role | Username | Password | Tenant |
|------|----------|----------|--------|
| SuperAdmin | `superadmin` | `Admin@123456` | — |
| ClinicOwner | `owner_demo` | `Owner@123456` | `demo-clinic` |
| Staff | `staff_sara` | `Staff@123456` | `demo-clinic` |
| Staff | `staff_ali` | `Staff@123456` | `demo-clinic` |
| Doctor | `dr_khaled` | `Doctor@123456` | `demo-clinic` |
| Doctor | `dr_mona` | `Doctor@123456` | `demo-clinic` |
| Patient | `patient_demo-clinic_1` through `_6` | `Patient@1234` | `demo-clinic` |

---

## 5. Run

```powershell
cd "c:\DATA\Elite Clinic"
dotnet run --project src/EliteClinic.Api
```

**Expected:** Server starts on `http://localhost:5094`

---

## 6. Test

### Phase 3 Tests (99 tests)
```powershell
cd "c:\DATA\Elite Clinic"
powershell -ExecutionPolicy Bypass -File tests/Phase3_Tests.ps1
```

**Expected:**
```
TOTAL: 99 | PASS: 99 | FAIL: 0
```

### Phase 2 Regression Tests (58 tests)
```powershell
cd "c:\DATA\Elite Clinic"
powershell -ExecutionPolicy Bypass -File tests/Phase2_Tests.ps1
```

**Expected:**
```
TOTAL: 58 | PASS: 58 | FAIL: 0
```

### Combined: 157 tests, 0 failures

---

## 7. Phase 3 File Inventory

### Domain Layer — Enums (4 files)
| File | Values |
|------|--------|
| `src/EliteClinic.Domain/Enums/TicketStatus.cs` | Waiting, Called, InVisit, Completed, Skipped, NoShow, Cancelled |
| `src/EliteClinic.Domain/Enums/VisitStatus.cs` | Open, Completed |
| `src/EliteClinic.Domain/Enums/InvoiceStatus.cs` | Unpaid, PartiallyPaid, Paid |
| `src/EliteClinic.Domain/Enums/LabRequestType.cs` | Lab, Imaging |

### Domain Layer — Entities (8 files)
| File | Key Fields |
|------|------------|
| `QueueSession.cs` | DoctorId, StartedAt, ClosedAt, IsActive, Tickets[] |
| `QueueTicket.cs` | SessionId, PatientId, TicketNumber, Status, IsUrgent, timestamps |
| `Visit.cs` | QueueTicketId?, DoctorId, PatientId, Status, vitals, Prescriptions[], LabRequests[], Invoice? |
| `Prescription.cs` | VisitId, MedicationName, Dosage, Frequency, Duration, Instructions |
| `LabRequest.cs` | VisitId, TestName, Type, IsUrgent, ResultText, ResultReceivedAt |
| `Invoice.cs` | VisitId, PatientId, DoctorId, Amount, PaidAmount, RemainingAmount, Status, Payments[] |
| `Payment.cs` | InvoiceId, Amount, PaymentMethod, ReferenceNumber, PaidAt |
| `Expense.cs` | Category, Amount, Notes, ExpenseDate, RecordedByUserId |

### Application Layer — DTOs (7 files)
| File | DTOs |
|------|------|
| `QueueDtos.cs` | QueueSessionDto, CreateQueueSessionRequest, QueueTicketDto, CreateQueueTicketRequest, QueueBoardDto, QueueBoardSessionDto |
| `VisitDtos.cs` | VisitDto, CreateVisitRequest, UpdateVisitRequest, CompleteVisitRequest, PatientSummaryDto, VisitSummaryDto |
| `PrescriptionDtos.cs` | PrescriptionDto, CreatePrescriptionRequest, UpdatePrescriptionRequest |
| `LabRequestDtos.cs` | LabRequestDto, CreateLabRequestRequest, UpdateLabRequestRequest, AddLabResultRequest |
| `InvoiceDtos.cs` | InvoiceDto, CreateInvoiceRequest, UpdateInvoiceRequest, PaymentDto, CreatePaymentRequest |
| `ExpenseDtos.cs` | ExpenseDto, CreateExpenseRequest, UpdateExpenseRequest |
| `FinanceDtos.cs` | DailyRevenueDto, DoctorRevenueDto, MonthlyRevenueDto, YearlyRevenueDto, ProfitReportDto |

### Application Layer — Services (14 files)
| Interface | Implementation |
|-----------|----------------|
| `IQueueService.cs` | `QueueService.cs` |
| `IVisitService.cs` | `VisitService.cs` |
| `IPrescriptionService.cs` | `PrescriptionService.cs` |
| `ILabRequestService.cs` | `LabRequestService.cs` |
| `IInvoiceService.cs` | `InvoiceService.cs` |
| `IExpenseService.cs` | `ExpenseService.cs` |
| `IFinanceService.cs` | `FinanceService.cs` |

### API Layer — Controllers (9 files)
| File | Endpoints | Route Base |
|------|-----------|------------|
| `QueueSessionsController.cs` | 5 | `api/clinic/queue/sessions` |
| `QueueTicketsController.cs` | 7 | `api/clinic/queue/tickets` |
| `QueueBoardController.cs` | 3 | `api/clinic/queue` |
| `VisitsController.cs` | 6 | `api/clinic/visits` + `api/clinic/patients` |
| `PrescriptionsController.cs` | 4 | `api/clinic/visits/{visitId}/prescriptions` |
| `LabRequestsController.cs` | 4 | `api/clinic/visits/{visitId}/labs` |
| `InvoicesController.cs` | 6 | `api/clinic/invoices` + `api/clinic/payments` |
| `ExpensesController.cs` | 4 | `api/clinic/expenses` |
| `FinanceController.cs` | 5 | `api/clinic/finance` |

### Tests (1 file)
| File | Tests |
|------|-------|
| `tests/Phase3_Tests.ps1` | 99 tests across 19 sections |

### Modified Files
| File | Changes |
|------|---------|
| `EliteClinicDbContext.cs` | Added 8 DbSets for Phase 3 entities |
| `Program.cs` | Added 7 DI registrations, JsonStringEnumConverter, SeedPhase3WorkflowAsync |

---

## 8. API Endpoint Summary (40 Phase 3 Endpoints)

| Module | Count | Methods |
|--------|-------|---------|
| Queue Sessions | 5 | POST open, POST close, GET list, GET by-id, GET tickets |
| Queue Tickets | 7 | POST issue, POST call, POST start-visit, POST finish, POST skip, POST cancel, POST urgent |
| Queue Board | 3 | GET board, GET my-queue, GET my-ticket |
| Visits | 6 | POST create, PUT update, POST complete, GET by-id, GET patient-visits, GET patient-summary |
| Prescriptions | 4 | POST add, PUT update, DELETE remove, GET list |
| Lab Requests | 4 | POST add, PUT update, POST add-result, GET list |
| Invoices | 6 | POST create, PUT update, GET by-id, GET list, POST payment, GET payments |
| Expenses | 4 | POST add, PUT update, DELETE remove, GET list |
| Finance | 5 | GET daily, GET by-doctor, GET monthly, GET yearly, GET profit |

**Phase 3 total: 44 endpoints** (40 new + 4 that use Phase 2 entity routes like `/patients/{id}/visits`)

---

## 9. Key Design Decisions

1. **Invoice per Visit (not per ticket):** Each visit has at most one invoice. Payments are recorded against invoices with partial payment support.

2. **Partial Payments:** `PaidAmount` and `RemainingAmount` tracked on Invoice. Status auto-transitions: Unpaid → PartiallyPaid → Paid. Cannot overpay or reduce amount below paid amount.

3. **Manual Visit Creation:** Visits can be created without a queue ticket (`POST /api/clinic/visits` with `queueTicketId = null`). Supports walk-in patients or ad-hoc consultations.

4. **Tenant-Scoped Queue Sessions:** All queue/visit/finance data is tenant-scoped via `TenantBaseEntity`. Queue data is isolated per clinic.

5. **JSON Enum Serialization:** All enums serialize as strings (e.g., `"Waiting"`, `"Completed"`, `"Paid"`) via `JsonStringEnumConverter` in `AddControllers()`.

6. **Ticket State Machine:** `Waiting → Called → InVisit → Completed` (normal flow), with `Skipped`, `NoShow`, `Cancelled` terminal/re-callable states. Session close auto-marks remaining Waiting/Called tickets as NoShow.
