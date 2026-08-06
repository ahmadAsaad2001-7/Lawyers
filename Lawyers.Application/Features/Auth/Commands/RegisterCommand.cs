using Lawyers.Application.DTOs.Auth;
using Lawyers.Domain.Entities.Enums;
using Lawyers.Domain.ValueObjects;
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;

public class RegisterCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Roles Role { get; set; }
    public string? LawFirmName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Address Address { get; set; } = new Address();
}