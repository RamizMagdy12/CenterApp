// Center/Entities.cs
using CenterApp.Entity.Base;

namespace CenterApp.Entity.Center;

public class GradeLevel : BaseEntity
{
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class Subject : BaseEntity
{
    public string Name { get; set; } = null!;
}

public class Teacher : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Student : BaseEntity
{
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhone { get; set; }
    public long? GradeLevelId { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DiscountKind DiscountKind { get; set; }
    public decimal DiscountValue { get; set; }

    public virtual GradeLevel? GradeLevel { get; set; }
    public virtual ICollection<GroupEnrollment> Enrollments { get; set; } = new List<GroupEnrollment>();
}

public class Group : BaseEntity
{
    public string Name { get; set; } = null!;
    public long TeacherId { get; set; }
    public long SubjectId { get; set; }
    public long? GradeLevelId { get; set; }
    public decimal MonthlyFee { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Teacher Teacher { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
    public virtual GradeLevel? GradeLevel { get; set; }
    public virtual ICollection<GroupSchedule> Schedules { get; set; } = new List<GroupSchedule>();
    public virtual ICollection<GroupEnrollment> Enrollments { get; set; } = new List<GroupEnrollment>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}

public class GroupSchedule : BaseEntity
{
    public long GroupId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Room { get; set; }

    public virtual Group Group { get; set; } = null!;
}

public class GroupEnrollment : BaseEntity
{
    public long GroupId { get; set; }
    public long StudentId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public decimal? CustomMonthlyFee { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Group Group { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual ICollection<MonthlyInvoice> Invoices { get; set; } = new List<MonthlyInvoice>();
}

public class Session : BaseEntity
{
    public long GroupId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public SessionStatus Status { get; set; }
    public string? Notes { get; set; }

    public virtual Group Group { get; set; } = null!;
    public virtual ICollection<SessionAttendance> Attendances { get; set; } = new List<SessionAttendance>();
}

public class SessionAttendance : BaseEntity
{
    public long SessionId { get; set; }
    public long StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }

    public virtual Session Session { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}

public class MonthlyInvoice : BaseEntity
{
    public long EnrollmentId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; }

    public virtual GroupEnrollment Enrollment { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class Payment : BaseEntity
{
    public long InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Note { get; set; }

    public virtual MonthlyInvoice Invoice { get; set; } = null!;
}