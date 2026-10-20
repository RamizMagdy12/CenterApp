using CenterApp.Domain.Base;

namespace CenterApp.Domain.Center;

public enum ExamStatus { Draft = 1, Published = 2 }
public enum ExamAttemptStatus { InProgress = 1, Submitted = 2, AutoSubmitted = 3 }

public class OnlineExam : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string LocalTitle => Bilingual.Show(Title, TitleEn);
    public string LocalDescription => Bilingual.Show(Description, DescriptionEn);
    public long? TeacherId { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public DateTime ResultsAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public virtual Teacher? Teacher { get; set; }
    public virtual ICollection<OnlineExamGroup> Groups { get; set; } = new List<OnlineExamGroup>();
    public virtual ICollection<OnlineExamQuestion> Questions { get; set; } = new List<OnlineExamQuestion>();
    public virtual ICollection<OnlineExamAttempt> Attempts { get; set; } = new List<OnlineExamAttempt>();
}

public class OnlineExamGroup : BaseEntity
{
    public long ExamId { get; set; }
    public long GroupId { get; set; }
    public virtual OnlineExam Exam { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}

public class OnlineExamQuestion : BaseEntity
{
    public long ExamId { get; set; }
    public string Text { get; set; } = null!;
    public string? TextEn { get; set; }
    public int SortOrder { get; set; }
    public decimal Points { get; set; }
    public string? ImageStoredName { get; set; }
    public virtual OnlineExam Exam { get; set; } = null!;
    public virtual ICollection<OnlineExamChoice> Choices { get; set; } = new List<OnlineExamChoice>();
}

public class OnlineExamChoice : BaseEntity
{
    public long QuestionId { get; set; }
    public string Text { get; set; } = null!;
    public string? TextEn { get; set; }
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
    public virtual OnlineExamQuestion Question { get; set; } = null!;
}

public class OnlineExamAttempt : BaseEntity
{
    public long ExamId { get; set; }
    public long StudentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;
    public decimal? Score { get; set; }
    public int ViolationCount { get; set; }
    public string? EndReason { get; set; }
    public DateTime? RetakeUntil { get; set; }

    public virtual OnlineExam Exam { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual ICollection<OnlineExamAnswer> Answers { get; set; } = new List<OnlineExamAnswer>();
}

public class OnlineExamAnswer : BaseEntity
{
    public long AttemptId { get; set; }
    public long QuestionId { get; set; }
    public long? ChoiceId { get; set; }

    public virtual OnlineExamAttempt Attempt { get; set; } = null!;
    public virtual OnlineExamQuestion Question { get; set; } = null!;
    public virtual OnlineExamChoice? Choice { get; set; }
}

public class OfflineExam : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? TitleEn { get; set; }
    public string LocalTitle => Bilingual.Show(Title, TitleEn);
    public decimal MaxScore { get; set; }
    public long GroupId { get; set; }
    public long? TeacherId { get; set; }
    public DateTime ExamDate { get; set; }

    public virtual Group Group { get; set; } = null!;
    public virtual Teacher? Teacher { get; set; }
    public virtual ICollection<OfflineExamGrade> Grades { get; set; } = new List<OfflineExamGrade>();
}

public class OfflineExamGrade : BaseEntity
{
    public long OfflineExamId { get; set; }
    public long StudentId { get; set; }
    public decimal? Score { get; set; }

    public virtual OfflineExam OfflineExam { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}
