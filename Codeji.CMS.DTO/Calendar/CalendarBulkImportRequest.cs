using System.ComponentModel.DataAnnotations;
using Codeji.CMS.Utility.Enums;

namespace Codeji.CMS.DTO.Calendar;

public sealed class CalendarBulkImportRequest
{
    public const int MaxItems = 500;
    [Required, MinLength(1), MaxLength(MaxItems)] public List<CalendarBulkImportItem> Items { get; set; } = [];
}

public sealed class CalendarBulkImportItem
{
    [Required, StringLength(160)] public string Name { get; set; } = string.Empty;
    [Required] public DateTime Date { get; set; }
    [EnumDataType(typeof(EnumsHelper.CalendarItem))] public EnumsHelper.CalendarItem Type { get; set; }
}
