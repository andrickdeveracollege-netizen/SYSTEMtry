# UST Angelicum College – Event Management System

Role-secured REST Web API (ASP.NET Core 8, EF Core, SQL Server, JWT) with a plain HTML/CSS/JavaScript client.
Domain: **Event Ticketing & Registration** (facility booking + registration with a capacity cap).

## What it does
- Landing page: bulletin board (cork board) with approved upcoming events, and a facility calendar for visitors
- Sign in with an Angelicum account (`@ustangelicum.edu.ph`), or continue as a visitor (outsider) with just an email
- **Request an Event**: tick the offices you need (Auxiliary, ITMC, College, TVET, Library); per office choose facilities and equipment, and optionally attach that office's endorsement letter
- Each office approves or rejects **only its own part** and can leave a note for the requester at any time; the event is Approved when every office approved, Rejected when any rejects
- **Register for an Event**: must match the event's target audience; registrations can never exceed the event capacity
- The requester sees **who registered** and can **export the list to Excel (.xlsx)**
- Double booking of a facility is refused (409); school events have priority over outsiders

## Roles (derived from the email, never chosen)
| Email format | Role |
|---|---|
| `firstname.surname.college@ustangelicum.edu.ph` (also `.shs` `.jhs` `.elem`) | Student of that level |
| `firstname.surname@ustangelicum.edu.ph` (no extension) | Faculty / staff |
| Faculty promoted by the super admin | Administrator (posts school events) |
| Office admin (Auxiliary, ITMC, College, TVET, Library) | approve / reject / cancel for their office; cannot register or request |
| `admin@ustangelicum.edu.ph` | Super admin: posts institutional events, resets passwords, sets positions, acts on any office |
| Guest / outsider (no account, email only) | request or register without signing in |

Rules: school requests have priority over outsiders; outsiders may book **weekends only**; only facilities big enough for the attendee count are offered; registration cannot exceed capacity; institutional events need no registration; requesters can edit, move or cancel; reset password = `Ustangelicum72`.

## Status workflow (enforced on the server)
`Pending → Approved | Rejected`, and `Pending/Approved → Cancelled` (by the requester). Reviewed requests cannot be reviewed again; cancelled events cannot be approved.

## Prerequisites
.NET 8 SDK, SQL Server / SQL Express / LocalDB, and `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Run
Secrets are **not** in Git. Set them on your own machine first.

```bash
cd EventManagement
# 1. JWT signing key (32+ characters), stored in user-secrets
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "put-a-random-string-of-32-or-more-characters-here"
# 2. only if your SQL Server is not LocalDB, point to yours (also kept out of Git)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\\SQLEXPRESS;Database=EventManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
# 3. create the database and seed data
dotnet ef database update
# 4. run the API and the front end together
dotnet run
```
In Visual Studio: right-click the project, **Manage User Secrets**, paste the same keys as JSON; create the database with `Update-Database` in the Package Manager Console.

- Front end: https://localhost:7263/ (or http://localhost:5162/)
- Swagger UI: https://localhost:7263/swagger — click **Authorize** and paste the token returned by `/api/auth/login`
- The app creates and seeds the database on start. If the project has no `Migrations` folder yet it builds the schema from the model; with migrations it applies them.

## How the requirements are met
| Requirement | Where |
|---|---|
| Roles + server-side checks | `[Authorize(Roles=...)]` on Admin / SuperAdmin controllers; office admins are limited to their own office in the service layer; admins cannot request or register (403) |
| Status workflow | Event/office request: `Pending → Approved / Rejected`, `Pending/Approved → Cancelled`; invalid moves return 400. Registration: `Registered → Cancelled` (can re-register) |
| Business rule | Registrations cannot exceed the event capacity (409); facility capacity and double booking are refused (409); outsiders weekends only |
| Report | `GET /api/admin/reports/events` (registered vs capacity per event) shown in the Approvals page; registrant list + Excel export per event |
| Passwords | Hashed with PBKDF2 (salted, 100,000 iterations) in `Service/PasswordHasher.cs`; never stored in plain text |
| Validation | Data annotations on the DTOs (server) and checks in the forms (client) |
| Errors | `{ "message": "..." }` with 400 / 401 / 403 / 404 / 409; created resources return 201, cancels return 204 |

## Test accounts
| Role | Email | Password |
|---|---|---|
| Super admin | admin@ustangelicum.edu.ph | Admin@2026 |
| Admin – Auxiliary | auxiliary@ustangelicum.edu.ph | Admin@2026 |
| Admin – ITMC | itmc@ustangelicum.edu.ph | Admin@2026 |
| Admin – College (Ms. Judith) | judith@ustangelicum.edu.ph | Admin@2026 |
| Admin – TVET (Ms. Chavez) | chavez@ustangelicum.edu.ph | Admin@2026 |
| Admin – Library | library@ustangelicum.edu.ph | Admin@2026 |
| College student | juan.delacruz.college@ustangelicum.edu.ph | Student@2026 |
| SHS student | maria.santos.shs@ustangelicum.edu.ph | Student@2026 |
| Faculty | pedro.reyes@ustangelicum.edu.ph | Faculty@2026 |
| Administrator | ana.lopez@ustangelicum.edu.ph | Faculty@2026 |

Seed data: facilities (capacities are placeholders, edit in AppDBContext), equipment, and one approved sample event.

## Main endpoints
| Method | Route | Who |
|---|---|---|
| POST | /api/auth/register, /api/auth/login | anyone |
| GET | /api/events/bulletin, /api/events/calendar, /api/facilities | anyone |
| GET/POST/PUT/DELETE | /api/events/mine, /api/events/{id}, /api/events/request, /api/events/{id} | signed-in user (own events) |
| GET/POST | /api/registrations/mine, /api/registrations, /api/registrations/{id}/cancel | signed-in user |
| GET | /api/admin/requests?status=, /api/admin/requests/{id}/endorsement | Admin (own office) |
| POST | /api/admin/requests/{id}/approve, /reject | Admin (own office) |
| GET | /api/admin/reports/events | Admin – registrations vs capacity per event |
| DELETE | /api/admin/events/{id} | Admin – cancel an event |
| GET | /api/events/{id}/registrations (and /export for .xlsx) | Requester or office admin – who registered |
| GET | /api/guest/events/{id}/registrations (and /export)?email=&code= | Guest requester |
| POST | /api/guest/events/request, /api/guest/registrations | guest (email) |
| GET/PUT/DELETE | /api/guest/events/{id}?email=&code= | guest (manage code) |
| GET/POST/PUT | /api/superadmin/users, /users/{id}/reset-password, /users/{id}/position, /events | Super admin |

Errors are returned as `{ "message": "..." }` with 400 / 401 / 403 / 404 / 409.
