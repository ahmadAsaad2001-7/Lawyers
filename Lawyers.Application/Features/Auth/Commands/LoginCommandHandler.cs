using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Lawyers.Application.Features.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        
        var loginRequest = new LoginRequest(request.Email, request.Password);
        
        var authResponse = await _authService.LoginAsync(loginRequest);

        return new AuthResponseDto
        {
            Token = authResponse.Token,
            Email = authResponse.Email,
            Role = authResponse.Role.ToString() 
        };
    }
}