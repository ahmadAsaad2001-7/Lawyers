namespace Lawyers.Application.DTOs;

public class AdminVoteDto
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int TargetUserId { get; set; }
    public bool IsSelfApplied { get; set; } 
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
    public string? TargetProfileImageUrl { get; set; }
    public string InitiatorName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int ApprovalCount { get; set; }
    public int DisapprovalCount { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasCurrentAdminVoted { get; set; }
}