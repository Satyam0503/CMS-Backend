using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Recruitments
{
    public class ApplicantResultFilters
    {
        public string Name { get; set; }
        public DateTime? FilterFrom { get; set; }
        public DateTime? FilterTo { get; set; }
        public int[] Status { get; set; }
        public ActivityType[] ActivityTypes { get; set; }
        public string[] VacancyIds { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public int PageNo { get; set; }
        public int Records { get; set; }
    }
}

