using System.Text.RegularExpressions;
using CenterApp.Entity.Center;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CenterApp.Service.Repository;

public class ExamService : IExamService
{
    private const long MaxImage = 4L * 1024 * 1024;
    private static readonly Regex ImageName = new(@"^[a-f0-9]{32}\.(jpg|png|webp|gif)$", RegexOptions.Compiled);
    private readonly IUnitOfWork _u;
    private readonly IWebHostEnvironment _env;
    public ExamService(IUnitOfWork u, IWebHostEnvironment env)
    {
        _u = u;
        _env = env;
    }

    private static DateTime Now() => DateTime.Now;

    public async Task<long?> TeacherIdOfUserAsync(string userId)
        => await _u.Teacher.GetAll(t => t.UserId == userId).Select(t => (long?)t.Id).FirstOrDefaultAsync();

    public async Task<long?> StudentIdOfUserAsync(string userId)
        => await _u.Student.GetAll(s => s.UserId == userId && s.IsActive).Select(s => (long?)s.Id).FirstOrDefaultAsync();

    public async Task<List<OnlineExamRow>> ListOnlineAsync(long? teacherId)
    {
        var list = await _u.OnlineExam
            .GetAll(x => teacherId == null || x.TeacherId == teacherId, "Groups.Group,Questions,Attempts,Teacher")
            .OrderByDescending(x => x.OpensAt).ThenByDescending(x => x.Id)
            .ToListAsync();

        return list.Select(MapRow).ToList();
    }

    public async Task<OnlineExamEditVm?> GetOnlineAsync(long id, long? teacherId)
    {
        OnlineExam? exam = null;
        if (id != 0)
        {
            exam = await _u.OnlineExam
                .GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Questions.Choices,Attempts")
                .FirstOrDefaultAsync();
            if (exam == null) return null;
        }

        var selected = exam?.Groups.Select(g => g.GroupId).ToHashSet() ?? new HashSet<long>();
        var groups = await _u.Group.GetAll(g => g.IsActive && (teacherId == null || g.TeacherId == teacherId))
            .OrderBy(g => g.Name).Select(g => new { g.Id, g.Name }).ToListAsync();

        return new OnlineExamEditVm
        {
            Id = exam?.Id ?? 0,
            Title = exam?.Title ?? "",
            Description = exam?.Description,
            Status = exam?.Status ?? ExamStatus.Draft,
            OpensAt = exam?.OpensAt,
            ClosesAt = exam?.ClosesAt,
            ResultsAt = exam?.ResultsAt,
            Locked = exam != null && IsLocked(exam),
            Groups = groups.Select(g => new GroupOption { Id = g.Id, Name = g.Name, Selected = selected.Contains(g.Id) }).ToList(),
            Questions = exam?.Questions.OrderBy(q => q.SortOrder).ThenBy(q => q.Id).Select(q => new ExamQuestionVm
            {
                Text = q.Text,
                Points = q.Points,
                ImageStoredName = q.ImageStoredName,
                Choices = q.Choices.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).Select(c => new ExamChoiceVm
                {
                    Text = c.Text,
                    IsCorrect = c.IsCorrect
                }).ToList()
            }).ToList() ?? new()
        };
    }

    public async Task<OpResult> SaveOnlineAsync(OnlineExamSaveDto dto, long? teacherId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return OpResult.Fail("عنوان الاختبار مطلوب");
        var scheduleError = ValidateSchedule(dto.OpensAt, dto.ClosesAt, dto.ResultsAt);
        if (scheduleError != null) return OpResult.Fail(scheduleError);

        var ids = (dto.GroupIds ?? new()).Distinct().ToList();
        if (ids.Count == 0) return OpResult.Fail("اختار مجموعة واحدة على الأقل");
        var allowed = await _u.Group.GetAll(g => ids.Contains(g.Id) && (teacherId == null || g.TeacherId == teacherId))
            .Select(g => g.Id).ToListAsync();
        if (allowed.Count != ids.Count) return OpResult.Fail("فيه مجموعة مش تابعة لك");

        OnlineExam? exam;
        if (dto.Id == 0)
        {
            exam = new OnlineExam { TeacherId = teacherId };
            _u.OnlineExam.Add(exam);
        }
        else
        {
            exam = await _u.OnlineExam
                .GetAll(x => x.Id == dto.Id && (teacherId == null || x.TeacherId == teacherId), "Groups,Questions.Choices,Attempts", tracking: true)
                .FirstOrDefaultAsync();
            if (exam == null) return OpResult.Fail("الاختبار غير موجود");
        }

        if (exam.Id != 0 && IsLocked(exam))
        {
            if (dto.ResultsAt < exam.ClosesAt)
                return OpResult.Fail("وقت ظهور النتيجة لازم يكون بعد قفل الاختبار");
            exam.ResultsAt = dto.ResultsAt;
            await _u.SaveAsync();
            return new OpResult(true, "الاختبار بدأ، فاتحدّث وقت ظهور النتيجة بس", exam.Id);
        }

        var questionError = ValidateQuestions(dto.Questions, required: false);
        if (questionError != null) return OpResult.Fail(questionError);

        exam.Title = dto.Title.Trim();
        exam.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        exam.OpensAt = dto.OpensAt;
        exam.ClosesAt = dto.ClosesAt;
        exam.ResultsAt = dto.ResultsAt;

        foreach (var g in exam.Groups.Where(g => !ids.Contains(g.GroupId)).ToList()) _u.OnlineExamGroup.Remove(g);
        foreach (var gid in ids.Where(i => !exam.Groups.Any(g => g.GroupId == i)))
            exam.Groups.Add(new OnlineExamGroup { GroupId = gid });

        ReplaceQuestions(exam, dto.Questions);
        await _u.SaveAsync();
        return new OpResult(true, "تم الحفظ", exam.Id);
    }

    public async Task<OpResult> PublishOnlineAsync(long id, long? teacherId)
    {
        var exam = await _u.OnlineExam
            .GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Questions.Choices", tracking: true)
            .FirstOrDefaultAsync();
        if (exam == null) return OpResult.Fail("الاختبار غير موجود");

        var scheduleError = ValidateSchedule(exam.OpensAt, exam.ClosesAt, exam.ResultsAt);
        if (scheduleError != null) return OpResult.Fail(scheduleError);
        if (exam.ClosesAt <= Now()) return OpResult.Fail("ميعاد قفل الاختبار عدّى، عدّل الموعد قبل النشر");
        if (exam.Groups.Count == 0) return OpResult.Fail("اختار مجموعة واحدة على الأقل");
        var questionError = ValidateQuestions(exam.Questions.OrderBy(q => q.SortOrder).Select(q => new ExamQuestionInput
        {
            Text = q.Text,
            Points = q.Points,
            Choices = q.Choices.Select(c => new ExamChoiceInput { Text = c.Text, IsCorrect = c.IsCorrect }).ToList()
        }).ToList(), required: true);
        if (questionError != null) return OpResult.Fail(questionError);

        if (exam.Status != ExamStatus.Published)
        {
            exam.Status = ExamStatus.Published;
            exam.PublishedAt ??= Now();
        }
        await _u.SaveAsync();
        return OpResult.Success("تم النشر");
    }

    public async Task<OpResult> DeleteOnlineAsync(long id, long? teacherId)
    {
        var exam = await _u.OnlineExam
            .GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Questions.Choices,Attempts", tracking: true)
            .FirstOrDefaultAsync();
        if (exam == null) return OpResult.Fail("الاختبار غير موجود");
        if (exam.Attempts.Count > 0) return OpResult.Fail("فيه طلاب بدأوا الاختبار، مينفعش يتحذف");

        foreach (var g in exam.Groups.ToList()) _u.OnlineExamGroup.Remove(g);
        foreach (var q in exam.Questions.ToList())
        {
            foreach (var c in q.Choices.ToList()) _u.OnlineExamChoice.Remove(c);
            _u.OnlineExamQuestion.Remove(q);
        }
        _u.OnlineExam.Remove(exam);
        await _u.SaveAsync();
        return OpResult.Success("تم الحذف");
    }

    public async Task<OnlineGradesVm?> OnlineGradesAsync(long? examId, long? teacherId)
    {
        var exams = await ListOnlineAsync(teacherId);
        if (exams.Count == 0)
            return new OnlineGradesVm { ExamId = 0, Exams = exams };

        var picked = exams.FirstOrDefault(x => x.Id == examId) ?? exams[0];
        var exam = await _u.OnlineExam
            .GetAll(x => x.Id == picked.Id && (teacherId == null || x.TeacherId == teacherId), "Groups.Group,Questions")
            .FirstOrDefaultAsync();
        if (exam == null) return null;

        await CloseExpiredAsync(exam.Id);

        var groupIds = exam.Groups.Select(g => g.GroupId).ToList();
        var enrolled = await _u.GroupEnrollment
            .GetAll(e => groupIds.Contains(e.GroupId) && e.IsActive && e.LeftAt == null && e.Student.IsActive, "Student")
            .Select(e => new { e.StudentId, Code = e.Student.Code, e.Student.Name })
            .ToListAsync();

        var attempts = await _u.OnlineExamAttempt
            .GetAll(a => a.ExamId == exam.Id, "Student")
            .ToListAsync();

        var students = enrolled
            .Select(e => (e.StudentId, Code: e.Code ?? "", e.Name))
            .Concat(attempts.Select(a => (a.StudentId, Code: a.Student.Code ?? "", a.Student.Name)))
            .GroupBy(s => s.StudentId)
            .Select(g => g.First())
            .OrderBy(s => s.Name)
            .ToList();

        var byStudent = attempts.ToDictionary(a => a.StudentId);
        var now = Now();

        return new OnlineGradesVm
        {
            ExamId = exam.Id,
            Title = exam.Title,
            Groups = string.Join("، ", exam.Groups.Select(g => g.Group.Name)),
            MaxScore = exam.Questions.Sum(q => q.Points),
            OpensAt = exam.OpensAt,
            ClosesAt = exam.ClosesAt,
            ResultsAt = exam.ResultsAt,
            ResultsVisible = now >= exam.ResultsAt,
            Exams = exams,
            Rows = students.Select(s =>
            {
                byStudent.TryGetValue(s.StudentId, out var a);
                var close = EffectiveClose(exam, a);
                var taking = a?.Status == ExamAttemptStatus.InProgress && now < close;
                var finished = a != null && a.Status != ExamAttemptStatus.InProgress;
                return new OnlineGradeRow
                {
                    StudentId = s.StudentId,
                    StudentCode = s.Code,
                    StudentName = s.Name,
                    StatusText = AttemptText(a, now, exam),
                    SubmittedAt = a?.SubmittedAt,
                    Score = a?.Status == ExamAttemptStatus.InProgress ? null : a?.Score,
                    Violations = a?.ViolationCount ?? 0,
                    EndReason = a?.EndReason,
                    CanReopen = !taking && (finished || now >= exam.ClosesAt)
                };
            }).ToList()
        };
    }

    public async Task<List<OfflineExamRow>> ListOfflineAsync(long? teacherId)
    {
        var list = await _u.OfflineExam
            .GetAll(x => teacherId == null || x.TeacherId == teacherId || x.Group.TeacherId == teacherId, "Group,Grades")
            .OrderByDescending(x => x.ExamDate).ThenByDescending(x => x.Id)
            .ToListAsync();

        var groupIds = list.Select(x => x.GroupId).Distinct().ToList();
        if (groupIds.Count == 0)
            return list.Select(x => new OfflineExamRow
            {
                Id = x.Id,
                Title = x.Title,
                GroupName = x.Group.Name,
                ExamDate = x.ExamDate,
                MaxScore = x.MaxScore
            }).ToList();

        var counts = await _u.GroupEnrollment
            .GetAll(e => groupIds.Contains(e.GroupId) && e.IsActive && e.LeftAt == null && e.Student.IsActive)
            .GroupBy(e => e.GroupId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        var map = counts.ToDictionary(x => x.Key, x => x.Count);

        return list.Select(x => new OfflineExamRow
        {
            Id = x.Id,
            Title = x.Title,
            GroupName = x.Group.Name,
            ExamDate = x.ExamDate,
            MaxScore = x.MaxScore,
            GradedCount = x.Grades.Count(g => g.Score != null),
            StudentCount = map.GetValueOrDefault(x.GroupId)
        }).ToList();
    }

    public async Task<OfflineExamEditVm?> GetOfflineAsync(long id, long? teacherId)
    {
        OfflineExam? exam = null;
        if (id != 0)
        {
            exam = await _u.OfflineExam
                .GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId || x.Group.TeacherId == teacherId), "Grades")
                .FirstOrDefaultAsync();
            if (exam == null) return null;
        }

        var groups = await _u.Group.GetAll(g => g.IsActive && (teacherId == null || g.TeacherId == teacherId))
            .OrderBy(g => g.Name).Select(g => new SimpleItem(g.Id, g.Name)).ToListAsync();

        var vm = new OfflineExamEditVm
        {
            Id = exam?.Id ?? 0,
            Title = exam?.Title ?? "",
            MaxScore = exam?.MaxScore ?? 0,
            GroupId = exam?.GroupId ?? 0,
            ExamDate = exam?.ExamDate,
            GroupLocked = exam != null,
            Groups = groups
        };

        if (exam != null)
            vm.Rows = await GradeRowsAsync(exam.GroupId, exam.Grades.ToDictionary(g => g.StudentId, g => g.Score));

        return vm;
    }

    public async Task<List<OfflineGradeRow>?> GroupStudentsAsync(long groupId, long? teacherId)
    {
        var ok = await _u.Group.GetAll(g => g.Id == groupId && g.IsActive && (teacherId == null || g.TeacherId == teacherId)).AnyAsync();
        if (!ok) return null;
        return await GradeRowsAsync(groupId, new Dictionary<long, decimal?>());
    }

    public async Task<OpResult> SaveOfflineAsync(OfflineExamSaveDto dto, long? teacherId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return OpResult.Fail("اسم الامتحان مطلوب");
        if (dto.MaxScore <= 0) return OpResult.Fail("درجة الامتحان لازم تكون أكبر من صفر");
        if (dto.ExamDate == default) return OpResult.Fail("حدد تاريخ الامتحان");

        OfflineExam? exam;
        if (dto.Id == 0)
        {
            if (dto.GroupId == 0) return OpResult.Fail("اختار المجموعة");
            var groupOk = await _u.Group.GetAll(g => g.Id == dto.GroupId && g.IsActive && (teacherId == null || g.TeacherId == teacherId)).AnyAsync();
            if (!groupOk) return OpResult.Fail("المجموعة مش تابعة لك");

            exam = new OfflineExam { TeacherId = teacherId, GroupId = dto.GroupId };
            _u.OfflineExam.Add(exam);
        }
        else
        {
            exam = await _u.OfflineExam
                .GetAll(x => x.Id == dto.Id && (teacherId == null || x.TeacherId == teacherId || x.Group.TeacherId == teacherId), "Grades", tracking: true)
                .FirstOrDefaultAsync();
            if (exam == null) return OpResult.Fail("الامتحان غير موجود");
        }

        exam.Title = dto.Title.Trim();
        exam.MaxScore = dto.MaxScore;
        exam.ExamDate = dto.ExamDate.Date;

        var members = await _u.GroupEnrollment
            .GetAll(e => e.GroupId == exam.GroupId && e.IsActive && e.LeftAt == null && e.Student.IsActive)
            .Select(e => e.StudentId).ToListAsync();
        var memberSet = members.ToHashSet();

        foreach (var row in dto.Grades ?? new())
        {
            if (!memberSet.Contains(row.StudentId)) return OpResult.Fail("فيه طالب مش في المجموعة");
            if (row.Score < 0) return OpResult.Fail("الدرجة مينفعش تكون بالسالب");
            if (row.Score > dto.MaxScore) return OpResult.Fail("درجة الطالب أكبر من درجة الامتحان");
        }

        await _u.SaveAsync();

        var existing = exam.Id == 0
            ? new List<OfflineExamGrade>()
            : await _u.OfflineExamGrade.GetAll(g => g.OfflineExamId == exam.Id, tracking: true).ToListAsync();

        foreach (var row in dto.Grades ?? new())
        {
            var grade = existing.FirstOrDefault(g => g.StudentId == row.StudentId);
            if (grade == null)
            {
                if (row.Score == null) continue;
                _u.OfflineExamGrade.Add(new OfflineExamGrade
                {
                    OfflineExamId = exam.Id,
                    StudentId = row.StudentId,
                    Score = row.Score
                });
            }
            else grade.Score = row.Score;
        }

        await _u.SaveAsync();
        return new OpResult(true, "تم حفظ الدرجات", exam.Id);
    }

    public async Task<OpResult> DeleteOfflineAsync(long id, long? teacherId)
    {
        var exam = await _u.OfflineExam
            .GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId || x.Group.TeacherId == teacherId), "Grades", tracking: true)
            .FirstOrDefaultAsync();
        if (exam == null) return OpResult.Fail("الامتحان غير موجود");
        foreach (var g in exam.Grades.ToList()) _u.OfflineExamGrade.Remove(g);
        _u.OfflineExam.Remove(exam);
        await _u.SaveAsync();
        return OpResult.Success("تم الحذف");
    }

    public async Task<StudentExamsVm?> ListForStudentAsync(long studentId)
    {
        var studentOk = await _u.Student.GetAll(s => s.Id == studentId && s.IsActive).AnyAsync();
        if (!studentOk) return null;

        var groupIds = await StudentGroupIds(studentId);
        var now = Now();
        var exams = await _u.OnlineExam
            .GetAll(x => x.Status == ExamStatus.Published && x.Groups.Any(g => groupIds.Contains(g.GroupId)), "Groups.Group,Questions")
            .OrderByDescending(x => x.OpensAt)
            .ToListAsync();

        var examIds = exams.Select(x => x.Id).ToList();
        await CloseExpiredAsync(examIds);
        var attempts = await _u.OnlineExamAttempt
            .GetAll(a => a.StudentId == studentId && examIds.Contains(a.ExamId))
            .ToListAsync();
        var byExam = attempts.ToDictionary(a => a.ExamId);

        var cards = exams.Select(x =>
        {
            byExam.TryGetValue(x.Id, out var attempt);
            var (phase, text, score) = StudentPhase(x, attempt, now);
            return new StudentExamCard
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Groups = string.Join("، ", x.Groups.Select(g => g.Group.Name)),
                OpensAt = x.OpensAt,
                ClosesAt = x.ClosesAt,
                ResultsAt = x.ResultsAt,
                MaxScore = x.Questions.Sum(q => q.Points),
                QuestionsCount = x.Questions.Count,
                Phase = phase,
                PhaseText = text,
                Score = score,
                CanStart = phase == "open",
                CanResume = phase == "progress"
            };
        }).ToList();

        var offline = await _u.OfflineExam
            .GetAll(x => groupIds.Contains(x.GroupId), "Group")
            .OrderByDescending(x => x.ExamDate)
            .ToListAsync();
        var offIds = offline.Select(x => x.Id).ToList();
        var grades = await _u.OfflineExamGrade
            .GetAll(g => g.StudentId == studentId && offIds.Contains(g.OfflineExamId))
            .ToListAsync();
        var gradeMap = grades.ToDictionary(g => g.OfflineExamId, g => g.Score);

        return new StudentExamsVm
        {
            Online = cards,
            Offline = offline.Select(x => new StudentOfflineGradeCard
            {
                Id = x.Id,
                Title = x.Title,
                GroupName = x.Group.Name,
                ExamDate = x.ExamDate,
                MaxScore = x.MaxScore,
                Score = gradeMap.GetValueOrDefault(x.Id)
            }).ToList(),
            OpenCount = cards.Count(c => c.CanStart || c.CanResume)
        };
    }

    public async Task<int> OpenCountAsync(long studentId)
    {
        var vm = await ListForStudentAsync(studentId);
        return vm?.OpenCount ?? 0;
    }

    public async Task<ExamIntroVm?> IntroAsync(long studentId, long examId)
    {
        var exam = await StudentExamAsync(studentId, examId);
        if (exam == null) return null;
        await CloseExpiredAsync(exam.Id);
        var attempt = await _u.OnlineExamAttempt.GetAll(a => a.ExamId == examId && a.StudentId == studentId).FirstOrDefaultAsync();
        var (phase, text, score) = StudentPhase(exam, attempt, Now());
        return new ExamIntroVm
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            OpensAt = exam.OpensAt,
            ClosesAt = EffectiveClose(exam, attempt),
            ResultsAt = exam.ResultsAt,
            MaxScore = exam.Questions.Sum(q => q.Points),
            QuestionsCount = exam.Questions.Count,
            Phase = phase,
            PhaseText = text,
            Score = score,
            CanStart = phase == "open",
            CanResume = phase == "progress"
        };
    }

    public async Task<OpResult> ReopenAsync(long examId, long studentId, int minutes, long? teacherId)
    {
        var exam = await _u.OnlineExam
            .GetAll(x => x.Id == examId && x.Status == ExamStatus.Published && (teacherId == null || x.TeacherId == teacherId))
            .FirstOrDefaultAsync();
        if (exam == null) return OpResult.Fail("الاختبار غير موجود");

        var groupIds = await _u.OnlineExamGroup.GetAll(g => g.ExamId == examId).Select(g => g.GroupId).ToListAsync();
        var enrolled = await _u.GroupEnrollment
            .GetAll(e => e.StudentId == studentId && e.IsActive && e.LeftAt == null && groupIds.Contains(e.GroupId))
            .AnyAsync();
        if (!enrolled) return OpResult.Fail("الطالب مش في مجموعات الاختبار");
        if (minutes < 5 || minutes > 360) return OpResult.Fail("المدة من 5 لـ 360 دقيقة");

        var until = Now().AddMinutes(minutes);

        var attempt = await _u.OnlineExamAttempt
            .GetAll(a => a.ExamId == examId && a.StudentId == studentId, "Answers", tracking: true)
            .FirstOrDefaultAsync();
        if (attempt == null)
        {
            _u.OnlineExamAttempt.Add(new OnlineExamAttempt
            {
                ExamId = examId,
                StudentId = studentId,
                StartedAt = Now(),
                Status = ExamAttemptStatus.InProgress,
                RetakeUntil = until
            });
        }
        else
        {
            foreach (var answer in attempt.Answers.ToList()) _u.OnlineExamAnswer.Remove(answer);
            attempt.Status = ExamAttemptStatus.InProgress;
            attempt.Score = null;
            attempt.SubmittedAt = null;
            attempt.EndReason = null;
            attempt.ViolationCount = 0;
            attempt.StartedAt = Now();
            attempt.RetakeUntil = until;
        }

        await _u.SaveAsync();
        return OpResult.Success($"اتفتح الاختبار لمدة {minutes} دقيقة");
    }

    public async Task<OpResult> StartAsync(long studentId, long examId)
    {
        var exam = await StudentExamAsync(studentId, examId);
        if (exam == null) return OpResult.Fail("الاختبار غير متاح");
        var now = Now();
        var existing = await _u.OnlineExamAttempt
            .GetAll(a => a.ExamId == examId && a.StudentId == studentId, tracking: true)
            .FirstOrDefaultAsync();
        if (now < exam.OpensAt) return OpResult.Fail("الاختبار لسه مافتحش");
        if (now >= EffectiveClose(exam, existing)) return OpResult.Fail("وقت الاختبار خلّص");

        if (existing != null)
        {
            if (existing.Status != ExamAttemptStatus.InProgress)
                return OpResult.Fail("الاختبار اتسلّم قبل كده ومينفعش تعيده");
            return new OpResult(true, "كمّل الاختبار", examId);
        }

        _u.OnlineExamAttempt.Add(new OnlineExamAttempt
        {
            ExamId = examId,
            StudentId = studentId,
            StartedAt = now,
            Status = ExamAttemptStatus.InProgress
        });
        try
        {
            await _u.SaveAsync();
        }
        catch (DbUpdateException)
        {
            return OpResult.Fail("حاول تاني، الاختبار اتفتح من مكان تاني");
        }
        return new OpResult(true, "بدأ الاختبار", examId);
    }

    public async Task<ExamTakeVm?> TakeAsync(long studentId, long examId)
    {
        var exam = await StudentExamAsync(studentId, examId);
        if (exam == null) return null;

        var attempt = await _u.OnlineExamAttempt
            .GetAll(a => a.ExamId == examId && a.StudentId == studentId, "Answers", tracking: true)
            .FirstOrDefaultAsync();
        if (attempt == null || attempt.Status != ExamAttemptStatus.InProgress) return null;

        var close = EffectiveClose(exam, attempt);
        if (Now() >= close)
        {
            await FinishAsync(attempt, exam, "timeout");
            await _u.SaveAsync();
            return null;
        }

        var student = await _u.Student.GetAll(s => s.Id == studentId).Select(s => new { s.Name, s.Code }).FirstAsync();
        var picked = attempt.Answers.ToDictionary(a => a.QuestionId, a => a.ChoiceId);

        return new ExamTakeVm
        {
            ExamId = exam.Id,
            Title = exam.Title,
            StudentName = student.Name,
            StudentCode = student.Code ?? "",
            ClosesAt = close,
            ResultsAt = exam.ResultsAt,
            RemainingSeconds = Math.Max(0, (int)(close - Now()).TotalSeconds),
            Questions = exam.Questions.OrderBy(q => q.SortOrder).ThenBy(q => q.Id).Select(q => new ExamTakeQuestionVm
            {
                Id = q.Id,
                Text = q.Text,
                Points = q.Points,
                HasImage = !string.IsNullOrEmpty(q.ImageStoredName),
                SelectedChoiceId = picked.GetValueOrDefault(q.Id),
                Choices = q.Choices.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).Select(c => new ExamTakeChoiceVm
                {
                    Id = c.Id,
                    Text = c.Text
                }).ToList()
            }).ToList()
        };
    }

    public async Task<ExamSyncResult> SyncAsync(long studentId, ExamSubmitDto dto, bool finish)
    {
        var exam = await _u.OnlineExam
            .GetAll(x => x.Id == dto.ExamId && x.Status == ExamStatus.Published, "Questions.Choices", tracking: true)
            .FirstOrDefaultAsync();
        if (exam == null) return ExamSyncResult.Fail("الاختبار غير موجود");

        var groupIds = await StudentGroupIds(studentId);
        var allowed = await _u.OnlineExamGroup.GetAll(g => g.ExamId == exam.Id && groupIds.Contains(g.GroupId)).AnyAsync();
        if (!allowed) return ExamSyncResult.Fail("الاختبار مش متاح لك");

        var attempt = await _u.OnlineExamAttempt
            .GetAll(a => a.ExamId == exam.Id && a.StudentId == studentId, "Answers", tracking: true)
            .FirstOrDefaultAsync();
        if (attempt == null) return ExamSyncResult.Fail("ابدأ الاختبار الأول");
        if (attempt.Status != ExamAttemptStatus.InProgress)
            return ExamSyncResult.Done("الاختبار اتسلّم");

        var now = Now();
        var close = EffectiveClose(exam, attempt);
        var timedOut = now >= close;
        await SaveAnswersAsync(attempt, exam, dto.Answers);

        if (!finish && !timedOut)
        {
            await _u.SaveAsync();
            return new ExamSyncResult
            {
                Ok = true,
                Message = "تم الحفظ",
                Finished = false,
                RemainingSeconds = Math.Max(0, (int)(close - now).TotalSeconds)
            };
        }

        var reason = timedOut ? "timeout" : NormalizeReason(dto.Reason);
        await FinishAsync(attempt, exam, reason);
        await _u.SaveAsync();
        return ExamSyncResult.Done(attempt.EndReason ?? "تم التسليم");
    }

    private async Task<List<OfflineGradeRow>> GradeRowsAsync(long groupId, Dictionary<long, decimal?> scores)
    {
        var students = await _u.GroupEnrollment
            .GetAll(e => e.GroupId == groupId && e.IsActive && e.LeftAt == null && e.Student.IsActive, "Student")
            .OrderBy(e => e.Student.Name)
            .Select(e => new { e.StudentId, Code = e.Student.Code, e.Student.Name })
            .ToListAsync();

        return students.Select(s => new OfflineGradeRow
        {
            StudentId = s.StudentId,
            StudentCode = s.Code ?? "",
            StudentName = s.Name,
            Score = scores.GetValueOrDefault(s.StudentId)
        }).ToList();
    }

    private async Task<List<long>> StudentGroupIds(long studentId)
        => await _u.GroupEnrollment
            .GetAll(e => e.StudentId == studentId && e.IsActive && e.LeftAt == null)
            .Select(e => e.GroupId).ToListAsync();

    private async Task<OnlineExam?> StudentExamAsync(long studentId, long examId)
    {
        var groupIds = await StudentGroupIds(studentId);
        return await _u.OnlineExam
            .GetAll(x => x.Id == examId && x.Status == ExamStatus.Published && x.Groups.Any(g => groupIds.Contains(g.GroupId)), "Groups.Group,Questions.Choices")
            .FirstOrDefaultAsync();
    }

    private async Task CloseExpiredAsync(long examId) => await CloseExpiredAsync(new List<long> { examId });

    private async Task CloseExpiredAsync(List<long> examIds)
    {
        if (examIds.Count == 0) return;
        var now = Now();
        var hanging = await _u.OnlineExamAttempt
            .GetAll(a => examIds.Contains(a.ExamId) && a.Status == ExamAttemptStatus.InProgress && ((a.RetakeUntil != null && a.RetakeUntil <= now) || (a.RetakeUntil == null && a.Exam.ClosesAt <= now)), "Answers,Exam.Questions.Choices", tracking: true)
            .ToListAsync();
        if (hanging.Count == 0) return;
        foreach (var attempt in hanging)
            await FinishAsync(attempt, attempt.Exam, "timeout");
        await _u.SaveAsync();
    }

    private async Task SaveAnswersAsync(OnlineExamAttempt attempt, OnlineExam exam, List<ExamAnswerDto>? answers)
    {
        if (answers == null || answers.Count == 0) return;
        var questions = exam.Questions.ToDictionary(q => q.Id);
        foreach (var row in answers)
        {
            if (!questions.TryGetValue(row.QuestionId, out var question)) continue;
            long? choiceId = null;
            if (row.ChoiceId != null && question.Choices.Any(c => c.Id == row.ChoiceId))
                choiceId = row.ChoiceId;

            var current = attempt.Answers.FirstOrDefault(a => a.QuestionId == row.QuestionId);
            if (current == null)
            {
                if (choiceId == null) continue;
                attempt.Answers.Add(new OnlineExamAnswer { AttemptId = attempt.Id, QuestionId = row.QuestionId, ChoiceId = choiceId });
            }
            else current.ChoiceId = choiceId;
        }
        await Task.CompletedTask;
    }

    private Task FinishAsync(OnlineExamAttempt attempt, OnlineExam exam, string reason)
    {
        if (attempt.Status != ExamAttemptStatus.InProgress) return Task.CompletedTask;

        decimal score = 0;
        foreach (var question in exam.Questions)
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (answer?.ChoiceId != null && question.Choices.Any(c => c.Id == answer.ChoiceId && c.IsCorrect))
                score += question.Points;
        }

        attempt.Score = score;
        attempt.SubmittedAt = Now();
        var auto = reason != "submit";
        attempt.Status = auto ? ExamAttemptStatus.AutoSubmitted : ExamAttemptStatus.Submitted;
        attempt.EndReason = reason switch
        {
            "submit" => "تسليم",
            "timeout" => "انتهى وقت الاختبار",
            _ => "خرجت من صفحة الاختبار"
        };
        if (auto) attempt.ViolationCount += 1;
        return Task.CompletedTask;
    }

    private void ReplaceQuestions(OnlineExam exam, List<ExamQuestionInput>? questions)
    {
        var previous = exam.Questions
            .Select(q => q.ImageStoredName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var q in exam.Questions.ToList())
        {
            foreach (var c in q.Choices.ToList()) _u.OnlineExamChoice.Remove(c);
            _u.OnlineExamQuestion.Remove(q);
        }

        var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var order = 0;
        foreach (var q in questions ?? new())
        {
            var image = q.ImageStoredName?.Trim().ToLowerInvariant();
            if (image != null && (!ImageName.IsMatch(image) || !previous.Contains(image))) image = null;
            if (image != null) keep.Add(image);

            var entity = new OnlineExamQuestion
            {
                Text = q.Text.Trim(),
                Points = q.Points,
                SortOrder = order++,
                ImageStoredName = image
            };
            var cOrder = 0;
            foreach (var c in q.Choices)
            {
                entity.Choices.Add(new OnlineExamChoice
                {
                    Text = c.Text.Trim(),
                    IsCorrect = c.IsCorrect,
                    SortOrder = cOrder++
                });
            }
            exam.Questions.Add(entity);
        }

        if (exam.Id > 0)
        {
            foreach (var name in previous)
                if (!string.IsNullOrWhiteSpace(name) && !keep.Contains(name))
                    DeleteImage(exam.Id, name);
        }
    }

    private static bool IsLocked(OnlineExam exam)
        => exam.Attempts.Count > 0 || (exam.Status == ExamStatus.Published && DateTime.Now >= exam.OpensAt);

    private static string? ValidateSchedule(DateTime opens, DateTime closes, DateTime results)
    {
        if (opens == default || closes == default || results == default) return "حدد موعد الفتح والقفل وظهور النتيجة";
        if (opens >= closes) return "ميعاد القفل لازم يكون بعد ميعاد الفتح";
        if (results < closes) return "وقت ظهور النتيجة لازم يكون بعد قفل الاختبار، عشان كل الطلاب يشوفوا الدرجة في نفس الوقت";
        return null;
    }

    private static string? ValidateQuestions(List<ExamQuestionInput>? questions, bool required)
    {
        questions ??= new();
        if (questions.Count == 0) return required ? "ضيف سؤال واحد على الأقل" : null;
        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var n = i + 1;
            if (string.IsNullOrWhiteSpace(q.Text)) return $"اكتب نص السؤال {n}";
            if (q.Points <= 0) return $"درجة السؤال {n} لازم تكون أكبر من صفر";
            if (q.Choices == null || q.Choices.Count < 2) return $"السؤال {n} محتاج اختيارين على الأقل";
            if (q.Choices.Any(c => string.IsNullOrWhiteSpace(c.Text))) return $"فيه اختيار فاضي في السؤال {n}";
            if (q.Choices.Count(c => c.IsCorrect) != 1) return $"السؤال {n} لازم يكون له إجابة صحيحة واحدة";
        }
        return null;
    }

    private static OnlineExamRow MapRow(OnlineExam x) => new()
    {
        Id = x.Id,
        Title = x.Title,
        Teacher = x.Teacher?.Name ?? "",
        Groups = string.Join("، ", x.Groups.Select(g => g.Group.Name)),
        QuestionsCount = x.Questions.Count,
        MaxScore = x.Questions.Sum(q => q.Points),
        Status = x.Status,
        OpensAt = x.OpensAt,
        ClosesAt = x.ClosesAt,
        ResultsAt = x.ResultsAt,
        AttemptsCount = x.Attempts.Count
    };

    private static string AttemptText(OnlineExamAttempt? attempt, DateTime now, OnlineExam exam)
    {
        if (attempt == null)
        {
            if (now < exam.OpensAt) return "لسه مافتحش";
            if (now < exam.ClosesAt) return "لم يبدأ";
            return "لم يدخل";
        }
        if (attempt.Status == ExamAttemptStatus.InProgress)
        {
            var close = EffectiveClose(exam, attempt);
            if (now < close && attempt.RetakeUntil != null)
                return "مفتوح تاني لحد " + attempt.RetakeUntil.Value.ToString("HH:mm");
            if (now < close) return "جاري";
        }
        return attempt.EndReason ?? "تم التسليم";
    }

    private static DateTime EffectiveClose(OnlineExam exam, OnlineExamAttempt? attempt)
        => attempt?.RetakeUntil ?? exam.ClosesAt;

    private static (string Phase, string Text, decimal? Score) StudentPhase(OnlineExam exam, OnlineExamAttempt? attempt, DateTime now)
    {
        var revealed = now >= exam.ResultsAt;
        var close = EffectiveClose(exam, attempt);
        if (attempt?.Status == ExamAttemptStatus.InProgress && now < close)
            return ("progress", attempt?.RetakeUntil != null ? "اتفتح لك الاختبار تاني" : "الاختبار مفتوح، كمّل", null);
        if (attempt != null && attempt.Status != ExamAttemptStatus.InProgress)
        {
            if (!revealed) return ("submitted", "تم التسليم، النتيجة لسه", null);
            return ("result", "النتيجة ظهرت", attempt.Score ?? 0);
        }
        if (now < exam.OpensAt) return ("upcoming", "لسه مافتحش", null);
        if (now < exam.ClosesAt) return ("open", "مفتوح دلوقتي", null);
        if (!revealed) return ("closed", "وقت الاختبار خلّص", null);
        return ("missed", "لم تدخل الاختبار", null);
    }

    private static string NormalizeReason(string? reason)
    {
        var r = (reason ?? "").Trim().ToLowerInvariant();
        if (r is "left" or "fullscreen" or "timeout") return r;
        return "submit";
    }

    public async Task<OpResult> SaveQuestionImageAsync(long examId, int sortOrder, IFormFile file, long? teacherId)
    {
        var exam = await _u.OnlineExam
            .GetAll(x => x.Id == examId && (teacherId == null || x.TeacherId == teacherId), "Questions,Attempts", tracking: true)
            .FirstOrDefaultAsync();
        if (exam == null) return OpResult.Fail("الاختبار غير موجود");
        if (IsLocked(exam)) return OpResult.Fail("الاختبار بدأ، مش هينفع تعديل الصورة");

        var question = exam.Questions.FirstOrDefault(q => q.SortOrder == sortOrder);
        if (question == null) return OpResult.Fail("السؤال مش موجود");
        if (file == null || file.Length == 0) return OpResult.Fail("اختار صورة");
        if (file.Length > MaxImage) return OpResult.Fail("الصورة أكبر من 4 ميجا");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        ext = ext switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".png" => ".png",
            ".webp" => ".webp",
            ".gif" => ".gif",
            _ => ""
        };
        if (ext.Length == 0) return OpResult.Fail("الصورة لازم تكون jpg أو png أو webp أو gif");

        var folder = ExamFolder(examId);
        Directory.CreateDirectory(folder);
        var stored = Guid.NewGuid().ToString("N") + ext;
        var path = Path.Combine(folder, stored);
        await using (var fs = new FileStream(path, FileMode.Create))
            await file.CopyToAsync(fs);
        if (!ImageBytesMatch(path, ext))
        {
            File.Delete(path);
            return OpResult.Fail("الملف مش صورة سليمة");
        }

        var old = question.ImageStoredName;
        question.ImageStoredName = stored;
        await _u.SaveAsync();
        if (!string.IsNullOrWhiteSpace(old) && !string.Equals(old, stored, StringComparison.OrdinalIgnoreCase))
            DeleteImage(examId, old);
        return OpResult.Success("اتضافت الصورة");
    }

    public async Task<(string Path, string ContentType)?> OpenQuestionImageAsync(long examId, string storedName, long? teacherId)
    {
        var name = (storedName ?? "").Trim().ToLowerInvariant();
        if (!ImageName.IsMatch(name)) return null;
        var owns = await _u.OnlineExamQuestion
            .GetAll(q => q.ExamId == examId && q.ImageStoredName == name && (teacherId == null || q.Exam.TeacherId == teacherId))
            .AnyAsync();
        if (!owns) return null;
        var path = Path.Combine(ExamFolder(examId), name);
        return File.Exists(path) ? (path, ImageType(name)) : null;
    }

    public async Task<(string Path, string ContentType)?> OpenStudentImageAsync(long studentId, long questionId)
    {
        var question = await _u.OnlineExamQuestion
            .GetAll(q => q.Id == questionId && q.ImageStoredName != null, "Exam")
            .FirstOrDefaultAsync();
        if (question?.ImageStoredName == null || question.Exam.Status != ExamStatus.Published) return null;
        var name = question.ImageStoredName.ToLowerInvariant();
        if (!ImageName.IsMatch(name)) return null;

        var attempt = await _u.OnlineExamAttempt
            .GetAll(a => a.ExamId == question.ExamId && a.StudentId == studentId && a.Status == ExamAttemptStatus.InProgress)
            .FirstOrDefaultAsync();
        if (attempt == null || Now() >= EffectiveClose(question.Exam, attempt)) return null;

        var path = Path.Combine(ExamFolder(question.ExamId), name);
        return File.Exists(path) ? (path, ImageType(name)) : null;
    }

    private string ExamFolder(long examId) => Path.Combine(_env.ContentRootPath, "App_Data", "exams", examId.ToString());

    private void DeleteImage(long examId, string storedName)
    {
        var name = storedName.Trim().ToLowerInvariant();
        if (!ImageName.IsMatch(name)) return;
        var path = Path.Combine(ExamFolder(examId), name);
        if (File.Exists(path)) File.Delete(path);
    }

    private static bool ImageBytesMatch(string path, string ext)
    {
        var b = new byte[12];
        int n;
        using (var s = File.OpenRead(path)) n = s.Read(b, 0, b.Length);
        if (n < 4) return false;
        return ext switch
        {
            ".jpg" => b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
            ".png" => b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,
            ".gif" => b[0] == (byte)'G' && b[1] == (byte)'I' && b[2] == (byte)'F',
            ".webp" => n >= 12 && b[0] == (byte)'R' && b[1] == (byte)'I' && b[2] == (byte)'F' && b[3] == (byte)'F'
                        && b[8] == (byte)'W' && b[9] == (byte)'E' && b[10] == (byte)'B' && b[11] == (byte)'P',
            _ => false
        };
    }

    private static string ImageType(string name) => Path.GetExtension(name) switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "image/jpeg"
    };
}
