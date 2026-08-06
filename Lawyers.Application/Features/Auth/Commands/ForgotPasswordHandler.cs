using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Lawyers.Application.Features.Auth.Commands;

// 1. Request the reset link
public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResponse>;
public record ForgotPasswordResponse(string Message);

// 2. Actually reset the password

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordHandler(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "https://localhost:3000";
        
        await _authService.ForgotPasswordAsync(request.Email, frontendUrl);
        
        // Return a generic message for security
        return new ForgotPasswordResponse("If an account with that email exists, a password reset link has been sent.");
    }
}

