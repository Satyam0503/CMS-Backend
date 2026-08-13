using System.Linq.Expressions;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.Company.Policy;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Companies;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

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
        private readonly IMongoDbRepository<UserSecurityToken> _userSecurityTokenRepo;
        readonly IMongoDbRepository<Policy> _policyRepo;
        readonly IMongoDbRepository<PolicyVersion> _policyVersionRepo;
        private readonly IMongoDbRepository<Department> _departmentRepo;
        private readonly IMongoDbRepository<JobTitles> _jobTitleRepo;
        private readonly IMapper _mapper;
        private readonly IRoleService _roleService;
        private readonly IEmployeeService _employeeService;
        readonly IMiddlewareService _middlewareService;
        private readonly ILogger<CompanyService> _logger;
        private readonly IAttendanceStatusService _attendanceStatusService;
        private readonly IMongoClient _mongoClient;


        public CompanyService(
            IMongoDbRepository<Company> companyRepo,
            IMapper mapper,
            IMongoDbRepository<EmpUser> userRepo,
            IMongoDbRepository<Roles> companyRoleRepo,
            IMongoDbRepository<ModulePermission> modulePermissisonRepo,
            IMongoDbRepository<RolePermission> rolePermissionRepo,
            IMongoDbRepository<NotificationPreference> notificationPreferenceRepo,
            IMongoDbRepository<UserSecurityToken> userSecurityTokenRepo,
            IMongoDbRepository<Policy> policyRepo,
            IMongoDbRepository<PolicyVersion> policyVersionRepo,
            IMongoDbRepository<Department> departmentRepo,
            IMongoDbRepository<JobTitles> jobTitleRepo,
            IRoleService roleService,
            IEmployeeService employeeService,
            IMiddlewareService middlewareService,
            IAttendanceStatusService attendanceStatusService,
            IMongoClient mongoClient,
            ILogger<CompanyService> logger
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
            _userSecurityTokenRepo = userSecurityTokenRepo;
            _employeeService = employeeService;
            _middlewareService = middlewareService;
            _attendanceStatusService = attendanceStatusService;
            _policyRepo = policyRepo;
            _policyVersionRepo = policyVersionRepo;
            _departmentRepo = departmentRepo;
            _jobTitleRepo = jobTitleRepo;
            _mongoClient = mongoClient;
            _logger = logger;
        }

        public async Task<Result> Register(CompanyRequestModel companyModel)
        {
            Result result = new Result();

            // Email is globally unique in the employee identity model; check before
            // creating anything so a duplicate registration email can't slip past the
            // application layer the way legacy data did.
            bool emailExists = await _userRepo.Exist(e =>
                e.Email.Equals(companyModel.Email, StringComparison.OrdinalIgnoreCase));
            if (emailExists)
            {
                result.StatusCode = CustomStatusCode.EmployeeAlreadyExist;
                result.Message = "An employee/user with this email address already exists.";
                return result;
            }

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
                IsEmailVerified = false
            };
            //Company Creation and Addition in DB
            Company company = new Company()
            {
                CompanyId = companyId,
                PrimaryContact = user.UserId,
                CompanyName = companyModel.CompanyName,
                CareerSlug = await BuildUniqueCareerSlug(companyModel.CompanyName, companyId),
                PublicCompanyCode = await BuildUniquePublicCompanyCode(),
                CareerPortalEnabled = true,
                PublishJobsToMasterPortal = true,
                DefaultLanguage = Languages.English,
                // All supported UI languages are enabled by default so the language
                // switcher and per-attribute translation columns are usable immediately;
                // an admin can still narrow this later from Company Info.
                ApplicationLanguage = [Languages.English, Languages.Mandarin, Languages.Spanish, Languages.Japanese, Languages.German, Languages.French],
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
                // The registering administrator is also the first employee in the
                // company. Use the same company-owned atomic sequence as every
                // subsequent employee instead of bypassing EmployeeService.
                user.EmployeeId = await _employeeService.ReserveNextEmployeeId(companyId);
                await _userRepo.AddOne(user);
                await _notificationPreferenceRepo.AddOne(notificationPreferenceSetting);
                await _attendanceStatusService.EnsureCompanyDefaults(companyId);
                await SendCompanyUserVerificationEmail(user, company);
                await SeedDefaultDepartmentsAndJobTitles(company, user.UserId);
                return result;
            }
            return result;
        }

        private async Task SendCompanyUserVerificationEmail(EmpUser user, Company company)
        {
            string token = TokenHelper.GenerateToken();
            string tokenHash = TokenHelper.ComputeSha256Hash(token);
            int tokenExpiryHours = 24;
            UserSecurityToken securityToken = new()
            {
                UserId = user.UserId,
                TokenHash = tokenHash,
                IsUsed = false,
                Expiry = DateTime.UtcNow.AddHours(tokenExpiryHours),
                Type = EnumsHelper.SecurityTokenType.EmailVerification
            };
            await _userSecurityTokenRepo.AddOne(securityToken);

            string verifyLink = EmailVerificationUrlBuilder.Build(ConfigManager.AppSettings.APIUrl, token);
            string body = HtmlTemplate.Render(
                "<h2>Welcome, [EmployeeName]!</h2>" +
                "<p>Please verify your email address to activate your account.</p>" +
                "<p><a href=\"[EmailVerificationLink]\">Verify email</a></p>" +
                "<p>This link will expire in [LinkExpiryTime].</p>",
                new
                {
                    EmployeeName = $"{user.FirstName} {user.LastName}",
                    EmailVerificationLink = verifyLink,
                    LinkExpiryTime = $"{tokenExpiryHours} hours"
                });

            await _middlewareService.EmailSendAndSave(new EmpEmailLogs()
            {
                UserTo = user.UserId,
                Subject = $"Verify your email for {company.CompanyName}",
                Body = body,
                EmailLogType = EnumsHelper.MailType.EmployeeWelcomeMail,
                Email = user.Email,
                UserFrom = user.UserId,
            });
        }

        // Seeds the generic set of departments and job titles for a freshly created company.
        // Idempotent (skips when records already exist for the company) and never propagates
        // failures — registration must not fail because a default seed could not be inserted.
        private async Task SeedDefaultDepartmentsAndJobTitles(Company company, string createdBy)
        {
            try
            {
                var languages = company.ApplicationLanguage is { Count: > 0 }
                    ? company.ApplicationLanguage
                    : new List<string> { company.DefaultLanguage ?? Languages.English };

                bool hasDepartments = await _departmentRepo.Exist(d => d.CompanyId == company.CompanyId);
                if (!hasDepartments)
                {
                    var departments = DefaultCompanySeeds.BuildDepartments(company.CompanyId, languages, createdBy);
                    foreach (var dept in departments)
                    {
                        await _departmentRepo.AddOne(dept);
                    }
                }

                var companyDepartments = (await _departmentRepo.GetAll(d => d.CompanyId == company.CompanyId && !d.IsDeleted)).ToList();
                var departmentIds = companyDepartments
                    .Select(d => new { Name = d.Titles.FirstOrDefault(t => t.Language == Languages.English)?.Label ?? d.Titles.FirstOrDefault()?.Label, d.DepartmentId })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .GroupBy(x => x.Name!, StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() == 1)
                    .ToDictionary(x => x.Key, x => x.Single().DepartmentId, StringComparer.OrdinalIgnoreCase);

                bool hasJobTitles = await _jobTitleRepo.Exist(j => j.CompanyId == company.CompanyId);
                if (!hasJobTitles)
                {
                    var jobTitles = DefaultCompanySeeds.BuildJobTitles(company.CompanyId, languages, departmentIds, createdBy);
                    foreach (var jt in jobTitles)
                    {
                        await _jobTitleRepo.AddOne(jt);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to seed default departments / job titles for company {CompanyId}. Registration will continue.",
                    company.CompanyId);
            }
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

        public async Task<Result<PublicCareerCompanyDto>> GetPublicCareerCompany()
        {
            Company? company = (await _companyRepo.GetAll(x =>
                    x.Status &&
                    !x.IsDeleted &&
                    x.CareerPortalEnabled,
                    withDefaultFilter: false))
                .OrderBy(x => x.CreatedDate)
                .FirstOrDefault();

            if (company is null)
                return new Result<PublicCareerCompanyDto>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Career portal not found."
                };

            await EnsureCareerPortal(company);
            return BuildPublicCareerCompanyResult(company);
        }

        public async Task<Result<PublicCareerCompanyDto>> GetPublicCareerCompany(string publicCompanyCode)
        {
            string normalizedCode = NormalizePublicCompanyCode(publicCompanyCode);
            Company? company = (await _companyRepo.GetAll(x =>
                    x.Status &&
                    !x.IsDeleted &&
                    x.CareerPortalEnabled,
                    withDefaultFilter: false))
                .FirstOrDefault(x => NormalizePublicCompanyCode(x.PublicCompanyCode) == normalizedCode);

            if (company is null)
                return new Result<PublicCareerCompanyDto>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Career portal not found."
                };

            await EnsureCareerPortal(company);
            return BuildPublicCareerCompanyResult(company);
        }

        private static Result<PublicCareerCompanyDto> BuildPublicCareerCompanyResult(Company company) =>
            new()
            {
                Success = true,
                MethodResult = new PublicCareerCompanyDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    CareerSlug = company.CareerSlug,
                    PublicCompanyCode = company.PublicCompanyCode,
                    CompanyLogo = company.CompanyLogo
                }
            };

        private async Task<string> BuildUniqueCareerSlug(string companyName, string? currentCompanyId = null)
        {
            string baseSlug = NormalizeCareerSlug(companyName);
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "company";

            string candidate = baseSlug;
            for (int suffix = 2; await _companyRepo.Exist(x =>
                x.CareerSlug == candidate &&
                (string.IsNullOrEmpty(currentCompanyId) || x.CompanyId != currentCompanyId)); suffix++)
                candidate = $"{baseSlug}-{suffix}";

            return candidate;
        }

        private static string NormalizeCareerSlug(string? value) =>
            Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');

        private async Task<string> BuildUniquePublicCompanyCode()
        {
            string code;
            do
            {
                code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            }
            while (await _companyRepo.Exist(x => x.PublicCompanyCode == code));

            return code;
        }

        private static string NormalizePublicCompanyCode(string? value) =>
            Regex.Replace(value ?? string.Empty, @"\D", string.Empty);

        private static string NormalizeEmployeeIdPrefix(string? prefix, string companyName)
        {
            string value = new string((prefix ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(value))
            {
                value = string.Concat((companyName ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => word[0]))
                    .ToUpperInvariant();
            }

            return value.Length > 6 ? value[..6] : value;
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
                await EnsureCareerPortal(company);
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
            company.EmployeeIdPrefix = NormalizeEmployeeIdPrefix(model.EmployeeIdPrefix, company.CompanyName);
            company.AutoGenerateEmployeeId = model.AutoGenerateEmployeeId;
            company.CompanyLogo = model.CompanyLogo == null ? company.CompanyLogo : await UpdateCompanyLogo(model.CompanyLogo, companyId);
            await EnsureCareerPortal(company);

            Result result1 = await _companyRepo.Update(whereCondition, company);
            if (!result1.Success)
            {
                result.Message = "Failed To Update Company";
                return result;
            }
            company.CompanyLogo = Common.GetCompanyLogoUrl(company.CompanyLogo);
            result.MethodResult = company;
            result.Success = true;
            return result;
        }

        private async Task EnsureCareerPortal(Company company)
        {
            bool needsUpdate = false;

            if (string.IsNullOrWhiteSpace(company.CareerSlug))
            {
                company.CareerSlug = await BuildUniqueCareerSlug(company.CompanyName, company.CompanyId);
                needsUpdate = true;
            }

            if (!Regex.IsMatch(company.PublicCompanyCode ?? string.Empty, @"^\d{6}$"))
            {
                company.PublicCompanyCode = await BuildUniquePublicCompanyCode();
                needsUpdate = true;
            }

            if (!company.CareerPortalEnabled)
            {
                company.CareerPortalEnabled = true;
                needsUpdate = true;
            }

            if (!needsUpdate) return;

            await _companyRepo.UpdateMany(
                Builders<Company>.Filter.Eq(x => x.CompanyId, company.CompanyId),
                Builders<Company>.Update
                    .Set(x => x.CareerSlug, company.CareerSlug)
                    .Set(x => x.PublicCompanyCode, company.PublicCompanyCode)
                    .Set(x => x.CareerPortalEnabled, company.CareerPortalEnabled)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow));
        }

        public async Task<string> UpdateCompanyLogo(IFormFile companyLogo, string companyId)
        {
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "CompanyLogo");
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
            bool isPolicyExist = await _policyRepo.Exist(p => p.PolicyName.Equals(model.PolicyName, StringComparison.CurrentCultureIgnoreCase) && p.CompanyId == companyId);
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
                // A policy cannot be published before a document version exists.
                IsActive = false,
                CompanyId = companyId,
            };
            result = await _policyRepo.AddOne(policy);
            if (!result.Success) return result;
            result.Success = true;
            return result;
        }

        public async Task<Result> UpdatePolicy(UpdatePolicyRequestModel model, string companyId)
        {
            Result result = new();
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
            if (model.IsActive && !await HasCurrentVersionAsync(existingPolicy.PolicyId, companyId))
            {
                result.StatusCode = CustomStatusCode.PolicyDontHaveCurrentVersion;
                return result;
            }
            existingPolicy.IsActive = model.IsActive;

            result = await _policyRepo.Update(whereCondition, existingPolicy);
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
                Expression<Func<Policy, bool>> expression = pl => pl.CompanyId == companyId && !pl.IsDeleted;
                var policies = (await _policyRepo.GetAll(expression)).OrderBy(k => k.CreatedDate);
                policyResponse = _mapper.Map<List<PolicyResponseModel>>(policies);
            }
            else
            {
                var empUser = await _userRepo.FirstOrDefault(emp => emp.UserId == userId && emp.Status && emp.IsEmailVerified);
                if (empUser is null) return result;
                var userRole = empUser.RoleId;
                var userDepartment = empUser.Department ?? string.Empty;
                Expression<Func<Policy, bool>> expression = pl => (pl.Departments.Count == 0 || pl.Departments.Contains(userDepartment)) && (pl.Roles.Count == 0 || pl.Roles.Contains(userRole)) && pl.IsActive && !pl.IsDeleted && pl.CompanyId == companyId;
                var policies = await _policyRepo.GetAll(expression);
                // Do not disclose drafts or malformed active policies to employees.
                var visiblePolicies = new List<Policy>();
                foreach (var policy in policies)
                    if (await HasCurrentVersionAsync(policy.PolicyId, companyId)) visiblePolicies.Add(policy);
                policyResponse = _mapper.Map<List<PolicyResponseModel>>(visiblePolicies);
            }
            result.Success = true;
            result.MethodResults = policyResponse;
            return result;
        }
        public async Task<Result> DeletePolicy(string policyId, string companyId)
        {
            Result result = new();
            // check policy exist or not
            var policy = await _policyRepo.FirstOrDefault(p => p.PolicyId == policyId && p.CompanyId == companyId && !p.IsDeleted);
            if (policy == null)
            {
                result.StatusCode = CustomStatusCode.PolicyNotFound;
                return result;
            }
            policy.IsActive = false;
            policy.IsDeleted = true;

            try
            {
                var policyFilter = Builders<Policy>.Filter.Where(p => p.PolicyId == policy.PolicyId && p.CompanyId == companyId && !p.IsDeleted);
                var policyUpdate = Builders<Policy>.Update.Set(p => p.IsActive, false).Set(p => p.IsDeleted, true).Set(p => p.UpdatedDate, DateTime.UtcNow);
                var policyUpdateResult = await _policyRepo.GetCollection().UpdateOneAsync(policyFilter, policyUpdate);
                if (policyUpdateResult.ModifiedCount != 1) { result.StatusCode = CustomStatusCode.PolicyNotFound; return result; }
                var versionsFilter = Builders<PolicyVersion>.Filter.Where(pv => pv.PolicyId == policy.PolicyId && pv.CompanyId == companyId && !pv.IsDeleted);
                await _policyVersionRepo.GetCollection().UpdateManyAsync(versionsFilter, Builders<PolicyVersion>.Update.Set(pv => pv.IsDeleted, true).Set(pv => pv.IsCurrent, false).Set(pv => pv.UpdatedDate, DateTime.UtcNow));
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete policy {PolicyId} for company {CompanyId}", policyId, companyId);
                result.Message = "The policy could not be deleted. Please try again.";
                return result;
            }
        }
        public async Task<Result<PolicyVersionResponseModel>> AddPolicyVersion(PolicyVersionRequestModel model, string companyId)
        {
            Result<PolicyVersionResponseModel> response = new() { Success = false };
            var policy = await _policyRepo.FirstOrDefault(p => p.PolicyId == model.PolicyId && p.CompanyId == companyId && !p.IsDeleted);
            if (policy is null) { response.StatusCode = CustomStatusCode.PolicyNotFound; return response; }

            Result result = await AddUpdatePolicyDocument(model.PolicyDoc);
            if (!result.Success)
            {
                response.StatusCode = result.StatusCode;
                return response;
            }

            bool hasCurrentVersion = await HasCurrentVersionAsync(model.PolicyId, companyId);
            PolicyVersion policyVersion = new()
            {
                Id = Guid.NewGuid().ToString(),
                CompanyId = companyId,
                PolicyId = model.PolicyId,
                VersionName = model.VersionName,
                DocUrl = result.Message,
                IsCurrent = model.IsCurrent || (!hasCurrentVersion && policy.IsActive),
            };

            IClientSessionHandle? session = null;
            bool usedSession = false;
            try
            {
                try
                {
                    session = await _mongoClient.StartSessionAsync();
                    session.StartTransaction();
                    usedSession = true;
                }
                catch (Exception exStart)
                {
                    _logger.LogWarning(exStart, "MongoDB transactions not available, proceeding without a transaction for policy {PolicyId}", model.PolicyId);
                    usedSession = false;
                    session?.Dispose();
                    session = null;
                }

                if (usedSession)
                {
                    if (model.IsCurrent)
                    {
                        var otherVersions = Builders<PolicyVersion>.Filter.Where(pv => pv.PolicyId == model.PolicyId && pv.CompanyId == companyId && !pv.IsDeleted);
                        await _policyVersionRepo.GetCollection().UpdateManyAsync(session, otherVersions, Builders<PolicyVersion>.Update.Set(pv => pv.IsCurrent, false).Set(pv => pv.UpdatedDate, DateTime.UtcNow));
                    }
                    await _policyVersionRepo.GetCollection().InsertOneAsync(session, policyVersion);
                    await session.CommitTransactionAsync();
                }
                else
                {
                    if (model.IsCurrent)
                    {
                        var otherVersions = Builders<PolicyVersion>.Filter.Where(pv => pv.PolicyId == model.PolicyId && pv.CompanyId == companyId && !pv.IsDeleted);
                        await _policyVersionRepo.GetCollection().UpdateManyAsync(otherVersions, Builders<PolicyVersion>.Update.Set(pv => pv.IsCurrent, false).Set(pv => pv.UpdatedDate, DateTime.UtcNow));
                    }
                    await _policyVersionRepo.GetCollection().InsertOneAsync(policyVersion);
                }
            }
            catch (Exception ex)
            {
                try { DeletePolicyDocument(result.Message); } catch { }
                _logger.LogError(ex, "Failed to create policy version for policy {PolicyId}", model.PolicyId);
                response.Message = "The policy version could not be created. Please try again.";
                if (usedSession && session != null)
                {
                    try { await session.AbortTransactionAsync(); } catch { }
                }
                return response;
            }
            finally
            {
                session?.Dispose();
            }
            response.Success = true;
            response.MethodResult = new PolicyVersionResponseModel()
            {
                PolicyDocUrl = Common.GetPolicyDocumentPath(policyVersion.DocUrl),
                VersionName = policyVersion.VersionName,
                Id = policyVersion.Id,
                IsCurrent = policyVersion.IsCurrent,
            };
            return response;
        }

        public async Task<Result<PolicyVersionResponseModel>> EditPolicyVersion(PolicyVersionUpdateModel model, string companyId)
        {
            Result<PolicyVersionResponseModel> response = new() { Success = false };
            Result result = new();

            PolicyVersion? existingPolicyVersion = await _policyVersionRepo.FirstOrDefault(pr => pr.Id == model.Id && pr.CompanyId == companyId && !pr.IsDeleted);
            if (existingPolicyVersion is null) return response;

            var policy = await _policyRepo.FirstOrDefault(p => p.PolicyId == model.PolicyId && p.CompanyId == companyId && !p.IsDeleted);
            if (policy is null || existingPolicyVersion.PolicyId != model.PolicyId) { response.StatusCode = CustomStatusCode.PolicyNotFound; return response; }

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
            try
            {
                if (model.IsCurrent && !existingPolicyVersion.IsCurrent)
                {
                    var otherVersions = Builders<PolicyVersion>.Filter.Where(pv => pv.PolicyId == existingPolicyVersion.PolicyId && pv.CompanyId == companyId && !pv.IsDeleted && pv.Id != existingPolicyVersion.Id);
                    await _policyVersionRepo.GetCollection().UpdateManyAsync(otherVersions, Builders<PolicyVersion>.Update.Set(pv => pv.IsCurrent, false).Set(pv => pv.UpdatedDate, DateTime.UtcNow));
                }
                var versionFilter = Builders<PolicyVersion>.Filter.Where(pv => pv.Id == existingPolicyVersion.Id && pv.PolicyId == model.PolicyId && pv.CompanyId == companyId && !pv.IsDeleted);
                var versionUpdate = Builders<PolicyVersion>.Update.Set(pv => pv.VersionName, model.VersionName).Set(pv => pv.DocUrl, newDocFileName).Set(pv => pv.IsCurrent, model.IsCurrent).Set(pv => pv.UpdatedDate, DateTime.UtcNow);
                var updateResult = await _policyVersionRepo.GetCollection().UpdateOneAsync(versionFilter, versionUpdate);
                if (updateResult.ModifiedCount != 1) { DeletePolicyDocument(newDocFileName == existingPolicyVersion.DocUrl ? null : newDocFileName); return response; }
            }
            catch (Exception ex)
            {
                DeletePolicyDocument(newDocFileName == existingPolicyVersion.DocUrl ? null : newDocFileName);
                _logger.LogError(ex, "Failed to edit policy version {PolicyVersionId}", model.Id);
                response.Message = "The policy version could not be updated. Please try again.";
                return response;
            }
            if (newDocFileName != existingPolicyVersion.DocUrl) DeletePolicyDocument(existingPolicyVersion.DocUrl);
            existingPolicyVersion.VersionName = model.VersionName;
            existingPolicyVersion.DocUrl = newDocFileName;
            existingPolicyVersion.IsCurrent = model.IsCurrent;
            response.Success = true;
            response.MethodResult = new PolicyVersionResponseModel()
            {
                PolicyDocUrl = Common.GetPolicyDocumentPath(existingPolicyVersion.DocUrl),
                VersionName = existingPolicyVersion.VersionName,
                Id = existingPolicyVersion.Id,
                IsCurrent = existingPolicyVersion.IsCurrent,
            };
            return response;
        }

        private static async Task<Result> AddUpdatePolicyDocument(IFormFile? policyDoc, string? oldPolicyDocUrl = null)
        {
            Result result = new();
            string[] supportedFileFormat = [".doc", ".pdf", ".docx"];
            long maxAllowedFileSizeInMB = 5 * 1024 * 1024;

            if (policyDoc is null || policyDoc.Length == 0)
            {
                result.StatusCode = CustomStatusCode.InvalidFileFormat;
                return result;
            }

            string fileExtension = Path.GetExtension(policyDoc.FileName).ToLowerInvariant();
            long fileSize = policyDoc.Length;

            // validate file attribute

            if (!supportedFileFormat.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
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

        private static void DeletePolicyDocument(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal)) return;
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Policy", fileName);
            if (File.Exists(path)) File.Delete(path);
        }

        private Task<bool> HasCurrentVersionAsync(string policyId, string companyId) =>
            _policyVersionRepo.Exist(pv => pv.PolicyId == policyId && pv.CompanyId == companyId && pv.IsCurrent && !pv.IsDeleted);

        public async Task<Result<PolicyVersionResponseModel>> GetAllPolicyVersion(string policyId, string userId, string companyId)
        {
            var response = new Result<PolicyVersionResponseModel>();
            bool canViewAllPolicyVersion = await _roleService.VerifyUserAccess(AppModule.Policy, [Utility.Constraints.Permission.Create, Utility.Constraints.Permission.Edit], userId, companyId);

            var policy = await _policyRepo.FirstOrDefault(p => p.PolicyId == policyId && p.CompanyId == companyId && !p.IsDeleted);
            if (policy is null) { response.StatusCode = CustomStatusCode.PolicyNotFound; return response; }

            List<PolicyVersion> policyVersions = [];
            if (canViewAllPolicyVersion)
            {
                policyVersions = (await _policyVersionRepo.GetAll(pv => pv.PolicyId == policyId && pv.CompanyId == companyId && !pv.IsDeleted)).ToList();
            }
            else
            {
                var policyVersion = await _policyVersionRepo.FirstOrDefault(pv => pv.PolicyId == policyId && pv.CompanyId == companyId && pv.IsCurrent && !pv.IsDeleted);
                if (policy.IsActive && policyVersion != null)
                {
                    policyVersions.Add(policyVersion);
                }
            }

            response.Success = true;
            response.MethodResults = policyVersions.Select(pv => new PolicyVersionResponseModel
            {
                PolicyDocUrl = Path.GetExtension(Common.GetPolicyDocumentPath(pv.DocUrl) ?? string.Empty),
                VersionName = pv.VersionName,
                Id = pv.Id,
                IsCurrent = pv.IsCurrent
            }).ToList();
            return response;
        }

        public async Task<Result> DeletePolicyVersion(string policyVersionId, string companyId)
        {
            Result result = new();
            // check if version exist or not
            var policyVersion = await _policyVersionRepo.FirstOrDefault(pv => pv.Id == policyVersionId && pv.CompanyId == companyId && !pv.IsDeleted);
            if (policyVersion is null)
            {
                result.StatusCode = CustomStatusCode.PolicyVersionNotFound;
                return result;
            }
            // check if parent policy exist or not deleted
            var policy = await _policyRepo.FirstOrDefault(p => p.PolicyId == policyVersion.PolicyId && p.CompanyId == companyId && !p.IsDeleted);
            if (policy == null)
            {
                result.StatusCode = CustomStatusCode.PolicyNotFound;
                return result;
            }
            Expression<Func<PolicyVersion, bool>> expression = pv => pv.Id == policyVersion.Id && pv.CompanyId == companyId && !pv.IsDeleted;
            return await _policyVersionRepo.UpdateMany(expression, Builders<PolicyVersion>.Update.Set(pv => pv.IsDeleted, true));
        }

        public async Task<Result<PolicyVersionResponseModel>> SetCurrentPolicyVersion(SetCurrentPolicyVersionRequestModel model, string companyId)
        {
            var response = new Result<PolicyVersionResponseModel> { Success = false };
            var policy = await _policyRepo.FirstOrDefault(p => p.PolicyId == model.PolicyId && p.CompanyId == companyId && !p.IsDeleted);
            var version = await _policyVersionRepo.FirstOrDefault(v => v.Id == model.VersionId && v.PolicyId == model.PolicyId && v.CompanyId == companyId && !v.IsDeleted);
            if (policy is null || version is null) { response.StatusCode = policy is null ? CustomStatusCode.PolicyNotFound : CustomStatusCode.PolicyVersionNotFound; return response; }
            try
            {
                var allVersions = Builders<PolicyVersion>.Filter.Where(v => v.PolicyId == model.PolicyId && v.CompanyId == companyId && !v.IsDeleted);
                await _policyVersionRepo.GetCollection().UpdateManyAsync(allVersions, Builders<PolicyVersion>.Update.Set(v => v.IsCurrent, false).Set(v => v.UpdatedDate, DateTime.UtcNow));
                var target = Builders<PolicyVersion>.Filter.Where(v => v.Id == model.VersionId && v.PolicyId == model.PolicyId && v.CompanyId == companyId && !v.IsDeleted);
                var targetResult = await _policyVersionRepo.GetCollection().UpdateOneAsync(target, Builders<PolicyVersion>.Update.Set(v => v.IsCurrent, true).Set(v => v.UpdatedDate, DateTime.UtcNow));
                if (targetResult.ModifiedCount != 1 && targetResult.MatchedCount != 1) { response.StatusCode = CustomStatusCode.PolicyVersionNotFound; return response; }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set policy version {VersionId} current", model.VersionId);
                response.Message = "The current version could not be changed. Refresh and try again.";
                return response;
            }
            response.Success = true;
            response.MethodResult = new PolicyVersionResponseModel { Id = version.Id, VersionName = version.VersionName, PolicyDocUrl = Common.GetPolicyDocumentPath(version.DocUrl), IsCurrent = true };
            return response;
        }

        public async Task<Result<PolicyDocumentResult>> GetPolicyDocument(
            string policyVersionId,
            string userId,
            string companyId)
        {
            var response = new Result<PolicyDocumentResult>();
            bool canViewAllPolicyVersion =
                await _roleService.VerifyUserAccess(
                    AppModule.Policy,
                    [Utility.Constraints.Permission.Create, Utility.Constraints.Permission.Edit],
                    userId,
                    companyId);
            PolicyVersion? policyVersion;
            if (canViewAllPolicyVersion)
            {
                policyVersion = await _policyVersionRepo.FirstOrDefault(pv => pv.Id == policyVersionId && pv.CompanyId == companyId && !pv.IsDeleted);
            }
            else
            {
                policyVersion = await _policyVersionRepo
                    .FirstOrDefault(pv =>
                        pv.Id == policyVersionId &&
                        pv.CompanyId == companyId &&
                        pv.IsCurrent && !pv.IsDeleted);
            }
            if (policyVersion == null)
            {
                response.Success = false;
                response.Message = "Unauthorized or document not found";
                return response;
            }
            var policy = await _policyRepo.FirstOrDefault(p => p.PolicyId == policyVersion.PolicyId && p.CompanyId == companyId && !p.IsDeleted);
            if (policy is null || (!canViewAllPolicyVersion && !policy.IsActive))
            {
                response.Success = false;
                response.Message = "Unauthorized or document not found";
                return response;
            }
            if (!canViewAllPolicyVersion)
            {
                var employee = await _userRepo.FirstOrDefault(e => e.UserId == userId && e.CompanyId == companyId && e.Status && e.IsEmailVerified);
                if (employee is null || (policy.Departments.Count != 0 && !policy.Departments.Contains(employee.Department ?? string.Empty)) || (policy.Roles.Count != 0 && !policy.Roles.Contains(employee.RoleId)))
                {
                    response.Message = "Unauthorized or document not found";
                    return response;
                }
            }
            string uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                "Policy");
            if (string.IsNullOrWhiteSpace(policyVersion.DocUrl) || !string.Equals(policyVersion.DocUrl, Path.GetFileName(policyVersion.DocUrl), StringComparison.Ordinal))
            {
                response.Message = "Document file not found";
                return response;
            }
            string filePath = Path.Combine(uploadFolder, policyVersion.DocUrl);
            if (!File.Exists(filePath))
            {
                response.Success = false;
                response.Message = "Document file not found";
                return response;
            }
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            string contentType = Path.GetExtension(filePath).ToLower() switch
            {
                ".pdf"  => "application/pdf",
                ".doc"  => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _       => "application/octet-stream"
            };
            response.Success = true;
            response.MethodResult= 
                new PolicyDocumentResult
                {
                    FileContent = fileBytes,
                    FileName = policyVersion.DocUrl,
                    ContentType = contentType
                };
            return response;
        }
    }
}
