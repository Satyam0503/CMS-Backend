using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IMongoDbRepository<Company> _companyRepo;
        private readonly IMongoDbRepository<EmpUser> _userRepo;
        private readonly IMongoDbRepository<Roles> _companyRoleRepo;
        private readonly IMongoDbRepository<ModulePermission> _modulePermissisonRepo;
        private readonly IMongoDbRepository<RolePermission> _rolePermissionRepo;
        private readonly IMapper _mapper;
        private readonly IRoleService _roleService;


        public CompanyService(
            IMongoDbRepository<Company> companyRepo,
            IMapper mapper,
            IMongoDbRepository<EmpUser> userRepo,
            IMongoDbRepository<Roles> companyRoleRepo,
            IMongoDbRepository<ModulePermission> modulePermissisonRepo,
            IMongoDbRepository<RolePermission> rolePermissionRepo,
            IRoleService roleService

            )
        {
            _roleService = roleService;
            _companyRepo = companyRepo;
            _userRepo = userRepo;
            _companyRoleRepo = companyRoleRepo;
            _modulePermissisonRepo = modulePermissisonRepo;
            _rolePermissionRepo = rolePermissionRepo;
            _mapper = mapper;

        }

        public async Task<Result> Register(CompanyRequestModel companyModel)
        {
            Result result = new Result();
            string companyId = Guid.NewGuid().ToString();
            //Add Default Role
            List<Roles> adminRole = await _roleService.AddDefaultRole(companyId);

            EmpUser user = new EmpUser()
            {
                UserId = Guid.NewGuid().ToString(),
                Email = companyModel.Email,
                FirstName = companyModel.FirstName,
                LastName = companyModel.LastName,
                CompanyId = companyId,
                Password = AuthenticationHandler.HashedPassword(companyModel.Password),
                RoleId = adminRole.FirstOrDefault(x => x.RoleType == 1)?.RolesId ?? "",
                Status = true
            };
            //Company Creation and Addition in DB
            Company company = new Company()
            {
                CompanyId = companyId,
                PrimaryContact = user.UserId,
                CompanyName = companyModel.CompanyName,
                DefaultLanguage = Languages.English,
                ApplicationLanguage = [Languages.English],
                Status = true,
            };

            Result result1 = await _companyRepo.AddOne(company);
            if (!result1.Success)
            {
                return result1;
            }
            return await _userRepo.AddOne(user);
        }

        public async Task<List<Company>> GetAllCompanyList()
        {
            IEnumerable<Company> list = await _companyRepo.GetAll();
            return _mapper.Map<List<Company>>(list);
        }

        public async Task<bool> IsActiveCompanyExist(string companyId)
        {
            bool exist = await _companyRepo.Exist(x => x.CompanyId == companyId && x.Status);
            return exist;
        }

        public async Task<Result<Company>> GetCompanyDetails(string companyId)
        {
            Result<Company> result = new();
            Company? company = await _companyRepo.FirstOrDefault(x => x.CompanyId == companyId);
            if (company is null)
            {
                result.Success = false;
            }
            else
            {
                company.CompanyLogo = Common.GetCompanyLogoUrl(company.CompanyLogo);
                result.MethodResult = company;
            }
            return result;
        }

        public async Task<Result<Company>> UpdateCompanyDetails(UpdateCompanyInfoRequestModel model, string companyId)
        {
            Result<Company> result = new();
            Expression<Func<Company, bool>> whereCondition = x => x.CompanyId == companyId;
            Company? company = await _companyRepo.FirstOrDefault(whereCondition);
            if (company is null)
            {
                result.Success = false;
                return result;
            }
            if (model.ApplicationLanguage != null)
            {
                bool exist = model.ApplicationLanguage.Exists(x => x.Equals(model.DefaultLanguage));
                if (!exist)
                {
                    model.ApplicationLanguage.Add(model.DefaultLanguage);
                }
                company.ApplicationLanguage = model.ApplicationLanguage;
            }
            else
            {
                company.ApplicationLanguage = [model.DefaultLanguage];
            }
            company.CompanyName = model.CompanyName ?? company.CompanyName;
            company.DefaultLanguage = model.DefaultLanguage ?? company.DefaultLanguage;
            company.CompanyLogo = model.CompanyLogo == null ? company.CompanyLogo : await UpdateCompanyLogo(model.CompanyLogo, companyId);

            Result result1 = await _companyRepo.Update(whereCondition, company);
            if (!result.Success)
            {
                result.Message = "Failed To Update Company";
                return result;
            }
            company.CompanyLogo = Common.GetCompanyLogoUrl(company.CompanyLogo);
            result.MethodResult = company;
            result.Success = true;
            return result;
        }

        public async Task<string> UpdateCompanyLogo(IFormFile companyLogo, string companyId)
        {
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\CompanyLogo\\");
            string fileExtension = Path.GetExtension(companyLogo.FileName);

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
            string filePath = Path.Combine(uploadFolder, fileName);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                await companyLogo.CopyToAsync(fileStream);
            }
            ;

            string logo = await GetCompanyExistingLogo(companyId);
            if (!string.IsNullOrEmpty(logo))
            {
                string oldPath = Path.Combine(uploadFolder, logo);
                FileInfo fileInfo = new(oldPath);
                fileInfo.Delete(); // delete existing logo
            }
            return fileName;
        }

        public async Task<string> GetCompanyExistingLogo(string companyId)
        {
            Company? company = await _companyRepo.FirstOrDefault(x => x.CompanyId == companyId);
            return company?.CompanyLogo ?? string.Empty;
        }

        public async Task<bool> AddCompanyLogo(string fileName, string companyId)
        {
            Expression<Func<Company, bool>> whereCondition = x => x.CompanyId == companyId;
            Company? company = await _companyRepo.FirstOrDefault(whereCondition);
            if (company == null)
            {
                return false;
            }
            Result resutl = await _companyRepo.UpdateMany(whereCondition, Builders<Company>.Update.Set(x => x.CompanyLogo, fileName).Set(x => x.UpdatedDate, DateTime.UtcNow));
            return resutl.Success;
        }
    }
}
