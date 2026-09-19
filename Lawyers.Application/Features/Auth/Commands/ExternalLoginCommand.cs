using Lawyers.Application.DTOs.Auth;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities.Enums;
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;

public record ExternalLoginCommand(string Email, string Name, string? PictureUrl, Roles? Role) : IRequest<AuthResponseDto>;
public class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public ExternalLoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        return await _authService.ExternalLoginAsync(request.Email, request.Name, request.PictureUrl,request.Role);
    }
}