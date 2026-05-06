# Phase 1 — API aligned to new schema, unified Docker stack

## Summary

- Replaced the old Tenant/User model with the full domain schema (13 tables, 3 analytics views)
- Rewrote the EF Core entity layer to map to `schema.sql` using snake_case column naming
- Fixed JWT token signing (tokens were previously unsigned — a security bug)
- Merged the separate Schema and API docker-compose files into a single root `docker-compose.yml`
- Added a seeded admin account on fresh database startup

## What changed

### Deleted
| File | Reason |
|---|---|
| `Models/Tenant.cs` | Replaced by faculties/organisations in new schema |
| `Services/TenantService.cs` | No longer needed |
| `Interfaces/ITenantService.cs` | No longer needed |

### New entity models (all in `api/CobbleAPI/CobbleAPI/Models/`)
`Role`, `Permission`, `RolePermission`, `Faculty`, `Industry`, `Organisation`, `Contact`, `Project`, `ProjectApplication`, `Event`, `EventAttendance`, `AuditLog`

Each entity maps directly to a table in `schema.sql`. Snake_case column naming is applied automatically in `ApplicationDbContext.OnModelCreating` via a regex helper — no extra NuGet packages required.

### Updated
| File | What changed |
|---|---|
| `Models/User.cs` | Uses `RoleId` FK (→ `roles` table), adds `FacultyId`, `OrganisationId`, soft-delete, audit columns |
| `Models/Dto.cs` | Replaced tenant-based DTOs with `LoginRequest`, `RegisterUserRequest`, `AuthResponse` |
| `Data/ApplicationDbContext.cs` | Full rewrite — all 13 DbSets, explicit relationship config, snake_case naming loop |
| `Services/AuthService.cs` | Fixed: JWT tokens are now **signed** with HMAC-SHA256 (previously unsigned). Claims include `role`, `faculty_id`, `organisation_id` |
| `Interfaces/IAuthService.cs` | Cleaned up method parameter names |
| `Controllers/AuthController.cs` | `POST /api/auth/login` loads `Role` via `Include`, updates `last_login_at`. `POST /api/auth/register` (admin-only) creates new staff accounts |
| `Program.cs` | Removed `EnsureCreated()`. Added startup seeding (creates `admin@system.com` if no users exist). Added CORS policy for `localhost:3000` |
| `appsettings.json` | Connection string updated to `qut_crm` DB credentials |
| `docker-compose.yml` (root) | Unified file — runs Postgres 16, API, and pgAdmin. Postgres now auto-loads `Schema/schema.sql` on first run |
| `Schema/docker-compose.yml` | Ports changed to `5433`/`5051` to avoid conflicts when run alongside the main compose |

## How to run

Stop any existing containers first:
```bash
docker compose down -v   # -v wipes volumes for a clean DB
```

Then from the repo root:
```bash
docker compose up --build -d
```

Services:
| Service | URL |
|---|---|
| API + Swagger UI | http://localhost:8080/swagger |
| pgAdmin | http://localhost:5050 (login: `admin@local.dev` / `changeme_local_only`) |

## Test the login

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@system.com","password":"Admin123!"}'
```

Expected response includes a signed JWT and `"role": "admin"`.

## Auth endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/login` | Public | Returns JWT on valid credentials |
| `POST` | `/api/auth/register` | Admin only | Creates a new staff/admin account |

Valid roles for registration: `admin`, `course_organiser`, `industry_partner`

## Architecture notes

- **Schema-first**: `Schema/schema.sql` is the single source of truth for the database. EF Core entities map to it — EF Core does not manage schema creation or migrations.
- **Soft deletes**: All domain entities have a `deleted_at` column. Records are never physically deleted. Query filters for live records will be added per-endpoint in Phase 2.
- **Audit trail**: `audit_log` table is populated by Postgres triggers defined in `schema.sql`. No application-side audit code is needed.
- **JWT secret**: The dev secret is committed in `appsettings.json` for local convenience. For any deployed environment, override via the `JWT_SECRET` environment variable.

## What comes next (Phase 2)

- `GET/POST/PUT /api/organisations` — Industry partner CRUD with search, filter, and status management (CO-1, CO-2, CO-6)
- `GET/POST /api/organisations/{id}/contacts` — Contact management (PT-3)
- `GET/POST/PUT /api/projects` — Project CRUD linked to organisations (CO-4, CO-5)
- `POST /api/applications` — Industry partner EOI submission (IP-2)
- `PUT /api/applications/{id}/review` — Admin approval/rejection workflow (AD-2)
- `GET /api/analytics/*` — Wire up the three SQL views as read-only endpoints
- Next.js frontend scaffold with login tab
