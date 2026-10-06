// Security/Screens.cs
namespace CenterApp.Entity.Security;

public static class RoleNames
{
    public const string Admin = "Admin";
}

public static class Screens
{
    public const string Students = "students";
    public const string Groups = "groups";
    public const string Sessions = "sessions";
    public const string Invoices = "invoices";
    public const string Lookups = "lookups";
    public const string Users = "users";
    public const string Roles = "roles";

    public static readonly (string Code, string Name)[] All =
    {
        (Students, "الطلاب"),
        (Groups, "المجموعات"),
        (Sessions, "الحصص والحضور"),
        (Invoices, "الفواتير والتحصيل"),
        (Lookups, "البيانات الأساسية"),
        (Users, "المستخدمين"),
        (Roles, "الأدوار والصلاحيات")
    };
}