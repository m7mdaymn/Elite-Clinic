# SWAGGER_DOCUMENTATION.md — API Reference (Human-Readable)

> **Version:** 2.0  
> **Last Updated:** 2026-02-08  
> **Status:** Phase 1 & 2 Complete (43 Endpoints)  
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

## FUTURE PHASES

Endpoint documentation will be added as each phase is implemented. No aspirational or preview content.

- **Phase 3:** Queue system, visits, prescriptions, labs, payments, expenses, finance
- **Phase 4:** WhatsApp messaging, online booking, public SEO, PWA notifications
- **Phase 5:** Reporting, export, platform audit, analytics, SignalR, full seed

---

*This document is updated at every phase. Swagger UI reflects the actual implementation.*
