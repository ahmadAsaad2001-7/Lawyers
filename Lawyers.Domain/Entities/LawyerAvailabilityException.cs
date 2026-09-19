using Lawyers.Domain.Entities.Enums;

namespace Lawyers.Domain.Entities;

public class LawyerAvailabilityException : BaseEntity
{
    public int LawyerProfileId { get; set; }
    public LawyerProfile LawyerProfile { get; set; } = null!;
    
    public DateTime Date { get; set; }  // Specific date
    
    public ExceptionType Type { get; set; }  // Closed, OpenEarly, OpenLate
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    
    public string? Reason { get; set; }  // "Vacation", "Holiday", etc.
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }  // NULL = one day only
}

