// Center/Enums.cs
namespace CenterApp.Entity.Center;

public enum SessionStatus { Scheduled = 0, Done = 1, Cancelled = 2 }
public enum AttendanceStatus { Present = 0, Absent = 1, Excused = 2, Late = 3 }
public enum InvoiceStatus { Unpaid = 0, PartiallyPaid = 1, Paid = 2, Waived = 3 }
public enum PaymentMethod { Cash = 0, Wallet = 1, Transfer = 2 }
public enum DiscountKind { None = 0, Percentage = 1, Fixed = 2 }