# Notification system — how it should work, and what is broken

Platform notifications are meant to survive reloads (PostgreSQL `PlatformNotifications`) and arrive live (`ReceiveNotification` on SignalR group `user_{userId}`).

That contract is only half-implemented. Admin votes persist rows but booking/inquiry pushes do not; the booking event handler is not registered; the Nuxt app never listens or polls.

---

## Intended design

```
Domain action
    │
    ▼
INotificationService.NotifyAsync
    ├── INSERT PlatformNotification (unread)
    └── SignalR: Clients.Group("user_{id}").ReceiveNotification({ Title, Message, Type })

Later / on load
    GET  /api/Notifications
    GET  /api/Notifications/unread-count
    PATCH /api/Notifications/{id}/read
```

Hub `OnConnectedAsync` does add the connection to `user_{userId}`. JWT query-string auth for the hub is configured.

---

## What actually happens

### 1. Booking confirmation never fires (critical)

`NotifyPaymentSuccessCommandHandler` publishes `ConsultationConfirmedEvent` and **does not** call `INotificationService` itself (comment: the SignalR handler is the sole subscriber).

`ConsultationConfirmedSignalRHandler` lives in **Infrastructure**:

```csharp
// Lawyers.InfraStructure/Notification/ConsultationConfirmedSignalRHandler.cs
namespace Lawyers.Infrastructure.Notifications.SignalR;
public class ConsultationConfirmedSignalRHandler : INotificationHandler<ConsultationConfirmedEvent>
```

MediatR is registered from the **Application** assembly only:

```csharp
// Lawyers.Api/StartUp/Dependencies.cs
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly));
```

`IUnitOfWork` is in `Lawyers.Application`. The handler is never discovered. After payment (webhook or the Development auto-confirm), **nobody is notified**.

**Fix:** register the Infrastructure assembly as well:

```csharp
cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly);
cfg.RegisterServicesFromAssembly(typeof(ConsultationConfirmedSignalRHandler).Assembly);
```

Or move the handler into Application / Api.

---

### 2. Fallback worker uses profile ids, and never runs (critical)

`PaymentFallbackWorker.CheckAndConfirmPaymentStatusAsync` calls:

```csharp
await _notificationService.SendBookingConfirmedAsync(
    consultation.ClientId, consultation.LawyerId, consultation.ScheduledAt);
```

`Consultation.ClientId` / `LawyerId` are **profile FKs**, not `User.Id`. SignalR groups are `user_{userId}`, so even a correct call would miss both parties.

The webhook path already maps correctly (`consultation.Client.UserId` / `Lawyer.UserId`).

The worker is also dead infrastructure: not in DI, Hangfire dashboard not mapped (`HangFireBuild` unused), no job after `BookConsultation`.

---

### 3. Most pushes are fire-and-forget (high)

Only `INotificationService.NotifyAsync` writes `PlatformNotifications`.

| Trigger | Method | Persisted? | Live push? |
|---|---|---|---|
| Admin vote resolved / rejected / ban / verify / unverify | `CastAdminVoteHandler` private `NotifyAsync` | Yes (duplicates service) | `SendGenericNotificationAsync` |
| Booking confirmed | `SendBookingConfirmedAsync` | **No** | Would, if handler were registered |
| New booking request | `SendNewBookingAsync` | **No** | Method **never called** from `BookConsultationCommandHandler` |
| Free inquiry | `SendGenericNotificationAsync` | **No** | Yes, if lawyer is connected **right now** |

If the lawyer is offline, a free inquiry is gone. Booking confirmations never appear in `GET /notifications` even after you fix registration.

**Fix:** every user-visible event should go through `INotificationService.NotifyAsync` (persist + push). Delete the private copy in `CastAdminVoteHandler`.

`SendNewBookingAsync` is unused — either call it (and persist) after reserve, or remove it from the interface.

---

### 4. Duplicate persist in admin votes (medium)

`CastAdminVoteHandler` copies `NotifyAsync` instead of using the service. Two persistence implementations will drift (already have: service exists, handler ignores it).

`NotifyAsync` in the handler calls `SaveChangesAsync` in the middle of the vote transaction, then `Handle` saves again. Prefer one unit of work: add the row, save once at the end, then push.

---

### 5. Frontend never consumes notifications (critical)

- `stores/Chat.ts` listens for chat/call events only — **no** `connection.on("ReceiveNotification", ...)`.
- `useAdmin().getNotifications / getUnreadCount / markRead` are never called.
- `NavBar.vue` hardcodes `unreadCount = 0` and has no inbox.
- OS `Notification` API is used only for **incoming calls**.

So even a perfect backend would be invisible except in raw SignalR traces.

`POST /api/auth/refresh` exists specifically so a lawyer can pick up a new JWT after `"You're verified!"` without logging in again. That comment is unreachable until the inbox exists.

---

### 6. Navbar unread is a stub (low)

```ts
const unreadCount = computed(() => 0); // placeholder
```

Chat unread lives in `chatStore.unread` and is unused by the navbar. Platform unread is unused too.

---

## Recommended target flow

1. Register Infrastructure MediatR handlers (or move `ConsultationConfirmedSignalRHandler`).
2. Implement `SendBookingConfirmedAsync` / inquiry notify via `NotifyAsync` (persist + SignalR).
3. After persist, include `Id` in the SignalR payload so the client can mark read.
4. Global SignalR connection (already in `Chat.ts` `initializeGlobal`): handle `ReceiveNotification`, bump a Pinia unread count, toast.
5. Navbar bell: `GET /notifications` + `unread-count` on load; `PATCH /{id}/read` on open.
6. On title `"You're verified!"`, call `authStore.refresh()`.
7. Delete `TestController`; either schedule `PaymentFallbackWorker` with **user ids** or remove Hangfire.
8. Stop swallowing SignalR errors with empty `catch` — log them.

---

## Files involved

| File | Role |
|---|---|
| `Lawyers.Domain/Entities/PlatformNotification.cs` | Row |
| `Lawyers.Api/Services/SignalRNotificationService.cs` | Persist + hub push |
| `Lawyers.Application/Interfaces/INotificationService.cs` | Contract |
| `Lawyers.Api/Controllers/NotificationsController.cs` | REST |
| `Lawyers.Api/Hubs/ConsultationHub.cs` | `user_{id}` group |
| `Lawyers.Application/Features/Admin/Commands/CastAdminVoteHandler.cs` | Duplicate persist |
| `Lawyers.Application/Features/Consultation/Commands/SendFreeMessageCommandHandler.cs` | Push only |
| `Lawyers.Application/Features/Payments/Commands/NotifyPaymentSuccessCommandHandler.cs` | Publishes event |
| `Lawyers.InfraStructure/Notification/ConsultationConfirmedSignalRHandler.cs` | Unregistered handler |
| `Lawyers.InfraStructure/Repositories/PaymentFallBackWorker.cs` | Wrong ids, never scheduled |
| `ClientSide/app/composables/useAdmin.ts` | REST wrappers, unused |
| `ClientSide/app/stores/Chat.ts` | Hub client, no `ReceiveNotification` |
| `ClientSide/app/components/NavBar.vue` | Unread hardcoded to 0 |
