using CenterApp.Domain.Center;

namespace CenterApp.Application.Models;

public class OnlineExamRow
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Groups { get; set; } = "";
    public int QuestionsCount { get; set; }
    public decimal MaxScore { get; set; }
    public ExamStatus Status { get; set; }
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public DateTime ResultsAt { get; set; }
    public int AttemptsCount { get; set; }
    public bool CanStop { get; set; }
}

public class ExamChoiceInput
{
    public string Text { get; set; } = "";
    public string? TextEn { get; set; }
    public bool IsCorrect { get; set; }
}

public class ExamQuestionInput
{
    public string Text { get; set; } = "";
    public string? TextEn { get; set; }
    public decimal Points { get; set; }
    public string? ImageStoredName { get; set; }
    public List<ExamChoiceInput> Choices { get; set; } = new();
}

public class ExamChoiceVm
{
    public string Text { get; set; } = "";
    public string? TextEn { get; set; }
    public bool IsCorrect { get; set; }
}

public class ExamQuestionVm
{
    public string Text { get; set; } = "";
    public string? TextEn { get; set; }
    public decimal Points { get; set; }
    public string? ImageStoredName { get; set; }
    public List<ExamChoiceVm> Choices { get; set; } = new();
}

public class OnlineExamEditVm
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public ExamStatus Status { get; set; }
    public DateTime? OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public DateTime? ResultsAt { get; set; }
    public bool Locked { get; set; }
    public List<GroupOption> Groups { get; set; } = new();
    public List<ExamQuestionVm> Questions { get; set; } = new();
}

public class OnlineExamSaveDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public DateTime ResultsAt { get; set; }
    public List<long> GroupIds { get; set; } = new();
    public List<ExamQuestionInput> Questions { get; set; } = new();
}

public class OnlineGradeRow
{
    public long StudentId { get; set; }
    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string StatusText { get; set; } = "لم يبدأ";
    public DateTime? SubmittedAt { get; set; }
    public decimal? Score { get; set; }
    public int Violations { get; set; }
    public string? EndReason { get; set; }
    public bool CanReopen { get; set; }
}

public class OnlineGradesVm
{
    public long ExamId { get; set; }
    public string Title { get; set; } = "";
    public string Groups { get; set; } = "";
    public decimal MaxScore { get; set; }
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public DateTime ResultsAt { get; set; }
    public bool ResultsVisible { get; set; }
    public List<OnlineExamRow> Exams { get; set; } = new();
    public List<OnlineGradeRow> Rows { get; set; } = new();
}

public class OfflineExamRow
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string GroupName { get; set; } = "";
    public DateTime ExamDate { get; set; }
    public decimal MaxScore { get; set; }
    public int GradedCount { get; set; }
    public int StudentCount { get; set; }
}

public class OfflineGradeInput
{
    public long StudentId { get; set; }
    public decimal? Score { get; set; }
}

public class OfflineGradeRow
{
    public long StudentId { get; set; }
    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";
    public decimal? Score { get; set; }
}

public class OfflineExamEditVm
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? TitleEn { get; set; }
    public decimal MaxScore { get; set; }
    public long GroupId { get; set; }
    public DateTime? ExamDate { get; set; }
    public bool GroupLocked { get; set; }
    public List<SimpleItem> Groups { get; set; } = new();
    public List<OfflineGradeRow> Rows { get; set; } = new();
}

public class OfflineExamSaveDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? TitleEn { get; set; }
    public decimal MaxScore { get; set; }
    public long GroupId { get; set; }
    public DateTime ExamDate { get; set; }
    public List<OfflineGradeInput> Grades { get; set; } = new();
}

public class StudentExamCard
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Groups { get; set; } = "";
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public DateTime ResultsAt { get; set; }
    public decimal MaxScore { get; set; }
    public int QuestionsCount { get; set; }
    public string Phase { get; set; } = "";
    public string PhaseText { get; set; } = "";
    public decimal? Score { get; set; }
    public bool CanStart { get; set; }
    public bool CanResume { get; set; }
}

public class StudentOfflineGradeCard
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string GroupName { get; set; } = "";
    public DateTime ExamDate { get; set; }
    public decimal MaxScore { get; set; }
    public decimal? Score { get; set; }
}

public class StudentExamsVm
{
    public List<StudentExamCard> Online { get; set; } = new();
    public List<StudentOfflineGradeCard> Offline { get; set; } = new();
    public int OpenCount { get; set; }
}

public class ExamIntroVm
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public DateTime ResultsAt { get; set; }
    public decimal MaxScore { get; set; }
    public int QuestionsCount { get; set; }
    public string Phase { get; set; } = "";
    public string PhaseText { get; set; } = "";
    public decimal? Score { get; set; }
    public bool CanStart { get; set; }
    public bool CanResume { get; set; }
}

public class ExamTakeChoiceVm
{
    public long Id { get; set; }
    public string Text { get; set; } = "";
}

public class ExamTakeQuestionVm
{
    public long Id { get; set; }
    public string Text { get; set; } = "";
    public decimal Points { get; set; }
    public bool HasImage { get; set; }
    public long? SelectedChoiceId { get; set; }
    public List<ExamTakeChoiceVm> Choices { get; set; } = new();
}

public class ExamTakeVm
{
    public long ExamId { get; set; }
    public string Title { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string StudentCode { get; set; } = "";
    public DateTime ClosesAt { get; set; }
    public DateTime ResultsAt { get; set; }
    public int RemainingSeconds { get; set; }
    public bool SingleEntry { get; set; }
    public List<ExamTakeQuestionVm> Questions { get; set; } = new();
}

public class ExamAnswerDto
{
    public long QuestionId { get; set; }
    public long? ChoiceId { get; set; }
}

public class ExamSubmitDto
{
    public long ExamId { get; set; }
    public string? Reason { get; set; }
    public List<ExamAnswerDto> Answers { get; set; } = new();
}

public class ExamSyncResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public bool Finished { get; set; }
    public int RemainingSeconds { get; set; }

    public static ExamSyncResult Fail(string message) => new() { Ok = false, Message = message, Finished = true };
    public static ExamSyncResult Done(string message) => new() { Ok = true, Message = message, Finished = true };
}
