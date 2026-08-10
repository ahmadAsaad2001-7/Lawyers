using MediatR;

namespace Lawyers.Application.Features.Consultations.Commands;

public class ReplyToFreeMessageCommand : IRequest<bool>
{
    public int MessageId { get; set; }
    public string ReplyContent { get; set; } = string.Empty;
}