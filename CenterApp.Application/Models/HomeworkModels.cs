using CenterApp.Domain.Center;

namespace CenterApp.Application.Models;

public class HomeworkRow
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Groups { get; set; } = "";
    public int FilesCount { get; set; }
    public HomeworkStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class HomeworkFileVm
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public HomeworkFileKind Kind { get; set; }
    public string Size { get; set; } = "";
}

public class HomeworkEditVm
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public HomeworkStatus Status { get; set; }
    public List<GroupOption> Groups { get; set; } = new();
    public List<HomeworkFileVm> Files { get; set; } = new();
}

public class HomeworkSaveDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public List<long> GroupIds { get; set; } = new();
}

public class PortalHomeworkFileVm
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public HomeworkFileKind Kind { get; set; }
    public string Size { get; set; } = "";
}

public class PortalHomeworkRow
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Groups { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public int FilesCount { get; set; }
    public bool IsLocked { get; set; }
    public string? Reason { get; set; }
    public DateTime? GraceUntil { get; set; }
}

public class PortalHomeworkVm
{
    public PortalHomeworkRow Row { get; set; } = null!;
    public List<PortalHomeworkFileVm> Files { get; set; } = new();
}

public class StudentAttendanceItem
{
    public DateTime Date { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string GroupName { get; set; } = "";
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
}

public class StudentAttendanceVm
{
    public long StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentCode { get; set; } = "";
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
    public int Late { get; set; }
    public int Total { get; set; }
    public int Attended => Present + Late;
    public List<StudentAttendanceItem> Rows { get; set; } = new();
}
