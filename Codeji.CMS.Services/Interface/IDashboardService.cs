using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.DTO.RequestModels.Company;

namespace Codeji.CMS.Services.Interface
{
    public interface IDashboardService
    {
        Task<List<AllDepartmentDetailsResponseModel>> GetAllDepartmentsDetails(string companyId);
    }
}
