using CenterApp.Entity.Center;
using CenterApp.Entity.Identity;
using CenterApp.Entity.Security;
using CenterApp.Service.Data;
using CenterApp.Service.Models;
using CenterApp.Service.Services;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CenterApp.Service.Repository;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _u;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _um;
    public ChatService(IUnitOfWork u, AppDbContext db, UserManager<AppUser> um) { _u = u; _db = db; _um = um; }

    public async Task<long?> TeacherIdOfUserAsync(string userId)
        => await _u.Teacher.GetAll(t => t.UserId == userId).Select(t => (long?)t.Id).FirstOrDefaultAsync();

    public async Task<List<ConversationRow>> InboxAsync(long? teacherId)
    {
        var q = _u.Conversation.GetAll(c => teacherId == null || c.TeacherId == teacherId, "Student,Teacher");
        var list = await q.OrderByDescending(c => c.LastMessageAt).ToListAsync();
        return list.Select(c => new ConversationRow
        {
            Id = c.Id,
            StudentName = c.Student.Name,
            StudentCode = c.Student.Code ?? "",
            TeacherName = c.Teacher.Name,
            LastMessage = c.LastMessage ?? "",
            When = When(c.LastMessageAt),
            Unread = c.TeacherUnread
        }).ToList();
    }

    public async Task<ThreadVm?> OpenForStaffAsync(long conversationId, long? teacherId, string userId)
    {
        var c = await _u.Conversation.GetAll(x => x.Id == conversationId, "Student,Teacher").FirstOrDefaultAsync();
        if (c == null) return null;
        if (teacherId != null && c.TeacherId != teacherId) return null;

        await MarkReadAsync(userId, c.Id);
        return new ThreadVm
        {
            Id = c.Id,
            TeacherId = c.TeacherId,
            Title = c.Student.Name,
            Subtitle = string.IsNullOrEmpty(c.Student.Code) ? c.Teacher.Name : $"{c.Student.Code} · {c.Teacher.Name}",
            Messages = await LinesAsync(c.Id, userId)
        };
    }

    public async Task<PortalAskVm?> AskAsync(long studentId, string userId, long? teacherId)
    {
        var teachers = await PicksAsync(studentId);
        if (teacherId == null) return new PortalAskVm { Teachers = teachers };

        var teacher = teachers.FirstOrDefault(t => t.Id == teacherId);
        if (teacher == null) return null;

        var existing = await _u.Conversation.GetAll(c => c.StudentId == studentId && c.TeacherId == teacherId)
            .Select(c => new { c.Id }).FirstOrDefaultAsync();
        if (existing != null) await MarkReadAsync(userId, existing.Id);

        return new PortalAskVm
        {
            Teachers = teachers,
            Thread = new ThreadVm
            {
                Id = existing?.Id ?? 0,
                TeacherId = teacher.Id,
                Title = teacher.Name,
                Subtitle = teacher.Subjects,
                TeacherHasAccount = teacher.HasAccount,
                Messages = existing == null ? new() : await LinesAsync(existing.Id, userId)
            }
        };
    }

    public async Task<ChatSendResult> SendFromStudentAsync(long studentId, string userId, string senderName, long teacherId, string? body)
    {
        var text = Clean(body);
        if (text == null) return ChatSendResult.Fail("اكتب السؤال الأول");

        var allowed = await _u.GroupEnrollment.GetAll(e => e.StudentId == studentId && e.IsActive && e.Group.TeacherId == teacherId).AnyAsync();
        if (!allowed) return ChatSendResult.Fail("المدرس ده مش من مدرسينك");

        var teacher = await _u.Teacher.GetAll(t => t.Id == teacherId).Select(t => new { t.Id, t.Name, t.UserId }).FirstOrDefaultAsync();
        if (teacher == null) return ChatSendResult.Fail("المدرس غير موجود");

        var now = DateTime.Now;
        var conversation = _u.Conversation.GetFirstOrDefault(c => c.StudentId == studentId && c.TeacherId == teacherId, tracking: true);
        if (conversation == null)
        {
            conversation = new Conversation { StudentId = studentId, TeacherId = teacherId };
            _u.Conversation.Add(conversation);
        }

        conversation.LastMessage = Preview(text);
        conversation.LastMessageAt = now;
        conversation.TeacherUnread++;
        _u.ChatMessage.Add(new ChatMessage
        {
            Conversation = conversation,
            SenderUserId = userId,
            SenderName = senderName,
            FromStudent = true,
            Body = text,
            SentAt = now
        });

        try { await _u.SaveAsync(); }
        catch (DbUpdateException) { return ChatSendResult.Fail("حصل تعارض، ابعت الرسالة تاني"); }

        var recipients = new List<string>();
        if (!string.IsNullOrEmpty(teacher.UserId) && teacher.UserId != userId)
            recipients.Add(teacher.UserId);
        recipients.AddRange(await AdminIdsAsync(userId, teacher.UserId));

        var url = $"/Messages/Thread/{conversation.Id}";
        foreach (var id in recipients)
        {
            _u.UserNotification.Add(new UserNotification
            {
                UserId = id,
                Title = $"سؤال من {senderName}",
                Body = Preview(text),
                Url = url,
                ConversationId = conversation.Id,
                CreatedAt = now
            });
        }
        if (recipients.Count > 0) await _u.SaveAsync();

        var when = When(now);
        var live = recipients.Count == 0 ? null : new ChatLive(conversation.Id, senderName, text, when, true);
        var message = string.IsNullOrEmpty(teacher.UserId)
            ? "اتبعتت، بس المدرس لسه ملوش حساب دخول فمش هيوصله إشعار"
            : "تم الإرسال";
        return new ChatSendResult(true, message, conversation.Id, when, recipients, live);
    }

    public async Task<ChatSendResult> SendFromStaffAsync(long conversationId, long? teacherId, string userId, string senderName, string? body)
    {
        var text = Clean(body);
        if (text == null) return ChatSendResult.Fail("اكتب الرد الأول");

        var conversation = _u.Conversation.GetFirstOrDefault(c => c.Id == conversationId, tracking: true);
        if (conversation == null) return ChatSendResult.Fail("المحادثة غير موجودة");
        if (teacherId != null && conversation.TeacherId != teacherId) return ChatSendResult.Fail("المحادثة دي مش بتاعتك");

        var studentUserId = await _u.Student.GetAll(s => s.Id == conversation.StudentId).Select(s => s.UserId).FirstOrDefaultAsync();
        var now = DateTime.Now;
        conversation.LastMessage = Preview(text);
        conversation.LastMessageAt = now;
        conversation.StudentUnread++;
        _u.ChatMessage.Add(new ChatMessage
        {
            Conversation = conversation,
            SenderUserId = userId,
            SenderName = senderName,
            FromStudent = false,
            Body = text,
            SentAt = now
        });

        if (!string.IsNullOrEmpty(studentUserId))
        {
            _u.UserNotification.Add(new UserNotification
            {
                UserId = studentUserId,
                Title = $"رد من {senderName}",
                Body = Preview(text),
                Url = $"/Portal/Ask?teacherId={conversation.TeacherId}",
                ConversationId = conversation.Id,
                CreatedAt = now
            });
        }

        await _u.SaveAsync();
        var when = When(now);
        var recipients = string.IsNullOrEmpty(studentUserId) ? Array.Empty<string>() : new[] { studentUserId };
        var live = recipients.Length == 0 ? null : new ChatLive(conversation.Id, senderName, text, when, false);
        return new ChatSendResult(true, "تم الإرسال", conversation.Id, when, recipients, live);
    }

    public async Task<BellVm> BellAsync(string userId)
    {
        var unread = await _u.UserNotification.GetAll(n => n.UserId == userId && !n.IsRead).CountAsync();
        var rows = await _u.UserNotification.GetAll(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt).Take(15)
            .Select(n => new { n.Title, n.Body, n.Url, n.IsRead, n.CreatedAt })
            .ToListAsync();
        return new BellVm
        {
            Unread = unread,
            Items = rows.Select(n => new BellItem
            {
                Title = n.Title,
                Body = n.Body,
                Url = n.Url,
                IsRead = n.IsRead,
                When = When(n.CreatedAt)
            }).ToList()
        };
    }

    public async Task MarkReadAsync(string userId, long? conversationId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        if (conversationId == null)
        {
            var studentId = await _u.Student.GetAll(s => s.UserId == userId).Select(s => s.Id).FirstOrDefaultAsync();
            var teacherId = await TeacherIdOfUserAsync(userId);
            if (studentId > 0)
            {
                var rows = await _u.Conversation.GetAll(c => c.StudentId == studentId && c.StudentUnread > 0, tracking: true).ToListAsync();
                foreach (var row in rows) row.StudentUnread = 0;
            }
            if (teacherId != null)
            {
                var rows = await _u.Conversation.GetAll(c => c.TeacherId == teacherId && c.TeacherUnread > 0, tracking: true).ToListAsync();
                foreach (var row in rows) row.TeacherUnread = 0;
            }
        }
        else
        {
            var row = _u.Conversation.GetFirstOrDefault(c => c.Id == conversationId, tracking: true);
            if (row != null)
            {
                var studentUser = await _u.Student.GetAll(s => s.Id == row.StudentId).Select(s => s.UserId).FirstOrDefaultAsync();
                var teacherUser = await _u.Teacher.GetAll(t => t.Id == row.TeacherId).Select(t => t.UserId).FirstOrDefaultAsync();
                if (studentUser == userId) row.StudentUnread = 0;
                if (teacherUser == userId) row.TeacherUnread = 0;
            }
        }

        var notes = await _u.UserNotification.GetAll(
            n => n.UserId == userId && !n.IsRead && (conversationId == null || n.ConversationId == conversationId),
            tracking: true).ToListAsync();
        foreach (var note in notes) note.IsRead = true;
        await _u.SaveAsync();
    }

    public async Task<OpResult> ClearNotificationsAsync(string userId, bool isAdmin)
    {
        if (!await CanManageAsync(userId, isAdmin)) return OpResult.Fail("معندكش صلاحية لمسح الإشعارات");
        await _db.UserNotifications.IgnoreQueryFilters().Where(n => n.UserId == userId).ExecuteDeleteAsync();
        return OpResult.Success("تم مسح الإشعارات");
    }

    public async Task<OpResult> DeleteConversationAsync(long conversationId, long? teacherId, bool isAdmin, string userId)
    {
        if (!await CanManageAsync(userId, isAdmin)) return OpResult.Fail("معندكش صلاحية لمسح الشات");
        var row = await _u.Conversation.GetAll(c => c.Id == conversationId).Select(c => new { c.Id, c.TeacherId }).FirstOrDefaultAsync();
        if (row == null) return OpResult.Fail("المحادثة غير موجودة");
        if (!isAdmin && (teacherId == null || row.TeacherId != teacherId)) return OpResult.Fail("المحادثة دي مش بتاعتك");

        await _db.ChatMessages.IgnoreQueryFilters().Where(m => m.ConversationId == conversationId).ExecuteDeleteAsync();
        await _db.UserNotifications.IgnoreQueryFilters().Where(n => n.ConversationId == conversationId).ExecuteDeleteAsync();
        await _db.Conversations.IgnoreQueryFilters().Where(c => c.Id == conversationId).ExecuteDeleteAsync();
        return OpResult.Success("تم مسح الشات");
    }

    public async Task<int> StudentUnreadAsync(long studentId)
        => await _u.Conversation.GetAll(c => c.StudentId == studentId).SumAsync(c => (int?)c.StudentUnread) ?? 0;

    private async Task<List<TeacherChatPick>> PicksAsync(long studentId)
    {
        var rows = await _u.GroupEnrollment
            .GetAll(e => e.StudentId == studentId && e.IsActive, "Group.Teacher,Group.Subject")
            .Select(e => new
            {
                e.Group.TeacherId,
                Name = e.Group.Teacher.Name,
                Subject = e.Group.Subject.Name,
                HasAccount = e.Group.Teacher.UserId != null && e.Group.Teacher.UserId != ""
            }).ToListAsync();

        var unread = await _u.Conversation.GetAll(c => c.StudentId == studentId)
            .Select(c => new { c.TeacherId, c.StudentUnread }).ToListAsync();

        return rows.GroupBy(r => r.TeacherId).Select(g => new TeacherChatPick
        {
            Id = g.Key,
            Name = g.First().Name,
            Subjects = string.Join("، ", g.Select(x => x.Subject).Distinct()),
            HasAccount = g.First().HasAccount,
            Unread = unread.FirstOrDefault(u => u.TeacherId == g.Key)?.StudentUnread ?? 0
        }).OrderBy(t => t.Name).ToList();
    }

    private async Task<List<ChatLine>> LinesAsync(long conversationId, string userId)
    {
        var msgs = await _u.ChatMessage.GetAll(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Id).Take(300).ToListAsync();
        msgs.Reverse();
        return msgs.Select(m => new ChatLine
        {
            SenderName = m.SenderName,
            Body = m.Body,
            When = When(m.SentAt),
            Mine = m.SenderUserId == userId
        }).ToList();
    }

    private async Task<bool> CanManageAsync(string userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (await TeacherIdOfUserAsync(userId) != null) return true;
        var user = await _um.FindByIdAsync(userId);
        return user != null && await _um.IsInRoleAsync(user, RoleNames.Teacher);
    }

    private async Task<List<string>> AdminIdsAsync(params string?[] except)
    {
        var skip = except.Where(id => !string.IsNullOrEmpty(id)).ToHashSet();
        var admins = await _um.GetUsersInRoleAsync(RoleNames.Admin);
        return admins.Where(a => a.IsActive && !skip.Contains(a.Id)).Select(a => a.Id).ToList();
    }

    private static string? Clean(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        var text = body.Trim();
        return text.Length > 2000 ? text[..2000] : text;
    }

    private static string Preview(string text) => text.Length <= 140 ? text : text[..140] + "…";

    public static string When(DateTime t)
    {
        var now = DateTime.Now;
        if (t.Date == now.Date) return t.ToString("HH:mm");
        if (t.Date == now.Date.AddDays(-1)) return "أمس " + t.ToString("HH:mm");
        return t.ToString("d/M HH:mm");
    }
}
