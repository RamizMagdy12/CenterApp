using Microsoft.Extensions.Localization;

namespace CenterApp.Application.Localization;

public interface ILocService
{
    LocalizedString Get(string key);
    LocalizedString Get(string key, params object[] arguments);
}
