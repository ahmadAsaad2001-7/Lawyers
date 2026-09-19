// Lawyers.Application/Features/Auth/Commands/RefreshTokenCommandHandler.cs
using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Interfaces;
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;

    public RefreshTokenCommandHandler(IAuthService authService, ICurrentUserService currentUserService)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("Not authenticated.");

        var authResponse = await _authService.RefreshTokenAsync(userId);

        return new AuthResponseDto
        {
            UserId = authResponse.UserId,
            UserName = authResponse.UserName,
            Token = authResponse.Token,
            Email = authResponse.Email,
            Role = authResponse.Role.ToString(),
            FullName = authResponse.FullName,
            ProfileImageUrl = authResponse.ProfileImageUrl,
            IsPlatformVerified = authResponse.IsPlatformVerified,
            Message = "Token refreshed."
        };
    }
}