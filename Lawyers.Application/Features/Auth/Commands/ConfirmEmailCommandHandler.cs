using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lawyers.Application.Features.Auth.Commands;

public class ConfirmEmailCommandHandler:IRequestHandler<ConfirmEmailCommand,ConfirmEmailResponse>
{
    private readonly UserManager<User> _userManager;
    public ConfirmEmailCommandHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }
  
    public async Task<ConfirmEmailResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            throw new ApplicationException("Invalid user ID.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new ApplicationException($"Email confirmation failed: {errors}");
        }

        return new ConfirmEmailResponse("Email confirmed successfully. You can now log in.");
    }
}