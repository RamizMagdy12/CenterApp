using System.Globalization;

namespace CenterApp.Domain.Center;

public static class Bilingual
{
    public static string Show(string? arabic, string? english)
    {
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.StartsWith("en", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(english))
            return english.Trim();
        return (arabic ?? "").Trim();
    }

    public static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
