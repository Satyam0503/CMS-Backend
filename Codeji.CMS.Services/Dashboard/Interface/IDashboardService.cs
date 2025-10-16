using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Dashboard;

namespace Codeji.CMS.Services.Interface
{
    public interface IDashboardService
    {
        Task<List<DepartmentEmpResponseDto>> GetAllDepartmentsDetails(string companyId);
        Task<List<GenderDetailsResponseModel>> GetAllGenderDetails(string companyId);
        Task<Result<ApplicationDataResponseDto>> GetApplicationStatusData(string? vacancyId);
        Task<Result<UpComingHolidayEventResponseDto>> GetUpComingHolidayAndEvents();
        Task<Result<UpcomingCelebrations>> GetUpComingCelebrations();
    }
}
