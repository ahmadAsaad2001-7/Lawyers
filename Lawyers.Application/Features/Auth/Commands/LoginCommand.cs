using Lawyers.Application.DTOs.Auth;
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;

public class LoginCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}