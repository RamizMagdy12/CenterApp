using CenterApp.Entity.Base;

namespace CenterApp.Entity.Center;

public enum HomeworkStatus { Draft = 1, Published = 2 }
public enum HomeworkFileKind { Pdf = 1, Word = 2 }

public class Homework : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public long? TeacherId { get; set; }
    public HomeworkStatus Status { get; set; } = HomeworkStatus.Draft;
    public DateTime? PublishedAt { get; set; }

    public virtual Teacher? Teacher { get; set; }
    public virtual ICollection<HomeworkGroup> Groups { get; set; } = new List<HomeworkGroup>();
    public virtual ICollection<HomeworkFile> Files { get; set; } = new List<HomeworkFile>();
}

public class HomeworkGroup : BaseEntity
{
    public long HomeworkId { get; set; }
    public long GroupId { get; set; }
    public virtual Homework Homework { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}

public class HomeworkFile : BaseEntity
{
    public long HomeworkId { get; set; }
    public string FileName { get; set; } = null!;
    public string StoredName { get; set; } = null!;
    public HomeworkFileKind Kind { get; set; }
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public virtual Homework Homework { get; set; } = null!;
}
