using CenterApp.Entity.Center;

namespace CenterApp.Service.Models;

public record OpResult(bool Ok, string Message, long? Id = null, string? Extra = null)
{
    public static OpResult Success(string m = "تم الحفظ") => new(true, m);
    public static OpResult Fail(string m) => new(false, m);
}
public record SimpleItem(long Id, string Text);

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}

public static class ArabicDays
{
    static readonly string[] Names = { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
    public static string Name(DayOfWeek d) => Names[(int)d];
    public static int Order(DayOfWeek d) => ((int)d + 1) % 7;   // السبت أولاً
    public static readonly DayOfWeek[] WeekFromSaturday =
    {
        DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
        DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };
}

// ── Lookups
public class LookupVm
{
    public string Type { get; set; } = "subjects";
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Students
public class StudentVm
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhone { get; set; }
    public long? GradeLevelId { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<long> GroupIds { get; set; } = new();          // مجموعات هتتضاف
    public List<long> EnrolledGroupIds { get; set; } = new();  // مسجل فيها حالياً (للعرض فقط)
}

public class StudentCardVm
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}
// ── Groups
public class GroupScheduleDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = "16:00";
    public string EndTime { get; set; } = "17:30";
    public string? Room { get; set; }
}

public class GroupSaveDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long TeacherId { get; set; }
    public long SubjectId { get; set; }
    public long? GradeLevelId { get; set; }
    public decimal MonthlyFee { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public List<GroupScheduleDto> Schedules { get; set; } = new();
}

public class GroupRow
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Grade { get; set; } = "";
    public decimal Fee { get; set; }
    public int Capacity { get; set; }
    public int Enrolled { get; set; }
    public string Schedule { get; set; } = "";
    public bool IsActive { get; set; }
}

// ── Enrollment
public class EnrolledRow
{
    public long EnrollmentId { get; set; }
    public string StudentName { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime JoinedAt { get; set; }
    public decimal? CustomFee { get; set; }
}

public class ManageEnrollmentsVm
{
    public long GroupId { get; set; }
    public string GroupName { get; set; } = "";
    public int Capacity { get; set; }
    public decimal MonthlyFee { get; set; }
    public List<EnrolledRow> Enrolled { get; set; } = new();
    public List<SimpleItem> Available { get; set; } = new();
}

// ── Sessions
public class SessionRow
{
    public long Id { get; set; }
    public string GroupName { get; set; } = "";
    public DateTime Date { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public SessionStatus Status { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Total { get; set; }
}

public class AttendanceRowVm
{
    public long StudentId { get; set; }
    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}
public class AttendanceVm
{
    public long SessionId { get; set; }
    public string GroupName { get; set; } = "";
    public DateTime Date { get; set; }
    public List<AttendanceRowVm> Rows { get; set; } = new();
}

public class AttendanceRowDto
{
    public long StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
}

public class AttendanceSaveDto
{
    public long SessionId { get; set; }
    public List<AttendanceRowDto> Rows { get; set; } = new();
}

// ── Invoices
public class PaymentRow
{
    public DateTime PaidAt { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Note { get; set; }
}

public class InvoiceRow
{
    public long Id { get; set; }
    public string StudentName { get; set; } = "";
    public string GroupName { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining => Amount - Discount - Paid;
    public InvoiceStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public List<PaymentRow> Payments { get; set; } = new();
}