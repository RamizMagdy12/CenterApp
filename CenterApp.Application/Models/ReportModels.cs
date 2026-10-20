using CenterApp.Domain.Center;

namespace CenterApp.Application.Models;

public class ReportGroupOption
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

public class ReportStudentOption
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

public class ReportPageVm
{
    public long GroupId { get; set; }
    public long? StudentId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Kind { get; set; } = "attendance";
    public string GroupName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public List<ReportGroupOption> Groups { get; set; } = new();
    public List<ReportStudentOption> Students { get; set; } = new();
    public List<StudentReportVm> Reports { get; set; } = new();
}

public class ReportSheetModel
{
    public string Kind { get; set; } = "attendance";
    public StudentReportVm Report { get; set; } = null!;
}

public class StudentReportVm
{
    public long StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentCode { get; set; } = "";
    public string? ParentName { get; set; }
    public string GroupName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int Present { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
    public int Late { get; set; }
    public int Unrecorded { get; set; }
    public int Recorded => Present + Late + Absent + Excused;
    public int Attended => Present + Late;
    public int Rate => Recorded == 0 ? 0 : (int)Math.Round(Attended * 100.0 / Recorded);
    public List<AttendanceReportRow> Attendance { get; set; } = new();

    public List<GradeReportRow> Online { get; set; } = new();
    public List<GradeReportRow> Offline { get; set; } = new();
}

public class AttendanceReportRow
{
    public DateTime Date { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public AttendanceStatus? Status { get; set; }
    public bool Recorded { get; set; }
    public string? Note { get; set; }
}

public class GradeReportRow
{
    public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal? Score { get; set; }
    public decimal MaxScore { get; set; }
    public bool Taken { get; set; }
    public string MissingLabel { get; set; } = "لم يدخل";
}
