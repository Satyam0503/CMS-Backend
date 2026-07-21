namespace Codeji.CMS.Repository.Entities.Employees;

public class WeeklyOffSetting : BaseClass
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<int> OffDays { get; set; } = [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday];
}
