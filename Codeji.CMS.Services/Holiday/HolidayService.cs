using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Holiday;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Holidays;
using Codeji.CMS.Services.Holiday.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using ZstdSharp.Unsafe;

namespace Codeji.CMS.Services.Holiday;

public class HolidayService : IHolidayService
{

    private readonly IMongoDbRepository<Holidays> _holidaysRepo;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMongoDbRepository<EmpUser> _empUserRepo;


    public HolidayService(IMongoDbRepository<Holidays> holidaysRepo,
        IMongoDbRepository<EmpUser> _empUserRepo,
        IHttpContextAccessor httpContextAccessor)
    {
        _holidaysRepo = holidaysRepo;
        this.httpContextAccessor = httpContextAccessor;
        this._empUserRepo = _empUserRepo;
    }


    public async Task<Result> CreateEditHoliday(HolidayResponseDto holidayResponseDto)
    {
        Result result = new Result();
        var userId = CurrentContext.UserId(httpContextAccessor);
        if (string.IsNullOrEmpty(holidayResponseDto.HolidayId))
        {
            var existingHoliday = await _holidaysRepo.FirstOrDefault(h => h.HolidayName.Equals(holidayResponseDto.HolidayName, StringComparison.OrdinalIgnoreCase));
            if (existingHoliday != null)
            {
                result.Success = false;
                return result;
            }

            var newHoliday = new Holidays
            {
                HolidayName = holidayResponseDto.HolidayName,
                Date = holidayResponseDto.Date,
                Detail = holidayResponseDto.Detail,
                HolidayType = holidayResponseDto.HolidayType,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = userId
            };
            result = await _holidaysRepo.AddOne(newHoliday);
        }
        else
        {
            var existingHolidayName = await _holidaysRepo.FirstOrDefault(h => h.HolidayName.Equals(holidayResponseDto.HolidayName, StringComparison.OrdinalIgnoreCase));
            var existingHoliday = await _holidaysRepo.FirstOrDefault(h => h.HolidayId == holidayResponseDto.HolidayId);
            if (existingHoliday == null)
            {
                result.Success = false;
                return result;
            }
            if (existingHolidayName != null && existingHolidayName.HolidayName == holidayResponseDto.HolidayName && existingHolidayName.HolidayId != holidayResponseDto.HolidayId)
            {
                result.Success = false;
                return result;

            }
            Expression<Func<Holidays, bool>> whereCondition = h => h.HolidayId == holidayResponseDto.HolidayId;
            existingHoliday.HolidayName = holidayResponseDto.HolidayName;
            existingHoliday.Date = holidayResponseDto.Date;
            // existingHoliday.UpdatedBy = userId;
            existingHoliday.HolidayType = holidayResponseDto.HolidayType;
            existingHoliday.Detail = holidayResponseDto.Detail;
            existingHoliday.UpdatedDate = DateTime.UtcNow;
            result = await _holidaysRepo.Update(whereCondition, existingHoliday);
        }
        return result;
    }

    public async Task<Result<HolidayRequestDto>> GetAllHoliday(HolidayFilter? filter)
    {
        IEnumerable<Holidays> holidayList = [];
        if (filter is null)
        {
            holidayList = await _holidaysRepo.GetAll();
        }
        else
        {
            Expression<Func<Holidays, bool>> whereCondition = h =>
                 (string.IsNullOrEmpty(filter.HolidayName) || h.HolidayName.ToLower().Contains(filter.HolidayName.ToLower())) &&
                 (filter.HolidayType == null || !filter.HolidayType.Any() || filter.HolidayType.Contains(h.HolidayType))
                 && (filter.Date == null || !filter.Date.HasValue || (h.Date >= filter.Date));
            holidayList = (await _holidaysRepo.GetAggregateDataAsync<Holidays>(whereCondition, pageNo: filter.PageNo, pageSize: filter.PageSize, isAscending: true, orderedKey: "Date")).ToList();
        }

        List<string> usersId = holidayList.Select(h => h.CreatedBy).ToList();

        List<EmpUser> users = (await _empUserRepo.GetAll(u => usersId.Contains(u.UserId))).ToList();
        List<HolidayRequestDto> holidayData = (from holiday in holidayList
                                               join user in users on holiday.CreatedBy equals user.UserId
                                               select new HolidayRequestDto
                                               {
                                                   HolidayId = holiday.HolidayId,
                                                   HolidayName = holiday.HolidayName,
                                                   Date = holiday.Date,
                                                   Detail = holiday.Detail,
                                                   HolidayType = holiday.HolidayType,
                                                   CreatedAt = holiday.CreatedDate,
                                                   CreatedBy = user.FirstName + " " + user.LastName
                                               }
                                            ).ToList();
        var list = await _holidaysRepo.GetAll();
        var count = list.Count();
        return new Result<HolidayRequestDto>
        {
            Success = true,
            TotalRecords = count,
            MethodResults = holidayData
        };


    }

    public async Task<Result> DeleteHoliday(string holidayId)
    {
        Result result = new();
        Expression<Func<Holidays, bool>> whereCondition = h => h.HolidayId == holidayId;
        // var existingHoliday = await _holidaysRepo.FirstOrDefault(whereCondition);
        // existingHoliday.IsDeleted = true;
        // result = await _holidaysRepo.Update(whereCondition,existingHoliday);
        result = await _holidaysRepo.Delete(whereCondition);
        return result;
    }

    // public async Task<Result> EditHoliday( HolidayResponseDto holidayResponseDto)
    // {
    //     Result result = new();
    //     var existingHoliday = await _holidaysRepo.FirstOrDefault(h => h.HolidayId == id);
    //     if (existingHoliday == null)
    //     {
    //         result.Success = false;
    //         return result;
    //     }

    //     existingHoliday.HolidayName = holidayResponseDto.HolidayName;
    //     existingHoliday.Date = holidayResponseDto.Date;
    // }


}
