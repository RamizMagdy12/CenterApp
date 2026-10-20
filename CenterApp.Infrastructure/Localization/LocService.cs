using System.Reflection;
using CenterApp.Application.Localization;
using Microsoft.Extensions.Localization;

namespace CenterApp.Infrastructure.Localization;

public class LocService : ILocService
{
    private readonly IStringLocalizer _localizer;

    public LocService(IStringLocalizerFactory factory)
    {
        var type = typeof(SharedResource);
        var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName!);

        _localizer = factory.Create(
            "SharedResource",
            assemblyName.Name!);
    }

    public virtual LocalizedString Get(string key)
    {
        return _localizer[key];
    }

    public virtual LocalizedString Get(string key, params object[] arguments)
    {
        return _localizer[key, arguments];
    }
}
