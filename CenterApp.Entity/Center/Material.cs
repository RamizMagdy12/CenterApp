using CenterApp.Entity.Base;

namespace CenterApp.Entity.Center;

public enum MaterialStatus { Draft = 1, Published = 2 }
public enum MaterialFileKind { Video = 1, Pdf = 2, Image = 3 }

public class Material : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public long? TeacherId { get; set; }
    public MaterialStatus Status { get; set; } = MaterialStatus.Draft;
    public DateTime? PublishedAt { get; set; }

    public virtual Teacher? Teacher { get; set; }
    public virtual ICollection<MaterialGroup> Groups { get; set; } = new List<MaterialGroup>();
    public virtual ICollection<MaterialFile> Files { get; set; } = new List<MaterialFile>();
}

public class MaterialGroup : BaseEntity
{
    public long MaterialId { get; set; }
    public long GroupId { get; set; }
    public virtual Material Material { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}

public class MaterialFile : BaseEntity
{
    public long MaterialId { get; set; }
    public string FileName { get; set; } = null!;
    public string StoredName { get; set; } = null!;
    public MaterialFileKind Kind { get; set; }
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public int PageCount { get; set; } = 1;
    public virtual Material Material { get; set; } = null!;
}

public class AccessGrace : BaseEntity
{
    public long StudentId { get; set; }
    public long? GroupId { get; set; }          // null = كل المجموعات
    public int Days { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Reason { get; set; }
    public bool IsRevoked { get; set; }

    public virtual Student Student { get; set; } = null!;
    public virtual Group? Group { get; set; }
}