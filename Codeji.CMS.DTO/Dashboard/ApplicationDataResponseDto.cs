using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Dashboard;

public class ApplicationDataResponseDto
{
    public int TotalApplications { get; set; }
    public List<ApplicationStatusTypeData> ApplicationStatusData { get; set; } = [];
}

public class ApplicationStatusTypeData
{
    public ActivityType ActivityType { get; set; }
    public int Count { get; set; }
}