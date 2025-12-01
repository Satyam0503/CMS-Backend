using System.ComponentModel.DataAnnotations;
using static Codeji.CMS.Utility.Enums.EnumsHelper;
namespace Codeji.CMS.DTO.Calendar;

public class CalendarFilters
{

    [EnumDataType(typeof(CalendarResponseItem), ErrorMessage = "Invalid Calendar Item Type")]
    public CalendarResponseItem? Type { get; set; }

    [Range(1999, 2100, ErrorMessage = "Invalid Year")]
    public int? Year { get; set; }
}
