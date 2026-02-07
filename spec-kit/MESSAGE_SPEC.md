# MESSAGE_SPEC.md — WhatsApp & PWA Notification Templates

> **Version:** 5.0  
> **Last Updated:** 2026-02-07  
> **Status:** All templates implemented and tested

---

## GENERAL RULES

1. **WhatsApp is the official communication channel.** PWA notifications are supplementary.
2. All messages are **logged** with full audit trail: attempt time, delivery status, error reason.
3. Retry strategy: **3 attempts** with exponential backoff (1 min → 5 min → 15 min). Configurable per tenant.
4. Failed messages after all retries are marked `failed` with reason logged permanently.
5. Each tenant has its own configured WhatsApp sender number.
6. **No OTP, no verification codes, no email** — ever.
7. Message templates support Arabic + English (tenant configurable; default Arabic).

---

## MESSAGE DELIVERY STATES

```
pending → sending → sent → delivered → read
                  ↘ failed (with reason)
                  ↘ retrying (attempt 2, 3)
```

---

## WHATSAPP MESSAGE TEMPLATES

### WA-001: Patient Credentials (First Time)

| Property | Value |
|----------|-------|
| **Name** | `patient_credentials` |
| **Trigger** | Reception creates a new patient profile |
| **Frequency** | **ONE TIME ONLY** — never auto-resent |
| **Recipient** | Patient (phone number from profile) |
| **Variables** | `{{patientName}}`, `{{username}}`, `{{password}}`, `{{clinicName}}`, `{{pwaLink}}` |
| **Retry** | Yes (3 attempts) |
| **Failure Logging** | Log failure. Reception can manually trigger re-send. |

**Template:**
```
مرحباً {{patientName}} 👋

تم تسجيلك في {{clinicName}}.

بيانات الدخول الخاصة بك:
📱 اسم المستخدم: {{username}}
🔑 كلمة المرور: {{password}}

يمكنك متابعة حجوزاتك ونتائجك من هنا:
🔗 {{pwaLink}}

— {{clinicName}}
```

---

### WA-002: Booking Confirmation

| Property | Value |
|----------|-------|
| **Name** | `booking_confirmation` |
| **Trigger** | Patient books an appointment online |
| **Frequency** | Per booking |
| **Recipient** | Patient |
| **Variables** | `{{patientName}}`, `{{doctorName}}`, `{{date}}`, `{{time}}`, `{{clinicName}}`, `{{ticketNumber}}` |
| **Retry** | Yes (3 attempts) |
| **Failure Logging** | Logged. Booking is valid regardless of message delivery. |

**Template:**
```
تأكيد الحجز ✅

أهلاً {{patientName}},
تم حجز موعدك بنجاح.

👨‍⚕️ الدكتور: {{doctorName}}
📅 التاريخ: {{date}}احنا 
⏰ الوقت: {{time}}
🎫 رقم التذكرة: {{ticketNumber}}

— {{clinicName}}
```

---

### WA-003: Queue Ticket Issued

| Property | Value |
|----------|-------|
| **Name** | `queue_ticket_issued` |
| **Trigger** | Reception issues a walk-in queue ticket |
| **Frequency** | Per ticket |
| **Recipient** | Patient |
| **Variables** | `{{patientName}}`, `{{ticketNumber}}`, `{{doctorName}}`, `{{aheadCount}}`, `{{estimatedWait}}`, `{{clinicName}}` |
| **Retry** | Yes (3 attempts) |
| **Failure Logging** | Logged. Ticket is valid regardless. |

**Template:**
```
تذكرة الانتظار 🎫

أهلاً {{patientName}},
رقم تذكرتك: {{ticketNumber}}
👨‍⚕️ الدكتور: {{doctorName}}
⏳ أمامك: {{aheadCount}} مريض
⏱️ الوقت المتوقع: {{estimatedWait}} دقيقة

— {{clinicName}}
```

---

### WA-004: Your Turn

| Property | Value |
|----------|-------|
| **Name** | `your_turn` |
| **Trigger** | Doctor calls the patient's ticket number |
| **Frequency** | Per call |
| **Recipient** | Patient |
| **Variables** | `{{patientName}}`, `{{doctorName}}`, `{{roomNumber}}`, `{{clinicName}}` |
| **Retry** | Yes (2 attempts — time-sensitive, limited retries) |
| **Failure Logging** | Logged. Doctor proceeds regardless. |

**Template:**
```
دورك الآن! 🔔

{{patientName}}, الدكتور {{doctorName}} في انتظارك.
{{#if roomNumber}}🚪 الغرفة: {{roomNumber}}{{/if}}

توجه الآن من فضلك.

— {{clinicName}}
```

---

### WA-005: Visit Results Summary

| Property | Value |
|----------|-------|
| **Name** | `visit_summary` |
| **Trigger** | Doctor finishes visit and records are saved |
| **Frequency** | Per visit completion |
| **Recipient** | Patient |
| **Variables** | `{{patientName}}`, `{{doctorName}}`, `{{date}}`, `{{diagnosis}}`, `{{prescriptions}}`, `{{labRequests}}`, `{{followUpDate}}`, `{{clinicName}}`, `{{pwaLink}}` |
| **Retry** | Yes (3 attempts) |
| **Failure Logging** | Logged. Records exist in system regardless. |

**Template:**
```
ملخص الزيارة 📋

أهلاً {{patientName}},
زيارتك مع د. {{doctorName}} بتاريخ {{date}}:

🔍 التشخيص: {{diagnosis}}

💊 الأدوية:
{{prescriptions}}

{{#if labRequests}}
🔬 التحاليل المطلوبة:
{{labRequests}}
{{/if}}

{{#if followUpDate}}
📅 موعد المتابعة: {{followUpDate}}
{{/if}}

لمزيد من التفاصيل:
🔗 {{pwaLink}}

— {{clinicName}}
```

---

### WA-006: Follow-Up Reminder

| Property | Value |
|----------|-------|
| **Name** | `followup_reminder` |
| **Trigger** | Scheduled: 1 day before follow-up date |
| **Frequency** | Once per follow-up |
| **Recipient** | Patient |
| **Variables** | `{{patientName}}`, `{{doctorName}}`, `{{followUpDate}}`, `{{clinicName}}`, `{{clinicPhone}}` |
| **Retry** | Yes (3 attempts) |
| **Failure Logging** | Logged. |

**Template:**
```
تذكير بموعد المتابعة 📅

أهلاً {{patientName}},
لديك موعد متابعة غداً مع د. {{doctorName}}.

📅 {{followUpDate}}

للاستفسار أو التغيير:
📞 {{clinicPhone}}

— {{clinicName}}
```

---

### WA-007: Password Reset (Manual by Staff)

| Property | Value |
|----------|-------|
| **Name** | `password_reset` |
| **Trigger** | Staff manually resets patient password |
| **Frequency** | Per reset action |
| **Recipient** | Patient |
| **Variables** | `{{patientName}}`, `{{username}}`, `{{newPassword}}`, `{{clinicName}}`, `{{pwaLink}}` |
| **Retry** | Yes (3 attempts) |
| **Failure Logging** | Logged. Staff informed of delivery failure. |

**Template:**
```
تحديث كلمة المرور 🔑

أهلاً {{patientName}},
تم تحديث كلمة مرورك.

📱 اسم المستخدم: {{username}}
🔑 كلمة المرور الجديدة: {{newPassword}}

🔗 {{pwaLink}}

— {{clinicName}}
```

---

## PWA NOTIFICATION TEMPLATES

### PWA-001: Medication Reminder

| Property | Value |
|----------|-------|
| **Name** | `medication_reminder` |
| **Trigger** | Scheduled based on prescription frequency |
| **Feature Flag** | `pwa_notifications` must be enabled |
| **Recipient** | Patient (PWA) |
| **Variables** | `{{medicationName}}`, `{{dosage}}`, `{{instructions}}` |

**Notification:**
```
Title: تذكير بالدواء 💊
Body: حان موعد {{medicationName}} - {{dosage}}
{{#if instructions}}ملاحظة: {{instructions}}{{/if}}
```

---

### PWA-002: Follow-Up Reminder

| Property | Value |
|----------|-------|
| **Name** | `followup_reminder_pwa` |
| **Trigger** | 1 day before follow-up date |
| **Feature Flag** | `pwa_notifications` must be enabled |
| **Recipient** | Patient (PWA) |
| **Variables** | `{{doctorName}}`, `{{followUpDate}}` |

**Notification:**
```
Title: تذكير بموعد المتابعة 📅
Body: لديك موعد متابعة غداً مع د. {{doctorName}} - {{followUpDate}}
```

---

### PWA-003: Queue Turn Approaching

| Property | Value |
|----------|-------|
| **Name** | `queue_approaching_pwa` |
| **Trigger** | When patient is 3 positions away in queue |
| **Feature Flag** | `pwa_notifications` must be enabled |
| **Recipient** | Patient (PWA) |
| **Variables** | `{{aheadCount}}`, `{{doctorName}}` |

**Notification:**
```
Title: دورك قرب! 🔔
Body: باقي {{aheadCount}} مريض قبلك عند د. {{doctorName}}. توجه للعيادة.
```

---

## MESSAGE LOG SCHEMA

Every message (WhatsApp or PWA) is logged:

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique log ID |
| `tenantId` | GUID | Tenant scope |
| `templateName` | string | e.g., `patient_credentials` |
| `recipientPhone` | string | Phone number |
| `recipientUserId` | GUID | Patient user ID |
| `channel` | enum | `WhatsApp` or `PWA` |
| `status` | enum | `pending`, `sending`, `sent`, `delivered`, `read`, `failed`, `retrying` |
| `attemptCount` | int | Current attempt (1-3) |
| `lastAttemptAt` | datetime | Last attempt timestamp |
| `sentAt` | datetime? | When successfully sent |
| `deliveredAt` | datetime? | When delivered |
| `failureReason` | string? | Error message if failed |
| `variables` | JSON | Template variables snapshot |
| `createdAt` | datetime | Log creation |

---

*All templates and variables finalized before implementation. Updated per phase as triggers are wired.*
