using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Holiday;
using Codeji.CMS.Repository.Entities.Holidays;

namespace Codeji.CMS.Services.Holiday.Interface;

public interface IHolidayService
{
    Task<Result> CreateEditHoliday(HolidayRequestDto model);
    Task<Result<HolidayResponseDto>> GetAllHoliday(HolidayFilter? filter);
    Task<Result> DeleteHoliday(string holidayId);
    // Task<Result> EditHoliday(string id, HolidayRequestDto holidayRequestDto);
}
