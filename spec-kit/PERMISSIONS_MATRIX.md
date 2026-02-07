# PERMISSIONS_MATRIX.md — Role/Action Access Control

> **Version:** 1.1  
> **Last Updated:** 2026-02-07  
> **Status:** Phase 1 Complete

---

## ROLES DEFINITION (Clean — No Duplicates)

| Role | Scope | Description |
|------|-------|-------------|
| **SuperAdmin** | Platform | Platform owner. Manages tenants, subscriptions, feature flags, platform analytics. Does NOT operate daily clinic workflows. |
| **ClinicOwner** | Tenant | Clinic administrator. Full control over clinic configuration, staff, doctors, reports. Also referred to as ClinicAdmin. |
| **ClinicManager** | Tenant | Operations staff (reception/manager). Handles daily flow: patients, queue, payments, today's finance. |
| **Doctor** | Tenant | Medical provider. Manages own queue, creates visits, prescriptions, labs. |
| **Patient** | Tenant | End user. Persistent session. Views own data, queue status, bookings. |

---

## LEGEND

| Symbol | Meaning |
|--------|---------|
| ✅ | Full access |
| 📖 | Read-only access |
| 🔒 | Own data only |
| ⚙️ | Conditional (feature flag or setting dependent) |
| ❌ | No access |

---

## MODULE: Platform Administration

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Create tenant | ✅ | ❌ | ❌ | ❌ | ❌ |
| List all tenants | ✅ | ❌ | ❌ | ❌ | ❌ |
| View tenant details | ✅ | ❌ | ❌ | ❌ | ❌ |
| Activate/suspend/block tenant | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage subscriptions | ✅ | ❌ | ❌ | ❌ | ❌ |
| View/edit feature flags | ✅ | ❌ | ❌ | ❌ | ❌ |
| View platform analytics | ✅ | ❌ | ❌ | ❌ | ❌ |
| View platform audit logs | ✅ | ❌ | ❌ | ❌ | ❌ |
| View cross-tenant WhatsApp logs | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage tenant WhatsApp config | ✅ | ❌ | ❌ | ❌ | ❌ |
| Lock clinic for abuse | ✅ | ❌ | ❌ | ❌ | ❌ |

---

## MODULE: Authentication

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Login (staff) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Login (patient) | ❌ | ❌ | ❌ | ❌ | ✅ |
| Refresh token | ✅ | ✅ | ✅ | ✅ | ✅ |
| View own profile (me) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Logout | ✅ | ✅ | ✅ | ✅ | ❌ (never) |

---

## MODULE: Clinic Settings

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| View clinic settings | ❌ | ✅ | 📖 | ❌ | ❌ |
| Update clinic settings | ❌ | ✅ | ❌ | ❌ | ❌ |
| Configure working hours | ❌ | ✅ | ❌ | ❌ | ❌ |
| Configure WhatsApp numbers | ❌ | ✅ | ❌ | ❌ | ❌ |
| Configure booking rules | ❌ | ✅ | ❌ | ❌ | ❌ |

---

## MODULE: Staff / Employee Management

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Create staff | ❌ | ✅ | ❌ | ❌ | ❌ |
| Edit staff | ❌ | ✅ | ❌ | ❌ | ❌ |
| View staff list | ❌ | ✅ | 📖 | ❌ | ❌ |
| Disable/enable staff | ❌ | ✅ | ❌ | ❌ | ❌ |
| View staff schedule | ❌ | ✅ | 📖 | ❌ | ❌ |
| View login history | ❌ | ✅ | ❌ | ❌ | ❌ |

---

## MODULE: Doctor Management

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Create doctor | ❌ | ✅ | ❌ | ❌ | ❌ |
| Edit doctor profile | ❌ | ✅ | ❌ | 🔒 (own) | ❌ |
| Enable/disable doctor | ❌ | ✅ | ❌ | ❌ | ❌ |
| Configure doctor visit fields | ❌ | ✅ | ❌ | ❌ | ❌ |
| Configure services/pricing | ❌ | ✅ | ❌ | ❌ | ❌ |
| View doctor list | ❌ | ✅ | 📖 | ❌ | ❌ |
| Configure urgent case behavior | ❌ | ✅ | ❌ | ❌ | ❌ |

---

## MODULE: Patient Management

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Create patient | ❌ | ✅ | ✅ | ❌ | ❌ |
| Edit patient | ❌ | ✅ | ✅ | ❌ | ❌ |
| View patient list | ❌ | ✅ | ✅ | ❌ | ❌ |
| View patient detail | ❌ | ✅ | ✅ | 🔒 (own patients) | 🔒 (own) |
| Add sub-profile (child) | ❌ | ✅ | ✅ | ❌ | ❌ |
| Reset patient password | ❌ | ✅ | ✅ | ❌ | ❌ |
| Delete patient | ❌ | ✅ | ❌ | ❌ | ❌ |

---

## MODULE: Queue System

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Open session | ❌ | ✅ | ✅ | ✅ (own) | ❌ |
| Close session | ❌ | ✅ | ✅ | ✅ (own) | ❌ |
| Issue ticket (walk-in) | ❌ | ✅ | ✅ | ❌ | ❌ |
| Call ticket (next) | ❌ | ❌ | ❌ | ✅ (own queue) | ❌ |
| Start visit | ❌ | ❌ | ❌ | ✅ (own queue) | ❌ |
| Finish visit | ❌ | ❌ | ❌ | ✅ (own queue) | ❌ |
| Skip / no-show | ❌ | ✅ | ✅ | ✅ (own queue) | ❌ |
| Cancel ticket | ❌ | ✅ | ✅ | ❌ | ⚙️ (within rules) |
| View reception board | ❌ | ✅ | ✅ | ❌ | ❌ |
| View own queue | ❌ | ❌ | ❌ | ✅ | ❌ |
| View own ticket status | ❌ | ❌ | ❌ | ❌ | ✅ |
| Insert urgent ticket | ❌ | ✅ | ✅ | ✅ (own queue) | ❌ |

---

## MODULE: Visits & Medical Records

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Create visit record | ❌ | ❌ | ❌ | ✅ | ❌ |
| Edit visit record | ❌ | ❌ | ❌ | ✅ (own, same day) | ❌ |
| View visit details | ❌ | ✅ | ❌ | ✅ (own default) | 🔒 (own) |
| View all visits for patient | ❌ | ✅ | ❌ | ⚙️ (if permitted) | 🔒 (own) |
| Add prescription | ❌ | ❌ | ❌ | ✅ | ❌ |
| Add lab/imaging request | ❌ | ❌ | ❌ | ✅ | ❌ |
| Schedule follow-up | ❌ | ❌ | ❌ | ✅ | ❌ |
| View prescriptions | ❌ | ✅ | ❌ | ✅ | 🔒 (own) |

---

## MODULE: Payments & Finance

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Record payment | ❌ | ✅ | ✅ | ❌ | ❌ |
| Update payment status | ❌ | ✅ | ✅ | ❌ | ❌ |
| View payment status | ❌ | ✅ | ✅ | 📖 | 🔒 (own) |
| View daily revenue | ❌ | ✅ (all) | ✅ (today) | ❌ | ❌ |
| View monthly revenue | ❌ | ✅ | ❌ | ❌ | ❌ |
| View yearly revenue | ❌ | ✅ | ❌ | ❌ | ❌ |
| View revenue per doctor | ❌ | ✅ (all) | ✅ (today) | 🔒 (own) | ❌ |
| Add expense | ❌ | ✅ | ✅ | ❌ | ❌ |
| View expenses | ❌ | ✅ (all) | ✅ (today) | ❌ | ❌ |
| View profit | ❌ | ✅ (all) | ✅ (today) | ❌ | ❌ |

---

## MODULE: Online Booking

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| Book appointment online | ❌ | ❌ | ❌ | ❌ | ⚙️ (if enabled + account exists) |
| Cancel booking | ❌ | ✅ | ✅ | ❌ | ⚙️ (within rules) |
| Reschedule booking | ❌ | ✅ | ✅ | ❌ | ⚙️ (within rules) |
| View bookings | ❌ | ✅ | ✅ | 🔒 (own) | 🔒 (own) |
| Configure booking rules | ❌ | ✅ | ❌ | ❌ | ❌ |

---

## MODULE: WhatsApp & Notifications

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| View tenant WhatsApp logs | ❌ | ✅ | ❌ | ❌ | ❌ |
| View cross-tenant WA logs | ✅ | ❌ | ❌ | ❌ | ❌ |
| Send manual WhatsApp | ❌ | ✅ | ✅ | ❌ | ❌ |
| Configure WA sender number | ✅ | ❌ | ❌ | ❌ | ❌ |
| Send doctor note to reception | ❌ | ❌ | ❌ | ✅ | ❌ |
| Receive doctor notes | ❌ | ❌ | ✅ | ❌ | ❌ |

---

## MODULE: Reporting & Export

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| View doctor performance | ❌ | ✅ | ❌ | 🔒 (own) | ❌ |
| View patient trends | ❌ | ✅ | ❌ | ❌ | ❌ |
| Export reports | ❌ | ⚙️ (if enabled) | ❌ | ❌ | ❌ |
| View platform KPIs | ✅ | ❌ | ❌ | ❌ | ❌ |
| View expiring subscriptions | ✅ | ❌ | ❌ | ❌ | ❌ |
| View WA delivery health | ✅ | ❌ | ❌ | ❌ | ❌ |

---

## MODULE: Public SEO

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| View public clinic profile | — | — | — | — | — |
| View public doctors list | — | — | — | — | — |
| View public services | — | — | — | — | — |
| View public working hours | — | — | — | — | — |

> Public endpoints require NO authentication. They require `X-Tenant` header only. Always return 200.

---

## MODULE: Audit

| Action | SuperAdmin | ClinicOwner | ClinicManager | Doctor | Patient |
|--------|-----------|-------------|---------------|--------|---------|
| View platform audit logs | ✅ | ❌ | ❌ | ❌ | ❌ |
| View tenant audit logs | ❌ | ✅ | ❌ | ❌ | ❌ |
| View own login history | ✅ | ✅ | ✅ | ✅ | ❌ |

---

*Updated per phase as new modules are implemented.*
