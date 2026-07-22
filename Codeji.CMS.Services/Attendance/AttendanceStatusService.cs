using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using System.Linq.Expressions;

public interface IAttendanceStatusService
{
    Task<List<AttendanceStatusSettingDto>> Get(string companyId, bool activeOnly);
    Task<Result> Save(string companyId, AttendanceStatusSettingDto dto);
}

public class AttendanceStatusService : IAttendanceStatusService
{
    private readonly IMongoDbRepository<AttendanceStatusSetting> _repository;
    private static readonly (string Code, string Name, bool Time, decimal Paid, decimal Unpaid)[] Defaults =
    [
        ("P","Present",true,1,0), ("A","Absent",false,0,1), ("SL","Sick Leave",false,1,0),
        ("CL","Casual Leave",false,1,0), ("EL","Earned Leave",false,1,0), ("WFH","Work From Home",true,1,0),
        ("HD","Half Day",true,.5m,.5m), ("ED","Early Departure",true,1,0), ("LHD","Late Arrival-Half Day",true,.5m,.5m),
        ("WFH+WFO","Half WFH and Half WFO",true,1,0), ("COMP-OFF","Compensatory Off",false,1,0),
        ("CL-HALF","Casual Leave (Half Day)",false,.5m,0), ("SL-HALF","Sick Leave (Half Day)",false,.5m,0),
        ("WFH-HD","WFH with Half Day",true,.5m,.5m)
        ,("UL","Unpaid Leave",false,0,1)
    ];
    public AttendanceStatusService(IMongoDbRepository<AttendanceStatusSetting> repository) => _repository = repository;

    public async Task<List<AttendanceStatusSettingDto>> Get(string companyId, bool activeOnly)
    {
        var items = (await _repository.GetAll(x => x.CompanyId == companyId)).ToList();
        if (items.Count == 0)
        {
            for (var i = 0; i < Defaults.Length; i++)
            {
                var d = Defaults[i];
                var setting = new AttendanceStatusSetting { CompanyId=companyId, Code=d.Code, Name=d.Name, IsSystem=true, SortOrder=i, RequiresTime=d.Time, PaidDayFraction=d.Paid, UnpaidDayFraction=d.Unpaid };
                items.Add(setting);
            }
            await _repository.AddMany(items);
        }
        return items.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.SortOrder).Select(x => new AttendanceStatusSettingDto { Id=x.Id, Code=x.Code, Name=x.Name, IsActive=x.IsActive, IsSystem=x.IsSystem, SortOrder=x.SortOrder, RequiresTime=x.RequiresTime, PaidDayFraction=x.PaidDayFraction, UnpaidDayFraction=x.UnpaidDayFraction }).ToList();
    }

    public async Task<Result> Save(string companyId, AttendanceStatusSettingDto dto)
    {
        if (dto.PaidDayFraction < 0 || dto.PaidDayFraction > 1 || dto.UnpaidDayFraction < 0 || dto.UnpaidDayFraction > 1 || dto.PaidDayFraction + dto.UnpaidDayFraction > 1)
            return new Result { Success=false, Message="Paid and unpaid fractions must each be between 0 and 1 and their sum cannot exceed 1." };
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return new Result { Success=false, Message="Attendance status code and name are required." };
        dto.Code = dto.Code.Trim().ToUpperInvariant();
        var existing = await _repository.FirstOrDefault(x => x.CompanyId == companyId && (x.Id == dto.Id || x.Code == dto.Code));
        if (existing == null)
            return await _repository.AddOne(new AttendanceStatusSetting { CompanyId=companyId, Code=dto.Code, Name=dto.Name.Trim(), IsActive=dto.IsActive, SortOrder=dto.SortOrder, RequiresTime=dto.RequiresTime, PaidDayFraction=dto.PaidDayFraction, UnpaidDayFraction=dto.UnpaidDayFraction });
        existing.Name=dto.Name.Trim(); existing.IsActive=dto.IsActive; existing.SortOrder=dto.SortOrder; existing.RequiresTime=dto.RequiresTime; existing.PaidDayFraction=dto.PaidDayFraction; existing.UnpaidDayFraction=dto.UnpaidDayFraction;
        Expression<Func<AttendanceStatusSetting, bool>> filter = x => x.Id == existing.Id && x.CompanyId == companyId;
        return await _repository.Update(filter, existing);
    }
}
