namespace Lawyers.Domain.Entities;

public class AdminVote : BaseEntity
{
    public string ActionType { get; set; } = string.Empty; // "BanUser", "VerifyLawyer", etc.
    public int TargetUserId { get; set; }
    public int InitiatorAdminId { get; set; }
    public string Reason { get; set; } = string.Empty;
    
    public int ApprovalCount { get; set; } = 0;
    public int DisapprovalCount { get; set; } = 0;
    public bool IsResolved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User TargetUser { get; set; } = null!;
    public User InitiatorAdmin { get; set; } = null!;
    public ICollection<VoteParticipant> Participants { get; set; } = new List<VoteParticipant>();
}