using CenterApp.Application.Models;

namespace CenterApp.Application.Abstractions;

public interface IChatNotifier
{
    Task PushAsync(string userId, ChatLive live);
}

public interface IChatService
{
    Task<long?> TeacherIdOfUserAsync(string userId);
    Task<List<ConversationRow>> InboxAsync(long? teacherId);
    Task<ThreadVm?> OpenForStaffAsync(long conversationId, long? teacherId, string userId);
    Task<PortalAskVm?> AskAsync(long studentId, string userId, long? teacherId);
    Task<ChatSendResult> SendFromStudentAsync(long studentId, string userId, string senderName, long teacherId, string? body);
    Task<ChatSendResult> SendFromStaffAsync(long conversationId, long? teacherId, string userId, string senderName, string? body);
    Task<BellVm> BellAsync(string userId);
    Task MarkReadAsync(string userId, long? conversationId);
    Task<OpResult> ClearNotificationsAsync(string userId, bool isAdmin);
    Task<OpResult> DeleteConversationAsync(long conversationId, long? teacherId, bool isAdmin, string userId);
    Task<int> StudentUnreadAsync(long studentId);
}
