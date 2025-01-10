using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IMongoDbRepository<Company> _companyRepo;
        private readonly IMongoDbRepository<User> _userRepo;
        private readonly IMongoDbRepository<CompanyRole> _companyRoleRepo;
        private IMapper _mapper;

        public CompanyService(IMongoDbRepository<Company> companyRepo, IMapper mapper, IMongoDbRepository<User> userRepo, IMongoDbRepository<CompanyRole> companyRoleRepo)
        {
            _companyRepo = companyRepo;
            _userRepo = userRepo;
            _companyRoleRepo = companyRoleRepo;
            _mapper = mapper;
            
        }

        public bool AddDefaultRole(string companyId)
        {
            var roles = _companyRoleRepo.Get(x => x.IsDefault).ToList();
            
            foreach (var role in roles)
            {
                CompanyRole defaultData = new CompanyRole()
                {
                    RoleType = role.RoleType,
                    Titles = role.Titles,
                    Description = role.Description,
                    IsNotEditable = role.IsNotEditable,
                    IsDefault = false,
                    HasAppAccess= role.HasAppAccess,
                    CompanyId = companyId,
                    CreatedDate = DateTime.UtcNow
                };
               _companyRoleRepo.AddOne(defaultData);
            }
            return true;
        }

        public async Task<CompanyRequestModel> Register(CompanyRequestModel companyModel)
        {
            var company = new Company()
            {
                CompanyId = Guid.NewGuid().ToString(),
                CompanyName = companyModel.CompanyName,
            };

            await _companyRepo.AddOne(company);

            //Add Default Role
            AddDefaultRole(company.CompanyId);


            var adminRole = await _companyRoleRepo.FirstOrDefault(x => x.RoleType == 1 && x.CompanyId == company.CompanyId);
            User user = new User()
            {
                Email = companyModel.Email,
                FirstName = companyModel.FirstName,
                LastName = companyModel.LastName,
                CompanyId = company.CompanyId,
                Password = AuthenticationHandler.HashedPassword(companyModel.Password),
                RoleId = adminRole?.CompanyRoleId,
            };
            var addedUser = _userRepo.AddOne(user);

            //company.PrimaryContact = "dfsdfsdf";
            


            return companyModel;




        }
    }
}
