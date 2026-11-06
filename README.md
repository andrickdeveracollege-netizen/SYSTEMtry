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

## Roles
| Role | Can do |
|---|---|
| Attendee / Organizer (`User`) | register, cancel own registration, request an event, edit/cancel own pending request |
| Admin (one per office) | review requests of their own office, view letters, approve / reject, see the registrations report |

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
| Admin – Auxiliary | auxiliary@ustangelicum.edu.ph | Admin@2026 |
| Admin – ITMC | itmc@ustangelicum.edu.ph | Admin@2026 |
| Admin – College (Ms. Judith) | judith@ustangelicum.edu.ph | Admin@2026 |
| Admin – TVET (Ms. Chavez) | chavez@ustangelicum.edu.ph | Admin@2026 |
| Admin – Library | library@ustangelicum.edu.ph | Admin@2026 |
| Attendee / Organizer | student@ustangelicum.edu.ph | Student@2026 |

Seed data: 29 facilities, 12 equipment items, and one approved sample event (200 seats).

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

Errors are returned as `{ "message": "..." }` with 400 / 401 / 403 / 404 / 409.
