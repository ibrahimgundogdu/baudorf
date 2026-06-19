using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Baudorf.Web.Services;

/// <summary>Erzeugt URL-sichere Slugs aus deutschen Titeln (Umlaute → ae/oe/ue/ss).</summary>
public static partial class SlugHelper
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var s = input.Trim().ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
            .Replace("ß", "ss").Replace("&", " und ");

        // Akzente entfernen
        s = new string(s.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray()).Normalize(NormalizationForm.FormC);

        s = NonAlnum().Replace(s, "-");      // alles außer a-z0-9 → Bindestrich
        s = MultiDash().Replace(s, "-").Trim('-');
        return s.Length > 200 ? s[..200].Trim('-') : s;
    }

    [GeneratedRegex("[^a-z0-9]+")] private static partial Regex NonAlnum();
    [GeneratedRegex("-{2,}")] private static partial Regex MultiDash();
}
