// Lawyers.Application/Features/Lawyers/Commands/CompleteLawyerProfileCommand.cs
using MediatR;

namespace Lawyers.Application.Features.Lawyers.Commands;

public record CompleteLawyerProfileCommand(
    string BarLicenseNumber,
    string? Specialization,
    string? Bio,
    decimal? HourlyRate,
    string? Street,
    string? City,
    string? State,
    string? Country,
    string? PostalCode
) : IRequest<bool>;