using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lawyers.Application.Features.Lawyers.Topics.Commands;
using Lawyers.Application.Features.Lawyers.Topics.Queries;
using Lawyers.Domain.Entities.Enums;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/lawyer-posts")]
public class LawyerPostsController(ISender sender) : ControllerBase
{
    // GET: api/lawyer-posts/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLawyerPostByIdQuery(id), cancellationToken);
        if (result == null) return NotFound();

        return Ok(result);
    }

    // GET: api/lawyer-posts/lawyer/guid-here?page=1&pageSize=10
    [HttpGet("lawyer/{lawyerId}")]
    public async Task<IActionResult> GetByLawyer(int lawyerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetLawyerPostsQuery(lawyerId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    // GET: api/lawyer-posts/search?term=court&type=1&page=1&pageSize=10
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? term, [FromQuery] PostType? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchLawyerPostsQuery(term, type, page, pageSize), cancellationToken);
        return Ok(result);
    }

    // POST: api/lawyer-posts
    private int GetCurrentLawyerId()
    {
        // Adjust this based on how your claims are structured.
        // If you use an ICurrentUserService, inject and use that instead.
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdString, out var id) ? id : 0;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLawyerPostCommand command, CancellationToken cancellationToken)
    {
        // 🚨 Overwrite the client's LawyerId with the secure Token ID
        var secureCommand = command with { LawyerId = GetCurrentLawyerId() };
    
        var id = await sender.Send(secureCommand, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLawyerPostCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("ID mismatch.");

        // 🚨 Overwrite the client's LawyerId with the secure Token ID
        var secureCommand = command with { LawyerId = GetCurrentLawyerId() };

        var success = await sender.Send(secureCommand, cancellationToken);
        if (!success) return NotFound("Post not found or unauthorized.");

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        // 🚨 Notice we don't take LawyerId from the query anymore
        var lawyerId = GetCurrentLawyerId();
        var success = await sender.Send(new DeleteLawyerPostCommand(id, lawyerId), cancellationToken);
    
        if (!success) return NotFound("Post not found or unauthorized.");

        return NoContent();
    }
}