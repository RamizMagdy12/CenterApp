using CenterApp.Entity.Base;

namespace CenterApp.Entity.Center;

public class Conversation : BaseEntity
{
    public long StudentId { get; set; }
    public long TeacherId { get; set; }
    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int StudentUnread { get; set; }
    public int TeacherUnread { get; set; }

    public virtual Student Student { get; set; } = null!;
    public virtual Teacher Teacher { get; set; } = null!;
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage : BaseEntity
{
    public long ConversationId { get; set; }
    public string SenderUserId { get; set; } = "";
    public string SenderName { get; set; } = "";
    public bool FromStudent { get; set; }
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;
}

public class UserNotification : BaseEntity
{
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Url { get; set; }
    public long? ConversationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
