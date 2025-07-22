namespace Codeji.CMS.DTO.RequestModels
{
    public class JobVacancyModel
    {
        public string JobId { get; set; }
        public string Title { get; set; }
        public int Vacancies { get; set; }
        public int JobType { get; set; }
        public bool Status { get; set; }
        public string Description { get; set; }
        public int? TotalSubmissions { get; set; } = null;
    }
}
