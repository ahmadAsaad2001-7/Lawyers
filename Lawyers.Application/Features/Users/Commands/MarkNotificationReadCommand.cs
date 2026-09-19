using MediatR;

namespace Lawyers.Application.Features.Users.Commands;

public record MarkNotificationReadCommand(int Id) : IRequest<bool>;
