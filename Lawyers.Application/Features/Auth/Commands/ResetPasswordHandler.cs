using Lawyers.Application.Interfaces;
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;


public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<ResetPasswordResponse>;
public record ResetPasswordResponse(string Message);
public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IAuthService _authService;

    public ResetPasswordHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        return new ResetPasswordResponse("Your password has been successfully reset. You can now log in.");
    }
}