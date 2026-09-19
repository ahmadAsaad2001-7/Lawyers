namespace Lawyers.Domain.Entities.Enums;
public enum Roles
{
    Client = 0,
    Lawyer = 1,
    PendingLawyer = 2, // ✅ new: registered as lawyer, not yet verified/promoted
    Admin = 3
}