namespace Lawyers.Domain.Entities;

public class VoteParticipant : AuditableEntity
{
    public int AdminVoteId { get; set; }
    public int AdminUserId { get; set; }
    public bool IsApproved { get; set; } // true = Approve, false = Reject
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AdminVote AdminVote { get; set; } = null!;
    public User AdminUser { get; set; } = null!;
}