using Lawyers.Application.DTOs;
using Lawyers.Application.Features.Users.Commands;
using Lawyers.Application.Features.Users.Queries;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
        [FromQuery] bool unreadOnly = false, [FromQuery] int limit = 50)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(unreadOnly, limit));
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadNotificationCountQuery());
        return Ok(result);
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<int>> MarkAllRead()
    {
        var count = await _mediator.Send(new MarkAllNotificationsReadCommand());
        return Ok(count);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var success = await _mediator.Send(new MarkNotificationReadCommand(id));
        return success ? Ok() : NotFound();
    }
}