using Codeji.CMS.Domain.Models;
using Codeji.CMS.GenericRepository.Interfaces;
using MongoDB.Driver;
using System.Linq.Expressions;

public interface IAttendanceStatusService
{
    Task<List<AttendanceStatusSettingDto>> Get(string companyId, bool activeOnly);
    Task EnsureCompanyDefaults(string companyId);
    Task<Result> Save(string companyId, AttendanceStatusSettingDto dto);
    Task<Result> Delete(string companyId, string statusId);
}

public class AttendanceStatusService : IAttendanceStatusService
{
    private readonly IMongoDbRepository<AttendanceStatusSetting> _repository;
    private static readonly (string Code, string Name, bool Time, decimal Paid, decimal Unpaid, bool LeaveEligible, string Color)[] Defaults =
    [
        ("P","Present",true,1,0,false,"#2E7D32"), ("A","Absent",false,0,1,false,"#D32F2F"), ("SL","Sick Leave",false,1,0,true,"#7B1FA2"),
        ("CL","Casual Leave",false,1,0,true,"#1565C0"), ("EL","Earned Leave",false,1,0,true,"#00838F"), ("WFH","Work From Home",true,1,0,false,"#0277BD"),
        ("HD","Half Day",false,.5m,.5m,true,"#EF6C00"), ("ED","Early Departure",true,1,0,false,"#6D4C41"), ("LHD","Late Arrival-Half Day",true,.5m,.5m,false,"#F57C00"),
        ("LHD+ED","Late Arrival-Half Day + Early Departure",true,.5m,.5m,false,"#6D4C41"),
        ("WFH+WFO","Half WFH and Half WFO",true,1,0,false,"#00838F"), ("COMP-OFF","Compensatory Off",false,1,0,true,"#388E3C"),
        ("CL-HALF","Casual Leave (Half Day)",false,.5m,0,true,"#5C6BC0"), ("SL-HALF","Sick Leave (Half Day)",false,.5m,0,true,"#8E24AA"),
        ("WFH-HD","WFH with Half Day",true,.5m,.5m,false,"#039BE5"), ("UL","Unpaid Leave",false,0,1,true,"#C62828")
    ];
    public AttendanceStatusService(IMongoDbRepository<AttendanceStatusSetting> repository) => _repository = repository;

    public async Task<List<AttendanceStatusSettingDto>> Get(string companyId, bool activeOnly)
    {
        await EnsureCompanyDefaults(companyId);
        var items = (await _repository.GetAll(x => x.CompanyId == companyId)).ToList();
        return items.Where(x => !activeOnly || x.IsActive).OrderBy(x => x.SortOrder).Select(x => new AttendanceStatusSettingDto { Id=x.Id, Code=x.Code, Name=x.Name, IsActive=x.IsActive, IsSystem=x.IsSystem, SortOrder=x.SortOrder, RequiresTime=x.RequiresTime, IsAvailableForLeaveManagement=x.IsAvailableForLeaveManagement, ColorHex=string.IsNullOrWhiteSpace(x.ColorHex) ? "#607D8B" : x.ColorHex, PaidDayFraction=x.PaidDayFraction, UnpaidDayFraction=x.UnpaidDayFraction }).ToList();
    }

    public async Task EnsureCompanyDefaults(string companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId)) throw new ArgumentException("Company id is required.", nameof(companyId));
        var existingStatuses = (await _repository.GetAll(x => x.CompanyId == companyId)).ToList();
        if (existingStatuses.Count > 0)
        {
            // UL-HALF was a legacy duplicate of HD. Unpaid Leave now uses UL for a
            // full day and HD for a half day, so it must never be offered or retained
            // as a separate company attendance status.
            foreach (var legacyUnpaidHalf in existingStatuses.Where(x =>
                         string.Equals(x.Code, "UL-HALF", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                await _repository.Delete(Builders<AttendanceStatusSetting>.Filter
                    .Where(x => x.CompanyId == companyId && x.Id == legacyUnpaidHalf.Id && x.Code == legacyUnpaidHalf.Code));
                existingStatuses.Remove(legacyUnpaidHalf);
            }

            var existingCodes = existingStatuses
                .Select(x => x.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingDefaults = Defaults
                .Where(defaultStatus => !existingCodes.Contains(defaultStatus.Code))
                .Select(defaultStatus => new AttendanceStatusSetting
                {
                    CompanyId = companyId,
                    Code = defaultStatus.Code,
                    Name = defaultStatus.Name,
                    IsSystem = true,
                    SortOrder = Defaults.ToList().FindIndex(x => x.Code == defaultStatus.Code),
                    RequiresTime = defaultStatus.Time,
                    IsAvailableForLeaveManagement = defaultStatus.LeaveEligible,
                    ColorHex = defaultStatus.Color,
                    PaidDayFraction = defaultStatus.Paid,
                    UnpaidDayFraction = defaultStatus.Unpaid
                })
                .ToList();
            if (missingDefaults.Count > 0) await _repository.AddMany(missingDefaults);

            // Earlier releases allowed LHD+ED to be created as a custom status. It is now
            // a protected timed default because automatic attendance classification depends on it.
            var combinedStatus = existingStatuses.FirstOrDefault(x =>
                string.Equals(x.Code, "LHD+ED", StringComparison.OrdinalIgnoreCase));
            if (combinedStatus is not null &&
                (!combinedStatus.IsSystem || !combinedStatus.RequiresTime || combinedStatus.IsAvailableForLeaveManagement ||
                 combinedStatus.PaidDayFraction != .5m || combinedStatus.UnpaidDayFraction != .5m))
            {
                combinedStatus.IsSystem = true;
                combinedStatus.RequiresTime = true;
                combinedStatus.IsAvailableForLeaveManagement = false;
                combinedStatus.PaidDayFraction = .5m;
                combinedStatus.UnpaidDayFraction = .5m;
                await _repository.Update(
                    Builders<AttendanceStatusSetting>.Filter.Where(x => x.Id == combinedStatus.Id && x.CompanyId == companyId),
                    combinedStatus);
            }

            // HD is the shared half-day leave status. Upgrade only the system
            // default so it can be mapped by Unpaid Leave without requiring
            // irrelevant clocking times.
            var halfDay = await _repository.FirstOrDefault(x => x.CompanyId == companyId && x.Code == "HD" && x.IsSystem);
            if (halfDay is not null && (halfDay.RequiresTime || !halfDay.IsAvailableForLeaveManagement))
            {
                halfDay.RequiresTime = false;
                halfDay.IsAvailableForLeaveManagement = true;
                var halfDayFilter = Builders<AttendanceStatusSetting>.Filter
                    .Where(x => x.Id == halfDay.Id && x.CompanyId == companyId);
                await _repository.Update(halfDayFilter, halfDay);
            }
            return;
        }

        var defaults = Defaults.Select((d, i) => new AttendanceStatusSetting
        {
            CompanyId = companyId, Code = d.Code, Name = d.Name, IsSystem = true,
            SortOrder = i, RequiresTime = d.Time, IsAvailableForLeaveManagement = d.LeaveEligible,
            ColorHex = d.Color, PaidDayFraction = d.Paid, UnpaidDayFraction = d.Unpaid
        }).ToList();
        await _repository.AddMany(defaults);
    }

    public async Task<Result> Save(string companyId, AttendanceStatusSettingDto dto)
    {
        if (dto.PaidDayFraction < 0 || dto.PaidDayFraction > 1 || dto.UnpaidDayFraction < 0 || dto.UnpaidDayFraction > 1 || dto.PaidDayFraction + dto.UnpaidDayFraction > 1)
            return new Result { Success=false, Message="Paid and unpaid fractions must each be between 0 and 1 and their sum cannot exceed 1." };
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return new Result { Success=false, Message="Attendance status code and name are required." };
        if (string.IsNullOrWhiteSpace(dto.ColorHex) || !System.Text.RegularExpressions.Regex.IsMatch(dto.ColorHex, "^#[0-9A-Fa-f]{6}$"))
            return new Result { Success=false, Message="Attendance status color must be a six-digit hexadecimal value." };
        if (dto.IsAvailableForLeaveManagement && dto.RequiresTime)
            return new Result { Success=false, Message="A status that requires clock-in/out cannot be available for leave policies." };
        dto.Code = dto.Code.Trim().ToUpperInvariant();
        var existing = await _repository.FirstOrDefault(x => x.CompanyId == companyId && (x.Id == dto.Id || x.Code == dto.Code));
        if (existing == null)
            return await _repository.AddOne(new AttendanceStatusSetting { CompanyId=companyId, Code=dto.Code, Name=dto.Name.Trim(), IsActive=dto.IsActive, SortOrder=dto.SortOrder, RequiresTime=dto.RequiresTime, IsAvailableForLeaveManagement=dto.IsAvailableForLeaveManagement, ColorHex=dto.ColorHex.ToUpperInvariant(), PaidDayFraction=dto.PaidDayFraction, UnpaidDayFraction=dto.UnpaidDayFraction });
        existing.Name=dto.Name.Trim(); existing.IsActive=dto.IsActive; existing.SortOrder=dto.SortOrder; existing.RequiresTime=dto.RequiresTime; existing.IsAvailableForLeaveManagement=dto.IsAvailableForLeaveManagement; existing.ColorHex=dto.ColorHex.ToUpperInvariant(); existing.PaidDayFraction=dto.PaidDayFraction; existing.UnpaidDayFraction=dto.UnpaidDayFraction;
        Expression<Func<AttendanceStatusSetting, bool>> filter = x => x.Id == existing.Id && x.CompanyId == companyId;
        return await _repository.Update(filter, existing);
    }

    public async Task<Result> Delete(string companyId, string statusId)
    {
        if (string.IsNullOrWhiteSpace(statusId))
            return new Result { Success = false, Message = "Attendance status id is required." };

        var status = await _repository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == statusId);
        if (status is null)
            return new Result { Success = false, Message = "The attendance status was not found in this company." };
        if (status.IsSystem)
            return new Result { Success = false, Message = "Default attendance statuses cannot be deleted." };

        return await _repository.Delete(Builders<AttendanceStatusSetting>.Filter
            .Where(x => x.CompanyId == companyId && x.Id == statusId && !x.IsSystem));
    }
}
