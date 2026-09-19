namespace Lawyers.Domain.Entities;

public class LawyerWeeklySchedule : BaseEntity
{
    public int LawyerProfileId { get; set; }
    public LawyerProfile LawyerProfile { get; set; } = null!;
    
    public DayOfWeek Day { get; set; }  // Monday, Tuesday, etc.
    public TimeSpan? StartTime { get; set; }  // NULL = OFF
    public TimeSpan? EndTime { get; set; }    // NULL = OFF
    
    public bool IsEnabled { get; set; } = true;
}