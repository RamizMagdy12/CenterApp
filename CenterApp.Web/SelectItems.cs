using CenterApp.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CenterApp.Web;

public static class SelectItems
{
    public static List<SelectListItem> From(IEnumerable<LookupItem> items)
        => items.Select(i => new SelectListItem(i.Text, i.Value)).ToList();
}
