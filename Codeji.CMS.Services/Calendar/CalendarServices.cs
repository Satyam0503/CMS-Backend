using System.Globalization;
using System.Linq.Expressions;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Calendar.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Codeji.CMS.Utility.middlewares;
using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.Services.Calendar;

public class CalendarServices : ICalendarServices
{
    readonly IMapper _mapper;
    readonly IMongoDbRepository<CalendarEntity> _calendarRepository;
    readonly IMongoDbRepository<EmpUser> _employeeRepository;
    readonly IHttpContextAccessor _context;
    readonly ILogger<CalendarServices> _logger;
    string CompanyId => CurrentContext.CompanyId(_context);
    public CalendarServices(IMapper mapper, IMongoDbRepository<CalendarEntity> calendarRepository, IMongoDbRepository<EmpUser> empRepository, IHttpContextAccessor context, ILogger<CalendarServices> logger)
    {
        _mapper = mapper;
        _calendarRepository = calendarRepository;
        _employeeRepository = empRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CalendarResponseDto>> GetAllCalendarItems(CalendarFilters filter)
    {
        // get all holidays
        Result<CalendarResponseDto> result = new();
        IEnumerable<CalendarResponseDto> calendarItems = [];
        var year = filter.Year ?? DateTime.UtcNow.Year;

        if (filter.Type == null || filter.Type == CalendarResponseItem.Holiday || filter.Type == CalendarResponseItem.Event)
        {
            Expression<Func<CalendarEntity, bool>> expression;
            if (filter.Type == null)
            {
                expression = cl => cl.CompanyId == CompanyId && (cl.Recurring || cl.Date.Year == year);
            }
            else if (filter.Type == CalendarResponseItem.Holiday)
            {
                expression = cl => cl.CompanyId == CompanyId && ((cl.Recurring && cl.Type == CalendarItem.Holiday) || (cl.Date.Year == year && cl.Type == CalendarItem.Holiday));
            }
            else
            {
                expression = cl => cl.CompanyId == CompanyId && ((cl.Recurring && cl.Type == CalendarItem.Event) || (cl.Date.Year == year && cl.Type == CalendarItem.Event));
            }

            calendarItems = (await _calendarRepository.GetAll(expression)).Select(ci =>
            {
                if (ci.Recurring)
                {
                    var date = ci.Date;
                    // Treat recurring holiday dates as calendar business dates.
                    // Do not carry over time components or timezone offsets when
                    // re-projecting them into the requested year.
                    ci.Date = new DateTime(year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
                }
                return new CalendarResponseDto
                {
                    Id = ci.Id,
                    Name = ci.Name,
                    Date = ci.Date,
                    Description = ci.Description,
                    Type = (CalendarResponseItem)ci.Type,
                    ImageUrl = Common.GetCalendarItemCoverImagePath(ci.ImageUrl),
                    Recurring = ci.Recurring
                };
            });
        }

        // get employee birthdays and work anniversaries
        var today = IndiaTime.Today;
        string dateFormat = "yyyy-MM-dd";
        var employeesList = await _employeeRepository.GetAll(e => e.CompanyId == CompanyId && (e.DateOfBirth != null || e.DateOfJoining != null));
        if (employeesList == null || !employeesList.Any())
        {
            result.MethodResults = calendarItems.ToList() ?? [];
            return result;
        }
        IEnumerable<CalendarResponseDto?> birthdaysThisYear = [];
        if (filter.Type == CalendarResponseItem.Birthday || filter.Type == null)
        {
            birthdaysThisYear = employeesList.Select(emp =>
            {
                if (!DateTime.TryParseExact(emp.DateOfBirth, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dob))
                    return null; // Skip if invalid format

                var thisYearBirthday = new DateTime(year, dob.Month, dob.Day);

                return new CalendarResponseDto
                {
                    Id = emp.UserId,
                    Name = $"{emp.FirstName} {emp.LastName}",
                    ImageUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                    Description = $"{emp.FirstName} {emp.LastName}'s Birthday",
                    Date = thisYearBirthday,
                    Type = EnumsHelper.CalendarResponseItem.Birthday
                };
            }).Where(x => x != null);
        }

        // get work anniversaries the year
        IEnumerable<CalendarResponseDto?> workAnniversariesThisYear = [];
        if (filter.Type == CalendarResponseItem.WorkAnniversary || filter.Type == null)
        {
            workAnniversariesThisYear = employeesList.Select(emp =>
            {
                if (!DateTime.TryParseExact(emp.DateOfJoining, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime doj))
                    return null;
                var thisYearAnniversary = new DateTime(year, doj.Month, doj.Day);
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
        }
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
                CompanyId = CompanyId,
                Name = model.Name,
                Date = DateTime.SpecifyKind(model.Date.Date, DateTimeKind.Unspecified),
                Description = model.Description,
                Type = model.Type,
                Recurring = model.Recurring,
                ImageUrl = model.Image == null ? null : await AddUpdateCalenderItemImage(model.Image)
            };
            result = await _calendarRepository.AddOne(newItem);
        }
        else
        {
            Expression<Func<CalendarEntity, bool>> whereCondition = c => c.Id == model.Id && c.CompanyId == CompanyId;
            var existingItem = await _calendarRepository.FirstOrDefault(ci => ci.CompanyId == CompanyId && ci.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));
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
            existingCalendarItem.Date = DateTime.SpecifyKind(model.Date.Date, DateTimeKind.Unspecified);
            existingCalendarItem.Type = model.Type;
            existingCalendarItem.Description = model.Description;
            existingCalendarItem.Recurring = model.Recurring;
            result = await _calendarRepository.Update(whereCondition, existingCalendarItem);
        }
        return result;
    }

    public async Task<Result> ImportCalendarItems(CalendarBulkImportRequest model)
    {
        if (model.Items.Count is < 1 or > CalendarBulkImportRequest.MaxItems)
        {
            _logger?.LogWarning("ImportCalendarItems rejected due to row count {Count}", model.Items.Count);
            return new Result { Success = false, Message = "CALENDAR_IMPORT_ROW_LIMIT: Import 1 to 500 rows." };
        }
        var normalized = model.Items.Select(x => new CalendarBulkImportItem
        {
            Name = x.Name?.Trim() ?? string.Empty,
            Date = DateTime.SpecifyKind(x.Date.Date, DateTimeKind.Unspecified),
            Type = x.Type
        }).ToList();
        if (normalized.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Date.Year is < 2000 or > 2100 || x.Type is not (CalendarItem.Holiday or CalendarItem.Event)))
        {
            _logger?.LogWarning("ImportCalendarItems contained invalid rows. Normalized count={Count}", normalized.Count);
            return new Result { Success = false, Message = "CALENDAR_IMPORT_INVALID_ROW: Name, date, and Holiday or Event type are required." };
        }
        var duplicateRows = normalized.GroupBy(x => $"{x.Type}|{x.Date:yyyy-MM-dd}|{x.Name}", StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1);
        if (duplicateRows)
        {
            _logger?.LogWarning("ImportCalendarItems detected duplicate rows in payload");
            return new Result { Success = false, Message = "CALENDAR_IMPORT_DUPLICATE_ROW: The file contains duplicate type, date, and name rows." };
        }
        var existing = (await _calendarRepository.GetAll(x => x.CompanyId == CompanyId && (x.Type == CalendarItem.Holiday || x.Type == CalendarItem.Event))).ToList();
        _logger?.LogInformation("ImportCalendarItems: existingCount={ExistingCount}, incoming={IncomingCount}", existing.Count, normalized.Count);
        var additions = normalized.Where(item => !existing.Any(x => x.Type == item.Type && x.Date.Date == item.Date && string.Equals(x.Name, item.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(item => new CalendarEntity { CompanyId = CompanyId, Name = item.Name, Date = item.Date, Type = item.Type, Description = string.Empty, Recurring = false }).ToList();
        if (additions.Count == 0) return new Result { Success = true, Message = "CALENDAR_IMPORT_NO_NEW_ITEMS" };
        var saved = await _calendarRepository.AddMany(additions);
        if (saved.Success)
        {
            _logger?.LogInformation("ImportCalendarItems: added {Added} new items", additions.Count);
            return new Result { Success = true, Message = $"CALENDAR_IMPORT_SUCCESS: Added {additions.Count} item(s); existing matching items were skipped." };
        }
        _logger?.LogError("ImportCalendarItems failed to save additions: {Message}", saved.Message);
        return saved;
    }

    public async Task<Result> DeleteItem(string itemId)
    {
        Result result = new();
        Expression<Func<CalendarEntity, bool>> whereCondition = ci => ci.Id == itemId && ci.CompanyId == CompanyId;
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
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "CalendarItemCoverPictures");

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
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "CalendarItemCoverPictures");
        string oldPath = Path.Combine(uploadFolder, imageName);
        FileInfo fileInfo = new(oldPath);
        fileInfo.Delete();
    }

    public async Task<Result<HolidayResponseDto>> GetHolidays(HolidayFilter filter)
    {
        Result<HolidayResponseDto> result = new();

        var allHolidays = await _calendarRepository.GetAll(cl => cl.CompanyId == CompanyId && cl.Type == EnumsHelper.CalendarItem.Holiday);

        List<HolidayResponseDto> holidaysInRange = new();

        // Use date-only comparison to avoid timezone-induced shifts. Treat stored
        // calendar dates as business dates (date-only) regardless of Kind.
        var rangeStart = DateOnly.FromDateTime(filter.FromDate);
        var rangeEnd = DateOnly.FromDateTime(filter.ToDate);

        foreach (var holiday in allHolidays)
        {
            var holidayDateOnly = DateOnly.FromDateTime(holiday.Date);
            if (holiday.Recurring)
            {
                // Generate the holiday date for each year in the range using the
                // business day (month/day) from the stored holiday.
                for (int year = rangeStart.Year; year <= rangeEnd.Year; year++)
                {
                    var recurringDateOnly = new DateOnly(year, holidayDateOnly.Month, holidayDateOnly.Day);
                    if (recurringDateOnly >= rangeStart && recurringDateOnly <= rangeEnd)
                    {
                        holidaysInRange.Add(new HolidayResponseDto
                        {
                            Id = holiday.Id,
                            Name = holiday.Name,
                            Date = recurringDateOnly.ToDateTime(TimeOnly.MinValue),
                            Description = holiday.Description
                        });
                    }
                }
            }
            else
            {
                if (holidayDateOnly >= rangeStart && holidayDateOnly <= rangeEnd)
                {
                    holidaysInRange.Add(new HolidayResponseDto
                    {
                        Id = holiday.Id,
                        Name = holiday.Name,
                        Date = holiday.Date.Date,
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
