// Base/AuditInfo.cs
namespace CenterApp.Entity.Base;

public record AuditInfo(string? UserId, string? UserName, DateTime Timestamp);