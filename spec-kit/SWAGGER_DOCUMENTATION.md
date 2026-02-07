# SWAGGER_DOCUMENTATION.md — API Reference (Human-Readable)

> **Version:** 5.0  
> **Last Updated:** 2026-02-07  
> **Status:** All 5 Phases Complete (109 Endpoints)  
> **Swagger URL:** `https://{host}/swagger` (Available in ALL environments including production)

---

## GENERAL NOTES

- Swagger UI is enabled in **production** (not just development).
- All endpoints use the standard response envelope (see FRONTEND_CONTRACT.md).
- Tenant-scoped endpoints require `X-Tenant` header.
- Authenticated endpoints require `Authorization: Bearer {token}` header.
- All dates are ISO 8601 UTC.

---

## MODULE: Health

### `GET /api/health`

| Property | Value |
|----------|-------|
| **Summary** | System health check |
| **Auth** | None |
| **Headers** | None |
| **Roles** | Public |
| **Description** | Returns API status, database connectivity, and version. Used for monitoring and load balancer health probes. |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "status": "Healthy",
    "database": "Connected",
    "version": "0.0.1",
    "timestamp": "2026-02-06T10:00:00Z"
  }
}
```

---

## MODULE: Authentication

### `POST /api/auth/login`

| Property | Value |
|----------|-------|
| **Summary** | Authenticate user (SuperAdmin, ClinicOwner, ClinicManager, Doctor) |
| **Auth** | None |
| **Headers** | `Content-Type: application/json`. `X-Tenant` required for tenant users; omit for SuperAdmin. |
| **Roles** | Public (produces auth token) |
| **Description** | Validates username + password. Returns JWT + refresh token. For tenant users, X-Tenant header determines tenant scope. SuperAdmin does not send X-Tenant. |

**Request Body:**
```json
{
  "username": "string (required)",
  "password": "string (required)"
}
```

**Response 200:** JWT token, refresh token, expiry, user profile with role.  
**Response 401:** Invalid credentials.  
**Response 404:** Tenant not found (if X-Tenant is invalid).  
**Response 403:** Tenant suspended/blocked (for tenant user login).

**Notes:**
- No OTP, no email verification.
- Login attempt is logged in audit trail (success and failure).
- Failed login does NOT lock account (no lockout policy — configurable later).

---

### `POST /api/auth/patient/login`

| Property | Value |
|----------|-------|
| **Summary** | Authenticate patient |
| **Auth** | None |
| **Headers** | `Content-Type: application/json`, `X-Tenant: {slug}` (required) |
| **Roles** | Public (produces auth token) |
| **Description** | Patient-specific login. Returns long-lived token (365 days). Includes profile list for multi-profile support. |

**Request Body:**
```json
{
  "username": "string (required)",
  "password": "string (required)"
}
```

**Response 200:** JWT token (long-lived), refresh token, expiry, user profile with `profiles[]` array.  
**Response 401:** Invalid credentials.  

**Notes:**
- Token expiry: 365 days (persistent session).
- Response includes all profiles under the patient account (parent + children).
- Frontend must NEVER show logout button.

---

### `POST /api/auth/refresh`

| Property | Value |
|----------|-------|
| **Summary** | Refresh authentication token |
| **Auth** | None (uses refresh token) |
| **Headers** | `Content-Type: application/json` |
| **Roles** | Any authenticated user type |
| **Description** | Exchanges a valid refresh token for a new JWT + refresh token pair. Used for silent session renewal. |

**Request Body:**
```json
{
  "refreshToken": "string (required)"
}
```

**Response 200:** New token + refresh token pair.  
**Response 401:** Invalid or expired refresh token.

---

### `GET /api/auth/me`

| Property | Value |
|----------|-------|
| **Summary** | Get current authenticated user profile |
| **Auth** | Bearer token |
| **Headers** | `Authorization: Bearer {token}`. `X-Tenant` for tenant users. |
| **Roles** | SuperAdmin, ClinicOwner, ClinicManager, Doctor, Patient |
| **Description** | Returns the authenticated user's profile, role, tenant info, and permissions. |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "username": "string",
    "displayName": "string",
    "role": "string",
    "tenantId": "guid | null",
    "tenantSlug": "string | null",
    "permissions": ["string"]
  }
}
```

---

## MODULE: Platform — Tenant Management

### `POST /api/platform/tenants`

| Property | Value |
|----------|-------|
| **Summary** | Create a new tenant (clinic) |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |
| **Description** | Creates a new tenant with auto-generated feature flags. Slug must be unique, lowercase alphanumeric with hyphens. Tenant starts with Status=Active. |

**Request Body:**
```json
{
  "name": "string (required, max 200)",
  "slug": "string (required, max 100, regex: ^[a-z0-9\\-]+$)",
  "contactPhone": "string? (phone format)",
  "address": "string? (max 500)",
  "logoUrl": "string?"
}
```

**Response 201:** Created tenant with ID.
```json
{
  "success": true,
  "message": "Tenant created successfully",
  "data": {
    "id": "guid",
    "name": "Nile Dental",
    "slug": "nile-dental",
    "status": 0,
    "contactPhone": "+201234567890",
    "createdAt": "2026-02-07T10:00:00Z"
  },
  "meta": { "timestamp": "...", "requestId": "guid" }
}
```

**Response 400:** Validation error (duplicate slug, invalid format).  
**Response 401:** Missing/invalid token.  
**Response 403:** Non-SuperAdmin role.

**Notes:**
- Slug is immutable after creation — cannot be changed via PUT.
- Feature flags are auto-created with PLAN.md §13 defaults (see Feature Flags module).
- Slug is forced to lowercase.

---

### `GET /api/platform/tenants`

| Property | Value |
|----------|-------|
| **Summary** | List all tenants (paginated) |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |
| **Query Params** | `pageNumber` (int, default 1), `pageSize` (int, default 10), `searchTerm` (string, optional — filters by Name or Slug) |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "guid",
        "name": "Nile Dental",
        "slug": "nile-dental",
        "status": 0,
        "contactPhone": "+201234567890",
        "createdAt": "2026-02-07T10:00:00Z"
      }
    ],
    "totalCount": 8,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  },
  "meta": { "timestamp": "...", "requestId": "guid" }
}
```

**Notes:**
- Soft-deleted tenants are excluded from results.
- `searchTerm` matches against `Name` or `Slug` (contains, case-insensitive).

---

### `GET /api/platform/tenants/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Get tenant details by ID |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "name": "Nile Dental",
    "slug": "nile-dental",
    "status": 0,
    "contactPhone": "+201234567890",
    "address": "123 Main St, Cairo",
    "logoUrl": "https://example.com/logo.png",
    "createdAt": "2026-02-07T10:00:00Z",
    "updatedAt": "2026-02-07T12:00:00Z"
  },
  "meta": { "timestamp": "...", "requestId": "guid" }
}
```

**Response 404:** Tenant not found or soft-deleted.

---

### `PUT /api/platform/tenants/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update tenant details (name, contact, address, logo) |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |
| **Description** | Updates mutable fields. Slug cannot be changed. |

**Request Body:**
```json
{
  "name": "string (required, max 200)",
  "contactPhone": "string? (phone format)",
  "address": "string? (max 500)",
  "logoUrl": "string?"
}
```

**Response 200:** Updated tenant (TenantDetailDto).  
**Response 404:** Tenant not found.

---

### `POST /api/platform/tenants/{id}/activate`

| Property | Value |
|----------|-------|
| **Summary** | Set tenant status to Active |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Response 200:** `{ "success": true, "message": "Tenant activated successfully" }`  
**Response 404:** Tenant not found.

---

### `POST /api/platform/tenants/{id}/suspend`

| Property | Value |
|----------|-------|
| **Summary** | Set tenant status to Suspended |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Response 200:** `{ "success": true, "message": "Tenant suspended successfully" }`  
**Response 404:** Tenant not found.

---

### `POST /api/platform/tenants/{id}/block`

| Property | Value |
|----------|-------|
| **Summary** | Set tenant status to Blocked |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Response 200:** `{ "success": true, "message": "Tenant blocked successfully" }`  
**Response 404:** Tenant not found.

---

### `DELETE /api/platform/tenants/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Soft-delete a tenant |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |
| **Description** | Sets `IsDeleted=true` and `DeletedAt`. Tenant no longer appears in list. No physical deletion. |

**Response 200:** `{ "success": true, "message": "Tenant deleted successfully" }`  
**Response 404:** Tenant not found or already deleted.

---

## MODULE: Platform — Subscriptions

### `POST /api/platform/subscriptions`

| Property | Value |
|----------|-------|
| **Summary** | Create a subscription record for a tenant |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |
| **Description** | Manual/offline billing record. TenantId is passed in request body (not URL). |

**Request Body:**
```json
{
  "tenantId": "guid (required)",
  "planName": "string (required, max 100)",
  "startDate": "datetime (required, ISO 8601)",
  "endDate": "datetime (required, must be > startDate)",
  "amount": "decimal (required, > 0)",
  "currency": "string (required, max 10, default 'EGP')",
  "notes": "string?"
}
```

**Response 201:** Created subscription.
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "tenantId": "guid",
    "tenantName": "Nile Dental",
    "planName": "Premium Annual",
    "startDate": "2026-01-01T00:00:00",
    "endDate": "2027-01-01T00:00:00",
    "amount": 12000.00,
    "currency": "EGP",
    "isPaid": false,
    "paidAt": null,
    "paymentMethod": null,
    "status": 0,
    "cancelledAt": null,
    "cancelReason": null,
    "createdAt": "2026-02-07T10:00:00Z"
  },
  "meta": { "timestamp": "...", "requestId": "guid" }
}
```

**Response 400:** Validation error (tenant not found, EndDate < StartDate, missing fields).

**Notes:**
- Defaults: `Status=Active (0)`, `IsPaid=false`.
- Tenant must exist and not be soft-deleted.
- ⚠️ **Known issue (SV03):** `StartDate` is a `DateTime` value type. If omitted from JSON, it defaults to `0001-01-01` instead of returning 400. Fix planned for Phase 2.

---

### `GET /api/platform/subscriptions`

| Property | Value |
|----------|-------|
| **Summary** | List subscriptions (paginated, optionally filtered by tenant) |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |
| **Query Params** | `pageNumber` (int), `pageSize` (int), `tenantId` (guid, optional filter) |

**Response 200:** Paginated list of `SubscriptionDto` items.

---

### `POST /api/platform/subscriptions/{id}/extend`

| Property | Value |
|----------|-------|
| **Summary** | Extend a subscription's end date |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Request Body:**
```json
{
  "newEndDate": "datetime (required, ISO 8601)",
  "notes": "string?"
}
```

**Response 200:** Updated subscription.  
**Response 400:** Subscription not found, or subscription is cancelled (cannot extend cancelled).

---

### `POST /api/platform/subscriptions/{id}/cancel`

| Property | Value |
|----------|-------|
| **Summary** | Cancel a subscription |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Request Body:**
```json
{
  "cancelReason": "string (required, max 500)"
}
```

**Response 200:** Cancelled subscription (Status=2, CancelledAt set).  
**Response 400:** Subscription not found, or already cancelled.

---

### `POST /api/platform/subscriptions/{id}/mark-paid`

| Property | Value |
|----------|-------|
| **Summary** | Record payment for a subscription |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Request Body:**
```json
{
  "paymentMethod": "string (required, max 100)",
  "paymentReference": "string? (max 200)",
  "paidAt": "datetime? (defaults to now if omitted)"
}
```

**Response 200:** Updated subscription (IsPaid=true, PaidAt set).  
**Response 400:** Subscription not found or already paid.

---

## MODULE: Platform — Feature Flags

### `GET /api/platform/feature-flags/{tenantId}`

| Property | Value |
|----------|-------|
| **Summary** | Get feature flags for a tenant |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "tenantId": "guid",
    "onlineBooking": false,
    "whatsappAutomation": true,
    "pwaNotifications": false,
    "expensesModule": true,
    "advancedMedicalTemplates": false,
    "ratings": false,
    "export": false
  },
  "meta": { "timestamp": "...", "requestId": "guid" }
}
```

**Response 404:** Tenant or flags not found.

**Notes:**
- Flags are auto-created when a tenant is created, with defaults from PLAN.md §13.

---

### `PUT /api/platform/feature-flags/{tenantId}`

| Property | Value |
|----------|-------|
| **Summary** | Update all feature flags for a tenant |
| **Auth** | Bearer token |
| **Roles** | SuperAdmin |
| **Description** | Replaces all 7 flags. No partial update — all fields required. |

**Request Body:**
```json
{
  "onlineBooking": "bool (required)",
  "whatsappAutomation": "bool (required)",
  "pwaNotifications": "bool (required)",
  "expensesModule": "bool (required)",
  "advancedMedicalTemplates": "bool (required)",
  "ratings": "bool (required)",
  "export": "bool (required)"
}
```

**Response 200:** Updated flags (FeatureFlagDto).  
**Response 400:** Tenant not found.

**Notes:**
- No partial update — all 7 boolean fields must be provided.
- Sending `true` for a flag that is already `true` is a no-op (HTTP 200, no error).

---

## MODULE: Clinic Settings

### `GET /api/clinic/settings`

| Property | Value |
|----------|-------|
| **Summary** | Get clinic configuration |
| **Auth** | Bearer token |
| **Headers** | `X-Tenant: {slug}` (required) |
| **Roles** | ClinicOwner, ClinicManager, Doctor (read-only for all) |
| **Description** | Returns clinic name, phone numbers, address, working hours, booking settings. Tenant-scoped. |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "tenantId": "guid",
    "clinicName": "Demo Dental Clinic",
    "phone": "+201000000099",
    "address": "123 Main St",
    "city": "Cairo",
    "bookingEnabled": true,
    "cancellationWindowHours": 2,
    "workingHours": [
      { "id": "guid", "dayOfWeek": 0, "startTime": "09:00:00", "endTime": "17:00:00", "isActive": true }
    ]
  }
}
```

---

### `PUT /api/clinic/settings`

| Property | Value |
|----------|-------|
| **Summary** | Update clinic configuration |
| **Auth** | Bearer token |
| **Headers** | `X-Tenant: {slug}` (required) |
| **Roles** | ClinicOwner |
| **Description** | Update all clinic settings including working hours. ClinicManager cannot write. |

**Request Body:**
```json
{
  "clinicName": "string (required, max 200)",
  "phone": "string? (max 20)",
  "address": "string?",
  "city": "string? (max 100)",
  "bookingEnabled": "bool",
  "cancellationWindowHours": "int (0-168, default 2)",
  "workingHours": [
    {
      "dayOfWeek": "int (0-6: Sun-Sat)",
      "startTime": "string (HH:MM:SS)",
      "endTime": "string (HH:MM:SS)",
      "isActive": "bool (default true)"
    }
  ]
}
```

**Response 200:** Updated ClinicSettingsDto.  
**Response 403:** ClinicManager or Staff cannot write.

---

## MODULE: Staff Management

### `POST /api/clinic/staff`

| Property | Value |
|----------|-------|
| **Summary** | Create staff member |
| **Auth** | Bearer token |
| **Headers** | `X-Tenant: {slug}` (required) |
| **Roles** | ClinicOwner |
| **Description** | Creates ApplicationUser with ClinicManager role + Employee entity. Auto-generates username if not provided. |

**Request Body:**
```json
{
  "name": "string (required, max 200)",
  "username": "string (required, max 50)",
  "password": "string (required, 6-100 chars)",
  "phone": "string? (max 20)",
  "salary": "decimal?",
  "notes": "string?"
}
```

**Response 201:** StaffDto with user details.

---

### `GET /api/clinic/staff`

| Property | Value |
|----------|-------|
| **Summary** | List all staff members (paginated) |
| **Auth** | Bearer token |
| **Headers** | `X-Tenant: {slug}` (required) |
| **Roles** | ClinicOwner, ClinicManager |
| **Description** | Returns paginated list of staff. Tenant-scoped. |

**Query Params:**
- `pageNumber` (default 1)
- `pageSize` (default 10, max 100)

**Response 200:** PagedResult<StaffDto>.

---

### `GET /api/clinic/staff/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Get staff details by ID |
| **Auth** | Bearer token |
| **Headers** | `X-Tenant: {slug}` (required) |
| **Roles** | ClinicOwner |
| **Description** | Returns full staff profile. |

**Response 200:** StaffDto.  
**Response 404:** Staff not found.

---

### `PUT /api/clinic/staff/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update staff profile |
| **Auth** | Bearer token |
| **Headers** | `X-Tenant: {slug}` (required) |
| **Roles** | ClinicOwner |
| **Description** | Update name, phone, salary, note. |

**Request Body:**
```json
{
  "name": "string (required, max 200)",
  "phone": "string? (max 20)",
  "salary": "decimal?",
  "notes": "string?"
}
```

**Response 200:** Updated StaffDto.

---

### `POST /api/clinic/staff/{id}/enable`

| Property | Value |
|----------|-------|
| **Summary** | Enable staff access |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |
| **Description** | Re-enable staff account for login. |

**Response 200:** Updated StaffDto with isEnabled=true.

---

### `POST /api/clinic/staff/{id}/disable`

| Property | Value |
|----------|-------|
| **Summary** | Disable staff access |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |
| **Description** | Disable staff login without deleting account. |

**Response 200:** Updated StaffDto with isEnabled=false.

---

## MODULE: Doctor Management

### `POST /api/clinic/doctors`

| Property | Value |
|----------|-------|
| **Summary** | Create doctor |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |
| **Description** | Creates ApplicationUser with Doctor role + Doctor entity. |

**Request Body:**
```json
{
  "name": "string (required, max 200)",
  "username": "string (required, max 50)",
  "password": "string (required, 6-100 chars)",
  "specialty": "string? (max 100)",
  "phone": "string? (max 20)",
  "urgentCaseMode": "int (0=UrgentNext, 1=UrgentBucket, 2=UrgentFront, default 0)",
  "avgVisitDurationMinutes": "int (1-120, default 15)"
}
```

**Response 201:** DoctorDto.

---

### `GET /api/clinic/doctors`

| Property | Value |
|----------|-------|
| **Summary** | List all doctors (paginated) |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner, ClinicManager, Doctor |
| **Description** | Tenant-scoped list. |

**Query Params:** `pageNumber`, `pageSize`

**Response 200:** PagedResult<DoctorDto>.

---

### `GET /api/clinic/doctors/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Get doctor with services and visit field config |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |
| **Description** | Full doctor profile including services array and visitor field toggles. |

**Response 200:** DoctorDto with nested services and visitFieldConfig.

---

### `PUT /api/clinic/doctors/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update doctor profile |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |

**Request Body:**
```json
{
  "name": "string",
  "specialty": "string?",
  "phone": "string?",
  "urgentCaseMode": "int",
  "avgVisitDurationMinutes": "int"
}
```

**Response 200:** Updated DoctorDto.

---

### `PUT /api/clinic/doctors/{id}/services`

| Property | Value |
|----------|-------|
| **Summary** | Configure doctor services and pricing |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |
| **Description** | Replace all services (not an append operation). Each service has name, price, duration. |

**Request Body:**
```json
{
  "services": [
    {
      "serviceName": "string (required, max 200)",
      "price": "decimal (≥0)",
      "durationMinutes": "int? (1-480)",
      "isActive": "bool (default true)"
    }
  ]
}
```

**Response 200:** List<DoctorServiceDto>.

---

### `PUT /api/clinic/doctors/{id}/visit-fields`

| Property | Value |
|----------|-------|
| **Summary** | Configure which vitals are required for this doctor |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |
| **Description** | Toggle blood pressure, temperature, weight, height, BMI, etc. for visits with this doctor. |

**Request Body:**
```json
{
  "bloodPressure": "bool",
  "heartRate": "bool",
  "temperature": "bool",
  "weight": "bool",
  "height": "bool",
  "bmi": "bool",
  "bloodSugar": "bool",
  "oxygenSaturation": "bool",
  "respiratoryRate": "bool"
}
```

**Response 200:** DoctorVisitFieldConfigDto.

---

### `POST /api/clinic/doctors/{id}/enable` / `disable`

| Property | Value |
|----------|-------|
| **Summary** | Enable / disable doctor (similar to staff) |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner |

---

## MODULE: Patient Management

### `POST /api/clinic/patients`

| Property | Value |
|----------|-------|
| **Summary** | Register new patient (walk-in) |
| **Auth** | Bearer token |
| **Headers** | `X-Tenant: {slug}` (required) |
| **Roles** | ClinicOwner, ClinicManager |
| **Description** | Creates ApplicationUser with Patient role + Patient entity. Returns auto-generated credentials. |

**Request Body:**
```json
{
  "name": "string (required, max 200)",
  "phone": "string (required, max 20)",
  "dateOfBirth": "datetime?",
  "gender": "int (0=Male, 1=Female, default 0)",
  "address": "string?",
  "notes": "string?"
}
```

**Response 201:** CreatePatientResponse (patient object + username + password).

---

### `GET /api/clinic/patients`

| Property | Value |
|----------|-------|
| **Summary** | List all patients (paginated, searchable) |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner, ClinicManager |

**Query Params:**
- `pageNumber` (default 1)
- `pageSize` (default 10)
- `search` (optional, filters by name)

**Response 200:** PagedResult<PatientDto>.

---

### `GET /api/clinic/patients/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Get patient with sub-profiles |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner, ClinicManager |
| **Description** | Returns patient with all sub-profiles (children under same account). |

**Response 200:** PatientDto (includes subProfiles array).

---

### `PUT /api/clinic/patients/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update patient profile |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner, ClinicManager |

**Request Body:**
```json
{
  "name": "string",
  "phone": "string",
  "dateOfBirth": "datetime?",
  "gender": "int",
  "address": "string?",
  "notes": "string?"
}
```

**Response 200:** Updated PatientDto.

---

### `POST /api/clinic/patients/{id}/profiles`

| Property | Value |
|----------|-------|
| **Summary** | Add sub-profile (child/dependent) |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner, ClinicManager |
| **Description** | Add a secondary profile (e.g., child) under same account. |

**Request Body:**
```json
{
  "name": "string (required)",
  "phone": "string (required)",
  "dateOfBirth": "datetime?",
  "gender": "int"
}
```

**Response 201:** PatientDto with updated subProfiles array.

---

### `POST /api/clinic/patients/{id}/reset-password`

| Property | Value |
|----------|-------|
| **Summary** | Generate new patient password |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner, ClinicManager |
| **Description** | Staff-initiated password reset. Returns new password. |

**Response 200:** ResetPasswordResponse (newPassword).

---

### `DELETE /api/clinic/patients/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Soft-delete patient |
| **Auth** | Bearer token |
| **Roles** | ClinicOwner (not ClinicManager) |
| **Description** | Mark patient as deleted (soft delete). Not returned in list. |

**Response 200:** success.

---

## MODULE: Authentication (Phase 2 Enhancements)

### `POST /api/auth/login` (ENHANCED)

| Property | Value |
|----------|-------|
| **Summary** | [Updated] Staff/Doctor login with tenant scope |
| **Auth** | None |
| **Headers** | `Content-Type: application/json`, `X-Tenant: {slug}` (required for staff/doctor, optional for SuperAdmin) |
| **Description** | **Phase 2 enhancement:** Tenant users must include X-Tenant header. SuperAdmin may omit. Response includes `tenantSlug` and `permissions` array for tenant users. |

**Response 200 (staff/doctor):**
```json
{
  "success": true,
  "data": {
    "token": "eyJ0eXAi...",
    "refreshToken": "...",
    "expiresAt": "2026-02-08T08:00:00Z",
    "user": {
      "id": "guid",
      "username": "staff_sara",
      "displayName": "Sara Ali",
      "role": "ClinicManager",
      "tenantId": "guid",
      "tenantSlug": "demo-clinic",
      "permissions": [ "clinic:read", "patient:create", "patient:write" ]
    }
  }
}
```

---

### `POST /api/auth/patient/login` (NEW)

| Property | Value |
|----------|-------|
| **Summary** | [NEW] Patient login endpoint |
| **Auth** | None |
| **Headers** | `Content-Type: application/json`, `X-Tenant: {slug}` (required) |
| **Roles** | Public (patient login only) |
| **Description** | Patient-only login. Returns long-lived token (365 days). Includes profiles array for multi-profile support. |

**Request Body:**
```json
{
  "username": "string (required)",
  "password": "string (required)"
}
```

**Response 200:**
```json
{
  "success": true,
  "data": {
    "token": "eyJ0eXAi...",
    "refreshToken": "...",
    "expiresAt": "2027-02-08T08:00:00Z",
    "user": {
      "id": "guid",
      "username": "patient_demo-clinic_1",
      "displayName": "Mohamed Ali",
      "role": "Patient",
      "tenantId": "guid",
      "profiles": [
        { "id": "guid", "name": "Mohamed Ali", "isDefault": true },
        { "id": "guid", "name": "Child of Mohamed", "isDefault": false }
      ]
    }
  }
}
```

**Notes:**
- Token lifetime: 365 days (vs 8 hours for staff/doctor).
- No `tenantSlug` in response (patients belong to one clinic).
- `profiles[]` array allows UI to switch between parent and sub-profiles.

---

### `GET /api/auth/me` (ENHANCED)

| Property | Value |
|----------|-------|
| **Summary** | [Updated] Get current user info with tenant context |
| **Auth** | Bearer token |
| **Description** | **Phase 2 enhancement:** Now includes `tenantSlug` and `permissions` for tenant users. SuperAdmin returns these as null. |

**Response 200 (staff):**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "username": "staff_sara",
    "displayName": "Sara Ali",
    "role": "ClinicManager",
    "tenantId": "guid",
    "tenantSlug": "demo-clinic",
    "permissions": [ "clinic:read", "patient:create", ... ]
  }
}
```

---

## MODULE: Queue Sessions

### `POST /api/clinic/queue/sessions`

| Property | Value |
|----------|-------|
| **Summary** | Open a new queue session (doctor's shift) |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Starts a doctor's shift, begins accepting patients. Only one active session per doctor allowed. |

**Request Body:**
```json
{
  "doctorId": "guid (optional, auto-resolved for Doctor role)",
  "notes": "string?"
}
```

**Response 201:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "doctorId": "guid",
    "doctorName": "Dr. Khaled",
    "startedAt": "2026-02-08T09:00:00Z",
    "closedAt": null,
    "isActive": true,
    "notes": null,
    "totalTickets": 0,
    "waitingCount": 0,
    "completedCount": 0,
    "createdAt": "2026-02-08T09:00:00Z"
  }
}
```

---

### `POST /api/clinic/queue/sessions/{id}/close`

| Property | Value |
|----------|-------|
| **Summary** | Close a queue session |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Ends the doctor's shift. Remaining Waiting/Called tickets automatically become NoShow. |

**Response 200:** `ApiResponse<QueueSessionDto>` with `isActive: false`

---

### `GET /api/clinic/queue/sessions`

| Property | Value |
|----------|-------|
| **Summary** | List all queue sessions (paginated) |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Query** | `pageNumber` (default 1), `pageSize` (default 20) |

**Response 200:** `ApiResponse<PagedResult<QueueSessionDto>>`

---

### `GET /api/clinic/queue/sessions/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Get session by ID with ticket summary |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |

**Response 200:** `ApiResponse<QueueSessionDto>`

---

### `GET /api/clinic/queue/sessions/{id}/tickets`

| Property | Value |
|----------|-------|
| **Summary** | Get all tickets for a session |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Returns tickets ordered: urgent first, then by issued time. |

**Response 200:** `ApiResponse<List<QueueTicketDto>>`

---

## MODULE: Queue Tickets

### `POST /api/clinic/queue/tickets`

| Property | Value |
|----------|-------|
| **Summary** | Issue a ticket to a patient (reception) |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Description** | Reception issues ticket to patient for a doctor session. One active ticket per patient per session. |

**Request Body:**
```json
{
  "sessionId": "guid",
  "patientId": "guid",
  "doctorId": "guid",
  "doctorServiceId": "guid? (optional)",
  "notes": "string?"
}
```

**Response 201:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "sessionId": "guid",
    "patientId": "guid",
    "patientName": "Mohamed Hassan",
    "doctorId": "guid",
    "doctorName": "Dr. Khaled",
    "ticketNumber": 1,
    "status": "Waiting",
    "isUrgent": false,
    "issuedAt": "2026-02-08T09:05:00Z",
    "calledAt": null,
    "visitStartedAt": null,
    "completedAt": null,
    "notes": null
  }
}
```

---

### `POST /api/clinic/queue/tickets/{id}/call`

| Property | Value |
|----------|-------|
| **Summary** | Call next patient (Waiting → Called) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |

---

### `POST /api/clinic/queue/tickets/{id}/start-visit`

| Property | Value |
|----------|-------|
| **Summary** | Start visit from ticket (Called → InVisit) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Transitions ticket to InVisit and auto-creates a Visit entity. |

---

### `POST /api/clinic/queue/tickets/{id}/finish`

| Property | Value |
|----------|-------|
| **Summary** | Finish ticket (InVisit → Completed) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Marks ticket and linked visit as completed. |

---

### `POST /api/clinic/queue/tickets/{id}/skip`

| Property | Value |
|----------|-------|
| **Summary** | Skip ticket (Called → Skipped) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Patient didn't answer when called. Can be re-called later. |

---

### `POST /api/clinic/queue/tickets/{id}/cancel`

| Property | Value |
|----------|-------|
| **Summary** | Cancel ticket |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |

---

### `POST /api/clinic/queue/tickets/{id}/urgent`

| Property | Value |
|----------|-------|
| **Summary** | Mark ticket as urgent |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Elevates ticket priority. Urgent tickets appear first in queue. |

---

## MODULE: Queue Board

### `GET /api/clinic/queue/board`

| Property | Value |
|----------|-------|
| **Summary** | Reception board — all active sessions with ticket counts |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Description** | Returns today's active sessions with waiting/called/in-visit/completed ticket counts and current ticket info. |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "sessions": [
      {
        "sessionId": "guid",
        "doctorName": "Dr. Khaled",
        "isActive": true,
        "waitingCount": 3,
        "calledCount": 0,
        "inVisitCount": 1,
        "completedCount": 5,
        "currentTicket": { ... },
        "waitingTickets": [ ... ]
      }
    ]
  }
}
```

---

### `GET /api/clinic/queue/my-queue`

| Property | Value |
|----------|-------|
| **Summary** | Doctor's own queue |
| **Auth** | Bearer Token |
| **Roles** | Doctor, SuperAdmin |
| **Description** | Shows the logged-in doctor's active session with all tickets. |

**Response 200:** `ApiResponse<QueueBoardSessionDto>`

---

### `GET /api/clinic/queue/my-ticket`

| Property | Value |
|----------|-------|
| **Summary** | Patient's active ticket status |
| **Auth** | Bearer Token |
| **Roles** | Patient, SuperAdmin |
| **Description** | Returns the patient's current active ticket (Waiting/Called/InVisit). |

**Response 200:** `ApiResponse<QueueTicketDto>`  
**Response 404:** No active ticket found

---

## MODULE: Visits

### `POST /api/clinic/visits`

| Property | Value |
|----------|-------|
| **Summary** | Create a visit manually (no ticket required) |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, Doctor, SuperAdmin |
| **Description** | Creates a visit without a queue ticket. Supports walk-in patients or ad-hoc consultations. Can also link to an existing ticket via `queueTicketId`. |

**Request Body:**
```json
{
  "queueTicketId": "guid? (optional)",
  "doctorId": "guid",
  "patientId": "guid",
  "complaint": "string?",
  "notes": "string?"
}
```

**Response 201:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "queueTicketId": null,
    "doctorId": "guid",
    "doctorName": "Dr. Mona",
    "patientId": "guid",
    "patientName": "Ahmed Ali",
    "status": "Open",
    "complaint": "Headache",
    "diagnosis": null,
    "bloodPressureSystolic": null,
    "bloodPressureDiastolic": null,
    "heartRate": null,
    "temperature": null,
    "weight": null,
    "height": null,
    "prescriptions": [],
    "labRequests": [],
    "invoice": null,
    "startedAt": "2026-02-08T10:00:00Z",
    "completedAt": null
  }
}
```

---

### `PUT /api/clinic/visits/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update visit (complaint, vitals, diagnosis, notes) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |
| **Description** | Only updatable while visit status is Open. |

**Request Body:**
```json
{
  "complaint": "string?",
  "diagnosis": "string?",
  "notes": "string?",
  "bloodPressureSystolic": 120,
  "bloodPressureDiastolic": 80,
  "heartRate": 75,
  "temperature": 37.0,
  "weight": 70.5,
  "height": 175.0,
  "bmi": 23.0,
  "bloodSugar": 90.0,
  "oxygenSaturation": 98.0,
  "respiratoryRate": 16,
  "followUpDate": "2026-02-15T00:00:00Z"
}
```

---

### `POST /api/clinic/visits/{id}/complete`

| Property | Value |
|----------|-------|
| **Summary** | Complete a visit |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |
| **Description** | Marks visit as Completed. Also completes linked ticket if present. |

**Request Body:**
```json
{
  "diagnosis": "string?",
  "notes": "string?"
}
```

---

### `GET /api/clinic/visits/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Get visit by ID with nested data |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |
| **Description** | Returns full visit with prescriptions, lab requests, and invoice. |

**Response 200:** `ApiResponse<VisitDto>` (includes `prescriptions[]`, `labRequests[]`, `invoice?`)

---

### `GET /api/clinic/patients/{patientId}/visits`

| Property | Value |
|----------|-------|
| **Summary** | Get patient visit history (paginated) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |
| **Query** | `pageNumber`, `pageSize` |

**Response 200:** `ApiResponse<PagedResult<VisitDto>>`

---

### `GET /api/clinic/patients/{patientId}/summary`

| Property | Value |
|----------|-------|
| **Summary** | Patient summary for doctor view |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |
| **Description** | Quick patient overview: info + total visits + last 5 visits. |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "patientId": "guid",
    "name": "Mohamed Hassan",
    "phone": "+201500000001",
    "dateOfBirth": "1990-01-15",
    "gender": "Male",
    "totalVisits": 12,
    "recentVisits": [
      {
        "id": "guid",
        "doctorName": "Dr. Khaled",
        "complaint": "Headache",
        "diagnosis": "Tension headache",
        "startedAt": "2026-02-08T10:00:00Z",
        "completedAt": "2026-02-08T10:30:00Z"
      }
    ]
  }
}
```

---

## MODULE: Prescriptions

### `POST /api/clinic/visits/{visitId}/prescriptions`

| Property | Value |
|----------|-------|
| **Summary** | Add prescription to visit |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |

**Request Body:**
```json
{
  "medicationName": "Amoxicillin 500mg",
  "dosage": "1 capsule",
  "frequency": "3 times daily",
  "duration": "7 days",
  "instructions": "Take after meals"
}
```

**Response 201:** `ApiResponse<PrescriptionDto>`

---

### `PUT /api/clinic/visits/{visitId}/prescriptions/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update prescription (same-day only) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |

---

### `DELETE /api/clinic/visits/{visitId}/prescriptions/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Delete prescription (same-day only) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |

---

### `GET /api/clinic/visits/{visitId}/prescriptions`

| Property | Value |
|----------|-------|
| **Summary** | List all prescriptions for a visit |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |

**Response 200:** `ApiResponse<List<PrescriptionDto>>`

---

## MODULE: Lab/Imaging Requests

### `POST /api/clinic/visits/{visitId}/labs`

| Property | Value |
|----------|-------|
| **Summary** | Add lab/imaging request to visit |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |

**Request Body:**
```json
{
  "testName": "CBC - Complete Blood Count",
  "type": "Lab",
  "notes": "Fasting required",
  "isUrgent": false
}
```

**Response 201:** `ApiResponse<LabRequestDto>`

---

### `PUT /api/clinic/visits/{visitId}/labs/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update lab/imaging request (same-day only) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |

---

### `POST /api/clinic/visits/{visitId}/labs/{id}/result`

| Property | Value |
|----------|-------|
| **Summary** | Add result to a lab/imaging request |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Description** | Usually entered by staff/manager when results arrive from the lab. |

**Request Body:**
```json
{
  "resultText": "WBC: 7.5, RBC: 4.8, Hemoglobin: 14.2..."
}
```

---

### `GET /api/clinic/visits/{visitId}/labs`

| Property | Value |
|----------|-------|
| **Summary** | List all lab/imaging requests for a visit |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, Doctor, SuperAdmin |

**Response 200:** `ApiResponse<List<LabRequestDto>>`

---

## MODULE: Invoices & Payments

### `POST /api/clinic/invoices`

| Property | Value |
|----------|-------|
| **Summary** | Create an invoice for a visit |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Description** | One invoice per visit. Invoice amount can be updated while visit is Open. |

**Request Body:**
```json
{
  "visitId": "guid",
  "amount": 500.00,
  "notes": "Consultation + X-ray"
}
```

**Response 201:**
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "visitId": "guid",
    "patientId": "guid",
    "patientName": "Mohamed Hassan",
    "doctorId": "guid",
    "doctorName": "Dr. Khaled",
    "amount": 500.00,
    "paidAmount": 0.00,
    "remainingAmount": 500.00,
    "status": "Unpaid",
    "notes": "Consultation + X-ray",
    "payments": [],
    "createdAt": "2026-02-08T10:30:00Z"
  }
}
```

---

### `PUT /api/clinic/invoices/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update invoice amount/notes |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Description** | Cannot reduce amount below already-paid amount. |

---

### `GET /api/clinic/invoices/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Get invoice by ID with payments |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |

---

### `GET /api/clinic/invoices`

| Property | Value |
|----------|-------|
| **Summary** | List invoices (filterable) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Query** | `from`, `to`, `doctorId`, `pageNumber`, `pageSize` |

---

### `POST /api/clinic/payments`

| Property | Value |
|----------|-------|
| **Summary** | Record a payment against an invoice |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Description** | Partial payments supported. Invoice status auto-transitions: Unpaid → PartiallyPaid → Paid. Cannot overpay (exceed remaining amount). |

**Request Body:**
```json
{
  "invoiceId": "guid",
  "amount": 200.00,
  "paymentMethod": "Cash",
  "referenceNumber": "REC-001",
  "notes": "Partial payment"
}
```

**Response 201:** `ApiResponse<PaymentDto>`

---

### `GET /api/clinic/invoices/{id}/payments`

| Property | Value |
|----------|-------|
| **Summary** | Get all payments for an invoice |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |

**Response 200:** `ApiResponse<List<PaymentDto>>`

---

## MODULE: Expenses

### `POST /api/clinic/expenses`

| Property | Value |
|----------|-------|
| **Summary** | Add expense |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |

**Request Body:**
```json
{
  "category": "Medical Supplies",
  "amount": 1500.00,
  "notes": "Gloves, syringes, bandages",
  "expenseDate": "2026-02-08T00:00:00Z"
}
```

**Response 201:** `ApiResponse<ExpenseDto>`

---

### `PUT /api/clinic/expenses/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Update expense |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |

---

### `DELETE /api/clinic/expenses/{id}`

| Property | Value |
|----------|-------|
| **Summary** | Delete expense |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, SuperAdmin |

---

### `GET /api/clinic/expenses`

| Property | Value |
|----------|-------|
| **Summary** | List expenses (filterable) |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Query** | `from`, `to`, `category`, `pageNumber`, `pageSize` |

**Response 200:** `ApiResponse<PagedResult<ExpenseDto>>`

---

## MODULE: Finance Reports

### `GET /api/clinic/finance/daily`

| Property | Value |
|----------|-------|
| **Summary** | Daily revenue summary |
| **Auth** | Bearer Token |
| **Headers** | `Authorization`, `X-Tenant` |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Query** | `date` (optional, defaults to today) |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "date": "2026-02-08",
    "totalRevenue": 5000.00,
    "totalPaid": 3500.00,
    "totalUnpaid": 1500.00,
    "invoiceCount": 12,
    "paymentCount": 10
  }
}
```

---

### `GET /api/clinic/finance/by-doctor`

| Property | Value |
|----------|-------|
| **Summary** | Revenue breakdown by doctor |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Query** | `date` (optional), `doctorId` (optional, filter to single doctor) |

**Response 200:**
```json
{
  "success": true,
  "data": [
    {
      "doctorId": "guid",
      "doctorName": "Dr. Khaled",
      "totalRevenue": 3000.00,
      "totalPaid": 2500.00,
      "visitCount": 8
    }
  ]
}
```

---

### `GET /api/clinic/finance/monthly`

| Property | Value |
|----------|-------|
| **Summary** | Monthly revenue summary with expenses |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, SuperAdmin |
| **Query** | `year` (optional), `month` (optional) |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "year": 2026,
    "month": 2,
    "totalRevenue": 50000.00,
    "totalPaid": 42000.00,
    "totalExpenses": 15000.00,
    "netProfit": 27000.00,
    "invoiceCount": 120
  }
}
```

---

### `GET /api/clinic/finance/yearly`

| Property | Value |
|----------|-------|
| **Summary** | Yearly revenue summary with monthly breakdown |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, SuperAdmin |
| **Query** | `year` (optional, defaults to current year) |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "year": 2026,
    "totalRevenue": 600000.00,
    "totalPaid": 520000.00,
    "totalExpenses": 180000.00,
    "netProfit": 340000.00,
    "invoiceCount": 1440,
    "months": [ ... ]
  }
}
```

---

### `GET /api/clinic/finance/profit`

| Property | Value |
|----------|-------|
| **Summary** | Profit report for a date range |
| **Auth** | Bearer Token |
| **Roles** | ClinicOwner, ClinicManager, SuperAdmin |
| **Query** | `from` (optional), `to` (optional) |

**Response 200:**
```json
{
  "success": true,
  "data": {
    "from": "2026-02-01",
    "to": "2026-02-28",
    "totalRevenue": 50000.00,
    "totalPaid": 42000.00,
    "totalExpenses": 15000.00,
    "netProfit": 27000.00,
    "invoiceCount": 120,
    "expenseCount": 25,
    "byDoctor": [ ... ]
  }
}
```

---

## ENUMS (Phase 3)

### TicketStatus
| Value | Description |
|-------|-------------|
| `Waiting` | Patient in queue, waiting to be called |
| `Called` | Doctor has called the patient |
| `InVisit` | Patient is with the doctor |
| `Completed` | Visit finished |
| `Skipped` | Patient didn't respond when called |
| `NoShow` | Session closed, patient never seen |
| `Cancelled` | Ticket was cancelled |

### VisitStatus
| Value | Description |
|-------|-------------|
| `Open` | Visit in progress |
| `Completed` | Visit finished |

### InvoiceStatus
| Value | Description |
|-------|-------------|
| `Unpaid` | No payments recorded |
| `PartiallyPaid` | Some payment received, balance remaining |
| `Paid` | Fully paid, remaining = 0 |

### LabRequestType
| Value | Description |
|-------|-------------|
| `Lab` | Laboratory test (blood, urine, etc.) |
| `Imaging` | Imaging study (X-ray, MRI, CT, etc.) |

---

## MODULE: Public SEO (Phase 4)

> **Base:** `/api/public`  
> **Auth:** None required  
> **X-Tenant:** Not required  
> **Purpose:** Public-facing endpoints for clinic profiles, SEO, and patient-facing booking pages

### GET `/api/public/{slug}/clinic`
- **Description:** Get public clinic profile by tenant slug
- **Response 200:** Clinic profile data
- **Response 404:** Returns `{"success": false, "message": "Clinic not found"}` when slug is invalid or tenant not found
- **Response:**
```json
{
  "success": true,
  "data": {
    "clinicName": "Demo Clinic",
    "tenantSlug": "demo-clinic",
    "address": "123 Main St",
    "phone": "+201234567890",
    "logoUrl": null,
    "isActive": true,
    "bookingEnabled": true
  }
}
```

### GET `/api/public/{slug}/doctors`
- **Description:** List active doctors for a clinic (public)
- **Response 200:** `data` is array of `PublicDoctorDto` with name, specialty, bio, photoUrl, services[]
- **Response 404:** Clinic not found

### GET `/api/public/{slug}/services`
- **Description:** List active services for a clinic (public)
- **Response 200:** `data` is array of `PublicDoctorServiceDto` with serviceName, price, durationMinutes, doctorName
- **Response 404:** Clinic not found

### GET `/api/public/{slug}/working-hours`
- **Description:** Get working hours for a clinic (public)
- **Response 200:** `data` is array of `PublicWorkingHourDto` with dayOfWeek, startTime, endTime, isOpen
- **Response 404:** Clinic not found

---

## MODULE: Online Booking (Phase 4)

> **Base:** `/api/clinic/bookings`  
> **Auth:** JWT required  
> **X-Tenant:** Required  
> **Purpose:** Online appointment booking with full lifecycle

### POST `/api/clinic/bookings`
- **Roles:** Patient, ClinicOwner, ClinicManager, Receptionist, SuperAdmin
- **Feature Flag:** `OnlineBooking` must be enabled
- **Request:**
```json
{
  "doctorId": "guid",
  "doctorServiceId": "guid (optional)",
  "patientId": "guid (optional, for staff booking on behalf of patient)",
  "bookingDate": "2026-03-01",
  "bookingTime": "09:00",
  "notes": "optional"
}
```
- **Validations:** Future date, doctor exists, no duplicate (same doctor+date+time), booking enabled in clinic settings. `patientId` is required for staff/manager/receptionist workflows.
- **Response:** `BookingDto` with status `Confirmed`

### POST `/api/clinic/bookings/{id}/cancel`
- **Request:** `{ "cancellationReason": "string" }`
- **Validations:** Only `Confirmed` bookings, within cancellation window
- **Response:** `BookingDto` with status `Cancelled`

### POST `/api/clinic/bookings/{id}/reschedule`
- **Request:** `{ "bookingDate": "2026-03-05", "bookingTime": "11:00" }`
- **Validations:** Only `Confirmed` bookings, new time must be future
- **Response:** `BookingDto` with status `Confirmed` (re-confirmed after reschedule)

### GET `/api/clinic/bookings/{id}`
- **Response:** Single `BookingDto`

### GET `/api/clinic/bookings`
- **Roles:** ClinicOwner, ClinicManager, Receptionist, Doctor, SuperAdmin
- **Query:** `?pageNumber=1&pageSize=10&doctorId=guid&status=Confirmed`
- **Response:** `PagedResult<BookingDto>`

### GET `/api/clinic/bookings/my`
- **Roles:** Patient
- **Response:** Array of `BookingDto` for the authenticated patient

### BookingDto
```json
{
  "id": "guid",
  "patientId": "guid",
  "patientName": "string",
  "patientPhone": "string",
  "doctorId": "guid",
  "doctorName": "string",
  "doctorServiceId": "guid | null",
  "serviceName": "string | null",
  "bookingDate": "2026-03-01T00:00:00",
  "bookingTime": "09:00",
  "status": "Confirmed",
  "notes": "string",
  "queueTicketId": "guid | null",
  "cancelledAt": "datetime | null",
  "cancellationReason": "string | null",
  "createdAt": "datetime"
}
```

---

## MODULE: WhatsApp Message Queue (Phase 4)

> **Base:** `/api/clinic/messages`  
> **Auth:** JWT required  
> **X-Tenant:** Required  
> **Purpose:** Queue and track WhatsApp/PWA messages

### POST `/api/clinic/messages/send`
- **Roles:** ClinicOwner, ClinicManager, Receptionist, Doctor, SuperAdmin
- **Request:**
```json
{
  "templateName": "patient_credentials",
  "recipientPhone": "+966500000001",
  "recipientUserId": "guid (optional)",
  "channel": "WhatsApp",
  "variables": { "patientName": "Ahmad", "clinicName": "Demo Clinic" }
}
```
- **Valid Templates:** `patient_credentials`, `queue_ticket_issued`, `your_turn`, `visit_summary`, `followup_reminder`, `medication_reminder`, `password_reset`, `booking_confirmation`, `booking_cancellation`, `booking_reminder`
- **WhatsApp requires:** `recipientPhone`
- **Response:** `MessageLogDto` with status `Sent`

### POST `/api/clinic/messages/{id}/retry`
- **Validations:** Only `Failed` messages, max 3 attempts
- **Response:** `MessageLogDto` with incremented attemptCount

### GET `/api/clinic/messages/{id}`
- **Response:** Single `MessageLogDto`

### GET `/api/clinic/messages`
- **Query:** `?pageNumber=1&pageSize=10&templateName=patient_credentials&channel=WhatsApp&status=Sent`
- **Response:** `PagedResult<MessageLogDto>`

### MessageLogDto
```json
{
  "id": "guid",
  "templateName": "patient_credentials",
  "recipientPhone": "+966500000001",
  "recipientUserId": "guid | null",
  "channel": "WhatsApp",
  "status": "Sent",
  "attemptCount": 1,
  "lastAttemptAt": "datetime",
  "sentAt": "datetime | null",
  "deliveredAt": "datetime | null",
  "failureReason": "string | null",
  "variables": "{\"patientName\":\"Ahmad\"}",
  "createdAt": "datetime"
}
```

---

## MODULE: Doctor Notes (Phase 4)

> **Base:** `/api/clinic/doctor-notes`  
> **Auth:** JWT required  
> **X-Tenant:** Required  
> **Purpose:** Doctor-to-reception messaging with read tracking

### POST `/api/clinic/doctor-notes`
- **Roles:** Doctor
- **Request:** `{ "message": "Please prepare room 3 for procedure" }`
- **Validations:** Message cannot be empty, caller must have a Doctor profile
- **Response:** `DoctorNoteDto` with isRead=false

### GET `/api/clinic/doctor-notes/unread`
- **Description:** Get all unread notes
- **Response:** Array of `DoctorNoteDto`

### GET `/api/clinic/doctor-notes`
- **Query:** `?pageNumber=1&pageSize=10&unreadOnly=true`
- **Response:** `PagedResult<DoctorNoteDto>`

### POST `/api/clinic/doctor-notes/{id}/read`
- **Description:** Mark a note as read
- **Validations:** Cannot re-mark already-read notes
- **Response:** `DoctorNoteDto` with isRead=true, readAt set

### DoctorNoteDto
```json
{
  "id": "guid",
  "doctorId": "guid",
  "doctorName": "Dr. Khaled",
  "message": "Please prepare room 3",
  "isRead": false,
  "readAt": "datetime | null",
  "readByUserId": "guid | null",
  "createdAt": "datetime"
}
```

---

## MODULE: PWA Notifications (Phase 4)

> **Base:** `/api/clinic/notifications`  
> **Auth:** JWT required  
> **X-Tenant:** Required  
> **Purpose:** PWA push notification subscription management

### POST `/api/clinic/notifications/subscribe`
- **Feature Flag:** `PwaNotifications` must be enabled
- **Request:**
```json
{
  "endpoint": "https://fcm.googleapis.com/fcm/send/...",
  "p256dh": "base64-key",
  "auth": "base64-auth"
}
```
- **Behavior:** Duplicate endpoint reactivates existing subscription
- **Response:** `NotificationSubscriptionDto`

### DELETE `/api/clinic/notifications/{id}`
- **Description:** Unsubscribe (soft delete)
- **Response:** Success message

### GET `/api/clinic/notifications/my`
- **Description:** List caller's active subscriptions
- **Response:** Array of `NotificationSubscriptionDto`

### POST `/api/clinic/notifications/send`
- **Roles:** ClinicOwner, ClinicManager, Receptionist, Doctor, SuperAdmin
- **Request:**
```json
{
  "userId": "guid",
  "title": "Medication Reminder",
  "body": "Time for your medication",
  "templateName": "medication_reminder (optional)"
}
```
- **Validations:** Target user must have an active subscription
- **Response:** `MessageLogDto` (logged as PWA channel)

---

## ENUMS (Phase 4)

### BookingStatus
| Value | Description |
|-------|-------------|
| `Confirmed` | Booking confirmed and active |
| `Cancelled` | Booking cancelled by patient or staff |
| `Rescheduled` | Booking rescheduled (transitional, auto-confirmed) |
| `Completed` | Booking completed (linked to visit) |

### MessageChannel
| Value | Description |
|-------|-------------|
| `WhatsApp` | WhatsApp message via template |
| `PWA` | PWA push notification |

### MessageStatus
| Value | Description |
|-------|-------------|
| `Pending` | Queued, not yet sent |
| `Sending` | Currently being sent |
| `Sent` | Successfully sent |
| `Delivered` | Confirmed delivered |
| `Read` | Confirmed read by recipient |
| `Failed` | Send attempt failed |
| `Retrying` | Retry in progress |

---

## FUTURE PHASES

Endpoint documentation will be added as each phase is implemented. No aspirational or preview content.

- **Phase 5:** Reporting, export, platform audit, analytics, SignalR, full seed

---

*This document is updated at every phase. Swagger UI reflects the actual implementation.*
