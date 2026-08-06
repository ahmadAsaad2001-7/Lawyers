using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Consultations.Commands;

public class SendFreeMessageCommand : IRequest<bool>
{
    public int LawyerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // The API Controller will populate this, not the Vue frontend
    public string IpAddress { get; set; } = string.Empty;
    
}