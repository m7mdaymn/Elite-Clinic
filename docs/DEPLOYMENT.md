# Deployment Guide

> **Version:** 5.0 | **Last Updated:** 2026-02-07

---

## Prerequisites

- .NET 9 SDK (or .NET 9 Runtime for production)
- SQL Server 2019+ (or Azure SQL Database)
- Outbound HTTPS for WhatsApp API integration (when configured)

---

## 1. Environment Configuration

### Required Environment Variables

```bash
# Database connection
ConnectionStrings__DefaultConnection="Server=your-server;Database=EliteClinic;User Id=your-user;Password=your-password;TrustServerCertificate=True"

# JWT Configuration
JwtSettings__SecretKey="your-secret-key-minimum-32-characters-long"
JwtSettings__Issuer="EliteClinic"
JwtSettings__Audience="EliteClinicApp"
JwtSettings__AccessTokenExpirationMinutes=1440
JwtSettings__RefreshTokenExpirationDays=7

# Runtime
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5094
```

### Security Recommendations

| Setting | Development | Production |
|---------|------------|------------|
| JWT Secret Key | Can use fallback | **Must** use env variable (32+ chars) |
| DB Connection | Can use fallback | **Must** use env variable |
| Swagger | Enabled | Consider disabling or restricting |
| CORS | Allow any origin | Restrict to specific origins |
| Logging Level | Debug | Warning or Information |
| Auto-Migrate | Enabled | Consider manual migrations |

---

## 2. Database Setup

### Automatic (Default Behavior)

The application automatically applies pending EF Core migrations and seeds initial data on every startup:

```csharp
// Program.cs behavior
db.Database.Migrate();  // Applies all pending migrations
SeedData(app);          // Idempotent seed data
```

This is suitable for development and small deployments. Seed data includes:
- 6 roles: SuperAdmin, ClinicOwner, ClinicManager, Receptionist, Doctor, Patient
- 4 demo tenants with subscriptions and feature flags
- Demo users for each role

### Manual Migration (Recommended for Production)

```bash
# Generate SQL script for review
dotnet ef migrations script --project src/EliteClinic.Infrastructure --startup-project src/EliteClinic.Api -o migration.sql

# Apply via SQL Server Management Studio or sqlcmd
sqlcmd -S your-server -d EliteClinic -U your-user -P your-password -i migration.sql
```

To disable auto-migration, comment out the `db.Database.Migrate()` call in Program.cs.

---

## 3. Build & Publish

```bash
# Build release
dotnet publish src/EliteClinic.Api -c Release -o ./publish

# The published output is in ./publish/
# Run with:
dotnet EliteClinic.Api.dll
```

### Docker (Optional)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY publish/ .
EXPOSE 5094
ENV ASPNETCORE_URLS=http://+:5094
ENTRYPOINT ["dotnet", "EliteClinic.Api.dll"]
```

```bash
docker build -t elite-clinic .
docker run -p 5094:5094 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=EliteClinic;..." \
  -e JwtSettings__SecretKey="your-32-char-secret-key-here-minimum" \
  elite-clinic
```

---

## 4. Reverse Proxy (Nginx Example)

```nginx
server {
    listen 80;
    server_name clinic-api.yourdomain.com;

    location / {
        proxy_pass http://localhost:5094;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

For HTTPS, add SSL certificate configuration and redirect HTTP to HTTPS.

---

## 5. WhatsApp Integration Setup

The system uses a **template-based** WhatsApp messaging architecture. Messages are queued and processed asynchronously.

### Current Implementation

The API **simulates** WhatsApp sending — messages are stored in the database with delivery status tracking (Pending → Sent → Delivered → Failed). To connect to a real WhatsApp provider:

1. **Configure tenant WhatsApp numbers** via `PUT /api/clinic/settings`:
   ```json
   {
     "whatsAppSenderNumber": "+966500000000",
     "supportWhatsAppNumber": "+966500000001"
   }
   ```

2. **Enable feature flag**: Set `whatsappAutomation: true` via `PUT /api/platform/feature-flags/{tenantId}`

3. **Integrate provider**: Modify `MessagesService.cs` to call your WhatsApp Business API provider (e.g., Twilio, MessageBird, WhatsApp Cloud API) instead of simulating sending.

### Message Templates (10 defined)

| Template Name | Trigger | Channel |
|--------------|---------|---------|
| `patient_credentials` | Patient creation | WhatsApp |
| `queue_ticket_issued` | Queue ticket created | WhatsApp |
| `queue_turn_approaching` | Patient's turn is near | WhatsApp |
| `booking_confirmed` | Booking created | WhatsApp |
| `booking_cancelled` | Booking cancelled | WhatsApp |
| `booking_rescheduled` | Booking rescheduled | WhatsApp |
| `booking_reminder` | Day before appointment | WhatsApp |
| `medication_reminder` | Post-visit medication | PWA |
| `followup_reminder` | Follow-up appointment | PWA |
| `visit_summary` | After visit completion | WhatsApp |

### Sending Messages via API

```bash
curl -X POST http://localhost:5094/api/clinic/messages/send \
  -H "Authorization: Bearer {token}" \
  -H "X-Tenant: demo-clinic" \
  -H "Content-Type: application/json" \
  -d '{
    "channel": "WhatsApp",
    "recipientPhone": "966500000001",
    "templateName": "patient_credentials",
    "variables": {
      "patientName": "Ahmed",
      "username": "patient_1",
      "password": "temp123"
    }
  }'
```

---

## 6. PWA Notifications Setup

The system supports Web Push (PWA) notifications via the standard Push API subscription model.

### Flow
1. Frontend subscribes: `POST /api/clinic/notifications/subscribe` with `{endpoint, p256dh, auth}`
2. Backend sends: `POST /api/clinic/notifications/send` with `{userId, title, body}`
3. Current implementation stores subscriptions and simulates push delivery

### Production Integration
To enable real push notifications, integrate a Web Push library (e.g., `web-push` for Node.js or `WebPush` NuGet package) in the `NotificationsService.cs`.

---

## 7. Monitoring

### Health Check

```bash
# Returns Healthy/Unhealthy with database status
curl http://localhost:5094/api/health
```

Response:
```json
{
  "success": true,
  "data": {
    "status": "Healthy",
    "database": "Connected",
    "version": "0.0.1",
    "timestamp": "2026-02-07T10:00:00Z"
  }
}
```

### Logging

The application uses **Serilog** for structured logging. Default configuration logs to console. For production, configure file or external sink:

```json
// appsettings.Production.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/elite-clinic-.log", "rollingInterval": "Day" } }
    ]
  }
}
```

### Swagger

Swagger UI is available at `/swagger` in all environments. For production, consider:
- Restricting access via reverse proxy (IP whitelist or basic auth)
- Disabling by removing `app.UseSwagger()` and `app.UseSwaggerUI()` from Program.cs

---

## 8. Security Checklist

- [ ] Set unique JWT secret key (32+ characters) via environment variable
- [ ] Set database connection string via environment variable
- [ ] Configure CORS to allow only your frontend domain(s)
- [ ] Restrict or disable Swagger in production
- [ ] Use HTTPS (terminate at reverse proxy or configure Kestrel)
- [ ] Review auto-migration behavior for production
- [ ] Set appropriate log levels (Warning or higher)
- [ ] Ensure SQL Server firewall rules are restrictive
- [ ] Review tenant seed data — remove demo tenants in production
- [ ] Set up database backups

---

## 9. Scaling Considerations

- **Stateless API**: Can run multiple instances behind a load balancer
- **Database**: Single SQL Server instance; consider read replicas for heavy read loads
- **JWT**: Stateless tokens — no session store needed across instances
- **Tenant isolation**: Global query filters ensure data safety regardless of routing
- **Audit trail**: Automatic via `SaveChangesAsync` override — no additional setup needed

---

## 10. Backup & Recovery

### Database Backup
```bash
# SQL Server backup
sqlcmd -S your-server -Q "BACKUP DATABASE [EliteClinic] TO DISK='C:\Backups\EliteClinic.bak'"
```

### Seed Data Recovery
If seed data is lost, simply restart the application — the idempotent seed routine will recreate roles, demo tenants, and demo users.
