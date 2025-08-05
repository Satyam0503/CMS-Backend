using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Holiday;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Holidays;
using Codeji.CMS.Services.Holiday.Interface;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Holiday;

public class HolidayService : IHolidayService
{

    private readonly IMongoDbRepository<Holidays> _holidaysRepo;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IMongoDbRepository<EmpUser> _empUserRepo;
    private readonly IMapper _mapper;


    public HolidayService(IMongoDbRepository<Holidays> holidaysRepo,
        IMongoDbRepository<EmpUser> _empUserRepo,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _holidaysRepo = holidaysRepo;
        this.httpContextAccessor = httpContextAccessor;
        this._empUserRepo = _empUserRepo;
        _mapper = mapper;
    }


    public async Task<Result> CreateEditHoliday(HolidayRequestDto model)
    {
        Result result = new();
        var userId = CurrentContext.UserId(httpContextAccessor);
        if (string.IsNullOrEmpty(model.HolidayId))
        {
            var existingHoliday = await _holidaysRepo.FirstOrDefault(h => h.HolidayName.Equals(model.HolidayName, StringComparison.OrdinalIgnoreCase));
            if (existingHoliday != null)
            {
                result.Success = false;
                return result;
            }

            var newHoliday = new Holidays
            {
                HolidayName = model.HolidayName,
                Date = model.Date,
                Detail = model.Detail,
                HolidayType = model.HolidayType,
                HolidayImageUrl = model.HolidayImage == null ? null : await AddUpdateHolidayImage(model.HolidayImage)
            };
            result = await _holidaysRepo.AddOne(newHoliday);
        }
        else
        {
            Expression<Func<Holidays, bool>> whereCondition = h => h.HolidayId == model.HolidayId;
            var existingHolidayName = await _holidaysRepo.FirstOrDefault(h => h.HolidayName.Equals(model.HolidayName, StringComparison.OrdinalIgnoreCase));
            var existingHoliday = await _holidaysRepo.FirstOrDefault(whereCondition);
            if (existingHoliday == null)
            {
                result.Success = false;
                return result;
            }
            if (existingHolidayName != null && existingHolidayName.HolidayName == model.HolidayName && existingHolidayName.HolidayId != model.HolidayId)
            {
                result.Success = false;
                return result;
            }
            if (model.HolidayImage != null)
            {
                existingHoliday.HolidayImageUrl = await AddUpdateHolidayImage(model.HolidayImage, existingHoliday.HolidayImageUrl);
            }
            else
            {
                if (existingHoliday.HolidayImageUrl != null)
                {
                    DeleteExistingCoverImage(existingHoliday.HolidayImageUrl);
                }
            }
            existingHoliday.HolidayName = model.HolidayName;
            existingHoliday.Date = model.Date;
            existingHoliday.HolidayType = model.HolidayType;
            existingHoliday.Detail = model.Detail;
            result = await _holidaysRepo.Update(whereCondition, existingHoliday);
        }
        return result;
    }

    public async Task<Result<HolidayResponseDto>> GetAllHoliday(HolidayFilter? filter)
    {
        Result<HolidayResponseDto> result = new();
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
                 && (filter.Date == null ? h.Date.Year == DateTime.UtcNow.Year : !filter.Date.HasValue || (h.Date >= filter.Date));
            holidayList = (await _holidaysRepo.GetAll(whereCondition)).ToList();
        }
        if (!holidayList.Any()) return result;
        var data = _mapper.Map<List<HolidayResponseDto>>(holidayList);
        result.TotalRecords = data.Count;
        result.MethodResults = data;
        return result;
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

    public static async Task<string> AddUpdateHolidayImage(IFormFile image, string? existingImageName = null)
    {
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\HolidayCoverPictures\\");

        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }
        if (existingImageName != null)
        {
            DeleteExistingCoverImage(existingImageName);
        }
        string fileExtension = Path.GetExtension(image.FileName);
        string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
        string filePath = Path.Combine(uploadFolder, fileName);
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(fileStream);
        }
        ;
        return fileName;
    }

    public static void DeleteExistingCoverImage(string imageName)
    {
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\HolidayCoverPictures\\");
        string oldPath = Path.Combine(uploadFolder, imageName);
        FileInfo fileInfo = new(oldPath);
        fileInfo.Delete();
    }
}
