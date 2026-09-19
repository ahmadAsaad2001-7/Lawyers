using MediatR;

namespace Lawyers.Application.Features.Lawyers.Commands;

    public record ApplyForVerificationCommand(string? Reason) : IRequest<int>;    
