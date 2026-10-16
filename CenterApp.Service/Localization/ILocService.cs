using Microsoft.Extensions.Localization;

namespace CenterApp.Service.Localization;

public interface ILocService
{
    LocalizedString Get(string key);
    LocalizedString Get(string key, params object[] arguments);
}
