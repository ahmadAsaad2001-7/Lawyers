using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
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
        // 1. Map MediatR Command to Service Request
        var registerRequest = new RegisterRequest(
            request.Email, 
            request.Password, 
            request.FullName, 
            (Roles)request.Role,
            request.LawFirmName,
            request.Address,
            request.PhoneNumber
        );
        
        // 2. Call the Service (This will now handle User Creation + Email Sending internally)
        var authResponse = await _authService.RegisterAsync(registerRequest);
        
        // 3. Return a response indicating verification is needed (No JWT Token yet!)
        return new AuthResponseDto
        {
            // Token = null, // Do not return a token yet!
            Email = authResponse.Email,
            Role = authResponse.Role.ToString(),
            Message = "Registration successful. Please check your email to verify your account before logging in." 
        };
    }
}