using System.Net;
using Codeji.CMS.Utility;

namespace Codeji.CMS.Utility.Helpers
{
    public class HtmlTemplate
    {
        public static string Render(string htmlTemplate, object values)
        {
            if (string.IsNullOrEmpty(htmlTemplate) || values is null) return htmlTemplate ?? string.Empty;

            var replacements = values.GetType().GetProperties()
                .ToDictionary(
                    property => property.Name,
                    property => WebUtility.HtmlEncode(property.GetValue(values)?.ToString() ?? string.Empty),
                    StringComparer.Ordinal);

            return System.Text.RegularExpressions.Regex.Replace(
                htmlTemplate,
                @"\[(?<name>[A-Za-z][A-Za-z0-9_]*)\]",
                match => replacements.TryGetValue(match.Groups["name"].Value, out var replacement)
                    ? replacement
                    : match.Value);
        }
    }
}
