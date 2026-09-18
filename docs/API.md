# API catalog and unused-endpoint audit

Base URL: `{apiBase}` (default `https://localhost:7129/api`).

Auth: `Authorization: Bearer {JWT}` unless marked anonymous. SignalR uses `?access_token=`.

**Used** means a Nuxt screen, payment gateway, or OAuth redirect actually calls it. Wrappers in `useAdmin()` that no page invokes count as **unused**.

---

## Auth — `/api/Auth`

| Method | Path | Auth | Client usage |
|---|---|---|---|
| POST | `/register` | anon | Login/register flow |
| POST | `/login` | anon | Login |
| GET | `/confirm-email` | anon | Confirm-email page |
| GET | `/google` | anon | `auth.ts` starts Google challenge |
| GET | `/google-callback` | anon | Google redirect; **frontend page mismatch** (`/auth/google-success` vs redirect `/auth/google-callback`) |
| POST | `/forgot-password` | anon | Forgot-password page |
| POST | `/reset-password` | anon | Reset-password page |
| POST | `/refresh` | JWT | Re-issue token after verification |
| GET | `/me` | JWT | Session restore |

`FrontendUrl` is not set in `appsettings.json`. The callback falls back to `https://localhost:3000` while Nuxt serves `http://localhost:3000`.

---

## Lawyers — `/api/Lawyers`

| Method | Path | Auth | Client usage |
|---|---|---|---|
| GET | `/search` | anon | Home search (`lawyerSearch` store) |
| GET | `/{id}` | anon | Lawyer profile page |

---

## Lawyer posts — `/api/lawyer-posts`

| Method | Path | Auth | Client usage |
|---|---|---|---|
| GET | `/{id}` | anon | **Unused** — no post-detail page |
| GET | `/lawyer/{lawyerId}` | anon | Public profile. Manager calls `/lawyer/me`; `{lawyerId}` is an `int`, so `"me"` **400s**. There is no current-user alias. |
| GET | `/search` | anon | **Unused** — no post search UI |
| POST | `/` | JWT | Create modal (relative `/api/...` — see [broken client URLs](#broken-client-urls)) |
| PUT | `/{id}` | JWT | Edit modal |
| DELETE | `/{id}` | JWT | Posts manager |

---

## Availability — `/api/lawyer-availability`

| Method | Path | Auth | Client usage |
|---|---|---|---|
| GET | `/{lawyerProfileId}/days` | anon | Booking calendar (N+1: one query per day of the month) |
| GET | `/{lawyerProfileId}/hours` | anon | Booking hour picker |

---

## Schedule — `/api/lawyer-schedule` (`Lawyer` role)

| Method | Path | Auth | Client usage |
|---|---|---|---|
| GET | `/my-weekly` | Lawyer | Profile schedule + settings page |
| POST | `/update-weekly` | Lawyer | Weekly editor |
| GET | `/my-exceptions` | Lawyer | Exception list |
| POST | `/add-exception` | Lawyer | Add exception |
| GET | `/next-7-days` | Lawyer | Preview |
| DELETE | `/exceptions/{id}` | — | **Does not exist.** `pages/lawyers/settings/availability.vue` calls it anyway. |

---

## Profile — `/api/profile`

| Method | Path | Auth | Client usage |
|---|---|---|---|
| GET | `/overview` | JWT | Profile overview |
| PATCH | `/lawyer-profile` | JWT | **Unused UI** — complete bar/firm/bio after register |
| POST | `/apply-verification` | JWT | **Unused UI** — pending lawyer cannot self-apply from the app |

Without those two screens, a pending lawyer can only be verified if an admin proposes it.

---

## Consultations — `/api/Consultations`

| Method | Path | Auth | Client usage |
|---|---|---|---|
| POST | `/book` | JWT | Booking modal → Kashier. Development auto-confirms payment. |
| GET | `/free-messages` | JWT | Lawyer inbox (chat store) |
| POST | `/free-message` | anon | Public inquiry form |
| POST | `/free-messages/{id}/reply` | JWT | Lawyer reply |
| GET | `/{id}/details` | JWT | Chat thread header |
| GET | `/my-consultations` | JWT | Chat list + consultation history |

---

## Notifications — `/api/Notifications`

All three exist and work if called. **No page, plugin, or navbar calls them.** `useAdmin()` wraps them and nothing consumes the wrappers.

| Method | Path | Auth | Client usage |
|---|---|---|---|
| GET | `/` | JWT | Unused (`?unreadOnly&limit`) |
| GET | `/unread-count` | JWT | Unused |
| PATCH | `/{id}/read` | JWT | Unused |

See [NOTIFICATIONS.md](NOTIFICATIONS.md).

---

## Admin — `/api/admin` (`Admin` role)

| Method | Path | Client usage |
|---|---|---|
| GET | `/users` | Users + Lawyers pages |
| GET | `/users/{userId}` | **Unused** |
| GET | `/users/{userId}/contact-log` | **Unused** |
| GET | `/users/{userId}/chart` | **Unused** |
| POST | `/users/{userId}/suspend` | Users page |
| POST | `/users/{userId}/extend-suspend` | Suspended page |
| POST | `/users/{userId}/decrease-suspend` | Suspended page |
| POST | `/users/{userId}/unsuspend` | Suspended page |
| POST | `/users/{userId}/propose-ban` | **Unused** — permanent ban vote never started from UI |
| GET | `/suspended` | Suspended page |
| GET | `/suspended/{userId}` | **Unused** |
| GET | `/lawyers/pending` | Lawyers page |
| GET | `/lawyers/{lawyerProfileId}/chart` | **Unused** |
| POST | `/lawyers/{userId}/propose-verification` | Lawyers page |
| POST | `/lawyers/{userId}/propose-unverification` | Lawyers page |
| GET | `/votes` | Votes page |
| POST | `/votes/{voteId}/cast` | Votes page |
| GET | `/dashboard/stats` | **Unused** (`getStats` never called; home uses `/analytics`) |
| GET | `/dashboard/analytics` | Admin home |
| GET | `/dashboard/chart` | Admin home chart |
| GET | `/dashboard/recent-activities` | **Unused** |
| DELETE | `/posts/{postId}` | **Unused** — no admin content-moderation screen |

Duplicate handler with no controller: `GetAllVotesQuery` / `GetAllVotesQueryHandler` (controller uses `GetVotesQuery`).

---

## Webhooks — `/api/Webhooks`

| Method | Path | Auth | Usage |
|---|---|---|---|
| POST | `/kashier` | HMAC header | Kashier. Keep. Not a browser route. |

---

## Test — `/api/Test`  (remove)

| Method | Path | Auth | Usage |
|---|---|---|---|
| POST | `/create-consultation` | **none** | Dev leftover. Creates a `Consultation` with only `ClientId = 1`. No lawyer, no schedule, no auth. |

---

## SignalR hub (not REST)

`MapHub<ConsultationHub>("/hubs/consultations")` — used by `stores/Chat.ts`. `useSignalRChat.ts` is an older per-room client; the live path is the Pinia store.

Hangfire: `HangFireConfig()` runs (`AddHangfire` + `AddHangfireServer`). `HangFireBuild()` is **never** called, so `/hangfire` is not mapped. `PaymentFallbackWorker` is never registered or scheduled.

---

## Broken client URLs

These Nuxt calls do not hit the .NET API as intended (no Nitro proxy in `nuxt.config.ts`):

| Client file | Request | Problem |
|---|---|---|
| `LawyerPostsManager.vue` | `GET /api/lawyer-posts/lawyer/me` | Nuxt origin + invalid `me` |
| `LawyerPostModal.vue` | `POST/PUT /api/lawyer-posts` | Missing `apiBase` |
| `WeeklyScheduleEditor.vue` | `POST /api/lawyer-schedule/update-weekly` | Missing `apiBase` |
| `availability.vue` | `useFetch('/api/lawyer-schedule/...')` + `DELETE .../exceptions/{id}` | Missing `apiBase` **and** missing DELETE |
| `ProfileOverview.vue` | `useFetch('/api/profile/overview')` | Missing `apiBase` |
| `ProfileConsultation.vue` | `useFetch('/api/consultations/my-consultations')` | Missing `apiBase` |

`LawyerScheduleManager.vue` and most admin/chat code correctly prefix `config.public.apiBase`.

---

## Recommended cleanup

**Safe to delete (no client, leftover):**

1. `TestController` and `POST /api/Test/create-consultation`
2. `GET /api/lawyer-posts/search` + `SearchLawyerPostsQuery` if you do not plan a knowledge-base search
3. `GET /api/lawyer-posts/{id}` until a post-detail page exists
4. `GetAllVotesQuery` / `GetAllVotesQueryHandler` (duplicate of `GetVotesQuery`)
5. `SendNewBookingAsync` — defined on `INotificationService`, never called

**Keep the API, add the UI (or drop both):**

- Profile: `PATCH lawyer-profile`, `POST apply-verification`
- Admin: user detail, contact log, per-user/lawyer charts, propose-ban, dashboard stats, recent activity, admin delete-post
- Notifications: list, unread-count, mark-read — plus a navbar inbox (required for verification UX)

**Add the missing API the UI already calls:**

- `DELETE /api/lawyer-schedule/exceptions/{id}`
- `GET /api/lawyer-posts/lawyer/me` (resolve current lawyer profile id)

**Wire Hangfire or remove it:** register `PaymentFallbackWorker`, call `HangFireBuild()`, enqueue a delayed check after booking — or delete Hangfire packages and `HangFireConfiguration`.
