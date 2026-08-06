
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;

public record ConfirmEmailCommand(int UserId, string Token) : IRequest<ConfirmEmailResponse>;

public record ConfirmEmailResponse(string Message);