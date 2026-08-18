using MediatR;

namespace Lawyers.Application.Features.Admin.Commands;

public record ProposeUserBanCommand(int TargetUserId, string Reason) : IRequest<int>; // Returns the new VoteId