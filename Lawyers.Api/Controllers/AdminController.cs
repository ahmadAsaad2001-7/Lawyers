using Lawyers.Application.DTOs;
using Lawyers.Application.Features.Admin.Commands;
using Lawyers.Application.Features.Admin.Queries;
using Lawyers.Application.Features.DTOs;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ═══════════════════════════════════════════════════════════════
    // USER MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get paginated list of all users with optional filtering
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<UserListDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? roleFilter,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!IsValidPage(page, pageSize))
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");

        if (!string.IsNullOrWhiteSpace(roleFilter) &&
            !Enum.TryParse<Roles>(roleFilter.Trim(), ignoreCase: true, out _))
            return BadRequest("Role filter is invalid.");

        var query = new GetAllUsersQuery(search, roleFilter, includeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a single user by ID with full profile details
    /// </summary>
    [HttpGet("users/{userId:int}")]
    public async Task<ActionResult<UserListDto>> GetUser(int userId)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(userId));
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Get a high-level log of who this user contacted (consultations + free inquiries)
    /// </summary>
    [HttpGet("users/{userId}/contact-log")]
    public async Task<ActionResult<PagedResult<UserContactLogDto>>> GetUserContactLog(
        int userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!IsValidPage(page, pageSize))
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");

        var query = new GetUserContactLogQuery(userId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get chart data for a specific user's consultation activity
    /// </summary>
    [HttpGet("users/{userId}/chart")]
    public async Task<ActionResult<List<ChartDataPointDto>>> GetUserChart(
        int userId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] string period = "daily")
    {
        if (start > end) return BadRequest("Start date must be before end date.");
        if (!IsValidPeriod(period))
            return BadRequest("Period must be daily, weekly, or monthly.");

        var query = new GetUserChartDataQuery(userId, start, end, period);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("suspended")]
    public async Task<ActionResult<PagedResult<SuspendedUserDto>>> GetSuspendedUsers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!IsValidPage(page, pageSize))
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");

        var result = await _mediator.Send(new GetSuspendedUsersQuery(page, pageSize));
        return Ok(result);
    }
    [HttpGet("suspended/{userId:int}")]
    public async Task<ActionResult<SuspendedUserDto>> GetSuspendedUser(int userId)
    {
        var result = await _mediator.Send(new GetSuspendedUserByIdQuery(userId));
        return result == null ? NotFound() : Ok(result);
    }
    // ═══════════════════════════════════════════════════════════════
    // USER SUSPENSION SYSTEM
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Suspend a user for a specified number of days
    /// </summary>
    [HttpPost("users/{userId}/suspend")]
    public async Task<IActionResult> SuspendUser(int userId, [FromBody] SuspendUserCommand command)
    {
        if (userId != command.UserId) return BadRequest("User ID mismatch.");
        if (string.IsNullOrWhiteSpace(command.Reason)) return BadRequest("Reason required.");
        if (command.Days < 1 || command.Days > 365) return BadRequest("Days must be 1–365.");

        try
        {
            await _mediator.Send(command);
            return Ok(new { message = "User suspended." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Extend an active suspension by additional days
    /// </summary>
    [HttpPost("users/{userId}/extend-suspend")]
    public async Task<IActionResult> ExtendSuspend(int userId, [FromBody] ExtendSuspendCommand command)
    {
        if (userId != command.UserId) return BadRequest("User ID mismatch.");
        if (command.AdditionalDays < 1) return BadRequest("Additional days must be positive.");

        try
        {
            var success = await _mediator.Send(command);
            return success ? Ok(new { message = "Suspension extended." }) : NotFound("Active suspension not found.");
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Decrease an active suspension by removing days (auto-unsuspends if expired)
    /// </summary>
    [HttpPost("users/{userId}/decrease-suspend")]
    public async Task<IActionResult> DecreaseSuspend(int userId, [FromBody] DecreaseSuspendCommand command)
    {
        if (userId != command.UserId) return BadRequest("User ID mismatch.");
        if (command.DaysToRemove < 1) return BadRequest("Days to remove must be positive.");

        try
        {
            var success = await _mediator.Send(command);
            return success ? Ok(new { message = "Suspension decreased." }) : NotFound("Active suspension not found.");
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Immediately unsuspend a user (terminate active suspension)
    /// </summary>
    [HttpPost("users/{userId}/unsuspend")]
    public async Task<IActionResult> UnsuspendUser(int userId, [FromBody] UnsuspendUserCommand command)
    {
        if (userId != command.UserId) return BadRequest("User ID mismatch.");

        try
        {
            var success = await _mediator.Send(command);
            return success ? Ok(new { message = "User unsuspended." }) : NotFound("Active suspension not found.");
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // LAWYER MANAGEMENT & VERIFICATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get paginated list of lawyers pending platform verification
    /// </summary>
    [HttpGet("lawyers/pending")]
    public async Task<ActionResult<PagedResult<PendingLawyerDto>>> GetPendingLawyers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!IsValidPage(page, pageSize))
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");

        var result = await _mediator.Send(new GetPendingLawyersQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Get chart data for a specific lawyer's revenue/consultations
    /// </summary>
    [HttpGet("lawyers/{lawyerProfileId}/chart")]
    public async Task<ActionResult<List<ChartDataPointDto>>> GetLawyerChart(
        int lawyerProfileId,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] string period = "daily")
    {
        if (start > end) return BadRequest("Start date must be before end date.");
        if (!IsValidPeriod(period))
            return BadRequest("Period must be daily, weekly, or monthly.");

        var query = new GetLawyerChartDataQuery(lawyerProfileId, start, end, period);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Admin proposes to verify a lawyer. Creates a vote and counts as first approval.
    /// </summary>
    [HttpPost("lawyers/{userId}/propose-verification")]
    public async Task<IActionResult> ProposeVerification(int userId, [FromBody] ProposeLawyerVerificationCommand command)
    {
        if (userId != command.LawyerUserId) return BadRequest("User ID mismatch.");
        if (string.IsNullOrWhiteSpace(command.Reason)) return BadRequest("A reason is required.");

        try
        {
            var voteId = await _mediator.Send(command);
            return Ok(new { voteId, message = "Verification proposal created. Waiting for second admin approval." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Admin proposes to unverify a lawyer (remove verified badge).
    /// </summary>
    [HttpPost("lawyers/{userId}/propose-unverification")]
    public async Task<IActionResult> ProposeUnverification(int userId, [FromBody] ProposeLawyerUnverificationCommand command)
    {
        if (userId != command.LawyerUserId) return BadRequest("User ID mismatch.");
        if (string.IsNullOrWhiteSpace(command.Reason)) return BadRequest("A reason is required.");

        try
        {
            var voteId = await _mediator.Send(command);
            return Ok(new { voteId, message = "Unverification proposal created. Waiting for second admin approval." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // VOTING SYSTEM (Ban / Verify / Unverify)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Admin proposes to ban a user. Creates a vote and counts as the first approval.
    /// </summary>
    [HttpPost("users/{userId}/propose-ban")]
    public async Task<IActionResult> ProposeBan(int userId, [FromBody] ProposeUserBanCommand command)
    {
        if (userId != command.TargetUserId) return BadRequest("User ID mismatch.");
        if (string.IsNullOrWhiteSpace(command.Reason)) return BadRequest("A reason is required.");

        try
        {
            var voteId = await _mediator.Send(command);
            return Ok(new { voteId, message = "Ban proposal created. Waiting for second admin approval." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Another admin casts their vote (Approve or Reject) on an existing proposal.
    /// </summary>
    [HttpPost("votes/{voteId}/cast")]
    public async Task<IActionResult> CastVote(int voteId, [FromBody] CastAdminVoteCommand command)
    {
        if (voteId != command.VoteId) return BadRequest("Vote ID mismatch.");

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Get paginated list of all admin votes (pending and optionally resolved)
    /// </summary>
    [HttpGet("votes")]
    public async Task<ActionResult<PagedResult<AdminVoteDto>>> GetVotes(
        [FromQuery] bool includeResolved = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!IsValidPage(page, pageSize))
            return BadRequest("Page must be at least 1 and pageSize must be between 1 and 100.");

        var query = new GetVotesQuery(includeResolved, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // DASHBOARD ANALYTICS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get high-level platform statistics (users, revenue, consultations)
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<DashBoardStatsDto>> GetSiteStats()
    {
        var result = await _mediator.Send(new GetDashBoardStatsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get comprehensive analytics for a date range (revenue, top lawyers, completion rate)
    /// </summary>
    [HttpGet("dashboard/analytics")]
    public async Task<ActionResult<PeriodAnalyticsDto>> GetPeriodAnalytics(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end)
    {
        if (start > end) return BadRequest("Start date must be before end date.");

        var result = await _mediator.Send(new GetPeriodAnalyticsQuery(start, end));
        return Ok(result);
    }

    /// <summary>
    /// Get platform-wide revenue chart data grouped by period
    /// </summary>
    [HttpGet("dashboard/chart")]
    public async Task<ActionResult<List<ChartDataPointDto>>> GetChartData(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        [FromQuery] string period = "daily")
    {
        if (start > end) return BadRequest("Start date must be before end date.");
        if (!IsValidPeriod(period))
            return BadRequest("Period must be daily, weekly, or monthly.");

        var query = new GetChartDataQuery(start, end, period);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get recent platform activity (latest consultations)
    /// </summary>
    [HttpGet("dashboard/recent-activities")]
    public async Task<ActionResult<List<RecentActivityDto>>> RecentActivities([FromQuery] int limit = 5)
    {
        if (limit is < 1 or > 100) return BadRequest("Limit must be between 1 and 100.");

        var query = new GetRecentActivityQuery(limit);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // CONTENT MODERATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Admin deletes an inappropriate lawyer post
    /// </summary>
    [HttpDelete("posts/{postId}")]
    public async Task<IActionResult> AdminDeletePost(int postId, [FromBody] AdminDeletePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("A reason for deletion is required.");

        var command = new AdminDeleteLawyerPostCommand(postId, request.Reason);
        var success = await _mediator.Send(command);

        return success ? NoContent() : NotFound("Post not found.");
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static bool IsValidPage(int page, int pageSize) =>
        page >= 1 && pageSize is >= 1 and <= 100;

    private static bool IsValidPeriod(string period) =>
        new[] { "daily", "weekly", "monthly" }.Contains(period, StringComparer.OrdinalIgnoreCase);
}