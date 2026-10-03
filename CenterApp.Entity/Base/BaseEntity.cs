// Base/BaseEntity.cs  (نفس بتاعك)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CenterApp.Entity.Base;

public class BaseEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDateTime { get; private set; }

    public string? CreatedById { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime? CreatedDateTime { get; private set; }

    public string? LastModifiedById { get; private set; }
    public string? LastModifiedByName { get; private set; }
    public DateTime? LastModifiedDateTime { get; private set; }

    public void SetCreated(AuditInfo a) { CreatedById = a.UserId; CreatedByName = a.UserName; CreatedDateTime = a.Timestamp; }
    public void SetModified(AuditInfo a) { LastModifiedById = a.UserId; LastModifiedByName = a.UserName; LastModifiedDateTime = a.Timestamp; }
    public void SetDeleted(AuditInfo a) { IsDeleted = true; DeletedDateTime = a.Timestamp; }
}