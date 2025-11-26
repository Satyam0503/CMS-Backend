using System.Globalization;
using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Calendar;

public class CalendarServices : ICalendarServices
{
    readonly IMapper _mapper;
    readonly IMongoDbRepository<CalendarEntity> _calendarRepository;
    readonly IMongoDbRepository<EmpUser> _employeeRepository;
    public CalendarServices(IMapper mapper, IMongoDbRepository<CalendarEntity> calendarRepository, IMongoDbRepository<EmpUser> empRepository)
    {
        _mapper = mapper;
        _calendarRepository = calendarRepository;
        _employeeRepository = empRepository;
    }

    public async Task<Result<CalendarResponseDto>> GetAllCalendarItems(CalendarFilters filter)
    {
        // get all holidays
        Result<CalendarResponseDto> result = new();
        IEnumerable<CalendarResponseDto> calendarItems = [];
        var year = filter.Year ?? DateTime.UtcNow.Year;
        Expression<Func<CalendarEntity, bool>> expression = cl => cl.Recurring || (filter.Type == null || cl.Type == filter.Type) && cl.Date.Year == year;
        calendarItems = (await _calendarRepository.GetAll(expression)).Select(ci =>
        {
            if (ci.Recurring)
            {
                var date = ci.Date;
                ci.Date = new DateTime(year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
            }
            return new CalendarResponseDto
            {
                Id = ci.Id,
                Name = ci.Name,
                Date = ci.Date,
                Description = ci.Description,
                Type = (EnumsHelper.CalendarResponseItem)ci.Type,
                ImageUrl = Common.GetCalendarItemCoverImagePath(ci.ImageUrl),
                Recurring = ci.Recurring
            };
        });

        // get employee birthdays and work anniversaries
        var today = DateTime.Today;
        string dateFormat = "yyyy-MM-dd";
        var employeesList = await _employeeRepository.GetAll(e => e.DateOfBirth != null || e.DateOfJoining != null);
        if (employeesList == null || !employeesList.Any())
        {
            result.MethodResults = calendarItems.ToList() ?? [];
            return result;
        }
        var birthdaysThisYear = employeesList.Select(emp =>
        {
            if (!DateTime.TryParseExact(emp.DateOfBirth, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dob))
                return null; // Skip if invalid format

            var thisYearBirthday = new DateTime(today.Year, dob.Month, dob.Day);

            return new CalendarResponseDto
            {
                Id = emp.UserId,
                Name = $"{emp.FirstName} {emp.LastName}",
                ImageUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                Description = $"{emp.FirstName} {emp.LastName}'s Birthday",
                Date = thisYearBirthday,
                Type = EnumsHelper.CalendarResponseItem.Birthday
            };
        })
        .Where(x => x != null);

        var workAnniversariesThisYear = employeesList.Select(emp =>
        {
            if (!DateTime.TryParseExact(emp.DateOfJoining, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime doj))
                return null;
            var thisYearAnniversary = new DateTime(today.Year, doj.Month, doj.Day);
            return new CalendarResponseDto
            {
                Id = emp.UserId,
                Name = $"{emp.FirstName} {emp.LastName}",
                ImageUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                Description = $"{emp.FirstName} {emp.LastName}'s Work Anniversary",
                Date = thisYearAnniversary,
                Type = EnumsHelper.CalendarResponseItem.WorkAnniversary
            };
        })
        .Where(x => x != null);
        result.MethodResults = calendarItems.Concat(birthdaysThisYear).Concat(workAnniversariesThisYear).ToList();
        return result;
    }

    public async Task<Result> AddUpdateCalendarItem(CalendarRequestDto model)
    {
        Result result = new();
        if (string.IsNullOrEmpty(model.Id))
        {
            // var existingItem = await _calendarRepository.FirstOrDefault(ci => ci.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase) && ci.Type == model.Type);
            // if (existingItem != null)
            // {
            //     result.Success = false;
            //     return result;
            // }

            var newItem = new CalendarEntity
            {
                Name = model.Name,
                Date = model.Date,
                Description = model.Description,
                Type = model.Type,
                Recurring = model.Recurring,
                ImageUrl = model.Image == null ? null : await AddUpdateCalenderItemImage(model.Image)
            };
            result = await _calendarRepository.AddOne(newItem);
        }
        else
        {
            Expression<Func<CalendarEntity, bool>> whereCondition = c => c.Id == model.Id;
            var existingItem = await _calendarRepository.FirstOrDefault(ci => ci.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));
            var existingCalendarItem = await _calendarRepository.FirstOrDefault(whereCondition);
            if (existingCalendarItem == null)
            {
                result.Success = false;
                return result;
            }
            if (existingItem != null && existingItem.Name == model.Name && existingItem.Id != model.Id)
            {
                result.Success = false;
                return result;
            }
            if (model.Image != null)
            {
                existingCalendarItem.ImageUrl = await AddUpdateCalenderItemImage(model.Image, existingCalendarItem.ImageUrl);
            }
            else
            {
                if (existingCalendarItem.ImageUrl != null)
                {
                    DeleteExistingCoverImage(existingCalendarItem.ImageUrl);
                    existingCalendarItem.ImageUrl = null;
                }
            }
            existingCalendarItem.Name = model.Name;
            existingCalendarItem.Date = model.Date;
            existingCalendarItem.Type = model.Type;
            existingCalendarItem.Description = model.Description;
            existingCalendarItem.Recurring = model.Recurring;
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

    public async Task<Result<HolidayResponseDto>> GetHolidays(HolidayFilter filter)
    {
        Result<HolidayResponseDto> result = new();

        var allHolidays = await _calendarRepository.GetAll(cl => cl.Type == EnumsHelper.CalendarItem.Holiday);

        List<HolidayResponseDto> holidaysInRange = new();

        foreach (var holiday in allHolidays)
        {
            if (holiday.Recurring)
            {
                // Generate the holiday date for each year in the range
                for (int year = filter.FromDate.Year; year <= filter.ToDate.Year; year++)
                {
                    var recurringDate = new DateTime(year, holiday.Date.Month, holiday.Date.Day, holiday.Date.Hour, holiday.Date.Minute, holiday.Date.Second, holiday.Date.Kind);

                    if (recurringDate >= filter.FromDate && recurringDate <= filter.ToDate)
                    {
                        holidaysInRange.Add(new HolidayResponseDto
                        {
                            Id = holiday.Id,
                            Name = holiday.Name,
                            Date = recurringDate,
                            Description = holiday.Description
                        });
                    }
                }
            }
            else
            {
                if (holiday.Date >= filter.FromDate && holiday.Date <= filter.ToDate)
                {
                    holidaysInRange.Add(new HolidayResponseDto
                    {
                        Id = holiday.Id,
                        Name = holiday.Name,
                        Date = holiday.Date,
                        Description = holiday.Description
                    });
                }
            }
        }

        result.MethodResults = holidaysInRange;
        result.Success = true;
        result.TotalRecords = holidaysInRange.Count;
        return result;
    }
}