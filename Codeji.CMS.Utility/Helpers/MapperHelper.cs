namespace Codeji.CMS.Utility.Helpers;

public class MapperHelper
{
    public static string GetEmploymentTypeLabel(int? employmentType)
    {
        if (employmentType == null) return string.Empty;
        var employmentTypes = new Dictionary<int, string>
        {
            { 1, "Full-Time" },
            { 2, "Part-Time" },
            { 3, "Contractual" },
            { 4, "Internship" },
            { 5, "Apprenticeship"}
        };

        return employmentTypes.TryGetValue((int)employmentType, out string? value) ? value : string.Empty;
    }

}