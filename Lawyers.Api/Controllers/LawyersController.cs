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
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] GetLawyersQuery query)
    {
        // MediatR automatically routes this to GetLawyersQueryHandler
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}