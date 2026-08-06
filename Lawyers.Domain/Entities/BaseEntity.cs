using System.ComponentModel.DataAnnotations;

namespace Lawyers.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; } = false;
    [Timestamp]
    public byte[] RowVersion { get; set; }}