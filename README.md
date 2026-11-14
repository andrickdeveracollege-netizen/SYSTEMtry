# UST Angelicum College – Event Management System

Role-secured REST Web API (ASP.NET Core 8, EF Core, SQL Server, JWT) with a plain HTML/CSS/JavaScript client.
Domain: **Event Ticketing & Registration** (facility booking + registration with a capacity cap).

## What it does
- Bulletin board (cork board) + shared facility calendar on the landing page
- Sign in with an Angelicum account (`@ustangelicum.edu.ph`)
- **Request an Event**: tick the offices you need (Auxiliary, ITMC, College, TVET, Library); per office choose facilities, equipment and upload that office's endorsement letter (optional only for College)
- Each office approves/rejects **only its own part**; the event is Approved when every office approved, Rejected when any rejects
- **Register for an Event**: must match the event's target audience; registrations can never exceed the event capacity
- Double booking of a facility is refused (409)

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
.NET 8 SDK, SQL Server / SQL Express / LocalDB, `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Run
```bash
cd EventManagement
# 1. set the JWT signing key (kept out of Git)
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "put-a-random-string-of-32-or-more-characters-here"
# 2. edit ConnectionStrings:DefaultConnection in appsettings.json for your SQL Server
# 3. create the database (the app also runs Migrate() on start)
dotnet ef migrations add InitialCreate
dotnet ef database update
# 4. run API + front end together
dotnet run
```
- Front end: http://localhost:5162/
- Swagger UI: http://localhost:5162/swagger (click **Authorize**, paste the token from `/api/auth/login`)

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
