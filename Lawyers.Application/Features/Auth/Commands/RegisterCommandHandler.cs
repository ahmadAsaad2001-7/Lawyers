using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Interfaces;
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var RegisterCmd = new RegisterRequest(request.Email, request.Password, request.FullName, request.Role);
        var authResponse= await _authService.RegisterAsync(RegisterCmd);
        return new AuthResponseDto
        {
            Token = authResponse.Token,
            Email = authResponse.Email,
            Role = authResponse.Role.ToString() 
        };
    }
}