using Lawyers.Application.DTOs;
using Lawyers.Application.Features.Lawyers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetLawyersQuery = Lawyers.Application.Features.Lawyers.Queries.GetLawyersQuery;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LawyersController : ControllerBase
{
    private readonly IMediator _mediator;

    public LawyersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet("search")] // Resolves to: GET /api/Lawyers/search
    public async Task<IActionResult> Search([FromQuery] GetLawyersQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("all")] // Resolves to: GET /api/Lawyers/all
    public async Task<IActionResult> GetAllLawyers([FromQuery] GetAllLawyerQuery query)
    {
        // 💡 BONUS FIX: You were missing the 'await' keyword here!
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    [AllowAnonymous]
    [HttpGet("{id}")] // Resolves to: GET /api/Lawyers/{id}
    public async Task<IActionResult> GetLawyerById(int id)
    {
        var result = await _mediator.Send(new GetLawyerByIdQuery(id));
        
        if (result == null)
        {
            return NotFound(new { message = "المحامي غير موجود أو غير موثق بعد." });
        }

        return Ok(result);
    }
}