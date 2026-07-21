using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

public interface IWeeklyOffService
{
    Task<WeeklyOffSettingDto> Get(string companyId);
    Task<Result> Save(string companyId, string userId, WeeklyOffSettingDto dto);
}

public class WeeklyOffService(IMongoDbRepository<WeeklyOffSetting> repository) : IWeeklyOffService
{
    public async Task<WeeklyOffSettingDto> Get(string companyId)
    {
        var setting = await repository.FirstOrDefault(x => x.CompanyId == companyId);
        return new WeeklyOffSettingDto { OffDays = setting?.OffDays ?? [(int)DayOfWeek.Saturday, (int)DayOfWeek.Sunday] };
    }

    public async Task<Result> Save(string companyId, string userId, WeeklyOffSettingDto dto)
    {
        var days = dto.OffDays.Distinct().OrderBy(x => x).ToList();
        if (days.Count == 0 || days.Any(x => x < 0 || x > 6)) return new Result { Success = false, Message = "Select at least one valid weekday." };
        var setting = await repository.FirstOrDefault(x => x.CompanyId == companyId);
        if (setting == null) return await repository.AddOne(new WeeklyOffSetting { CompanyId = companyId, OffDays = days, CreatedBy = userId });
        setting.OffDays = days; setting.UpdatedBy = userId; setting.UpdatedDate = DateTime.UtcNow;
        return await repository.Update(Builders<WeeklyOffSetting>.Filter.Eq(x => x.Id, setting.Id), setting);
    }
}
