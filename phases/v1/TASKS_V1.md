# TASKS_V1.md — Phase 1 Task Breakdown

> **Phase:** 1 — Tenant Management, Subscriptions & Feature Flags  
> **Status:** 🔲 PLANNING — Awaiting Approval  
> **Date Created:** 2026-02-06  
> **Estimated Effort:** 1–2 days (per PLAN.md §18)

---

## RULES

- Each task < 1 day of work
- Each task has: goal, done condition, acceptance criteria
- Each task references exact Spec Kit section
- No vague tasks

---

## DOMAIN LAYER TASKS

### T1.01 — Define Subscription Entity & SubscriptionStatus Enum

**Goal:** Create the Subscription entity and status enum in Domain layer.  
**Spec Reference:** PLAN.md §3.2 (manages subscriptions), SWAGGER_DOCUMENTATION.md §Subscriptions (5 endpoints imply entity fields), UPDATE_V1.md SPEC GAP #3  
**Done Condition:**  
- `Subscription` entity exists in `EliteClinic.Domain/Entities/`  
- `SubscriptionStatus` enum exists in `EliteClinic.Domain/Enums/`  
- Entity inherits from `BaseEntity`  
- Has FK to Tenant (TenantId)  
- Fields: PlanName, StartDate, EndDate, Amount, Currency, IsPaid, PaidAt, PaymentMethod, PaymentReference, Status, Notes, CancelledAt, CancelReason  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] Entity compiles  
- [x] Enum has values: Active, Expired, Cancelled  
- [x] FK constraint defined (TenantId → Tenant)  
- [x] Inherits BaseEntity (gets Id, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)  

---

### T1.02 — Define TenantFeatureFlag Entity

**Goal:** Create entity to store per-tenant feature flags.  
**Spec Reference:** PLAN.md §13 (7 feature flags with defaults)  
**Done Condition:**  
- `TenantFeatureFlag` entity exists in `EliteClinic.Domain/Entities/`  
- Has FK to Tenant (TenantId, unique — one row per tenant)  
- 7 boolean properties matching PLAN.md §13: OnlineBooking, WhatsappAutomation, PwaNotifications, ExpensesModule, AdvancedMedicalTemplates, Ratings, Export  
- Default values match PLAN.md §13  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] Entity compiles  
- [x] All 7 flags present as boolean properties  
- [x] Defaults: OnlineBooking=false, WhatsappAutomation=true, PwaNotifications=false, ExpensesModule=true, AdvancedMedicalTemplates=false, Ratings=false, Export=false  
- [x] One-to-one relationship with Tenant  

---

### T1.03 — Add Navigation Properties to Tenant Entity

**Goal:** Extend existing Tenant entity with navigation properties for Subscriptions and FeatureFlags.  
**Spec Reference:** PLAN.md §2 (tenant model), SWAGGER_DOCUMENTATION.md §Tenant Management (get tenant includes subscription & flags)  
**Done Condition:**  
- Tenant entity has `ICollection<Subscription> Subscriptions` navigation property  
- Tenant entity has `TenantFeatureFlag? FeatureFlags` navigation property  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] Navigation properties defined  
- [x] No breaking changes to existing Tenant functionality  
- [x] Build passes  

---

## INFRASTRUCTURE LAYER TASKS

### T1.04 — Register New DbSets & Configure EF Relationships

**Goal:** Add DbSets for Subscription and TenantFeatureFlag to AppDbContext. Configure relationships and constraints.  
**Spec Reference:** PLAN.md §2.3 (slug unique), §13 (flags per tenant)  
**Done Condition:**  
- `DbSet<Subscription>` added to AppDbContext  
- `DbSet<TenantFeatureFlag>` added to AppDbContext  
- Subscription → Tenant FK configured  
- TenantFeatureFlag → Tenant one-to-one configured with unique index on TenantId  
- Subscription.Amount has precision(18,2) for decimal  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] Both DbSets registered  
- [x] FK constraints configured  
- [x] Unique constraint on TenantFeatureFlag.TenantId  
- [x] Decimal precision configured  
- [x] Build passes  

---

### T1.05 — Create EF Migration for Phase 1 Entities

**Goal:** Generate and apply migration for Subscription and TenantFeatureFlag tables.  
**Spec Reference:** PLAN.md §18 (Phase 1 scope)  
**Done Condition:**  
- Migration generated with `dotnet ef migrations add Phase1_TenantSubscriptionFlags`  
- Migration applied with `dotnet ef database update`  
- Tables created in SQL Server: Subscriptions, TenantFeatureFlags  
- No data loss on existing tables  

**Acceptance Criteria:**  
- [x] Migration applies without errors  
- [x] Subscriptions table exists with correct columns  
- [x] TenantFeatureFlags table exists with correct columns  
- [x] Existing data (SuperAdmin, roles) intact  

---

### T1.06 — Enhance TenantMiddleware — Cross-Tenant Guard & Status Messages

**Goal:** Upgrade TenantMiddleware to prevent cross-tenant access and return proper error messages per tenant state.  
**Spec Reference:** PLAN.md §2.2 (tenant states → behavior), FRONTEND_CONTRACT.md §Tenant Blocked Behavior, FRONTEND_CONTRACT.md §Edge Cases  
**Done Condition:**  
- Middleware checks JWT `tenantId` claim against resolved `X-Tenant` tenant ID  
- If mismatch → 403 `"Access denied"` (FRONTEND_CONTRACT.md edge case: "User from Tenant A sends X-Tenant: tenant-b")  
- Suspended tenant on clinic route → 403 with message `"Tenant is suspended. Contact platform support."`  
- Blocked tenant on clinic route → 403 with message `"Tenant is blocked. Contact platform support."`  
- Inactive tenant on clinic route → 403 with message `"Tenant is inactive. Contact platform support."`  
- SuperAdmin bypasses tenant check (no tenantId in JWT)  
- Platform routes (`/api/platform/*`) skip tenant check entirely  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] Cross-tenant access returns 403  
- [x] Each tenant state has correct HTTP status and message  
- [x] SuperAdmin can access any tenant's platform routes  
- [x] Existing health/auth routes still work without X-Tenant  
- [x] Build passes  

---

## APPLICATION LAYER TASKS

### T1.07 — Create Tenant DTOs

**Goal:** Define request/response DTOs for tenant CRUD operations.  
**Spec Reference:** SWAGGER_DOCUMENTATION.md §Tenant Management, FRONTEND_CONTRACT.md §Phase 1  
**Done Condition:**  
- `CreateTenantRequest` DTO: Name (required), Slug (required), ContactPhone, Address, LogoUrl  
- `UpdateTenantRequest` DTO: Name, ContactPhone, Address, LogoUrl (slug excluded — immutable per PLAN.md §2.3)  
- `TenantDto` response DTO: Id, Name, Slug, Status, ContactPhone, Address, LogoUrl, CreatedAt, UpdatedAt  
- `TenantDetailDto` response DTO: extends TenantDto with FeatureFlags and ActiveSubscription  
- Validation attributes on required fields  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] DTOs compile  
- [x] Slug excluded from UpdateTenantRequest  
- [x] Required fields marked with `[Required]`  
- [x] Slug has regex validation: lowercase, URL-safe (letters, digits, hyphens)  
- [x] Build passes  

---

### T1.08 — Create Subscription DTOs

**Goal:** Define request/response DTOs for subscription operations.  
**Spec Reference:** SWAGGER_DOCUMENTATION.md §Subscriptions  
**Done Condition:**  
- `CreateSubscriptionRequest` DTO: PlanName (required), StartDate (required), EndDate (required), Amount (required), Currency, PaymentMethod, PaymentReference, Notes  
- `ExtendSubscriptionRequest` DTO: NewEndDate (required), Notes  
- `CancelSubscriptionRequest` DTO: CancelReason  
- `MarkPaidRequest` DTO: PaymentMethod, PaymentReference, Notes  
- `SubscriptionDto` response DTO: all fields  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] DTOs compile  
- [x] Required fields marked  
- [x] EndDate must be after StartDate (validation attribute or service-level check)  
- [x] Build passes  

---

### T1.09 — Create Feature Flag DTOs

**Goal:** Define request/response DTOs for feature flag operations.  
**Spec Reference:** PLAN.md §13, SWAGGER_DOCUMENTATION.md §Feature Flags  
**Done Condition:**  
- `FeatureFlagDto` response DTO: all 7 flags as booleans  
- `UpdateFeatureFlagRequest` DTO: all 7 flags as nullable booleans (only update provided fields)  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] DTOs compile  
- [x] All 7 flags represented  
- [x] Update DTO uses nullable bools for partial updates  
- [x] Build passes  

---

### T1.10 — Implement TenantService

**Goal:** Create business logic service for tenant CRUD operations.  
**Spec Reference:** PLAN.md §2 (tenancy model), §2.3 (slug rules), PERMISSIONS_MATRIX.md §Platform Administration  
**Done Condition:**  
- `ITenantService` interface in Application layer  
- `TenantService` implementation  
- Methods: CreateAsync, GetByIdAsync, ListAsync (paginated), UpdateAsync, ActivateAsync, SuspendAsync, BlockAsync, SoftDeleteAsync  
- Slug validation: lowercase, URL-safe, unique check  
- Auto-creates TenantFeatureFlag row with defaults when tenant is created  
- Returns proper DTOs  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] All CRUD methods implemented  
- [x] Slug uniqueness validated (409 Conflict on duplicate)  
- [x] Slug format validated (lowercase, letters/digits/hyphens)  
- [x] Creating tenant auto-creates feature flags row  
- [x] Status transitions work correctly  
- [x] Soft delete sets IsDeleted=true, never physical delete  
- [x] Pagination uses PaginatedResponse<T>  
- [x] Build passes  

---

### T1.11 — Implement SubscriptionService

**Goal:** Create business logic service for subscription management.  
**Spec Reference:** PLAN.md §3.2 (manages subscriptions), SWAGGER_DOCUMENTATION.md §Subscriptions  
**Done Condition:**  
- `ISubscriptionService` interface in Application layer  
- `SubscriptionService` implementation  
- Methods: CreateAsync, ListByTenantAsync, ExtendAsync, CancelAsync, MarkPaidAsync  
- Validates tenant exists before creating subscription  
- Extend updates EndDate  
- Cancel sets Status=Cancelled, CancelledAt, CancelReason  
- MarkPaid sets IsPaid=true, PaidAt, PaymentMethod, PaymentReference  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] All 5 methods implemented  
- [x] Tenant existence validated  
- [x] Cannot extend a cancelled subscription  
- [x] Cannot cancel an already cancelled subscription  
- [x] Cannot mark-paid an already paid subscription (or allow re-marking — TBD)  
- [x] Returns proper DTOs  
- [x] Build passes  

---

### T1.12 — Implement FeatureFlagService

**Goal:** Create business logic service for feature flag operations.  
**Spec Reference:** PLAN.md §13, SWAGGER_DOCUMENTATION.md §Feature Flags  
**Done Condition:**  
- `IFeatureFlagService` interface in Application layer  
- `FeatureFlagService` implementation  
- Methods: GetByTenantAsync, UpdateAsync  
- If tenant has no flags row (legacy data), auto-creates one with defaults  
- Partial update: only non-null fields in request are updated  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] Get returns current flag values  
- [x] Update applies partial changes  
- [x] Auto-creates flags row if missing  
- [x] Returns FeatureFlagDto  
- [x] Build passes  

---

## API LAYER TASKS

### T1.13 — Create TenantsController

**Goal:** Implement API controller for tenant CRUD endpoints.  
**Spec Reference:** SWAGGER_DOCUMENTATION.md §Tenant Management, FRONTEND_CONTRACT.md §Phase 1  
**Done Condition:**  
- Route: `/api/platform/tenants`  
- `[Authorize(Roles = "SuperAdmin")]` on controller  
- Endpoints: POST create, GET list, GET {id}, PUT {id} update, PATCH {id}/activate, PATCH {id}/suspend, PATCH {id}/block, DELETE {id}  
- All responses use ApiResponse<T> envelope  
- HTTP status codes match FRONTEND_CONTRACT.md (200, 201, 400, 404, 409)  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] All 8 endpoints implemented  
- [x] SuperAdmin-only authorization  
- [x] Proper HTTP status codes  
- [x] ApiResponse<T> envelope on all responses  
- [x] Paginated list uses PaginatedResponse<T>  
- [x] Validation errors return 400 with field-level errors  
- [x] Duplicate slug returns 409  
- [x] Not-found returns 404  
- [x] Build passes  

---

### T1.14 — Create SubscriptionsController

**Goal:** Implement API controller for subscription management endpoints.  
**Spec Reference:** SWAGGER_DOCUMENTATION.md §Subscriptions  
**Done Condition:**  
- Route: `/api/platform/tenants/{tenantId}/subscriptions`  
- `[Authorize(Roles = "SuperAdmin")]` on controller  
- Endpoints: POST create, GET list, PATCH {subId}/extend, PATCH {subId}/cancel, PATCH {subId}/mark-paid  
- All responses use ApiResponse<T> envelope  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] All 5 endpoints implemented  
- [x] SuperAdmin-only authorization  
- [x] Tenant existence validated (404 if not found)  
- [x] Subscription not found → 404  
- [x] Invalid state transitions → 409 Conflict  
- [x] Build passes  

---

### T1.15 — Create FeatureFlagsController

**Goal:** Implement API controller for feature flag endpoints.  
**Spec Reference:** SWAGGER_DOCUMENTATION.md §Feature Flags, PLAN.md §13  
**Done Condition:**  
- Route: `/api/platform/tenants/{tenantId}/feature-flags`  
- `[Authorize(Roles = "SuperAdmin")]` on controller  
- Endpoints: GET, PUT  
- All responses use ApiResponse<T> envelope  
- `dotnet build` passes  

**Acceptance Criteria:**  
- [x] Both endpoints implemented  
- [x] SuperAdmin-only authorization  
- [x] Tenant existence validated (404)  
- [x] GET returns all 7 flags  
- [x] PUT applies partial update  
- [x] Build passes  

---

### T1.16 — Register Phase 1 Services in DI Container

**Goal:** Wire up all Phase 1 services in Program.cs DI configuration.  
**Spec Reference:** N/A — infrastructure plumbing  
**Done Condition:**  
- ITenantService → TenantService registered as Scoped  
- ISubscriptionService → SubscriptionService registered as Scoped  
- IFeatureFlagService → FeatureFlagService registered as Scoped  
- `dotnet build` passes  
- `dotnet run` starts without DI errors  

**Acceptance Criteria:**  
- [x] All 3 services registered  
- [x] Application starts without errors  
- [x] Build passes  

---

## HOUSEKEEPING TASKS

### T1.17 — Upgrade JWT Library (CVE Fix)

**Goal:** Upgrade System.IdentityModel.Tokens.Jwt to resolve build warning NU1902.  
**Spec Reference:** Phase 0 UPDATE_V0.md §4 (known risk, mitigation planned for Phase 1)  
**Done Condition:**  
- Package upgraded to 8.0.0+ (latest stable compatible with .NET 9)  
- Build warning NU1902 eliminated  
- Login, refresh, me endpoints still work correctly  
- `dotnet build` passes with 0 warnings  

**Acceptance Criteria:**  
- [x] JWT library upgraded  
- [x] 0 build warnings  
- [x] All auth endpoints verified working  
- [x] Build passes  

---

## DOCUMENTATION TASKS

### T1.18 — Update SWAGGER_DOCUMENTATION.md with Phase 1 Details

**Goal:** Replace Phase 1 "preview" stubs with complete endpoint documentation.  
**Spec Reference:** SWAGGER_DOCUMENTATION.md (currently has preview tables only)  
**Done Condition:**  
- Full request/response schemas for all Phase 1 endpoints  
- HTTP status codes documented  
- Role requirements documented  
- Example request/response JSON included  

**Acceptance Criteria:**  
- [x] All 16 Phase 1 endpoints fully documented  
- [x] Request/response bodies defined  
- [x] Status codes and error cases listed  

---

### T1.19 — Update FRONTEND_CONTRACT.md with Phase 1 Endpoints

**Goal:** Add complete Phase 1 endpoint documentation to frontend contract.  
**Spec Reference:** FRONTEND_CONTRACT.md §Future Phases (currently has stubs)  
**Done Condition:**  
- Phase 1 stubs replaced with full endpoint definitions  
- Request/response examples added  
- Edge cases documented  

**Acceptance Criteria:**  
- [x] Frontend team can integrate based on this document  
- [x] All request shapes defined  
- [x] All response shapes defined  
- [x] Error cases covered  

---

## INTEGRATION TESTING TASKS

### T1.20 — End-to-End Verification of All Phase 1 Endpoints

**Goal:** Run manual HTTP requests against all Phase 1 endpoints and capture real results.  
**Spec Reference:** All Phase 1 spec sections  
**Done Condition:**  
- All TESTS_V1.md tests executed  
- Results captured with real HTTP responses  
- REQUESTS_V1.http file created with example requests  
- All tests pass or deferred items documented  

**Acceptance Criteria:**  
- [x] All happy-path tests pass  
- [x] All error-case tests pass  
- [x] All permission tests pass  
- [x] TESTS_V1.md updated with real results  

---

## TASK SUMMARY

| Category | Tasks | IDs |
|----------|-------|-----|
| Domain Layer | 3 | T1.01, T1.02, T1.03 |
| Infrastructure Layer | 3 | T1.04, T1.05, T1.06 |
| Application Layer (DTOs) | 3 | T1.07, T1.08, T1.09 |
| Application Layer (Services) | 3 | T1.10, T1.11, T1.12 |
| API Layer (Controllers) | 3 | T1.13, T1.14, T1.15 |
| DI Wiring | 1 | T1.16 |
| Housekeeping | 1 | T1.17 |
| Documentation | 2 | T1.18, T1.19 |
| Integration Testing | 1 | T1.20 |
| **TOTAL** | **20** | |

---

## EXECUTION ORDER (Dependency Chain)

```
T1.01 (Subscription entity) ──┐
T1.02 (FeatureFlag entity) ───┤
T1.03 (Tenant nav props) ─────┤
                               ├── T1.04 (DbSets + EF config)
                               │     └── T1.05 (Migration)
                               │
T1.07 (Tenant DTOs) ──────────┤
T1.08 (Subscription DTOs) ────┤
T1.09 (FeatureFlag DTOs) ─────┤
                               ├── T1.10 (TenantService)
                               ├── T1.11 (SubscriptionService)
                               ├── T1.12 (FeatureFlagService)
                               │     │
                               │     ├── T1.13 (TenantsController)
                               │     ├── T1.14 (SubscriptionsController)
                               │     ├── T1.15 (FeatureFlagsController)
                               │     │     │
                               │     │     └── T1.16 (DI registration)
                               │     │
T1.06 (Middleware enhancement) ┘
T1.17 (JWT upgrade) ── independent
T1.18 + T1.19 (Docs) ── after controllers
T1.20 (E2E testing) ── after everything
```

---

*All tasks ready for implementation upon approval. No task exceeds 1 day of effort.*
