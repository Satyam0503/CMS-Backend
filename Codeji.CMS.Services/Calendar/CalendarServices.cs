using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Calendar.Interface;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Calendar;

public class CalendarServices : ICalendarServices
{
    readonly IMapper _mapper;
    readonly IMongoDbRepository<CalendarEntity> _calendarRepository;
    readonly IMongoDbRepository<EmpUser> _empRepository;
    public CalendarServices(IMapper mapper, IMongoDbRepository<CalendarEntity> calendarRepository, IMongoDbRepository<EmpUser> empRepository)
    {
        _mapper = mapper;
        _calendarRepository = calendarRepository;
        _empRepository = empRepository;
    }

    public Task<Result<CalendarResponseDto>> GetAllCalendarItems(CalendarFilters? filter)
    {

        // write logic to get all holidays
        throw new NotImplementedException();
    }

    public async Task<Result> AddUpdateCalendarItem(CalendarRequestDto model)
    {
        Result result = new();
        if (string.IsNullOrEmpty(model.OccasionId))
        {
            var existingItem = await _calendarRepository.FirstOrDefault(ci => ci.Name.Equals(model.OccasionName, StringComparison.OrdinalIgnoreCase) && ci.Type == model.OccasionType);
            if (existingItem != null)
            {
                result.Success = false;
                return result;
            }

            var newItem = new CalendarEntity
            {
                Name = model.OccasionName,
                Date = model.Date,
                Description = model.Detail,
                Type = model.OccasionType,
                Recurring = model.Recurring,
                ImageUrl = model.OccasionImage == null ? null : await AddUpdateCalenderItemImage(model.OccasionImage)
            };
            result = await _calendarRepository.AddOne(newItem);
        }
        else
        {
            Expression<Func<CalendarEntity, bool>> whereCondition = c => c.Id == model.OccasionId;
            var existingItem = await _calendarRepository.FirstOrDefault(ci => ci.Name.Equals(model.OccasionName, StringComparison.OrdinalIgnoreCase));
            var existingCalendarItem = await _calendarRepository.FirstOrDefault(whereCondition);
            if (existingCalendarItem == null)
            {
                result.Success = false;
                return result;
            }
            if (existingItem != null && existingItem.Name == model.OccasionName && existingItem.Id != model.OccasionId)
            {
                result.Success = false;
                return result;
            }
            if (model.OccasionImage != null)
            {
                existingCalendarItem.ImageUrl = await AddUpdateCalenderItemImage(model.OccasionImage, existingCalendarItem.ImageUrl);
            }
            else
            {
                if (existingCalendarItem.ImageUrl != null)
                {
                    DeleteExistingCoverImage(existingCalendarItem.ImageUrl);
                    existingCalendarItem.ImageUrl = null;
                }
            }
            existingCalendarItem.Name = model.OccasionName;
            existingCalendarItem.Date = model.Date;
            existingCalendarItem.Type = model.OccasionType;
            existingCalendarItem.Description = model.Detail;
            result = await _calendarRepository.Update(whereCondition, existingCalendarItem);
        }
        return result;
    }

    public async Task<Result> DeleteItem(string itemId)
    {
        Result result = new();
        Expression<Func<CalendarEntity, bool>> whereCondition = ci => ci.Id == itemId;
        var existingItem = await _calendarRepository.FirstOrDefault(whereCondition);
        if (existingItem is null) return result;
        if (existingItem.ImageUrl != null)
        {
            DeleteExistingCoverImage(existingItem.ImageUrl);
        }
        result = await _calendarRepository.Delete(whereCondition);
        return result;
    }

    public static async Task<string> AddUpdateCalenderItemImage(IFormFile image, string? existingImageName = null)
    {
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\CalendarItemCoverPictures\\");

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
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\CalendarItemCoverPictures\\");
        string oldPath = Path.Combine(uploadFolder, imageName);
        FileInfo fileInfo = new(oldPath);
        fileInfo.Delete();
    }

}
