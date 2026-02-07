# PLAN.md — Elite Clinic SaaS Platform (Source of Truth)

> **Version:** 2.0  
> **Last Updated:** 2026-02-07  
> **Status:** All 5 Phases Complete — Production Ready  
> **Runtime:** .NET 9 Web API  
> **Database:** SQL Server (shared, multi-tenant with row-level isolation)

---

## 1. PRODUCT OVERVIEW

Elite Clinic is a **multi-tenant SaaS clinic platform** where multiple independent clinics operate within a single shared database. A **SuperAdmin** (platform owner) governs the entire ecosystem: tenant activation, subscriptions, feature flags, and platform-wide analytics. Each tenant (clinic) operates autonomously with its own staff hierarchy (ClinicOwner → ClinicManager/Reception → Doctor) and patients. The system is **queue-first**: every patient visit begins with a queue ticket, supports walk-in and online booking, manual payment capture, automated WhatsApp messaging, and persistent patient access via a PWA. The platform functions as a complete Clinic CRM/ERP covering operational, medical, financial, and communication workflows with full reporting and audit trails.

---

## 2. TENANCY MODEL

### 2.1 Architecture
- **Single database, shared schema** with `TenantId` column on every tenant-scoped entity.
- Tenant resolution via `X-Tenant` HTTP header containing the **tenant slug** (e.g., `elite-dental`).
- Platform-level routes (SuperAdmin) do NOT require `X-Tenant`.
- **Row-level isolation**: every query for tenant-scoped data MUST filter by `TenantId`. No exceptions.
- A global query filter on the DbContext ensures tenant isolation automatically.

### 2.2 Tenant States
| State | Clinic Routes | Public SEO Routes | Description |
|-------|--------------|-------------------|-------------|
| **Active** | Full access (200) | Full profile (200) | Normal operations |
| **Suspended** | 403 Forbidden | 200 + `isActive=false` + renewal message | Subscription expired or admin action |
| **Blocked** | 403 Forbidden | 200 + `isActive=false` + abuse message | Locked by SuperAdmin |
| **Inactive** | 403 Forbidden | 200 + `isActive=false` + contact message | Newly created, not yet activated |

### 2.3 Tenant Slug Rules
- Unique, lowercase, URL-safe (letters, digits, hyphens).
- Immutable after creation (cannot be renamed).
- Used in `X-Tenant` header for all tenant-scoped API calls.

---

## 3. ROLES & RESPONSIBILITIES

### 3.1 Role Hierarchy

```
SuperAdmin (Platform)
  └── ClinicOwner (Tenant Admin)
        ├── ClinicManager / Reception (Operations Staff)
        ├── Doctor
        └── Patient
```

### 3.2 SuperAdmin
- Platform-level god mode. Does NOT operate daily clinic workflows.
- Creates, activates, suspends, blocks tenants.
- Manages subscriptions (manual/offline billing).
- Configures feature flags per tenant.
- Manages WhatsApp sender configuration per tenant.
- Views platform-wide analytics and audit logs.
- Can lock clinics for abuse.
- Accesses cross-tenant WhatsApp delivery logs.

### 3.3 ClinicOwner (ClinicAdmin)
- Tenant-level administrator. One or more per clinic.
- Manages doctors: create, edit, enable/disable.
- Manages staff: create employees, assign roles, salaries, schedules.
- Configures services and pricing rules per doctor.
- Configures clinic settings: phones, WhatsApp numbers, address, working hours.
- Views full financial reports: daily/monthly/yearly revenue, expenses, profit.
- Configures doctor visit templates (toggle fields per doctor specialty).
- Views all patients, visits, records, payments.
- Configures online booking and cancellation rules.
- Exports reports (if feature flag enabled).

### 3.4 ClinicManager / Reception
- Operational staff. Handles daily clinic flow.
- Creates patient profiles (first visit registration).
- Registers walk-ins, manages check-in.
- Manages queue tickets and flow.
- Marks payments paid/unpaid.
- Adds daily expenses.
- Views **TODAY** finance only (revenue, expenses, profit).
- Receives real-time doctor notes/notifications.
- Can send manual WhatsApp messages (logged).
- Manages appointment reschedule/cancel within configured rules.

### 3.5 Doctor
- Medical provider. Operates within own queue.
- Actions: Next ticket → Call → Start visit → Finish visit.
- Sees patient summary: identity, chronic conditions, allergies, last visits (own by default; all visits if permitted).
- Structured visit capture with specialty-dependent toggled fields.
- Writes prescriptions, orders labs/imaging, schedules follow-ups.
- Sends internal notes to reception instantly.
- Views own performance metrics.

### 3.6 Patient
- End user via PWA/web. **Never sees logout button.**
- Persistent session (token never expires from user perspective — long-lived refresh token).
- Multi-profile support: parent account can have child profiles under same phone number.
- Views queue status: ticket number, ahead count, current number, paid/unpaid.
- Receives WhatsApp messages: credentials (first time only), booking confirmations, queue updates, turn notification, visit summary, follow-up reminders.
- Optional PWA notifications: med reminders, follow-up reminders.
- Online booking ONLY if account already exists (created by reception).

---

## 4. PATIENT LIFECYCLE

### 4.1 First Visit (Walk-in)
1. Patient arrives physically for the first time.
2. Reception creates patient profile (name, phone, DOB, gender, etc.).
3. System auto-generates **Username + Password**.
4. System sends WhatsApp credentials message (ONE TIME only).
5. Reception can create **multiple profiles** under same phone (e.g., children).
6. Reception selects default profile.
7. Patient is issued a queue ticket.

### 4.2 Returning Visit
- Patient may book online (if clinic has online booking enabled).
- Booking integrates with same queue logic — no conflict.
- Patient can be "in queue from home."
- Patient is guided to arrive before their number (e.g., "come when 4 patients are ahead").

### 4.3 Credentials Rules
- Credentials WhatsApp message sent **exactly once**.
- No email, no OTP, no verification of any kind.
- Staff can manually reset password and re-send WhatsApp.
- Patient logs in with username + password via PWA/web.

---

## 5. QUEUE SYSTEM

### 5.1 Core Concepts
- Queue is **per doctor per session (shift)**.
- Ticket numbering resets each session.
- A "session" is a doctor's working shift (open → close).

### 5.2 Ticket Lifecycle
```
Issued → Waiting → Called → In-Visit → Done
                 ↘ Skipped/No-Show
                 ↘ Cancelled
```

### 5.3 Urgent Cases (Doctor-Configurable)
| Mode | Behavior |
|------|----------|
| `urgent_next` | Urgent patient goes to position immediately after current |
| `urgent_bucket` | Urgent patients accumulate; doctor pulls from urgent bucket when ready |
| `urgent_front` | Urgent patient goes to front of queue |

### 5.4 Session Management
- Doctor or reception opens/closes sessions.
- Multiple sessions per doctor per day supported (morning + afternoon).
- Closing a session marks remaining tickets as skipped/no-show.

### 5.5 Views
| Role | Sees |
|------|------|
| **Reception** | All doctor cards stacked. Each: current number, waiting count, urgent count, next preview. Doctor-to-reception messages appear instantly. |
| **Doctor** | Own queue. Ultra-fast actions: Next, Start Visit, Finish. Instant history access. |
| **Patient** | Own ticket, ahead count, current number, paid/unpaid status. |

### 5.6 Real-Time Considerations
- SignalR hub for real-time queue updates (Phase 5).
- Polling fallback for PWA (Phase 5).
- "Your turn" WhatsApp message triggered when ticket is called.

### 5.7 Estimated Wait
- Avg visit duration per doctor (computed from history).
- `estimatedWaitMinutes = aheadCount × avgDuration`.

---

## 6. ONLINE BOOKING

### 6.1 Rules
- ONLY existing patient accounts can book online.
- Booking produces a queue ticket consistent with walk-in logic.
- Booking can start unpaid; staff marks paid later.
- Cancellation rules: configurable time window (e.g., "cancel up to 2 hours before").
- Reschedule within rules.
- Queue position remains consistent regardless of booking source (online vs. walk-in).

### 6.2 Feature Flag
- `online_booking` toggle per tenant (SuperAdmin or ClinicOwner).
- When disabled: booking endpoints return 403 for that tenant.

---

## 7. VISIT / MEDICAL RECORDS

### 7.1 Visit Data Capture
- **Complaint text** (mandatory).
- **Specialty-dependent toggled fields** (configured per doctor):
  - Blood pressure, heart rate, temperature, weight, height, BMI, blood sugar, oxygen saturation, respiratory rate, etc.
  - ClinicOwner configures which fields are enabled per doctor/specialty.
- **Diagnosis** (text).
- **Notes** (free text).

### 7.2 Prescription
- Name, dosage, frequency, duration, special instructions.
- Multiple medications per visit.

### 7.3 Labs / Imaging Requests
- Test name, type (lab/imaging), notes, urgency.
- Results can be attached later (text or notes).

### 7.4 Follow-Up
- Follow-up date → auto-creates an unpaid appointment/reservation.
- Patient receives WhatsApp follow-up reminder.

### 7.5 Visit Visibility
- Doctor sees **own visits** by default.
- "Show all visits" toggle (if permitted by ClinicOwner).
- Patient sees all own visits.

---

## 8. PAYMENTS & FINANCE

### 8.1 Payment Rules
- Visit CAN proceed unpaid. Payment is independent of visit flow.
- Staff marks payment as paid/unpaid at any time.
- **Payment method is free text** (e.g., "Cash", "Instapay", "VodafoneCash"). Not an enum.
- Receipt/reference number is optional free text.
- Payment status visible to doctor and patient.

### 8.2 Finance Reporting
| Report | ClinicManager | ClinicOwner |
|--------|--------------|-------------|
| Daily revenue | Today only | Full history |
| Revenue per doctor | Today only | Full history |
| Daily expenses | Today only | Full history |
| Profit (revenue − expenses) | Today only | Full history |
| Monthly/yearly summaries | ✗ | ✓ |

### 8.3 Expenses
- Category/type (text or predefined per clinic).
- Amount.
- Notes.
- RecordedBy (staff who entered).
- Timestamp.
- Includes maintenance, supplies, etc.

---

## 9. STAFF & EMPLOYEE MANAGEMENT

### 9.1 Employee Profile
- Name, role, phone, salary, hire date.
- Assigned by ClinicOwner.

### 9.2 Schedule
- Working days, shift times.
- Per employee.

### 9.3 Login Tracking
- Login/logout timestamps.
- IP address, device info.
- ClinicOwner can review.

---

## 10. WHATSAPP AUTOMATION

### 10.1 Architecture
- Each tenant can have a configured WhatsApp sender number.
- Messages are queued and processed asynchronously.
- All messages logged with status: `pending → sent → delivered → failed → retrying`.
- Retry strategy: 3 attempts with exponential backoff (configurable).
- Failures logged permanently with error reason.
- SuperAdmin can view WhatsApp logs across all tenants.

### 10.2 Trigger Points (see MESSAGE_SPEC.md)
1. Credentials (first time patient creation) — one-time.
2. Booking confirmation.
3. Queue ticket issued.
4. "Your turn" notification.
5. Visit results summary.
6. Follow-up reminder.

### 10.3 Per-Tenant Configuration
| Field | Description |
|-------|-------------|
| `whatsappSenderNumber` | Automated message sender |
| `supportPhoneNumber` | For voice calls |
| `supportWhatsAppNumber` | For manual chat support |

---

## 11. PWA NOTIFICATIONS

### 11.1 Supported Notifications
- Medication reminders (based on prescription).
- Follow-up reminders.
- Queue turn approaching.

### 11.2 Feature Flag
- `pwa_notifications` toggle per tenant.
- Independent of WhatsApp (can have both, one, or neither).

---

## 12. PUBLIC SEO ENDPOINTS

### 12.1 Behavior
- Require `X-Tenant` header.
- **ALWAYS return HTTP 200**. Never 403.
- If tenant is blocked/suspended/inactive:
  ```json
  {
    "isActive": false,
    "clinicName": "Elite Dental",
    "renewalMessage": "This clinic is currently unavailable. Please contact support.",
    "supportPhone": "+20..."
  }
  ```
- If tenant is active: full public clinic profile, doctors list, services, working hours.

### 12.2 Endpoints
- `GET /api/public/clinic` — clinic profile
- `GET /api/public/doctors` — doctors list
- `GET /api/public/services` — services list
- `GET /api/public/working-hours` — schedule

---

## 13. FEATURE FLAGS MODEL

Feature flags are **per-tenant** and managed by SuperAdmin (and optionally ClinicOwner where appropriate).

| Flag | Default | Description |
|------|---------|-------------|
| `online_booking` | false | Enables patient online booking |
| `whatsapp_automation` | true | Enables automated WhatsApp messages |
| `pwa_notifications` | false | Enables PWA push notifications |
| `expenses_module` | true | Enables expense tracking |
| `advanced_medical_templates` | false | Enables extended medical fields |
| `ratings` | false | Enables patient ratings for doctors |
| `export` | false | Enables data export endpoints |

---

## 14. AUDIT & LOGGING

### 14.1 Audit Trail
- Every create/update/delete operation logged with:
  - UserId, TenantId, EntityType, EntityId, Action, OldValues, NewValues, Timestamp, IP.
- Immutable log table (no deletes).

### 14.2 Application Logging
- Structured logging (Serilog).
- Log levels: Information, Warning, Error.
- Sensitive data excluded from logs.

### 14.3 Login Audit
- All login attempts logged (success/failure).
- Per-user login history.

---

## 15. RESPONSE FORMAT (PREDICTABLE)

All API responses follow a consistent envelope:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... },
  "errors": [],
  "meta": {
    "timestamp": "2026-02-06T10:00:00Z",
    "requestId": "uuid"
  }
}
```

Paginated responses:
```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

Error responses:
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    { "field": "phone", "message": "Phone is required" }
  ]
}
```

---

## 16. TECHNOLOGY STACK

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9 Web API |
| Database | SQL Server (provided connection string) |
| ORM | Entity Framework Core 9 |
| Auth | ASP.NET Identity + JWT Bearer |
| Docs | Swagger/OpenAPI (available in production) |
| Logging | Serilog (structured) |
| Real-time | SignalR (future phase) |
| Messaging | WhatsApp Business API integration (abstracted) |
| Caching | In-memory (upgrade to Redis in future) |

---

## 17. SEED DATA EXPECTATIONS

Seed must produce **zero empty tables**. Required data:
- 1 SuperAdmin account.
- 2 tenants: "Nile Dental Clinic" (active) + "Cairo Ortho Center" (blocked).
- Per active tenant: 2+ doctors, 3+ staff, 20+ patients (mix of adults and children sharing phones).
- Doctor sessions (shifts), queue tickets in various states.
- Completed visits with prescriptions, labs, diagnoses.
- Payments (paid/unpaid, various methods).
- Expenses (multiple categories).
- WhatsApp logs (sent, failed, pending).
- Online booking samples (for enabled tenant).
- Feature flag configurations.
- Subscription records.
- Audit log entries.

---

## 18. PHASE PLAN (5 PHASES)

### Phase 1 — Platform Foundation ✅ COMPLETE
**Scope:** Tenant CRUD, subscription management, feature flags, tenant middleware enforcement, JWT CVE fix.

**Delivered:**
- 15 platform endpoints (8 tenant, 5 subscription, 2 feature flag)
- TenantMiddleware with cross-tenant isolation
- Seed data: 4 tenants, 4 subscriptions, 4 feature flag records
- 82/102 tests passed (2 known non-blocking failures, 12 deferred)

**Known issues carried to Phase 2:**
- SV03: DateTime value type defaults (fix: make nullable)
- RF06: ProblemDetails vs ApiResponse format (fix: InvalidModelStateResponseFactory)

---

### Phase 2 — Clinic Setup & Users ✅ COMPLETE
**Scope:** Clinic settings, staff/employee management, doctor management, patient registration, tenant-scoped authentication.

**Why this phase:** Without staff, doctors, and patients created under a tenant, no clinical workflow can execute. This phase creates all tenant-scoped users and configurations.

**Modules:**
- Clinic settings (name, phone, WhatsApp numbers, address, working hours)
- Staff/employee CRUD (create, edit, enable/disable, schedules, salaries)
- Doctor CRUD (profile, services, pricing, visit field configuration, urgent case mode)
- Patient registration (walk-in create, multi-profile, auto-generated credentials)
- Patient login endpoint (`POST /api/auth/patient/login`)
- Tenant-scoped user authentication (staff/doctor login with `X-Tenant` header)

**Exit criteria:** ClinicOwner can log in, configure clinic, create staff/doctors/patients. Staff/doctors can log in. Patients can log in. Middleware enforces tenant isolation on all new routes.

---

### Phase 3 — Queue & Clinical Workflow ✅ COMPLETE
**Scope:** Queue system, visits, prescriptions, labs/imaging, follow-ups, payments, expenses, finance reporting.

**Why this phase:** The core clinical workflow — from patient arrival through queue, doctor visit, medical records, to payment. Everything needed for a clinic to operate daily.

**Modules:**
- Queue sessions (open/close per doctor)
- Queue tickets (issue, call, start visit, finish, skip, cancel, urgent)
- Reception board view, doctor queue view, patient ticket view
- Visit capture (complaint, vitals, diagnosis, notes, specialty-dependent fields)
- Prescriptions (per visit)
- Labs/imaging requests (per visit)
- Follow-up scheduling
- Payment recording (paid/unpaid, payment method free text)
- Expense tracking (categories, amounts)
- Finance reporting (daily/monthly/yearly revenue, expenses, profit)

**Exit criteria:** Full walk-in patient flow works end-to-end: arrive → queue ticket → called → visit → prescriptions → payment. Finance reports produce correct numbers.

---

### Phase 4 — Communication & Booking ✅ COMPLETE
**Scope:** WhatsApp messaging, online booking, public SEO endpoints, PWA notification infrastructure, doctor-to-reception messaging.

**Why this phase:** Extends the clinic with external communication channels and patient self-service. Builds on all entities from Phases 2-3.

**Modules:**
- WhatsApp message queue and processing (templates from MESSAGE_SPEC.md)
- WhatsApp delivery logging (pending → sent → delivered → failed → retry)
- Per-tenant WhatsApp sender configuration
- Online booking (existing patients only, produces queue ticket)
- Booking cancellation/reschedule rules
- Public SEO endpoints (clinic profile, doctors, services, hours — always 200)
- PWA notification infrastructure (medication reminders, follow-up reminders)
- Doctor-to-reception instant notes

**Exit criteria:** WhatsApp messages send on triggers (credential creation, booking, queue turn). Online booking creates queue tickets. Public endpoints return correct data for active/inactive tenants.

---

### Phase 5 — Production Readiness & Final Quality ✅ COMPLETE
**Scope:** Full codebase audit, Receptionist role support, AuditLog fix, booking workflow for staff, public endpoint hardening, comprehensive production-readiness testing.

**Why this phase:** Final quality gate ensuring all implemented features are correct, roles are properly seeded, audit trail is accurate, and the API is production-ready.

**Delivered:**
- Receptionist role seeding, staff creation support, and role validation
- AuditLog UserId fix (was incorrectly using TenantId)
- Staff booking workflow (PatientId field for booking on behalf of patients)
- Public endpoint 404 responses for invalid tenant slugs
- 105 production-readiness tests covering all modules
- Full spec-kit update to v5.0

**Exit criteria:** All test suites pass (351 total). Documentation matches implementation. Spec-kit accurate.

---

Each phase has its own `/phases/vX/` folder with UPDATE, TASKS, TESTS, and COMPLETION files.

---

*This document is the single source of truth. Updated at every phase boundary.*
