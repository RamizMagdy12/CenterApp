using CenterApp.Entity.Center;

namespace CenterApp.Service.Models;

public class GroupOption { public long Id { get; set; } public string Name { get; set; } = ""; public bool Selected { get; set; } }

public class MaterialRow
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Groups { get; set; } = "";
    public int FilesCount { get; set; }
    public MaterialStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class MaterialFileVm
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public MaterialFileKind Kind { get; set; }
    public string Size { get; set; } = "";
    public int PageCount { get; set; }
}

public class MaterialEditVm
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public MaterialStatus Status { get; set; }
    public List<GroupOption> Groups { get; set; } = new();
    public List<MaterialFileVm> Files { get; set; } = new();
}

public class MaterialSaveDto
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public List<long> GroupIds { get; set; } = new();
}

public class ViewerFileVm
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public MaterialFileKind Kind { get; set; }
    public int PageCount { get; set; }
    public string Token { get; set; } = "";
}

public class ViewerVm
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Watermark { get; set; } = "";
    public List<ViewerFileVm> Files { get; set; } = new();
}

public record StudentInfo(long Id, string Code, string Name);

public class PortalMaterialRow
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Groups { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public int FilesCount { get; set; }
    public int Videos { get; set; }
    public int Docs { get; set; }
    public bool IsLocked { get; set; }
    public string? Reason { get; set; }
    public DateTime? GraceUntil { get; set; }
}

public class PortalHomeVm
{
    public StudentInfo Student { get; set; } = null!;
    public List<PortalMaterialRow> Latest { get; set; } = new();
    public int Total { get; set; }
    public int Locked { get; set; }
}

public class PortalViewVm
{
    public PortalMaterialRow Row { get; set; } = null!;
    public ViewerVm? Viewer { get; set; }
}

public class GraceRow
{
    public long Id { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentCode { get; set; } = "";
    public string GroupName { get; set; } = "";
    public int Days { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Reason { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.Now;
}

public class GraceFormVm
{
    public long? StudentId { get; set; }
    public string? StudentName { get; set; }
    public long? GroupId { get; set; }
    public List<SimpleItem> Students { get; set; } = new();
    public List<SimpleItem> Groups { get; set; } = new();
}