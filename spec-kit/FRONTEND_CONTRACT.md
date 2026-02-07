# FRONTEND_CONTRACT.md — API Contract for Frontend Integration

> **Version:** 2.0  
> **Last Updated:** 2026-02-08  
> **Status:** Phase 1 & 2 Complete

---

## GLOBAL CONVENTIONS

### Base URL
```
https://{host}/api
```

### Required Headers

| Header | Required | Description |
|--------|----------|-------------|
| `X-Tenant` | Yes (tenant routes) | Tenant slug. e.g., `nile-dental`. NOT required for platform/SuperAdmin routes. |
| `Authorization` | Yes (authenticated routes) | `Bearer {jwt_token}` |
| `Content-Type` | Yes (POST/PUT/PATCH) | `application/json` |

### Standard Response Envelope

Every response uses this shape:

```json
{
  "success": true,
  "message": "string",
  "data": {},
  "errors": [],
  "meta": {
    "timestamp": "2026-02-06T10:00:00Z",
    "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  }
}
```

### Paginated Response

```json
{
  "success": true,
  "data": {
    "items": [],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 8
  },
  "meta": { ... }
}
```

> **Note:** Paginated data is inside `data.items`, with pagination metadata (`totalCount`, `pageNumber`, `pageSize`, `totalPages`) at the `data` level.

### Error Response

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    { "field": "phone", "message": "Phone number is required" }
  ],
  "meta": { ... }
}
```

### HTTP Status Codes Used

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad request / validation failure |
| 401 | Unauthenticated (no/invalid token) |
| 403 | Unauthorized (insufficient role/permission) OR tenant blocked/suspended |
| 404 | Resource not found |
| 409 | Conflict (duplicate, state conflict) |
| 500 | Internal server error |

### Tenant Blocked Behavior
- **Clinic (authenticated) routes** → HTTP 403:
  ```json
  { "success": false, "message": "Tenant is suspended. Contact platform support.", "errors": [] }
  ```
- **Public SEO routes** → HTTP 200 always:
  ```json
  { "success": true, "data": { "isActive": false, "clinicName": "...", "renewalMessage": "..." } }
  ```

---

## PHASE 0 — Foundation (Auth + Health)

### Endpoints Added in Phase 0

#### 1. Health Check
```
GET /api/health
```
- **Auth:** None
- **Headers:** None required
- **Response:**
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

#### 2. SuperAdmin Login
```
POST /api/auth/login
```
- **Auth:** None (produces a token)
- **Headers:** `Content-Type: application/json`
- **X-Tenant:** Not required (platform route)
- **Request:**
  ```json
  {
    "username": "superadmin",
    "password": "Admin@123456"
  }
  ```
- **Response (200):**
  ```json
  {
    "success": true,
    "message": "Login successful",
    "data": {
      "token": "eyJhbGciOiJIUzI1NiIs...",
      "refreshToken": "a1b2c3d4-e5f6-...",
      "expiresAt": "2026-02-06T22:00:00Z",
      "user": {
        "id": "guid",
        "username": "superadmin",
        "displayName": "Platform Admin",
        "role": "SuperAdmin",
        "tenantId": null
      }
    }
  }
  ```
- **Response (401 — bad credentials):**
  ```json
  {
    "success": false,
    "message": "Invalid username or password",
    "errors": []
  }
  ```
- **UI Expectation:** Login form with username + password. No "forgot password via email" link. No OTP.

#### 3. Tenant Staff/Doctor Login
```
POST /api/auth/login
```
- **Same endpoint** as SuperAdmin login.
- **Headers:** `X-Tenant: nile-dental` (required for tenant users)
- **Request:**
  ```json
  {
    "username": "dr.ahmed",
    "password": "Doctor@123"
  }
  ```
- **Response:** Same shape as above, but `user.role` will be `ClinicOwner`, `ClinicManager`, `Reception`, or `Doctor`, and `user.tenantId` will be populated.

#### 4. Patient Login
```
POST /api/auth/patient/login
```
- **Headers:** `X-Tenant: nile-dental` (required)
- **Request:**
  ```json
  {
    "username": "patient_20001",
    "password": "Welcome@1"
  }
  ```
- **Response (200):**
  ```json
  {
    "success": true,
    "data": {
      "token": "eyJ...",
      "refreshToken": "...",
      "expiresAt": "2027-02-06T00:00:00Z",
      "user": {
        "id": "guid",
        "username": "patient_20001",
        "displayName": "Ahmed Mohamed",
        "role": "Patient",
        "tenantId": "guid",
        "profiles": [
          { "id": "guid", "name": "Ahmed Mohamed", "isDefault": true },
          { "id": "guid", "name": "Sara Ahmed (child)", "isDefault": false }
        ]
      }
    }
  }
  ```
- **UI Expectation:** Patient token has very long expiry (365 days). Frontend should NEVER show a logout button for patients. Auto-refresh token silently.

#### 5. Refresh Token
```
POST /api/auth/refresh
```
- **Auth:** None (uses refresh token)
- **Request:**
  ```json
  {
    "refreshToken": "a1b2c3d4-e5f6-..."
  }
  ```
- **Response:** Same shape as login response with new token + refreshToken.

#### 6. Get Current User (Me)
```
GET /api/auth/me
```
- **Auth:** Bearer token required
- **Headers:** `X-Tenant` required for tenant users, not for SuperAdmin
- **Response (200):**
  ```json
  {
    "success": true,
    "data": {
      "id": "guid",
      "username": "dr.ahmed",
      "displayName": "Dr. Ahmed Hassan",
      "role": "Doctor",
      "tenantId": "guid",
      "tenantSlug": "nile-dental",
      "permissions": ["queue.view", "visit.create", "visit.own"]
    }
  }
  ```

---

## FUTURE PHASES (Stubs — Will Be Expanded Per Phase)

### Phase 1 — Tenant Management, Subscriptions & Feature Flags

> All Phase 1 endpoints require `Authorization: Bearer {superadmin_token}`.  
> No `X-Tenant` header needed (platform routes).

#### 1. Create Tenant
```
POST /api/platform/tenants
```
- **Request:**
  ```json
  {
    "name": "Nile Dental Clinic",
    "slug": "nile-dental",
    "contactPhone": "+201234567890",
    "address": "123 Main St, Cairo",
    "logoUrl": "https://example.com/logo.png"
  }
  ```
- **Response (201):** `ApiResponse<TenantDto>` — includes `id`, `name`, `slug`, `status` (0=Active), `contactPhone`, `createdAt`
- **Response (400):** Duplicate slug, invalid slug format, missing name
- **Validation:** Slug must match `^[a-z0-9\-]+$`, max 100 chars. Name required, max 200 chars.
- **Side effect:** Feature flags are auto-created with PLAN.md §13 defaults

#### 2. List Tenants
```
GET /api/platform/tenants?pageNumber=1&pageSize=10&searchTerm=nile
```
- **Response (200):** `ApiResponse<PagedResult<TenantDto>>`
  ```json
  {
    "success": true,
    "data": {
      "items": [ { "id": "guid", "name": "...", "slug": "...", "status": 0, "contactPhone": "...", "createdAt": "..." } ],
      "totalCount": 8,
      "pageNumber": 1,
      "pageSize": 10,
      "totalPages": 1
    }
  }
  ```
- **Params:** `pageNumber` (default 1), `pageSize` (default 10), `searchTerm` (filters by name/slug)
- **Note:** Soft-deleted tenants excluded

#### 3. Get Tenant Details
```
GET /api/platform/tenants/{id}
```
- **Response (200):** `ApiResponse<TenantDetailDto>` — adds `address`, `logoUrl`, `updatedAt` beyond list DTO
- **Response (404):** Tenant not found

#### 4. Update Tenant
```
PUT /api/platform/tenants/{id}
```
- **Request:**
  ```json
  {
    "name": "Updated Name",
    "contactPhone": "+201111111111",
    "address": "New Address",
    "logoUrl": "https://example.com/new-logo.png"
  }
  ```
- **Response (200):** Updated `TenantDetailDto`
- **Response (404):** Tenant not found
- **Note:** Slug is NOT in the update DTO — it is immutable

#### 5. Status Changes
```
POST /api/platform/tenants/{id}/activate
POST /api/platform/tenants/{id}/suspend
POST /api/platform/tenants/{id}/block
```
- **Request:** No body
- **Response (200):** `{ "success": true, "message": "Tenant activated/suspended/blocked successfully" }`
- **Response (404):** Tenant not found
- **Note:** Uses POST (not PATCH)

#### 6. Delete Tenant (Soft)
```
DELETE /api/platform/tenants/{id}
```
- **Response (200):** `{ "success": true, "message": "Tenant deleted successfully" }`
- **Response (404):** Tenant not found or already deleted
- **Note:** Soft-delete only (IsDeleted=true). No physical deletion.

#### 7. Create Subscription
```
POST /api/platform/subscriptions
```
- **Request:**
  ```json
  {
    "tenantId": "guid",
    "planName": "Premium Annual",
    "startDate": "2026-01-01",
    "endDate": "2027-01-01",
    "amount": 12000.00,
    "currency": "EGP",
    "notes": "First year subscription"
  }
  ```
- **Response (201):** `ApiResponse<SubscriptionDto>` — `id`, `tenantId`, `tenantName`, `planName`, `startDate`, `endDate`, `amount`, `currency`, `isPaid` (false), `status` (0=Active), `createdAt`
- **Response (400):** Tenant not found, EndDate before StartDate, missing required fields
- **Note:** TenantId is in the body, not the URL

#### 8. List Subscriptions
```
GET /api/platform/subscriptions?pageNumber=1&pageSize=10&tenantId={guid}
```
- **Response (200):** `ApiResponse<PagedResult<SubscriptionDto>>`
- **Params:** `tenantId` optional (filters by tenant)

#### 9. Extend Subscription
```
POST /api/platform/subscriptions/{id}/extend
```
- **Request:**
  ```json
  {
    "newEndDate": "2028-01-01",
    "notes": "Extended for renewal"
  }
  ```
- **Response (200):** Updated `SubscriptionDto`
- **Response (400):** Not found, or cannot extend cancelled subscription

#### 10. Cancel Subscription
```
POST /api/platform/subscriptions/{id}/cancel
```
- **Request:**
  ```json
  {
    "cancelReason": "Customer requested cancellation"
  }
  ```
- **Response (200):** Updated `SubscriptionDto` with `status=2` (Cancelled), `cancelledAt`, `cancelReason`
- **Response (400):** Not found, or already cancelled

#### 11. Mark Subscription Paid
```
POST /api/platform/subscriptions/{id}/mark-paid
```
- **Request:**
  ```json
  {
    "paymentMethod": "Cash",
    "paymentReference": "RECEIPT-001",
    "paidAt": "2026-02-07T10:00:00Z"
  }
  ```
- **Response (200):** Updated `SubscriptionDto` with `isPaid=true`
- **Response (400):** Not found, or already paid

#### 12. Get Feature Flags
```
GET /api/platform/feature-flags/{tenantId}
```
- **Response (200):**
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
    }
  }
  ```
- **Response (404):** Tenant or flags not found

#### 13. Update Feature Flags
```
PUT /api/platform/feature-flags/{tenantId}
```
- **Request:**
  ```json
  {
    "onlineBooking": true,
    "whatsappAutomation": true,
    "pwaNotifications": false,
    "expensesModule": true,
    "advancedMedicalTemplates": false,
    "ratings": true,
    "export": false
  }
  ```
- **Response (200):** Updated `FeatureFlagDto`
- **Response (400):** Tenant not found
- **IMPORTANT:** All 7 boolean fields are required. No partial update.

#### Phase 1 — Frontend Integration Notes

| Topic | Detail |
|-------|--------|
| **TenantStatus values** | 0=Active, 1=Suspended, 2=Blocked, 3=Inactive |
| **SubscriptionStatus values** | 0=Active, 1=Expired, 2=Cancelled |
| **Default tenant status** | Active (not Inactive as per original spec) |
| **Slug immutability** | Slug cannot be edited after creation - not in PUT DTO |
| **Feature flag creation** | Auto-created on tenant creation - no separate create endpoint |
| **Model validation errors** | ⚠️ ASP.NET model validation returns ProblemDetails format: `{"errors":{"Field":["msg"]}}` instead of ApiResponse format. Frontend must handle both formats. Fix planned for Phase 2. |
| **Subscription routes** | `/api/platform/subscriptions` (flat, not nested under tenants). TenantId is in request body. |
| **Action endpoints** | Status changes and subscription actions use POST (not PATCH) |

### Phase 2 — Clinic Setup & Users

#### ClinicSettingsDto

```json
{
  "id": "5fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": "guid",
  "clinicName": "Demo Dental Clinic",
  "phone": "+201000000099",
  "whatsAppSenderNumber": null,
  "supportWhatsAppNumber": null,
  "supportPhoneNumber": null,
  "address": "123 Main St",
  "city": "Cairo",
  "logoUrl": null,
  "bookingEnabled": true,
  "cancellationWindowHours": 2,
  "workingHours": [
    {
      "id": "guid",
      "dayOfWeek": 0,
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "isActive": true
    }
  ]
}
```

#### UpdateClinicSettingsRequest

```json
{
  "clinicName": "string (required, max 200)",
  "phone": "string? (max 20)",
  "whatsAppSenderNumber": "string? (max 20)",
  "supportWhatsAppNumber": "string? (max 20)",
  "supportPhoneNumber": "string? (max 20)",
  "address": "string?",
  "city": "string? (max 100)",
  "bookingEnabled": "bool",
  "cancellationWindowHours": "int (0-168, default 2)",
  "workingHours": [
    {
      "dayOfWeek": "int (0-6)",
      "startTime": "string (HH:MM:SS)",
      "endTime": "string (HH:MM:SS)",
      "isActive": "bool"
    }
  ]
}
```

#### StaffDto

```json
{
  "id": "guid",
  "userId": "guid",
  "name": "Sara Ali",
  "phone": "+201234567890",
  "role": "ClinicManager",
  "username": "staff_sara",
  "salary": 3500,
  "hireDate": "2025-01-15",
  "notes": null,
  "isEnabled": true,
  "createdAt": "2026-02-07T10:00:00Z"
}
```

#### DoctorDto

```json
{
  "id": "guid",
  "userId": "guid",
  "name": "Dr. Khaled",
  "specialty": "Pediatrics",
  "phone": "+201234567890",
  "bio": null,
  "photoUrl": null,
  "username": "dr_khaled",
  "isEnabled": true,
  "urgentCaseMode": 0,
  "avgVisitDurationMinutes": 15,
  "services": [
    {
      "id": "guid",
      "serviceName": "General Check-up",
      "price": 150,
      "durationMinutes": 15,
      "isActive": true
    }
  ],
  "visitFieldConfig": {
    "bloodPressure": false,
    "heartRate": false,
    "temperature": true,
    "weight": true,
    "height": false,
    "bmi": false,
    "bloodSugar": false,
    "oxygenSaturation": false,
    "respiratoryRate": false
  },
  "createdAt": "2026-02-07T10:00:00Z"
}
```

#### PatientDto

```json
{
  "id": "guid",
  "userId": "guid",
  "name": "Mohamed Ali",
  "phone": "+201234567890",
  "dateOfBirth": "1990-05-15",
  "gender": 0,
  "address": "456 Elm St",
  "notes": null,
  "isDefault": true,
  "parentPatientId": null,
  "username": "patient_demo-clinic_1",
  "subProfiles": [
    {
      "id": "guid",
      "name": "Child of Mohamed",
      "phone": "+201234567891",
      "dateOfBirth": "2020-01-01",
      "gender": 1,
      "isDefault": false
    }
  ],
  "createdAt": "2026-02-07T10:00:00Z"
}
```

#### CreatePatientRequest

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

#### CreatePatientResponse

```json
{
  "patient": {
    "id": "guid",
    "userId": "guid",
    "name": "Mohamed Ali",
    "phone": "+201234567890",
    "username": "patient_demo-clinic_1",
    ...
  },
  "username": "patient_demo-clinic_1",
  "password": "MxKd@9283"
}
```

#### PatientLoginResponse

```json
{
  "token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "refreshToken": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "expiresAt": "2027-02-08T08:00:00Z",
  "user": {
    "id": "guid",
    "username": "patient_demo-clinic_1",
    "displayName": "Mohamed Ali",
    "role": "Patient",
    "tenantId": "guid",
    "profiles": [
      {
        "id": "guid",
        "name": "Mohamed Ali",
        "isDefault": true
      },
      {
        "id": "guid",
        "name": "Child of Mohamed",
        "isDefault": false
      }
    ]
  }
}
```

#### Phase 2 — Frontend Integration Notes

| Topic | Detail |
|-------|--------|
| **Gender enum** | 0=Male, 1=Female |
| **UrgentCaseMode enum** | 0=UrgentNext, 1=UrgentBucket, 2=UrgentFront |
| **Auto-generated credentials** | Patient username/password auto-generated on creation. Returned in CreatePatientResponse. Store securely for staff to hand to patient. |
| **Tenant-scoped routes** | All `/api/clinic/*` routes require `X-Tenant` header. The header is validated against the user's tenantId from the token. |
| **Patient login token lifetime** | 365 days (vs 8 hours for staff/doctor). Refresh endpoint still required. |
| **Multi-profile support** | Patients with sub-profiles login once, then frontend switches profile via `profiles[]` array. No separate login per profile. |
| **Cross-tenant isolation** | User from Tenant A cannot access Tenant B even if they manually send `X-Tenant: tenant-b`. Request is rejected. |
| **Staff read-only clinic settings** | ClinicManager can GET settings but cannot PUT (only ClinicOwner can write). |
| **Working hours format** | DayOfWeek is 0-indexed (0=Sunday, 6=Saturday). Time as HH:MM:SS in 24-hour format. |
| **Password reset** | POST `/api/clinic/patients/{id}/reset-password` returns `newPassword`. No email/WhatsApp in Phase 2. |
| **Sub-profile creation** | Use POST `/api/clinic/patients/{id}/profiles` to add child. Response returns parent patient with updated subProfiles array. |
| **Patient deletion** | Soft-delete only (ClinicOwner). Patient marked as deleted but not removed from DB. Excluded from list queries. |

### Phase 3 — Queue & Clinical Workflow
> Will be expanded when Phase 3 is implemented.

### Phase 4 — Communication & Booking
> Will be expanded when Phase 4 is implemented.

### Phase 5 — Analytics, Audit & Final
> Will be expanded when Phase 5 is implemented.

---

## EDGE CASES (All Phases)

| Scenario | Expected Behavior |
|----------|-------------------|
| Missing `X-Tenant` on tenant route | 400: "X-Tenant header is required" |
| Invalid `X-Tenant` slug | 404: "Tenant not found" |
| Suspended tenant + clinic route | 403: "Tenant is suspended" |
| Suspended tenant + public route | 200: `isActive=false` |
| Expired JWT | 401: "Token expired" |
| Wrong role for endpoint | 403: "Insufficient permissions" |
| Patient tries to access doctor route | 403 |
| User from Tenant A sends `X-Tenant: tenant-b` | 403: "Access denied" (tenant mismatch) |

---

*Updated per phase. Frontend team should reference this document for integration.*
