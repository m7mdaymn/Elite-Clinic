# UPDATE_V3.md — Phase 3: Queue & Clinical Workflow

> **Phase:** 3  
> **Status:** ✅ COMPLETE — 99/99 Tests Pass  
> **Date Completed:** 2026-02-08  
> **Source of Truth:** spec-kit/ (PLAN.md §5, §7, §8, PERMISSIONS_MATRIX.md, MESSAGE_SPEC.md)

---

## 1. What Is IN SCOPE for Phase 3

Per **PLAN.md §18 Phase 3** — *"Queue system, visits, prescriptions, labs/imaging, follow-ups, payments, expenses, finance reporting."*

### 1.1 Queue Sessions (Doctor Shifts)

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Open session | PLAN.md §5.4, PERMISSIONS_MATRIX §Queue | `POST /api/clinic/queue/sessions` — Start doctor's shift, begin accepting patients |
| Close session | PLAN.md §5.4, PERMISSIONS_MATRIX §Queue | `POST /api/clinic/queue/sessions/{id}/close` — End shift, remaining tickets become no-show |
| List sessions | PLAN.md §5.4 | `GET /api/clinic/queue/sessions` — Active & recent sessions |
| Get session | PLAN.md §5.4 | `GET /api/clinic/queue/sessions/{id}` — Session details with ticket summary |

**New Entity: `QueueSession`**
```
QueueSession (extends TenantBaseEntity):
  - DoctorId: Guid (FK → Doctor)
  - StartedAt: DateTime
  - ClosedAt: DateTime?
  - IsActive: bool (default true)
  - Notes: string?
  - Doctor: Doctor (navigation)
  - Tickets: ICollection<QueueTicket> (navigation)
```

### 1.2 Queue Tickets

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Issue ticket (walk-in) | PLAN.md §5.2, PERMISSIONS_MATRIX §Queue | `POST /api/clinic/queue/tickets` — Reception issues ticket to patient for a doctor session |
| Call next ticket | PLAN.md §5.2, PERMISSIONS_MATRIX §Queue | `POST /api/clinic/queue/tickets/{id}/call` — Doctor calls patient |
| Start visit | PLAN.md §5.2 | `POST /api/clinic/queue/tickets/{id}/start-visit` — Begin clinical encounter |
| Finish visit | PLAN.md §5.2 | `POST /api/clinic/queue/tickets/{id}/finish` — Mark visit complete |
| Skip ticket | PLAN.md §5.2 | `POST /api/clinic/queue/tickets/{id}/skip` — Patient didn't show when called |
| Cancel ticket | PLAN.md §5.2 | `POST /api/clinic/queue/tickets/{id}/cancel` — Cancel before seeing doctor |
| Mark urgent | PLAN.md §5.3 | `POST /api/clinic/queue/tickets/{id}/urgent` — Elevate ticket priority |
| List tickets (by session) | PLAN.md §5.5 | `GET /api/clinic/queue/sessions/{id}/tickets` — All tickets for a session |
| Reception board | PLAN.md §5.5 | `GET /api/clinic/queue/board` — All active sessions with their current state |
| Doctor's own queue | PLAN.md §5.5 | `GET /api/clinic/queue/my-queue` — Doctor sees own active queue |
| Patient ticket status | PLAN.md §5.5 | `GET /api/clinic/queue/my-ticket` — Patient sees own active ticket |

**New Entity: `QueueTicket`**
```
QueueTicket (extends TenantBaseEntity):
  - SessionId: Guid (FK → QueueSession)
  - PatientId: Guid (FK → Patient)
  - DoctorServiceId: Guid? (FK → DoctorService, optional — service selected at ticket issue)
  - TicketNumber: int (sequential per session, resets each session)
  - Status: TicketStatus (enum)
  - IsUrgent: bool (default false)
  - QueuePosition: int (calculated position in queue)
  - IssuedAt: DateTime
  - CalledAt: DateTime?
  - VisitStartedAt: DateTime?
  - CompletedAt: DateTime?
  - CancelledAt: DateTime?
  - SkippedAt: DateTime?
  - Notes: string?
  - Session: QueueSession (navigation)
  - Patient: Patient (navigation)
  - DoctorService: DoctorService? (navigation)
  - Visit: Visit? (navigation, one-to-one)
  - Payment: Payment? (navigation, one-to-one)
```

**New Enum: `TicketStatus`**
```
TicketStatus:
  - Waiting = 0
  - Called = 1
  - InVisit = 2
  - Completed = 3
  - Skipped = 4
  - NoShow = 5
  - Cancelled = 6
```

### 1.3 Visits & Medical Records

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Create visit | PLAN.md §7.1, PERMISSIONS_MATRIX §Visits | Auto-created when ticket transitions to InVisit |
| Update visit | PLAN.md §7.1 | `PUT /api/clinic/visits/{id}` — Save complaint, vitals, diagnosis, notes |
| Get visit | PLAN.md §7.5 | `GET /api/clinic/visits/{id}` — Full visit with prescriptions, labs |
| List patient visits | PLAN.md §7.5 | `GET /api/clinic/patients/{patientId}/visits` — Visit history |
| Get patient summary | PLAN.md §3.5 | `GET /api/clinic/patients/{patientId}/summary` — Quick patient overview for doctor |

**New Entity: `Visit`**
```
Visit (extends TenantBaseEntity):
  - QueueTicketId: Guid (FK → QueueTicket, unique — one-to-one)
  - DoctorId: Guid (FK → Doctor)
  - PatientId: Guid (FK → Patient)
  - Complaint: string? (mandatory by end of visit)
  - Diagnosis: string?
  - Notes: string?
  - BloodPressureSystolic: int?
  - BloodPressureDiastolic: int?
  - HeartRate: int?
  - Temperature: decimal?
  - Weight: decimal?
  - Height: decimal?
  - BMI: decimal?
  - BloodSugar: decimal?
  - OxygenSaturation: decimal?
  - RespiratoryRate: int?
  - FollowUpDate: DateTime?
  - StartedAt: DateTime
  - CompletedAt: DateTime?
  - Doctor: Doctor (navigation)
  - Patient: Patient (navigation)
  - QueueTicket: QueueTicket (navigation)
  - Prescriptions: ICollection<Prescription> (navigation)
  - LabRequests: ICollection<LabRequest> (navigation)
```

### 1.4 Prescriptions

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Add prescription | PLAN.md §7.2, PERMISSIONS_MATRIX §Visits | `POST /api/clinic/visits/{visitId}/prescriptions` — Doctor adds medication |
| Update prescription | PLAN.md §7.2 | `PUT /api/clinic/visits/{visitId}/prescriptions/{id}` |
| Delete prescription | PLAN.md §7.2 | `DELETE /api/clinic/visits/{visitId}/prescriptions/{id}` |
| List prescriptions | PLAN.md §7.2 | `GET /api/clinic/visits/{visitId}/prescriptions` |

**New Entity: `Prescription`**
```
Prescription (extends TenantBaseEntity):
  - VisitId: Guid (FK → Visit)
  - MedicationName: string (required)
  - Dosage: string? (e.g., "500mg")
  - Frequency: string? (e.g., "3 times daily")
  - Duration: string? (e.g., "7 days")
  - Instructions: string? (e.g., "After meals")
  - Visit: Visit (navigation)
```

### 1.5 Lab / Imaging Requests

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Add lab request | PLAN.md §7.3, PERMISSIONS_MATRIX §Visits | `POST /api/clinic/visits/{visitId}/labs` — Doctor orders test |
| Update lab request | PLAN.md §7.3 | `PUT /api/clinic/visits/{visitId}/labs/{id}` |
| Add lab result | PLAN.md §7.3 | `POST /api/clinic/visits/{visitId}/labs/{id}/result` — Attach result text |
| List lab requests | PLAN.md §7.3 | `GET /api/clinic/visits/{visitId}/labs` |

**New Entity: `LabRequest`**
```
LabRequest (extends TenantBaseEntity):
  - VisitId: Guid (FK → Visit)
  - TestName: string (required)
  - Type: LabRequestType (enum: Lab=0, Imaging=1)
  - Notes: string?
  - IsUrgent: bool (default false)
  - ResultText: string? (filled in when result arrives)
  - ResultReceivedAt: DateTime?
  - Visit: Visit (navigation)
```

**New Enum: `LabRequestType`**
```
LabRequestType:
  - Lab = 0
  - Imaging = 1
```

### 1.6 Payments

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Record payment | PLAN.md §8.1, PERMISSIONS_MATRIX §Payments | `POST /api/clinic/payments` — Record payment for a ticket |
| Update payment | PLAN.md §8.1 | `PUT /api/clinic/payments/{id}` — Adjust amount or mark paid/unpaid |
| Mark paid | PLAN.md §8.1 | `POST /api/clinic/payments/{id}/mark-paid` — Quick status toggle |
| List payments | PLAN.md §8.1 | `GET /api/clinic/payments` — Paginated, filterable by date/doctor/status |
| Get payment | PLAN.md §8.1 | `GET /api/clinic/payments/{id}` |

**New Entity: `Payment`**
```
Payment (extends TenantBaseEntity):
  - QueueTicketId: Guid (FK → QueueTicket, unique — one-to-one)
  - PatientId: Guid (FK → Patient)
  - DoctorId: Guid (FK → Doctor)
  - Amount: decimal
  - PaymentMethod: string? (free text: "Cash", "Instapay", etc.)
  - ReferenceNumber: string? (receipt/reference)
  - IsPaid: bool (default false)
  - PaidAt: DateTime?
  - Notes: string?
  - QueueTicket: QueueTicket (navigation)
  - Patient: Patient (navigation)
  - Doctor: Doctor (navigation)
```

### 1.7 Expenses

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Add expense | PLAN.md §8.3, PERMISSIONS_MATRIX §Payments | `POST /api/clinic/expenses` |
| Update expense | PLAN.md §8.3 | `PUT /api/clinic/expenses/{id}` |
| Delete expense | PLAN.md §8.3 | `DELETE /api/clinic/expenses/{id}` |
| List expenses | PLAN.md §8.3 | `GET /api/clinic/expenses` — Paginated, filterable by date/category |

**New Entity: `Expense`**
```
Expense (extends TenantBaseEntity):
  - Category: string (required, e.g., "Supplies", "Maintenance", "Utilities")
  - Amount: decimal (required)
  - Notes: string?
  - ExpenseDate: DateTime (default today)
  - RecordedByUserId: Guid (FK → ApplicationUser)
  - RecordedBy: ApplicationUser (navigation)
```

### 1.8 Finance Reporting

| Capability | Spec Reference | Details |
|------------|---------------|---------|
| Daily revenue | PLAN.md §8.2 | `GET /api/clinic/finance/daily?date=` |
| Revenue per doctor | PLAN.md §8.2 | `GET /api/clinic/finance/by-doctor?date=&doctorId=` |
| Monthly summary | PLAN.md §8.2 | `GET /api/clinic/finance/monthly?year=&month=` |
| Yearly summary | PLAN.md §8.2 | `GET /api/clinic/finance/yearly?year=` |
| Profit report | PLAN.md §8.2 | `GET /api/clinic/finance/profit?from=&to=` |

These are read-only aggregation endpoints — no new entities needed.

---

## 2. What Is OUT OF SCOPE for Phase 3

| Feature | Phase | Reason |
|---------|-------|--------|
| WhatsApp messaging (ticket issued, your turn, visit results, follow-up reminder) | Phase 4 | WhatsApp integration is Phase 4. Phase 3 creates the trigger entities only. |
| Online booking | Phase 4 | Depends on clinic settings + queue system, but booking UI/flow is Phase 4 |
| Public SEO endpoints (doctor profiles, services) | Phase 4 | Public-facing module, not internal clinical |
| SignalR real-time queue updates | Phase 5 | Performance optimization; Phase 3 uses REST polling |
| PWA notifications ("Your turn" push) | Phase 4 / 5 | Push notifications not in Phase 3 |
| Login history / audit viewer | Phase 5 | Enhancement over existing audit logging |
| Advanced medical templates | Phase 5 | Specialty-specific form builders |
| Export (PDF prescriptions, CSV reports) | Phase 5 | Export module |
| Patient can cancel own ticket (self-service rules) | Phase 4 | Requires patient app integration depth |

---

## 3. Assumptions

| # | Assumption | Based On |
|---|-----------|----------|
| A1 | Queue ticket number resets per session (not per day). Session = doctor shift. | PLAN.md §5.1 — "Ticket numbering resets each session" |
| A2 | A patient can only have ONE active ticket at a time across all sessions | Inferred — prevents duplicate queue entries |
| A3 | Visit is auto-created when ticket transitions to InVisit status | PLAN.md §5.2 lifecycle — "Called → In-Visit" creates the record |
| A4 | Payment is per-ticket (not per-visit), since a ticket may not proceed to visit. Unpaid tickets can be created. | PLAN.md §8.1 — "Visit CAN proceed unpaid" |
| A5 | Payment amount defaults to `DoctorService.Price` if a service was selected at ticket time. Staff can override. | PLAN.md §8.1 — pricing linked to services |
| A6 | Expense categories are free text (not a predefined entity table in Phase 3). | PLAN.md §8.3 — "category/type (text or predefined per clinic)" — text for now |
| A7 | Finance reports are ClinicOwner-only for historical data; ClinicManager sees today only | PLAN.md §8.2 table |
| A8 | Follow-up date on visit creates a hint only — actual follow-up appointment/booking is Phase 4 | PLAN.md §7.4 — "auto-creates an unpaid appointment" requires booking system |
| A9 | Doctor can only edit their own visits, same day only | PERMISSIONS_MATRIX §Visits — "own, same day" |
| A10 | Session can be opened by Doctor (own), ClinicOwner, or ClinicManager | PERMISSIONS_MATRIX §Queue |
| A11 | Multiple sessions per doctor per day are allowed (morning + afternoon shifts) | PLAN.md §5.4 |
| A12 | When a session is closed, remaining Waiting/Called tickets transition to NoShow | PLAN.md §5.4 — "Closing a session marks remaining tickets as skipped/no-show" |

---

## 4. Risks

| # | Risk | Impact | Mitigation |
|---|------|--------|-----------|
| R1 | Queue ticket ordering logic with urgent modes is complex | Wrong queue position for urgent cases | Implement each mode as a strategy pattern; test all 3 modes explicitly |
| R2 | Concurrent ticket operations (two tabs issuing tickets simultaneously) | Duplicate ticket numbers | Use DB-level `MAX(TicketNumber) + 1` in transaction |
| R3 | Visit vitals fields are dynamic per doctor — could lead to complex forms | Frontend needs to know which fields to show | `DoctorVisitFieldConfig` already exists (Phase 2). Return it in visit response. |
| R4 | Finance reports may be slow on large datasets | Timeouts on yearly summaries | Use indexed queries; limit date ranges |
| R5 | Payment-visit relationship: payment exists before visit (at ticket level) | Confusing data model | Clear FK: Payment → QueueTicket, Visit → QueueTicket. Both reference the ticket. |
| R6 | Closing session with patients mid-visit | Data inconsistency | Reject session close if any tickets are InVisit status |

---

## 5. Dependencies on Phase 2

| Dependency | Status | Notes |
|------------|--------|-------|
| Doctor entity with services | ✅ Phase 2 | Sessions per doctor, services linked to tickets |
| DoctorVisitFieldConfig | ✅ Phase 2 | Controls which vitals appear in visit form |
| Patient entity with sub-profiles | ✅ Phase 2 | Tickets issued to patients |
| UrgentCaseMode enum on Doctor | ✅ Phase 2 | Already per-doctor setting |
| ClinicSettings (working hours) | ✅ Phase 2 | Session validation against working hours (optional) |
| TenantMiddleware | ✅ Phase 2 | All new endpoints are tenant-scoped |
| Staff/ClinicManager users | ✅ Phase 2 | Issue tickets, record payments |
| AvgVisitDurationMinutes on Doctor | ✅ Phase 2 | Used for estimated wait time calculation |

---

## 6. Spec Gaps Discovered

### SPEC GAP #10 — Payment Timing

**Issue:** PLAN.md says "Payment is independent of visit flow" and can be recorded at any time. But when exactly is the Payment entity created?  
**Proposed resolution:** Payment is optionally created at ticket-issue time (with `IsPaid=false`), or staff can add payment any time via `POST /api/clinic/payments` referencing a ticket. If ticket has a DoctorService, amount defaults to service price.  
**Action required:** Approve or modify.

### SPEC GAP #11 — Queue Position Calculation

**Issue:** How exactly is `QueuePosition` maintained? Is it physical order or computed?  
**Proposed resolution:** `QueuePosition` is a computed value derived from ticket ordering:  
- Normal tickets: ordered by `IssuedAt`  
- Urgent tickets: position depends on `Doctor.UrgentCaseMode`:  
  - `UrgentNext`: Insert after currently-being-seen ticket  
  - `UrgentBucket`: Separate list; doctor manually pulls  
  - `UrgentFront`: Insert at position 1 in waiting list  
- Frontend fetches sorted list; no stored "position" field — just query order.  
**Action required:** Approve or define alternative.

### SPEC GAP #12 — Session Auto-Close

**Issue:** Should sessions auto-close at end of working hours, or only manually?  
**Proposed resolution:** Manual close only in Phase 3. Auto-close can be added in Phase 5 as a background job.  
**Action required:** Approve.

### SPEC GAP #13 — Visit Edit Window

**Issue:** PERMISSIONS_MATRIX says doctor can edit visit "own, same day." What defines "same day" — calendar day (midnight) or 24 hours from creation?  
**Proposed resolution:** Calendar day in clinic's timezone. After midnight, visit becomes read-only for the doctor (ClinicOwner can always view).  
**Action required:** Approve or define alternative.

### SPEC GAP #14 — Patient Summary Endpoint

**Issue:** PLAN.md §3.5 mentions doctor sees "patient summary: identity, chronic conditions, allergies, last visits." But there are no chronic conditions or allergies fields yet.  
**Proposed resolution:** `GET /api/clinic/patients/{id}/summary` returns patient info + last 5 visits (with diagnoses). Chronic conditions / allergies fields deferred to Phase 5 (advanced medical templates).  
**Action required:** Approve or add fields now.

---

## 7. Deliverables Summary

| # | Deliverable | Type |
|---|------------|------|
| D1 | `QueueSession` entity | New entity |
| D2 | `QueueTicket` entity + `TicketStatus` enum | New entity + enum |
| D3 | `Visit` entity (with inline vitals fields) | New entity |
| D4 | `Prescription` entity | New entity |
| D5 | `LabRequest` entity + `LabRequestType` enum | New entity + enum |
| D6 | `Payment` entity | New entity |
| D7 | `Expense` entity | New entity |
| D8 | QueueSessionController (open/close/list/get) | New controller |
| D9 | QueueTicketController (issue/call/start/finish/skip/cancel/urgent) | New controller |
| D10 | QueueBoardController (board/my-queue/my-ticket) | New controller |
| D11 | VisitController (CRUD + patient visits + patient summary) | New controller |
| D12 | PrescriptionController (CRUD nested under visit) | New controller |
| D13 | LabRequestController (CRUD nested under visit + add result) | New controller |
| D14 | PaymentController (CRUD + mark-paid) | New controller |
| D15 | ExpenseController (CRUD) | New controller |
| D16 | FinanceController (daily/monthly/yearly/by-doctor/profit) | New controller |
| D17 | EF Migration for all new entities | Migration |
| D18 | Seed: sessions, tickets (various states), visits with prescriptions/labs, payments, expenses | Seed data |
| D19 | Tests (real HTTP, runnable) | Tests |
| D20 | SWAGGER_DOCUMENTATION.md updated | Docs |
| D21 | FRONTEND_CONTRACT.md updated | Docs |
| D22 | TESTS_V3.md with real HTTP evidence | Docs |
| D23 | COMPLETION_V3.md runbook | Docs |

---

## 8. Endpoint Summary

### Queue Sessions (4 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/queue/sessions` | Bearer + X-Tenant | ClinicOwner, ClinicManager, Doctor (own) |
| POST | `/api/clinic/queue/sessions/{id}/close` | Bearer + X-Tenant | ClinicOwner, ClinicManager, Doctor (own) |
| GET | `/api/clinic/queue/sessions` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| GET | `/api/clinic/queue/sessions/{id}` | Bearer + X-Tenant | ClinicOwner, ClinicManager, Doctor (own) |

### Queue Tickets (7 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/queue/tickets` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| POST | `/api/clinic/queue/tickets/{id}/call` | Bearer + X-Tenant | Doctor (own queue) |
| POST | `/api/clinic/queue/tickets/{id}/start-visit` | Bearer + X-Tenant | Doctor (own queue) |
| POST | `/api/clinic/queue/tickets/{id}/finish` | Bearer + X-Tenant | Doctor (own queue) |
| POST | `/api/clinic/queue/tickets/{id}/skip` | Bearer + X-Tenant | ClinicOwner, ClinicManager, Doctor (own) |
| POST | `/api/clinic/queue/tickets/{id}/cancel` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| POST | `/api/clinic/queue/tickets/{id}/urgent` | Bearer + X-Tenant | ClinicOwner, ClinicManager, Doctor (own) |

### Queue Views (3 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| GET | `/api/clinic/queue/board` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| GET | `/api/clinic/queue/my-queue` | Bearer + X-Tenant | Doctor |
| GET | `/api/clinic/queue/my-ticket` | Bearer + X-Tenant | Patient |

### Visits & Medical Records (5 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| PUT | `/api/clinic/visits/{id}` | Bearer + X-Tenant | Doctor (own, same day) |
| GET | `/api/clinic/visits/{id}` | Bearer + X-Tenant | ClinicOwner, Doctor (own) |
| GET | `/api/clinic/patients/{patientId}/visits` | Bearer + X-Tenant | ClinicOwner, Doctor (if permitted) |
| GET | `/api/clinic/patients/{patientId}/summary` | Bearer + X-Tenant | Doctor |
| GET | `/api/clinic/queue/sessions/{sessionId}/tickets` | Bearer + X-Tenant | ClinicOwner, ClinicManager, Doctor (own) |

### Prescriptions (4 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/visits/{visitId}/prescriptions` | Bearer + X-Tenant | Doctor |
| PUT | `/api/clinic/visits/{visitId}/prescriptions/{id}` | Bearer + X-Tenant | Doctor (own, same day) |
| DELETE | `/api/clinic/visits/{visitId}/prescriptions/{id}` | Bearer + X-Tenant | Doctor (own, same day) |
| GET | `/api/clinic/visits/{visitId}/prescriptions` | Bearer + X-Tenant | ClinicOwner, Doctor |

### Lab Requests (4 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/visits/{visitId}/labs` | Bearer + X-Tenant | Doctor |
| PUT | `/api/clinic/visits/{visitId}/labs/{id}` | Bearer + X-Tenant | Doctor (own, same day) |
| POST | `/api/clinic/visits/{visitId}/labs/{id}/result` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| GET | `/api/clinic/visits/{visitId}/labs` | Bearer + X-Tenant | ClinicOwner, Doctor |

### Payments (5 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/payments` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| PUT | `/api/clinic/payments/{id}` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| POST | `/api/clinic/payments/{id}/mark-paid` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| GET | `/api/clinic/payments` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| GET | `/api/clinic/payments/{id}` | Bearer + X-Tenant | ClinicOwner, ClinicManager |

### Expenses (4 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| POST | `/api/clinic/expenses` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| PUT | `/api/clinic/expenses/{id}` | Bearer + X-Tenant | ClinicOwner, ClinicManager |
| DELETE | `/api/clinic/expenses/{id}` | Bearer + X-Tenant | ClinicOwner |
| GET | `/api/clinic/expenses` | Bearer + X-Tenant | ClinicOwner, ClinicManager |

### Finance Reports (5 endpoints)
| Method | Route | Auth | Roles |
|--------|-------|------|-------|
| GET | `/api/clinic/finance/daily` | Bearer + X-Tenant | ClinicOwner (all), ClinicManager (today) |
| GET | `/api/clinic/finance/by-doctor` | Bearer + X-Tenant | ClinicOwner (all), ClinicManager (today) |
| GET | `/api/clinic/finance/monthly` | Bearer + X-Tenant | ClinicOwner |
| GET | `/api/clinic/finance/yearly` | Bearer + X-Tenant | ClinicOwner |
| GET | `/api/clinic/finance/profit` | Bearer + X-Tenant | ClinicOwner (all), ClinicManager (today) |

**Total new endpoints: 41**

---

## 9. Test Coverage Expectations

### Category Breakdown

| Category | Test Count (est.) | Notes |
|----------|------------------|-------|
| Queue sessions (open/close/list) | 8–10 | Open, close, list, doctor-only, auto-NoShow on close |
| Queue tickets (issue/call/start/finish/skip/cancel/urgent) | 20–25 | Full lifecycle, urgent modes x3, position logic, validation |
| Queue views (board/my-queue/my-ticket) | 6–8 | Reception board, doctor queue, patient ticket |
| Visits (create via ticket, update, get, patient history) | 10–12 | Auto-create, vitals, same-day edit rule, patient summary |
| Prescriptions (CRUD) | 6–8 | Add, update, delete, list, doctor-only |
| Lab requests (CRUD + result) | 6–8 | Add, update, add result, list, doctor-only |
| Payments (CRUD + mark-paid) | 8–10 | Create, update, mark-paid, list, filter by date |
| Expenses (CRUD) | 6–8 | Add, update, delete, list, ClinicManager vs Owner |
| Finance reports | 8–10 | Daily, monthly, yearly, by-doctor, profit, ClinicManager today-only |
| Authorization & validation | 8–10 | Wrong role, cross-tenant, invalid transitions |
| Phase 2 regression | 6 | All Phase 2 endpoints still work |
| **Total** | **~92–119** | |

### Exit Criteria (What "Phase 3 Complete" Means)

- [ ] All 41 new endpoints implemented and reachable
- [ ] 0 build errors
- [ ] All runnable tests pass
- [ ] Full walk-in patient flow works end-to-end: arrive → ticket → called → visit → prescriptions → payment
- [ ] Queue board shows all active doctors with ticket counts
- [ ] Doctor can open session, call patients, complete visits
- [ ] Prescriptions and lab requests can be added to visits
- [ ] Payments can be recorded and marked paid/unpaid
- [ ] Expenses can be tracked
- [ ] Finance reports produce correct daily/monthly/yearly numbers
- [ ] ClinicManager sees TODAY only for finance; ClinicOwner sees all history
- [ ] Urgent ticket modes work (UrgentNext, UrgentBucket, UrgentFront)
- [ ] Closing session transitions remaining tickets to NoShow
- [ ] Visit edit restricted to same day for doctor
- [ ] Phase 2 regression tests pass
- [ ] Seed data: active sessions, tickets in various states, completed visits
- [ ] Documentation updated

---

*This document is the Phase 3 scope definition. No implementation until explicitly approved.*
