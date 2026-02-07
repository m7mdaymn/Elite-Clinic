# Phase 4 — Communication & Booking — Completion Runbook

**Status:** ✅ COMPLETE
**Date:** 2026-02-07
**Tests:** 89/89 PASS

---

## 1. Prerequisites

- .NET 9 SDK
- SQL Server (remote: `db40344.public.databaseasp.net`)
- Phase 3 fully applied and tested (99/99 PASS)

## 2. Build

```powershell
dotnet build
# Expected: 0 errors (26 pre-existing nullable reference warnings are acceptable)
```

## 3. Migration

Migration `Phase4_CommunicationBooking` is applied automatically on startup.

**New tables created:**
- `MessageLogs` — WhatsApp/PWA message queue with template validation and retry tracking
- `Bookings` — Online appointment booking with full lifecycle management
- `DoctorNotes` — Doctor-to-reception messaging with read tracking
- `NotificationSubscriptions` — PWA push notification subscription management

## 4. Run

```powershell
dotnet run --project src/EliteClinic.Api
# Server starts at http://localhost:5094
```

## 5. Seed Data (Idempotent)

Phase 4 seed data is applied automatically alongside existing phases:

- **Feature Flags:** `OnlineBooking=true`, `PwaNotifications=true` enabled for demo-clinic
- **Clinic Settings:** `BookingEnabled=true`, WhatsApp sender numbers configured
- **Bookings:** 1 confirmed booking (3 days from now) for patient_demo-clinic_1 with dr_khaled
- **Message Logs:** 2 WhatsApp messages (patient_credentials: Sent, queue_ticket_issued: Delivered)
- **Doctor Notes:** 1 unread note from dr_khaled, 1 read note from dr_mona

## 6. Test

```powershell
# Phase 4 tests (89 tests)
powershell -ExecutionPolicy Bypass -File tests/Phase4_Tests.ps1

# Phase 3 regression (99 tests)
powershell -ExecutionPolicy Bypass -File tests/Phase3_Tests.ps1

# Phase 2 regression (58 tests)
powershell -ExecutionPolicy Bypass -File tests/Phase2_Tests.ps1
```

## 7. New Endpoints (22)

### Public SEO (4 endpoints, NO auth, NO X-Tenant)
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/public/{slug}/clinic` | Public clinic profile |
| GET | `/api/public/{slug}/doctors` | Public doctor listing with services |
| GET | `/api/public/{slug}/services` | Public services listing |
| GET | `/api/public/{slug}/working-hours` | Public working hours |

### Online Booking (6 endpoints, auth required)
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/clinic/bookings` | Create booking (Patient+) |
| POST | `/api/clinic/bookings/{id}/cancel` | Cancel booking |
| POST | `/api/clinic/bookings/{id}/reschedule` | Reschedule booking |
| GET | `/api/clinic/bookings/{id}` | Get booking by ID |
| GET | `/api/clinic/bookings` | List all bookings (paginated) |
| GET | `/api/clinic/bookings/my` | Patient's own bookings |

### WhatsApp Message Queue (4 endpoints, auth required)
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/clinic/messages/send` | Send WhatsApp/PWA message |
| POST | `/api/clinic/messages/{id}/retry` | Retry failed message |
| GET | `/api/clinic/messages/{id}` | Get message by ID |
| GET | `/api/clinic/messages` | List messages (paginated/filterable) |

### Doctor Notes (4 endpoints, auth required)
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/clinic/doctor-notes` | Create note (Doctor only) |
| GET | `/api/clinic/doctor-notes/unread` | Get unread notes |
| GET | `/api/clinic/doctor-notes` | List all notes (paginated) |
| POST | `/api/clinic/doctor-notes/{id}/read` | Mark note as read |

### PWA Notifications (4 endpoints, auth required)
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/clinic/notifications/subscribe` | Subscribe to push |
| DELETE | `/api/clinic/notifications/{id}` | Unsubscribe |
| GET | `/api/clinic/notifications/my` | List my subscriptions |
| POST | `/api/clinic/notifications/send` | Send push notification |

## 8. New Entities

| Entity | Base | Fields |
|--------|------|--------|
| `Booking` | TenantBaseEntity | PatientId, DoctorId, DoctorServiceId?, BookingDate, BookingTime, Status, Notes, QueueTicketId?, CancelledAt, CancellationReason |
| `MessageLog` | TenantBaseEntity | TemplateName, RecipientPhone, RecipientUserId, Channel, Status, AttemptCount, LastAttemptAt, SentAt, DeliveredAt, FailureReason, Variables |
| `DoctorNote` | TenantBaseEntity | DoctorId, Message, IsRead, ReadAt, ReadByUserId |
| `NotificationSubscription` | TenantBaseEntity | UserId, Endpoint, P256dh, Auth, IsActive, LastUsedAt |

## 9. New Enums

| Enum | Values |
|------|--------|
| `BookingStatus` | Confirmed, Cancelled, Rescheduled, Completed |
| `MessageChannel` | WhatsApp, PWA |
| `MessageStatus` | Pending, Sending, Sent, Delivered, Read, Failed, Retrying |

## 10. Files Created/Modified

### New Files (26)
- Enums: `MessageChannel.cs`, `MessageStatus.cs`, `BookingStatus.cs`
- Entities: `MessageLog.cs`, `Booking.cs`, `DoctorNote.cs`, `NotificationSubscription.cs`
- DTOs: `MessageLogDtos.cs`, `BookingDtos.cs`, `DoctorNoteDtos.cs`, `NotificationDtos.cs`, `PublicDtos.cs`
- Services: `IPublicService.cs`, `PublicService.cs`, `IMessageService.cs`, `MessageService.cs`, `IBookingService.cs`, `BookingService.cs`, `IDoctorNoteService.cs`, `DoctorNoteService.cs`, `INotificationService.cs`, `NotificationService.cs`
- Controllers: `PublicController.cs`, `BookingsController.cs`, `MessagesController.cs`, `DoctorNotesController.cs`, `NotificationsController.cs`

### Modified Files (3)
- `EliteClinicDbContext.cs` — 4 new DbSets, entity configurations, global query filters
- `Program.cs` — 5 new DI registrations, Phase 4 seed data
- `TenantMiddleware.cs` — Added `/api/public` to public route exclusion list

## 11. Key Design Decisions

1. **Public endpoints bypass tenant middleware** — `/api/public` routes are added to both `IsPublicRoute()` and `RequiresTenant()` exclusion lists
2. **Message queue simulates sending** — No actual WhatsApp API integration; messages move to `Sent` status immediately
3. **Template validation** — Only 10 valid template names from MESSAGE_SPEC.md are accepted
4. **Booking reschedule re-confirms** — Status transitions: Confirmed → Rescheduled → Confirmed (automatic)
5. **Feature flag gating** — `OnlineBooking` flag gates all booking operations; `PwaNotifications` flag gates subscription
6. **Subscription deduplication** — Existing subscription with same endpoint is reactivated instead of creating duplicate
