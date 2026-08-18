using Lawyers.Application.DTOs;
using Lawyers.Application.Features.Admin.Commands;
using Lawyers.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController :ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    //view all users
    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserListDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? roleFilter,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllUsersQuery(search, roleFilter, includeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    //view user activity who called who 

    /// <summary>
    /// Get a high-level log of who this user contacted (or who contacted them)
    /// </summary>
    [HttpGet("users/{userId}/contact-log")]
    public async Task<ActionResult<PagedResult<UserContactLogDto>>> GetUserContactLog(
        int userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var query = new GetUserContactLogQuery(userId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    // ban suspend user for a period of time
    // casting vote
    // ═══════════════════════════════════════════════════════════════
// VOTING & MODERATION
// ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Admin proposes to ban a user. Creates a vote and counts as the first approval.
    /// </summary>
    [HttpPost("users/{userId}/propose-ban")]
    public async Task<IActionResult> ProposeBan(int userId, [FromBody] ProposeUserBanCommand command)
    {
        if (userId != command.TargetUserId) return BadRequest("User ID mismatch.");
    
        var voteId = await _mediator.Send(command);
        return Ok(new { voteId, message = "Ban proposal created. Waiting for second admin approval." });
    }

    /// <summary>
    /// Another admin casts their vote (Approve or Reject) on an existing proposal.
    /// </summary>
    [HttpPost("votes/{voteId}/cast")]
    public async Task<IActionResult> CastVote(int voteId, [FromBody] CastAdminVoteCommand command)
    {
        if (voteId != command.VoteId) return BadRequest("Vote ID mismatch.");
    
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    //reset password
    // verify lawyers 
    //set lawyer visibility
    //view and approve lawyers profile pages 
    //✅ View all payments and transactions
    //✅ Process refunds
    //✅ View platform revenue analytics
    //✅ Set platform commission rates
    //✅ Handle payment disputes
    //✅ View all consultations (not just own)
    //✅ Cancel consultations (with refund)
    //✅ Extend consultation duration (for disputes)
    //✅ View consultation transcripts (for quality assurance)
    /*✅ Delete inappropriate lawyer posts
       ✅ Review reported messages
       ✅ Ban users for spam/abuse
       
       
       6. Platform Configuration
       
           ✅ Set consultation duration limits
           ✅ Configure payment gateway settings
           ✅ Set platform-wide discount codes
           ✅ Manage email templates
           ✅ Configure notification settings
       
       7. Analytics & Reporting
       
           ✅ View platform-wide statistics (users, revenue, consultations)
           ✅ Export data (CSV/PDF reports)
           ✅ View lawyer performance metrics
           ✅ Track consultation completion rates
       
       8. Support & Disputes
       
           ✅ Access all chat histories (for dispute resolution)
           ✅ Join any ongoing call (as silent observer or moderator)
           ✅ Issue platform credits/refunds
           ✅ Escalate legal complaints
       
       */
    
    
}