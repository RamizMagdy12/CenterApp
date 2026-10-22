using System.Globalization;
using CenterApp.Domain.Center;
using CenterApp.Application.Abstractions;
using CenterApp.Application.Models;
using CenterApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CenterApp.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _u;
    public ReportService(IUnitOfWork u) => _u = u;

    public async Task<long?> TeacherIdOfUserAsync(string userId)
        => await _u.Teacher.GetAll(t => t.UserId == userId).Select(t => (long?)t.Id).FirstOrDefaultAsync();

    public async Task<ReportPageVm> BuildAsync(long? teacherId, long groupId, long? studentId, DateTime? from, DateTime? to, string? kind)
    {
        var today = DateTime.Today;
        var start = (from ?? new DateTime(today.Year, today.Month, 1)).Date;
        var end = (to ?? start.AddMonths(1).AddDays(-1)).Date;
        if (end < start) (start, end) = (end, start);
        var endExclusive = end.AddDays(1);
        var reportKind = kind == "grades" ? "grades" : "attendance";

        var groupRows = await _u.Group
            .GetAll(g => teacherId == null || g.TeacherId == teacherId)
            .OrderByDescending(g => g.IsActive).ThenBy(g => g.Name)
            .Select(g => new { g.Id, g.Name, g.NameEn, g.IsActive })
            .ToListAsync();
        var stopped = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            ? " (inactive)" : " (متوقفة)";
        var groups = groupRows.Select(g => new ReportGroupOption
        {
            Id = g.Id,
            Name = Bilingual.Show(g.Name, g.NameEn) + (g.IsActive ? "" : stopped)
        }).ToList();

        var vm = new ReportPageVm
        {
            From = start,
            To = end,
            Kind = reportKind,
            Groups = groups
        };

        if (groupId == 0 || groups.All(g => g.Id != groupId))
            return vm;

        var group = await _u.Group
            .GetAll(g => g.Id == groupId && (teacherId == null || g.TeacherId == teacherId), "Subject,Teacher")
            .FirstOrDefaultAsync();
        if (group == null) return vm;

        vm.GroupId = group.Id;
        vm.GroupName = Bilingual.Show(group.Name, group.NameEn);
        vm.SubjectName = Bilingual.Show(group.Subject?.Name, group.Subject?.NameEn);
        vm.TeacherName = Bilingual.Show(group.Teacher?.Name, group.Teacher?.NameEn);

        var enrollments = await _u.GroupEnrollment
            .GetAll(e => e.GroupId == groupId
                && e.JoinedAt < endExclusive
                && (e.LeftAt == null || e.LeftAt >= start), "Student")
            .ToListAsync();

        var students = enrollments
            .Where(e => e.Student != null)
            .GroupBy(e => e.StudentId)
            .Select(g =>
            {
                var open = g.Any(e => e.LeftAt == null);
                return new
                {
                    Student = g.OrderByDescending(e => e.JoinedAt).First().Student,
                    Joined = g.Min(e => e.JoinedAt).Date,
                    Left = open ? (DateTime?)null : g.Max(e => e.LeftAt)!.Value.Date
                };
            })
            .OrderBy(s => s.Student.Name)
            .ToList();

        vm.Students = students.Select(s => new ReportStudentOption
        {
            Id = s.Student.Id,
            Name = Bilingual.Show(s.Student.Name, s.Student.NameEn),
            Code = s.Student.Code ?? ""
        }).ToList();

        if (studentId is long sid && students.Any(s => s.Student.Id == sid))
            vm.StudentId = sid;

        var target = vm.StudentId is long picked
            ? students.Where(s => s.Student.Id == picked).ToList()
            : students;
        if (target.Count == 0) return vm;

        var studentIds = target.Select(s => s.Student.Id).ToList();

        var sessions = await _u.Session
            .GetAll(s => s.GroupId == groupId
                && s.Status != SessionStatus.Cancelled
                && s.Date >= start && s.Date < endExclusive)
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var attendance = sessionIds.Count == 0
            ? new List<SessionAttendance>()
            : await _u.SessionAttendance
                .GetAll(a => sessionIds.Contains(a.SessionId) && studentIds.Contains(a.StudentId))
                .ToListAsync();

        var offlineExams = await _u.OfflineExam
            .GetAll(x => x.GroupId == groupId && x.ExamDate >= start && x.ExamDate < endExclusive)
            .OrderBy(x => x.ExamDate).ThenBy(x => x.Title)
            .ToListAsync();
        var offlineIds = offlineExams.Select(x => x.Id).ToList();
        var offlineGrades = offlineIds.Count == 0
            ? new List<OfflineExamGrade>()
            : await _u.OfflineExamGrade
                .GetAll(g => offlineIds.Contains(g.OfflineExamId) && studentIds.Contains(g.StudentId))
                .ToListAsync();

        var onlineIds = await _u.OnlineExamGroup
            .GetAll(x => x.GroupId == groupId)
            .Select(x => x.ExamId)
            .Distinct()
            .ToListAsync();
        var onlineExams = onlineIds.Count == 0
            ? new List<OnlineExam>()
            : await _u.OnlineExam
                .GetAll(e => onlineIds.Contains(e.Id)
                    && e.Status == ExamStatus.Published
                    && e.OpensAt < endExclusive
                    && e.ClosesAt >= start)
                .OrderBy(e => e.OpensAt).ThenBy(e => e.Title)
                .ToListAsync();
        var publishedIds = onlineExams.Select(e => e.Id).ToList();
        var questions = publishedIds.Count == 0
            ? new List<OnlineExamQuestion>()
            : await _u.OnlineExamQuestion.GetAll(q => publishedIds.Contains(q.ExamId)).ToListAsync();
        var attempts = publishedIds.Count == 0
            ? new List<OnlineExamAttempt>()
            : await _u.OnlineExamAttempt
                .GetAll(a => publishedIds.Contains(a.ExamId) && studentIds.Contains(a.StudentId))
                .ToListAsync();

        var maxByExam = questions.GroupBy(q => q.ExamId).ToDictionary(g => g.Key, g => g.Sum(q => q.Points));

        foreach (var member in target)
        {
            var st = member.Student;
            bool InWindow(DateTime date)
            {
                var day = date.Date;
                return day >= member.Joined && (member.Left == null || day <= member.Left);
            }

            var lastMissedId = sessions
                .Where(ses => ses.Date.Date < member.Joined)
                .OrderByDescending(ses => ses.Date).ThenByDescending(ses => ses.StartTime).ThenByDescending(ses => ses.Id)
                .Select(ses => (long?)ses.Id)
                .FirstOrDefault();

            var bySession = attendance.Where(a => a.StudentId == st.Id).ToDictionary(a => a.SessionId);
            var rows = sessions.Where(ses => InWindow(ses.Date) || ses.Id == lastMissedId).Select(ses =>
            {
                bySession.TryGetValue(ses.Id, out var row);
                return new AttendanceReportRow
                {
                    Date = ses.Date,
                    Start = ses.StartTime,
                    End = ses.EndTime,
                    Status = row?.Status,
                    Recorded = row != null,
                    Note = Bilingual.Show(row?.Note, row?.NoteEn)
                };
            }).ToList();

            var report = new StudentReportVm
            {
                StudentId = st.Id,
                StudentName = Bilingual.Show(st.Name, st.NameEn),
                StudentCode = string.IsNullOrWhiteSpace(st.Code) ? st.Id.ToString() : st.Code,
                ParentName = Bilingual.Show(st.ParentName, st.ParentNameEn),
                GroupName = vm.GroupName,
                SubjectName = vm.SubjectName,
                TeacherName = vm.TeacherName,
                From = start,
                To = end,
                Present = rows.Count(a => a.Recorded && a.Status == AttendanceStatus.Present),
                Late = rows.Count(a => a.Recorded && a.Status == AttendanceStatus.Late),
                Absent = rows.Count(a => a.Recorded && a.Status == AttendanceStatus.Absent),
                Excused = rows.Count(a => a.Recorded && a.Status == AttendanceStatus.Excused),
                Unrecorded = rows.Count(a => !a.Recorded),
                Attendance = rows,
                Offline = offlineExams.Where(exam => InWindow(exam.ExamDate)).Select(exam =>
                {
                    var grade = offlineGrades.FirstOrDefault(g => g.OfflineExamId == exam.Id && g.StudentId == st.Id);
                    return new GradeReportRow
                    {
                        Title = Bilingual.Show(exam.Title, exam.TitleEn),
                        Date = exam.ExamDate,
                        Score = grade?.Score,
                        MaxScore = exam.MaxScore,
                        Taken = grade?.Score != null,
                        MissingLabel = "لم يُرصد"
                    };
                }).ToList(),
                Online = onlineExams.Where(exam => InWindow(exam.OpensAt)).Select(exam =>
                {
                    var attempt = attempts.FirstOrDefault(a => a.ExamId == exam.Id && a.StudentId == st.Id);
                    var submitted = attempt != null
                        && attempt.Status != ExamAttemptStatus.InProgress
                        && attempt.Score != null;
                    return new GradeReportRow
                    {
                        Title = Bilingual.Show(exam.Title, exam.TitleEn),
                        Date = attempt?.SubmittedAt ?? exam.OpensAt,
                        Score = submitted ? attempt!.Score : null,
                        MaxScore = maxByExam.GetValueOrDefault(exam.Id),
                        Taken = submitted,
                        MissingLabel = "لم يدخل"
                    };
                }).ToList()
            };
            vm.Reports.Add(report);
        }

        return vm;
    }
}
