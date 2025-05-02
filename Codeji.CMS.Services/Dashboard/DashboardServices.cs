using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Interface;

namespace Codeji.CMS.Services.Dashboard
{
    public class DashboardServices : IDashboardService
    {

        private readonly IMongoDbRepository<Department> _departmentRepository;
        private readonly IMongoDbRepository<EmpUser> _empUserRepository;
        private readonly IMapper _mapper;
        public DashboardServices(
            IMongoDbRepository<Department> departmentRepository,
            IMongoDbRepository<EmpUser> empUserRepository,
            IMapper mapper) 
        
        {
            _departmentRepository = departmentRepository;
            _empUserRepository = empUserRepository;
            _mapper = mapper;
        }

        public async Task<List<AllDepartmentDetailsResponseModel>> GetAllDepartmentsDetails(string companyId)
        {
            var allDepartments = await _departmentRepository.GetAll(x => x.CompanyId == companyId && x.IsDeleted == false);
            var allEmpUser = await _empUserRepository.GetAll(x => x.CompanyId == companyId);

            var result = from ad in allDepartments
                         join aeu in allEmpUser on ad.DepartmentId equals aeu.Department into empGroup
                         select new AllDepartmentDetailsResponseModel
                         {
                             Titles = ad.Titles,
                             DepartmentId = ad.DepartmentId,
                             EmployeeCount = empGroup.Count(),
                         };

            return result.ToList();

        }

        public async Task<List<GenderDetailsResponseModel>> GetAllGenderDetails(string companyId)
        {
            var allEmpUser = await _empUserRepository.GetAll(x => x.CompanyId == companyId);
            var result = (from aeu in allEmpUser
                         group aeu by aeu.Gender into genderGroup
                         select new GenderDetailsResponseModel 
                         {
                             Gender = genderGroup.Key,
                             Count= genderGroup.Count()
                         }).ToList();

            return result;

        }

    }
}
