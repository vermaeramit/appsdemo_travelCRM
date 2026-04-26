# Appsdemo Travel CRM

Multi-tenant Travel CRM / ERP built on **ASP.NET Core 8 + Dapper + PostgreSQL**.

> One DB per tenant. Subscription plans gate features. Roles & per-permission access control.
> Razor MVC + Tabler UI for the front-end. JWT-secured `/api/*` endpoints for integrations.

---

## 1. Prerequisites

- **.NET 8 SDK** (or .NET 9 — the projects target net8.0).
- **PostgreSQL 17** running locally on `localhost:5432` with the `postgres` user (default password `postgres`). Adjust in `appsettings.json` -> `ConnectionStrings` if your password is different.
- (Optional) `psql` on PATH for inspecting databases.

## 2. Solution layout

```
Appsdemo.TravelCrm.sln
├── src/
│   ├── Appsdemo.TravelCrm.Web         ← MVC + API host (the runnable project)
│   ├── Appsdemo.TravelCrm.Core        ← models, security catalog, DTOs
│   ├── Appsdemo.TravelCrm.Data        ← Dapper repositories + connection factories
│   └── Appsdemo.TravelCrm.Migrations  ← DbUp runner + embedded .sql scripts
└── tests/
    └── Appsdemo.TravelCrm.Tests
```

## 3. First run

```bash
dotnet restore
dotnet build
dotnet run --project src/Appsdemo.TravelCrm.Web
```

On first start, the Web app will:

1. Create database `appsdemo_master` (if missing) and run master migrations.
2. Seed three subscription plans: **Starter / Pro / Enterprise**.
3. Seed the super-admin from `appsettings.json` → `SuperAdmin` (default `vermaeramit@gmail.com` / `Admin@123`).

If Postgres isn't running, the app starts anyway and logs a warning — restart it once the DB is up.

## 4. Logging in

- **Super admin**: open `http://localhost:5000/admin/login` → sign in with the seeded super-admin → land on the **Tenants** page.
- **Create your first tenant** at `/admin/tenants/new`. The provisioner will:
  - create database `appsdemo_<code>` on the same server
  - run all tenant migrations (identity, master data, CRM, quotes, bookings, invoices, payments, audit)
  - seed system roles and permissions
  - create the tenant-admin user you specified
- **Tenant login**:
  - **Subdomain mode** (recommended for prod): point `*.appsdemo.local` at `127.0.0.1` in your hosts file, then visit `http://<code>.appsdemo.local:5000/auth/login`.
  - **Path-prefix mode** (works out-of-the-box on `localhost`): the dev fallback resolves the tenant code from `Tenancy:DefaultDevTenantCode` (`demo` by default). Change it in `appsettings.json` after creating a tenant called e.g. `demo`.

## 5. Multi-tenancy details

- **Master DB** (`appsdemo_master`) holds: `tenants`, `subscription_plans`, `plan_features`, `features`, `global_users`, `tenant_invoices`, `audit_log_global`.
- **Tenant DBs** (`appsdemo_<code>`) each hold: `users`, `roles`, `permissions`, `role_permissions`, `branches`, `audit_log`, master data, CRM/quotes/bookings/invoices/payments.
- The tenant DB password is encrypted at rest in `tenants.db_password_encrypted` using **ASP.NET Core Data Protection**. Keys live in `App_Data/DataProtection-Keys/`.
- Tenant resolution is in `Middleware/TenantResolutionMiddleware.cs`. Behavior:
  - URL on `<sub>.appsdemo.local` → tenant `<sub>`
  - URL `/admin/...` or `admin.appsdemo.local` → super-admin portal (no tenant resolved)
  - URL `/t/<code>/...` → tenant `<code>` (path-prefix mode)
  - `localhost` with no other hint → falls back to `Tenancy:DefaultDevTenantCode`

## 6. Access control

Three layers, all enforced server-side:

1. **Plan** → which features/modules are unlocked. Use `[RequireFeature("module.bookings")]` on a controller / action.
2. **Role** → bundle of permissions. System roles: `TenantAdmin`, `Manager`, `Sales`, `Ops`, `Accounts`, `ReadOnly`.
3. **Permission** → atomic, e.g. `leads.create`. Use `[HasPermission(Permissions.Leads.Create)]`.

The `TenantAdmin` role implicitly bypasses every permission check.

## 7. Background jobs

Hangfire is wired up with PostgreSQL storage (`appsdemo_hangfire` DB). The dashboard is at `/hangfire`, restricted to super-admins.

## 8. Manual migration runs

The migration project can also be run as a CLI:

```bash
# Master DB
dotnet run --project src/Appsdemo.TravelCrm.Migrations -- master "Host=localhost;Port=5432;Database=appsdemo_master;Username=postgres;Password=postgres"

# A specific tenant
dotnet run --project src/Appsdemo.TravelCrm.Migrations -- tenant "Host=localhost;Port=5432;Database=appsdemo_acme;Username=postgres;Password=postgres"
```

## 9. v1 module roadmap

- [x] Tenant onboarding & super-admin portal
- [x] Auth & user management (login, lockout, audit log, roles, permissions)
- [x] Master data tables (Branches, Destinations, Hotels, Suppliers, Services) — schema only
- [ ] CRM core UI (Leads, Follow-ups)
- [ ] Quotations UI (build itinerary, cost sheet, PDF, send)
- [ ] Bookings / Vouchers UI
- [ ] Invoicing & Receipts UI
- [ ] Dashboard widgets & Reports

## 10. Tech reference

| Concern        | Library / version |
|----------------|-------------------|
| Framework      | ASP.NET Core 8    |
| ORM            | Dapper 2.1        |
| Postgres       | Npgsql 8          |
| Migrations     | DbUp 6 (Postgres) |
| UI             | Tabler 1.0-beta20 + Bootstrap 5 + HTMX 2 + Alpine 3 |
| Auth           | Cookie + JWT, ASP.NET Data Protection, PBKDF2 password hashing |
| Background     | Hangfire 1.8 + Hangfire.PostgreSql |
| Logging        | Serilog (file + console) |
| PDF / Excel    | QuestPDF, ClosedXML |
| Validation     | FluentValidation 11 |
| Mapping        | Mapster 7 |
