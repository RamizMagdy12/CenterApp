namespace CenterApp.Application.Models;

public class TeacherChatPick
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Subjects { get; set; } = "";
    public int Unread { get; set; }
    public bool HasAccount { get; set; }
}

public class ConversationRow
{
    public long Id { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentCode { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string LastMessage { get; set; } = "";
    public string When { get; set; } = "";
    public int Unread { get; set; }
}

public class ChatLine
{
    public string SenderName { get; set; } = "";
    public string Body { get; set; } = "";
    public string When { get; set; } = "";
    public bool Mine { get; set; }
}

public class ThreadVm
{
    public long Id { get; set; }
    public long TeacherId { get; set; }
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public bool TeacherHasAccount { get; set; } = true;
    public List<ChatLine> Messages { get; set; } = new();
}

public class PortalAskVm
{
    public List<TeacherChatPick> Teachers { get; set; } = new();
    public ThreadVm? Thread { get; set; }
}

public class BellItem
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Url { get; set; }
    public bool IsRead { get; set; }
    public string When { get; set; } = "";
}

public class BellVm
{
    public int Unread { get; set; }
    public List<BellItem> Items { get; set; } = new();
}

public record ChatLive(long ConversationId, string SenderName, string Body, string SentAtText, bool FromStudent);

public record ChatSendResult(bool Ok, string Message, long? Id, string? When, IReadOnlyList<string> ToUserIds, ChatLive? Live)
{
    public static ChatSendResult Fail(string message) => new(false, message, null, null, Array.Empty<string>(), null);
}
