namespace Lawyers.Domain.Entities.Enums;

public enum ExceptionType
{
    Closed,           // Override: normally open → closed
    OpenEarly,        // Override: closed → open (extra hours)
    OpenLate,         // Override: closed → open (extra hours)
    ModifiedHours     // Override: different hours than usual
}