using Lawyers.Application.Features.LawyerSchedule.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/lawyer-availability")]
public class LawyerAvailabilityController : ControllerBase
{
    private readonly IMediator _mediator;

    public LawyerAvailabilityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{lawyerProfileId:int}/days")]
    public async Task<ActionResult<List<int>>> GetAvailableDays(
        int lawyerProfileId, 
        [FromQuery] int year, 
        [FromQuery] int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12) 
            return BadRequest("Invalid month.");

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var availableDays = new List<int>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            try
            {
                var hours = await _mediator.Send(new GetActualAvailabilityQuery(lawyerProfileId, date));
                if (hours.Count > 0)
                    availableDays.Add(day);
            }
            catch
            {
                // A single bad schedule row must not blank the whole month.
            }
        }

        return Ok(availableDays);
    }

    [HttpGet("{lawyerProfileId:int}/hours")]
    public async Task<ActionResult<List<int>>> GetAvailableHours(
        int lawyerProfileId, 
        [FromQuery] string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        var hours = await _mediator.Send(new GetActualAvailabilityQuery(lawyerProfileId, parsedDate.Date));
        return Ok(hours);
    }
}