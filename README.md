# البينة (Al-Bayyina) — Lawyers Platform

Arabic legal-consultation marketplace: clients search verified lawyers, book paid hourly sessions, chat and call in real time, and send free inquiries. Admins moderate users, lawyer verification, and content through a two-admin vote system.

The product name in the UI is **البينة**. The codebase and solution are named `Lawyers`.

---

## Architecture

Clean Architecture, four .NET projects plus a Nuxt 3 SPA:

```
ClientSide/                 Nuxt 3 + Vue 3 + Pinia + Nuxt UI Pro (RTL Arabic)
Lawyers.Api/                ASP.NET Core hosts HTTP + SignalR
Lawyers.Application/        MediatR commands/queries, DTOs, validators
Lawyers.Domain/             Entities, enums, value objects
Lawyers.InfraStructure/     EF Core (PostgreSQL), Identity, payments, email
```

```
Browser (Nuxt :3000)
    │  REST JWT          │  SignalR JWT query-string
    ▼                    ▼
Lawyers.Api  ──►  ConsultationHub (/hubs/consultations)
    │
    ├── MediatR handlers (Application)
    ├── EF Core / Identity (Infrastructure → PostgreSQL)
    ├── Kashier (card / wallet / InstaPay)
    └── SMTP (MailHog in development)
```

---

## Tech stack

| Layer | Stack |
|---|---|
| API | .NET 10, ASP.NET Core, MediatR, FluentValidation, Hangfire (registered, unused) |
| Auth | ASP.NET Identity + JWT Bearer + Google OAuth |
| Realtime | SignalR hub for chat, WebRTC signaling, and in-app notifications |
| Data | PostgreSQL via Npgsql / EF Core |
| Payments | Kashier test/live checkout + HMAC webhook |
| Frontend | Nuxt 3, Vue 3, Pinia, `@microsoft/signalr`, Cairo + Amiri fonts, RTL |

Scalar OpenAPI UI is mapped in Development (`/scalar`).

---

## Roles

| Role | Meaning |
|---|---|
| `Client` | Books consultations, chats, pays |
| `PendingLawyer` | Registered as a lawyer, waiting for verification |
| `Lawyer` | Verified; owns schedule, posts, free-inquiry inbox |
| `Admin` | Users, suspensions, verification votes, dashboard |

Verification and bans require **two admin approvals**. A lawyer can also self-apply (`POST /api/profile/apply-verification`); that path is implemented on the API but not yet wired in the Nuxt UI.

---

## Features

- Email/password register + login, email confirmation, forgot/reset password, Google OAuth, JWT refresh after role changes
- Public lawyer search and profile pages (posts, bio, rate, booking calendar)
- Lawyer weekly schedule + date exceptions; availability computed from schedule, exceptions, and existing bookings
- Book a 60-minute-multiple slot, pay via Kashier; in Development the API auto-confirms payment so chat unlocks without a public webhook
- Consultation chat (persisted messages) and 1:1 audio/video calls (WebRTC over SignalR)
- Anonymous free inquiries to a lawyer; lawyer replies by email
- Lawyer posts (articles / achievements / case victories)
- Admin dashboard: users, suspend/unsuspend, pending lawyers, two-admin votes, revenue analytics
- Platform notifications (DB + SignalR) — **currently broken end-to-end**; see [docs/NOTIFICATIONS.md](docs/NOTIFICATIONS.md)

---

## Getting started

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL 16+
- Optional: MailHog (`localhost:1025`) for outgoing mail in Development

### API

```bash
# connection string, JWT, Google, Kashier, SMTP: Lawyers.Api/appsettings.json
# (and appsettings.Development.json)

dotnet ef database update --project Lawyers.InfraStructure --startup-project Lawyers.Api
dotnet run --project Lawyers.Api
```

Startup seeds Identity roles, one admin, sample lawyers, and sample clients.

Default seed admin:

- Email: `admin@lawyers.com`
- Password: `Admin@123456` (must satisfy Identity: 14+ chars, digit, upper, lower, symbol)

Typical URLs (see `Lawyers.Api/Properties/launchSettings.json`):

- HTTP: `http://localhost:5112`
- HTTPS: `https://localhost:7129`
- Scalar: `/scalar` in Development

### Frontend

```bash
cd ClientSide
npm install
# optional: NUXT_PUBLIC_API_BASE=https://localhost:7129/api
npm run dev
```

Defaults:

- App: `http://localhost:3000`
- API base: `https://localhost:7129/api` (`nuxt.config.ts` → `runtimeConfig.public.apiBase`)

CORS allows `http://localhost:3000` and `http://127.0.0.1:3000` with credentials (required for SignalR).

Set `NUXT_PUBLIC_TURN_URL` / username / credential if you need a TURN server for calls behind NAT.

### Payments (Kashier)

1. Create a booking → API returns a Kashier client secret / checkout URL.
2. Kashier POSTs `POST /api/webhooks/kashier` with `X-Kashier-Signature` (HMAC-SHA256 hex of the raw body).
3. `NotifyPaymentSuccessCommand` marks the consultation `Confirmed` and publishes `ConsultationConfirmedEvent`.

In **Development**, `ConsultationsController.Book` also fires that command immediately so you can chat without exposing a public webhook.

Hangfire is registered for a payment-fallback worker, but the dashboard is never mapped and no jobs are enqueued. See [docs/API.md](docs/API.md).

---

## Frontend routes

| Route | Who |
|---|---|
| `/` | Public lawyer search |
| `/lawyers/{id}` | Public lawyer profile + booking |
| `/auth/login`, `/register`, `/forgot-password`, `/reset-password`, `/Confirm-email` | Auth |
| `/profile` | Authenticated profile (posts/schedule for lawyers) |
| `/chat` | Consultations + free inquiries |
| `/consultations/{id}` | Single consultation chat |
| `/admin`, `/admin/users`, `/admin/Lawyers`, `/admin/votes`, `/admin/suspended` | Admin |

---

## Realtime contract

Hub: **`/hubs/consultations`** (JWT as `?access_token=`).

On connect, the server adds the connection to group `user_{userId}`. Chat rooms are groups named with the consultation id.

| Direction | Method / event | Purpose |
|---|---|---|
| Client → server | `JoinConsultation`, `LeaveConsultation` | Room membership |
| Client → server | `SendMessage`, `GetRecentMessages` | Chat |
| Client → server | `RequestCall`, `AcceptCall`, `RejectCall`, `EndCall` | Call lifecycle |
| Client → server | `SendWebRtcOffer`, `SendWebRtcAnswer`, `SendIceCandidate` | WebRTC signaling |
| Server → client | `ReceiveMessage`, `IncomingCall`, `CallAccepted`, `CallRejected`, `UserEndedCall`, SDP/ICE events | Chat + calls |
| Server → client | `ReceiveNotification` | In-app notification **(no Nuxt listener today)** |

---

## Documentation

- [API catalog, unused endpoints, missing routes](docs/API.md)
- [Notification system and bugs](docs/NOTIFICATIONS.md)

---

## Known gaps (high level)

1. Platform notifications do not reach the UI (event handler not registered, most events never persisted, client never listens or polls).
2. Several API endpoints and `useAdmin()` wrappers have no screen.
3. Some Nuxt calls hit `/api/...` on the Nuxt origin instead of `apiBase`, or call routes the API does not implement (`DELETE /lawyer-schedule/exceptions/{id}`, `GET /lawyer-posts/lawyer/me`).
4. Google OAuth redirects to `/auth/google-callback`; the Nuxt page is `/auth/google-success`.
5. Hangfire server starts with no jobs and no dashboard.
