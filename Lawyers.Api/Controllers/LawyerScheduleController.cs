    using System.Globalization;
    using Lawyers.Application.Features.Lawyers.Commands;
    using Lawyers.Application.Features.Lawyers.Queries;
    using Lawyers.Application.Features.LawyerSchedule.DTOs; // ✅ Add this using
    using Lawyers.Application.Interfaces;
    using MediatR;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using GetActualAvailabilityQuery = Lawyers.Application.Features.LawyerSchedule.Queries.GetActualAvailabilityQuery;
    using WeeklyScheduleDto = Lawyers.Application.Features.LawyerSchedule.DTOs.WeeklyScheduleDto;
    using WeeklyScheduleInput = Lawyers.Application.Features.LawyerSchedule.DTOs.WeeklyScheduleInput; // ✅ Required for FirstOrDefaultAsync

    namespace Lawyers.API.Controllers;

    [ApiController]
    [Route("api/lawyer-schedule")]
    [Authorize(Roles = "Lawyer")]
    public class LawyerScheduleController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork; // ✅ Injected to fetch the lawyer profile

        public LawyerScheduleController(
            IMediator mediator, 
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork)
        {
            _mediator = mediator;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("my-weekly")]
        public async Task<ActionResult<List<WeeklyScheduleDto>>> GetMyWeeklySchedule()
        {
            var lawyerId = await GetMyLawyerProfileId();
            return Ok(await _mediator.Send(new GetWeeklyScheduleQuery(lawyerId)));
        }

        [HttpPost("update-weekly")]
        public async Task<IActionResult> UpdateWeeklySchedule(
            [FromBody] List<WeeklyScheduleInput> schedule)
        {
            var lawyerId = await GetMyLawyerProfileId();
            await _mediator.Send(new UpdateWeeklyScheduleCommand(lawyerId, schedule));
            return Ok(new { message = "Schedule updated successfully" });
        }   

        [HttpGet("my-exceptions")]
        public async Task<ActionResult<List<ExceptionDto>>> GetMyExceptions()
        {
            var lawyerId = await GetMyLawyerProfileId();
        
            // ✅ Use lawyerId in the query
            var exceptions = await _unitOfWork.LawyerAvailabilityExceptions.Query()
                .AsNoTracking()
                .Where(e => e.LawyerProfileId == lawyerId)
                .Select(e => new ExceptionDto
                {
                    Id = e.Id,
                    Date = e.Date,
                    Type = (int)e.Type,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    Reason = e.Reason
                })
                .ToListAsync();
        
            return Ok(exceptions);
        }

        [HttpPost("add-exception")]
        public async Task<IActionResult> AddException(
            [FromBody] AddAvailabilityExceptionCommand command)
        {
            var lawyerId = await GetMyLawyerProfileId();
            var cmd = new AddAvailabilityExceptionCommand(
                lawyerId, command.Date, command.Type, 
                command.StartTime, command.EndTime, command.Reason);
            
            await _mediator.Send(cmd);
            return Ok(new { message = "Exception added" });
        }

        [HttpGet("next-7-days")]
        public async Task<ActionResult<List<DayPreviewDto>>> GetNext7Days()
        {
            var lawyerId = await GetMyLawyerProfileId();
            var previews = new List<DayPreviewDto>();
            
            TimeZoneInfo cairo;
            try { cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"); }
            catch { cairo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); }
            var cairoToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cairo).Date;

            for (int i = 0; i < 7; i++)
            {
                var date = cairoToday.AddDays(i);
                var hours = await _mediator.Send(new GetActualAvailabilityQuery(lawyerId, date));
                
                previews.Add(new DayPreviewDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    DayName = date.ToString("dddd", new CultureInfo("ar-EG")),
                    AvailableHours = hours,
                    IsFullyAvailable = hours.Count == 24,
                    IsFullyClosed = hours.Count == 0,
                });
            }
            
            return Ok(previews);
        }

        // ✅ Fully implemented helper method
        private async Task<int> GetMyLawyerProfileId()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var lawyerProfile = await _unitOfWork.LawyerProfiles.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(lp => lp.UserId == _currentUser.UserId.Value);

            if (lawyerProfile == null)
            {
                throw new InvalidOperationException("Lawyer profile not found for the current user.");
            }

            return lawyerProfile.Id;
        }
    }