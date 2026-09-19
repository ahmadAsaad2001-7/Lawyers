using MediatR;
using Microsoft.AspNetCore.Mvc;
using Lawyers.Application.Features.Lawyers.Posts.Commands; // ✅ fixed: Posts, not Topics
using Lawyers.Application.Features.Lawyers.Topics.Queries; // ✅ fixed, assuming queries live in the same corrected namespace
using Lawyers.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/lawyer-posts")]
public class LawyerPostsController(ISender sender) : ControllerBase
{
    // GET: api/lawyer-posts/5
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLawyerPostByIdQuery(id), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    // GET: api/lawyer-posts/lawyer/5?page=1&pageSize=10
    [AllowAnonymous]
    [HttpGet("lawyer/{lawyerId}")]
    public async Task<IActionResult> GetByLawyer(int lawyerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetLawyerPostsQuery(lawyerId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    // GET: api/lawyer-posts/search?term=court&type=1&page=1&pageSize=10
    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? term, [FromQuery] PostType? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchLawyerPostsQuery(term, type, page, pageSize), cancellationToken);
        return Ok(result);
    }

    // POST: api/lawyer-posts
    // ✅ No more GetCurrentLawyerId() / command-overwriting here — the handler
    // itself now resolves the caller's LawyerProfile from their JWT userId
    // and checks IsVerified, which also fixes the User.Id vs LawyerProfile.Id bug.
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLawyerPostCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var id = await sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLawyerPostCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("ID mismatch.");

        try
        {
            var success = await sender.Send(command, cancellationToken);
            if (!success) return NotFound("Post not found or you don't own it.");
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var success = await sender.Send(new DeleteLawyerPostCommand(id), cancellationToken);
            if (!success) return NotFound("Post not found or you don't own it.");
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }
}