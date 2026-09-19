using Lawyers.Domain.Entities.Enums;
using MediatR;

namespace Lawyers.Application.Features.Lawyers.Commands;

public record AddAvailabilityExceptionCommand(
    int LawyerProfileId,
    DateTime Date,
    ExceptionType Type,
    TimeSpan? StartTime,
    TimeSpan? EndTime,
    string? Reason) : IRequest;