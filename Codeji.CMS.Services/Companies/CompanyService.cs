using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.Policy;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
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
        private readonly IMongoDbRepository<NotificationPreference> _notificationPreferenceRepo;
        readonly IMongoDbRepository<Policy> _policyRepo;
        readonly IMongoDbRepository<PolicyVersion> _policyVersionRepo;
        private readonly IMapper _mapper;
        private readonly IRoleService _roleService;
        private readonly IEmployeeService _employeeService;
        readonly IMiddlewareService _middlewareService;


        public CompanyService(
            IMongoDbRepository<Company> companyRepo,
            IMapper mapper,
            IMongoDbRepository<EmpUser> userRepo,
            IMongoDbRepository<Roles> companyRoleRepo,
            IMongoDbRepository<ModulePermission> modulePermissisonRepo,
            IMongoDbRepository<RolePermission> rolePermissionRepo,
            IMongoDbRepository<NotificationPreference> notificationPreferenceRepo,
            IMongoDbRepository<Policy> policyRepo,
            IMongoDbRepository<PolicyVersion> policyVersionRepo,
            IRoleService roleService,
            IEmployeeService employeeService,
            IMiddlewareService middlewareService
            )
        {
            _roleService = roleService;
            _companyRepo = companyRepo;
            _userRepo = userRepo;
            _companyRoleRepo = companyRoleRepo;
            _modulePermissisonRepo = modulePermissisonRepo;
            _rolePermissionRepo = rolePermissionRepo;
            _mapper = mapper;
            _notificationPreferenceRepo = notificationPreferenceRepo;
            _employeeService = employeeService;
            _middlewareService = middlewareService;
            _policyRepo = policyRepo;
            _policyVersionRepo = policyVersionRepo;
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
                Status = true,
                //  change verification logic later
                IsEmailVerified = true
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

            var notificationPreferenceSetting = new NotificationPreference()
            {
                UserId = user.UserId,
                Preferences = _middlewareService.GetDefaultNotificationPreferences(),
            };

            result = await _companyRepo.AddOne(company);
            if (result.Success)
            {
                await _userRepo.AddOne(user);
                await _notificationPreferenceRepo.AddOne(notificationPreferenceSetting);
                return result;
            }
            return result;
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
            company.Address = model.Address ?? company.Address;
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

        // services related to company policies
        public async Task<Result> AddPolicy(CreatePolicyRequestModel model, string companyId)
        {
            Result result = new();
            // check for existing policy with same name in the company
            bool isPolicyExist = await _policyRepo.Exist(p => p.PolicyName.Equals(model.PolicyName, StringComparison.CurrentCultureIgnoreCase) && p.CompanyId == companyId && p.IsActive);
            if (isPolicyExist)
            {
                result.StatusCode = CustomStatusCode.PolicyAlreadyExist;
                return result;
            }
            Policy policy = new()
            {
                PolicyName = model.PolicyName,
                Description = model.Description,
                Departments = model.Departments,
                Roles = model.Roles,
                IsActive = model.IsActive,
            };
            result = await _policyRepo.AddOne(policy);
            if (!result.Success) return result;
            result.Success = true;
            return result;
        }

        public async Task<Result<PolicyResponseModel>> UpdatePolicy(UpdatePolicyRequestModel model, string companyId)
        {
            Result<PolicyResponseModel> result = new();
            Expression<Func<Policy, bool>> whereCondition = x => x.PolicyId == model.PolicyId && x.CompanyId == companyId;
            Policy? existingPolicy = await _policyRepo.FirstOrDefault(whereCondition);
            if (existingPolicy is null)
            {
                result.Success = false;
                return result;
            }
            existingPolicy.PolicyName = model.PolicyName;
            existingPolicy.Description = model.Description;
            existingPolicy.Departments = model.Departments;
            existingPolicy.Roles = model.Roles;
            existingPolicy.IsActive = model.IsActive;

            Result updateResult = await _policyRepo.Update(whereCondition, existingPolicy);
            if (!updateResult.Success)
            {
                result.Success = false;
                return result;
            }
            PolicyResponseModel responseModel = _mapper.Map<PolicyResponseModel>(existingPolicy);
            result.MethodResult = responseModel;
            result.Success = true;
            return result;
        }
        public async Task<Result<PolicyResponseModel>> GetAllPolicies(string userId, string companyId)
        {
            Result<PolicyResponseModel> result = new() { Success = false };
            // check if user has create or edit permission for policy.
            List<PolicyResponseModel> policyResponse = [];
            bool canViewAllPolicies = await _roleService.VerifyUserAccess(AppModule.Policy, [Utility.Constraints.Permission.Create, Utility.Constraints.Permission.Edit], userId, companyId);
            if (canViewAllPolicies)
            {
                Expression<Func<Policy, bool>> expression = pl => pl.CompanyId == companyId;
                var policies = (await _policyRepo.GetAll(expression)).OrderBy(k => k.CreatedDate);
                policyResponse = _mapper.Map<List<PolicyResponseModel>>(policies);
            }
            else
            {
                var empUser = await _userRepo.FirstOrDefault(emp => emp.UserId == userId && emp.Status && emp.IsEmailVerified);
                if (empUser is null) return result;
                var userRole = empUser.RoleId;
                var userDepartment = empUser.Department ?? string.Empty;
                Expression<Func<Policy, bool>> expression = pl => (pl.Departments.Count == 0 || pl.Departments.Contains(userDepartment)) && (pl.Roles.Count == 0 || pl.Roles.Contains(userRole)) && pl.IsActive && pl.CompanyId == companyId;
                var policies = await _policyRepo.GetAll(expression);
                policyResponse = _mapper.Map<List<PolicyResponseModel>>(policies);
            }
            // check if user has create or edit permission for policy module
            result.Success = true;
            result.MethodResults = policyResponse;
            return result;
        }

        public async Task<Result<PolicyVersionResponseModel>> AddPolicyVersion(PolicyVersionRequestModel model)
        {
            Result<PolicyVersionResponseModel> response = new() { Success = false };
            bool isPolicyExist = await _policyRepo.Exist(p => p.PolicyId == model.PolicyId && p.IsActive);
            if (!isPolicyExist) return response;

            Result result = await AddUpdatePolicyDocument(model.PolicyDoc);
            if (!result.Success)
            {
                response.StatusCode = result.StatusCode;
                return response;
            }

            // Ensure only one current version
            if (model.IsCurrent)
            {
                Expression<Func<PolicyVersion, bool>> expression = pv => pv.PolicyId == model.PolicyId;
                await _policyVersionRepo.UpdateMany(expression, Builders<PolicyVersion>.Update.Set(pv => pv.IsCurrent, false));
            }

            PolicyVersion policyVersion = new()
            {
                Id = Guid.NewGuid().ToString(),
                PolicyId = model.PolicyId,
                VersionName = model.VersionName,
                DocUrl = result.Message,
                IsCurrent = model.IsCurrent,
            };

            var addResult = await _policyVersionRepo.AddOne(policyVersion);
            if (!addResult.Success) return response;
            response.Success = true;
            response.MethodResult = new PolicyVersionResponseModel()
            {
                PolicyDocUrl = Common.GetPolicyDocumentPath(policyVersion.DocUrl),
                VersionName = policyVersion.VersionName,
                Id = policyVersion.Id,
            };
            return response;
        }

        public async Task<Result<PolicyVersionResponseModel>> EditPolicyVersion(PolicyVersionUpdateModel model)
        {
            Result<PolicyVersionResponseModel> response = new() { Success = false };
            Result result = new();

            PolicyVersion? existingPolicyVersion = await _policyVersionRepo.FirstOrDefault(pr => pr.Id == model.Id);
            if (existingPolicyVersion is null) return response;

            bool isPolicyExist = await _policyRepo.Exist(p => p.PolicyId == model.PolicyId && p.IsActive);
            if (!isPolicyExist) return response;

            string? newDocFileName = existingPolicyVersion.DocUrl;

            if (model.PolicyDoc != null)
            {
                result = await AddUpdatePolicyDocument(model.PolicyDoc, existingPolicyVersion.DocUrl);
                if (!result.Success)
                {
                    response.StatusCode = result.StatusCode;
                    return response;
                }
                newDocFileName = result.Message;
            }
            existingPolicyVersion.VersionName = model.VersionName;
            existingPolicyVersion.DocUrl = newDocFileName;
            existingPolicyVersion.IsCurrent = model.IsCurrent;

            // Ensure only one current version
            if (model.IsCurrent)
            {
                Expression<Func<PolicyVersion, bool>> whereCondition = pv => pv.PolicyId == existingPolicyVersion.PolicyId;
                await _policyVersionRepo.UpdateMany(whereCondition, Builders<PolicyVersion>.Update.Set(pv => pv.IsCurrent, false));
            }
            Expression<Func<PolicyVersion, bool>> expression = pv => pv.Id == existingPolicyVersion.Id;
            var updateResult = await _policyVersionRepo.Update(expression, existingPolicyVersion);

            if (!updateResult.Success) return response;
            response.Success = true;
            response.MethodResult = new PolicyVersionResponseModel()
            {
                PolicyDocUrl = Common.GetPolicyDocumentPath(existingPolicyVersion.DocUrl),
                VersionName = existingPolicyVersion.VersionName,
                Id = existingPolicyVersion.Id,
            };
            return response;
        }



        private static async Task<Result> AddUpdatePolicyDocument(IFormFile policyDoc, string? oldPolicyDocUrl = null)
        {
            Result result = new();
            string[] supportedFileFormat = [".doc", ".pdf", ".docx"];
            long maxAllowedFileSizeInMB = 5 * 1024 * 1024;

            string fileExtension = Path.GetExtension(policyDoc.FileName);
            long fileSize = policyDoc.Length;

            // validate file attribute

            if (!supportedFileFormat.Contains(fileExtension))
            {
                result.StatusCode = CustomStatusCode.InvalidFileFormat;
                return result;
            }
            if (fileSize > maxAllowedFileSizeInMB)
            {
                result.StatusCode = CustomStatusCode.FileSizeLimitExceed;
                return result;
            }

            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Policy");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
            string filePath = Path.Combine(uploadFolder, fileName);

            try
            {
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await policyDoc.CopyToAsync(stream);
                }

                // delete old file only after successful upload
                if (!string.IsNullOrEmpty(oldPolicyDocUrl))
                {
                    string oldFilePath = Path.Combine(uploadFolder, oldPolicyDocUrl);
                    if (File.Exists(oldFilePath))
                        File.Delete(oldFilePath);
                }

                result.Success = true;
                result.Message = fileName;
                return result;
            }
            catch
            {
                result.StatusCode = CustomStatusCode.FileUploadFailed;
                return result;
            }
        }


        public async Task<Result<PolicyVersionResponseModel>> GetAllPolicyVersion(string policyId)
        {
            var policyVersions = await _policyVersionRepo.GetAll(pv => pv.PolicyId == policyId);
            var response = new Result<PolicyVersionResponseModel>();
            var policyVersionResponseModels = policyVersions.Select(pv => new PolicyVersionResponseModel
            {
                PolicyDocUrl = Common.GetPolicyDocumentPath(pv.DocUrl),
                VersionName = pv.VersionName,
                Id = pv.Id
            }).ToList();

            response.Success = true;
            response.MethodResults = policyVersionResponseModels;
            return response;
        }
    }
}