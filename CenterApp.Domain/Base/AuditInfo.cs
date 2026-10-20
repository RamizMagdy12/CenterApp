// Base/AuditInfo.cs
namespace CenterApp.Domain.Base;

public record AuditInfo(string? UserId, string? UserName, DateTime Timestamp);