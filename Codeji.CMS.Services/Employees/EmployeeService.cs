using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using MapsterMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Attendance;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees
{
    public class EmployeeService : IEmployeeService
    {
        private static readonly Regex EmployeeNamePattern = new(@"^[A-Za-z]+(?:[-'][A-Za-z]+)*(?: [A-Za-z]+(?:[-'][A-Za-z]+)*)*$", RegexOptions.CultureInvariant);
        readonly IMongoDbRepository<EmpEducationDetails> _educationDetailsRepo;
        readonly IMongoDbRepository<EmpCertificationDetails> _certificationDetailsRepo;
        readonly IMongoDbRepository<EmpSummary> _employeeSummaryRepo;
        readonly IMapper _mapper;
        readonly IPriorityTaskQueue _priorityTaskQueue;
        readonly IMongoDbRepository<EmpUser> _employeeRepository;
        readonly IMongoDbRepository<EmployeeIdSequence> _employeeIdSequenceRepository;
        readonly IMongoDbRepository<Roles> _rolesRepository;
        readonly IRoleService _roleService;
        readonly IMongoDbRepository<EmpSkills> _employeeSkillsRepository;
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
        readonly IMiddlewareService _middlewareService;
        readonly IMongoDbRepository<Department> _departmentRepository;
        readonly IMongoDbRepository<Skills> _skillsRepository;
        readonly IMongoDbRepository<EmpWorkHistory> _empWorkHistoryRepository;
        readonly IMongoDbRepository<UserNotifications> _userNotificationRepository;
        readonly IMongoDbRepository<Notifications> _notificationsRepository;
        readonly IMongoDbRepository<JobTitles> _jobTitlesRepository;
        readonly IHttpContextAccessor _httpContextAccessor;
        readonly IMongoDbRepository<CustomAttribute> _customAttributeRepository;
        readonly IMongoDbRepository<CustomAttributeValue> _customAttributeValueRepository;
        readonly IMongoDbRepository<NotificationPreference> _notificationPreferenceRepository;
        readonly INotificationService _notificationService;
        readonly IMongoDbRepository<UserSecurityToken> _userSecurityTokenRepository;
        readonly ILogger<EmployeeService> _logger;
        readonly IMongoDbRepository<CompanyOfficeSchedule> _officeScheduleRepository;
        readonly IMongoDbRepository<EmployeeScheduleAssignment> _employeeScheduleAssignmentRepository;

        public EmployeeService(IMongoDbRepository<EmpEducationDetails> educationDetailsRepo,
            IMapper mapper, IMongoDbRepository<EmpCertificationDetails> certificationDetailsRepo,
            IMongoDbRepository<EmpSummary> userSummary,
            IMongoDbRepository<EmpUser> employeeRepository,
            IMongoDbRepository<EmployeeIdSequence> employeeIdSequenceRepository,
            IMongoDbRepository<Roles> rolesRepository,
            IRoleService roleService,
            IMongoDbRepository<EmpSkills> employeeSkillsRepository,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IPriorityTaskQueue priorityTaskQueue,
            IMiddlewareService middlewareService,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbRepository<Department> departmentRepository,
            IMongoDbRepository<Skills> skillsRepository,
            IMongoDbRepository<EmpWorkHistory> empWorkHistoryRepository,
            IMongoDbRepository<UserNotifications> userNotificationRepository,
            IMongoDbRepository<Notifications> notificationsRepository,
            IMongoDbRepository<JobTitles> jobTitlesRepository,
            IMongoDbRepository<CustomAttribute> customAttributeRepository,
            IMongoDbRepository<CustomAttributeValue> customAttributeValueRepository,
            INotificationService notificationService,
            IMongoDbRepository<NotificationPreference> notificationPreferenceRepository,
            IMongoDbRepository<UserSecurityToken> userSecurityTokenRepository,
            IMongoDbRepository<CompanyOfficeSchedule> officeScheduleRepository,
            IMongoDbRepository<EmployeeScheduleAssignment> employeeScheduleAssignmentRepository,
            ILogger<EmployeeService> logger
            )
        {
            _employeeRepository = employeeRepository;
            _employeeIdSequenceRepository = employeeIdSequenceRepository;
            _rolesRepository = rolesRepository;
            _roleService = roleService;
            _educationDetailsRepo = educationDetailsRepo;
            _mapper = mapper;
            _certificationDetailsRepo = certificationDetailsRepo;
            _employeeSummaryRepo = userSummary;
            _employeeSkillsRepository = employeeSkillsRepository;
            _companyRepository = companyRepository;
            _mailTemplateRepository = mailTemplateRepository;
            _priorityTaskQueue = priorityTaskQueue;
            _middlewareService = middlewareService;
            _departmentRepository = departmentRepository;
            _skillsRepository = skillsRepository;
            _httpContextAccessor = httpContextAccessor;
            _empWorkHistoryRepository = empWorkHistoryRepository;
            _userNotificationRepository = userNotificationRepository;
            _notificationsRepository = notificationsRepository;
            _jobTitlesRepository = jobTitlesRepository;
            _customAttributeRepository = customAttributeRepository;
            _customAttributeValueRepository = customAttributeValueRepository;
            _notificationService = notificationService;
            _notificationPreferenceRepository = notificationPreferenceRepository;
            _userSecurityTokenRepository = userSecurityTokenRepository;
            _officeScheduleRepository = officeScheduleRepository;
            _employeeScheduleAssignmentRepository = employeeScheduleAssignmentRepository;
            _logger = logger;
        }

        public async Task<Result<InviteEmployeeDto>> InviteNewEmployee(InviteEmployeeDto model, string currentUserId)
        {
            Result<InviteEmployeeDto> result = new() { Success = false };
            UserModel? currentUser = await _middlewareService.GetUserById(currentUserId);
            if (string.IsNullOrWhiteSpace(currentUser?.CompanyId))
            {
                result.Message = "Current user does not have a company assigned.";
                return result;
            }

            string companyId = currentUser.CompanyId;
            if (!string.IsNullOrWhiteSpace(model.ScheduleId) && !await _officeScheduleRepository.Exist(x =>
                x.CompanyId == companyId && x.ScheduleId == model.ScheduleId && x.IsActive && !x.IsDeleted))
            {
                result.Message = "Selected shift is not available in the current company.";
                return result;
            }
            if (!TryNormalizeEmployeeName(model.FirstName, "First name", out string firstName, out string firstNameError))
            {
                result.Message = firstNameError;
                return result;
            }
            if (!TryNormalizeEmployeeName(model.LastName, "Last name", out string lastName, out string lastNameError))
            {
                result.Message = lastNameError;
                return result;
            }
            model.FirstName = firstName;
            model.LastName = lastName;
            model.Email = model.Email.Trim();

            // Email is globally unique in the existing employee identity model.
            // Check it before reserving an automatically generated employee ID so a
            // predictable email conflict neither consumes an ID nor reports as an ID conflict.
            bool emailExists = await _employeeRepository.Exist(e =>
                e.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
            if (emailExists)
            {
                result.StatusCode = CustomStatusCode.EmployeeAlreadyExist;
                result.Message = "An employee/user with this email address already exists.";
                return result;
            }

            if (!string.IsNullOrWhiteSpace(model.RoleId) && !await _rolesRepository.Exist(x =>
                x.RolesId == model.RoleId && x.CompanyId == companyId && !x.IsDeleted))
            {
                result.Message = "Selected access role is not available in the current company.";
                return result;
            }
            if (!string.IsNullOrWhiteSpace(model.DepartmentId) && !await _departmentRepository.Exist(x =>
                x.DepartmentId == model.DepartmentId && x.CompanyId == companyId && !x.IsDeleted))
            {
                result.Message = "Selected department is not available in the current company.";
                return result;
            }
            if (!string.IsNullOrWhiteSpace(model.JobRoleId))
            {
                JobTitles? jobRole = await _jobTitlesRepository.FirstOrDefault(x =>
                    x.JobTitleId == model.JobRoleId && x.CompanyId == companyId && x.IsActive && !x.IsDeleted);
                if (jobRole is null)
                {
                    result.Message = "Selected job role is not available in the current company.";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(model.DepartmentId) || jobRole.DepartmentId != model.DepartmentId)
                {
                    result.Message = "Selected job role does not belong to the selected department.";
                    return result;
                }
            }

            Company? company = await _companyRepository.FirstOrDefault(c => c.CompanyId == companyId);
            if (company?.AutoGenerateEmployeeId != false)
            {
                model.EmployeeId = await ReserveNextEmployeeId(companyId);
            }

            if (string.IsNullOrWhiteSpace(model.EmployeeId))
            {
                result.Message = "Employee ID is required when automatic generation is disabled.";
                return result;
            }

            bool employeeIdExists = await _employeeRepository.Exist(e =>
                e.CompanyId == companyId && e.EmployeeId.Equals(model.EmployeeId, StringComparison.OrdinalIgnoreCase));
            if (employeeIdExists)
            {
                result.StatusCode = CustomStatusCode.EmployeeIdAlreadyExist;
                result.Message = $"Employee ID {model.EmployeeId} is already assigned.";
                return result;
            }
            var userId = Guid.NewGuid().ToString();
            if (!string.IsNullOrWhiteSpace(model.ReportingManager))
            {
                if (string.Equals(model.ReportingManager, userId, StringComparison.Ordinal))
                {
                    result.Message = "An employee cannot be their own reporting manager.";
                    return result;
                }
                var manager = await _employeeRepository.FirstOrDefault(x => x.UserId == model.ReportingManager && x.CompanyId == companyId && x.Status && !x.IsDeleted);
                if (manager is null)
                {
                    result.Message = "Reporting manager must be an active employee in the same company.";
                    return result;
                }
            }
            EmpUser employee = new EmpUser()
            {
                UserId = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                EmployeeId = model.EmployeeId,
                CompanyId = companyId,
                RoleId = model.RoleId ?? string.Empty,
                Department = model.DepartmentId ?? string.Empty,
                JobRole = model.JobRoleId ?? string.Empty,
                Gender = model.Gender ?? string.Empty,
                ReportingManager = model.ReportingManager ?? string.Empty,
                IsEmailVerified = false,
                Status = true
            };
            var res = await _employeeRepository.AddOne(employee);
            if (!res.Success)
            {
                // A concurrent insert can pass the pre-check. Re-check each identity
                // independently so the response remains accurate for unique indexes.
                if (await _employeeRepository.Exist(e => e.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    result.StatusCode = CustomStatusCode.EmployeeAlreadyExist;
                    result.Message = "An employee/user with this email address already exists.";
                    return result;
                }
                if (await _employeeRepository.Exist(e => e.CompanyId == companyId && e.EmployeeId.Equals(model.EmployeeId, StringComparison.OrdinalIgnoreCase)))
                {
                    result.StatusCode = CustomStatusCode.EmployeeIdAlreadyExist;
                    result.Message = $"Employee ID {model.EmployeeId} is already assigned.";
                    return result;
                }
                result.Message = "Unable to create employee.";
                return result;
            }
            // No row is needed for the default: EffectiveOfficeScheduleService
            // resolves the active company default. Persist an override only when
            // HR selected a specific shift during employee creation.
            if (!string.IsNullOrWhiteSpace(model.ScheduleId))
            {
                await _employeeScheduleAssignmentRepository.AddOne(new EmployeeScheduleAssignment
                {
                    AssignmentId = Guid.NewGuid().ToString(), CompanyId = companyId,
                    UserId = employee.UserId, ScheduleId = model.ScheduleId,
                    EffectiveFrom = DateTime.UtcNow.Date, IsActive = true,
                    CreatedBy = currentUserId, CreatedDate = DateTime.UtcNow
                });
            }

            if (res.Success)
            {
                model.UserId = employee.UserId;
                result.Success = true;
                result.MethodResult = model;
            }
            // set default notification preferences for new employee
            var notificationPreferences = new NotificationPreference()
            {
                UserId = userId,
                Preferences = _middlewareService.GetDefaultNotificationPreferences(),
            };
            await _notificationPreferenceRepository.AddOne(notificationPreferences);
            await SendInvitationLink(currentUserId, employee);
            return result;
        }

        public async Task<Result<BulkImportEmployeesResponseDto>> BulkImportEmployees(BulkImportEmployeesRequestDto model, string currentUserId)
        {
            Result<BulkImportEmployeesResponseDto> result = new() { Success = false };
            if (model?.Employees == null || model.Employees.Count == 0 || model.Employees.Count > BulkImportEmployeesRequestDto.MaxBatchSize)
            {
                result.Message = $"Employees list must contain 1 to {BulkImportEmployeesRequestDto.MaxBatchSize} records.";
                return result;
            }

            result.Success = true;
            BulkImportEmployeesResponseDto response = new()
            {
                Total = model.Employees.Count
            };

            UserModel? currentUser = await _middlewareService.GetUserById(currentUserId);
            if (currentUser == null)
            {
                result.Success = false;
                result.Message = "Current user not found.";
                return result;
            }
            string companyId = currentUser.CompanyId;
            if (string.IsNullOrWhiteSpace(companyId))
            {
                result.Success = false;
                result.Message = "Current user does not have a company assigned.";
                return result;
            }

            Company? company = await _companyRepository.FirstOrDefault(c => c.CompanyId == companyId);
            string defaultLanguage = string.IsNullOrWhiteSpace(company?.DefaultLanguage) ? "en" : company.DefaultLanguage;

            // Bulk employee import must never create additional company
            // administrators. Administrator assignment is an explicit account-
            // ownership operation, not employee master data.
            List<Roles> availableRoles = (await _rolesRepository.GetAll(r =>
                r.CompanyId == companyId && !r.IsDeleted &&
                r.RoleType != (int)EnumsHelper.Roles.Administrator)).ToList();
            List<Department> availableDepartments = (await _departmentRepository.GetAll(d => d.CompanyId == companyId)).ToList();
            List<JobTitles> availableJobTitles = (await _jobTitlesRepository.GetAll(j =>
                j.CompanyId == companyId && j.IsActive && !j.IsDeleted)).ToList();

            var seenEmpIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < model.Employees.Count; i++)
            {
                BulkImportEmployeeItemDto item = model.Employees[i];
                string empId = item.EmpId?.Trim() ?? string.Empty;
                string firstName = NormalizeEmployeeName(item.FirstName);
                string lastName = NormalizeEmployeeName(item.LastName);
                string email = item.Email?.Trim() ?? string.Empty;
                string roleName = item.Role?.Trim() ?? string.Empty;
                string departmentName = item.Department?.Trim() ?? string.Empty;
                string jobRoleName = item.JobRole?.Trim() ?? string.Empty;
                string gender = item.Gender?.Trim() ?? string.Empty;
                int rowNumber = i + 1;

                BulkImportEmployeeRowResultDto rowResult = new()
                {
                    RowNumber = rowNumber,
                    EmpId = empId,
                    Email = email
                };

                if (string.IsNullOrWhiteSpace(empId) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email)
                    || string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(departmentName) || string.IsNullOrWhiteSpace(jobRoleName))
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Required fields are missing.";
                    response.Results.Add(rowResult);
                    continue;
                }

                if (!EmployeeNamePattern.IsMatch(firstName) || !EmployeeNamePattern.IsMatch(lastName))
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "First name and last name may contain letters, spaces, hyphens, and apostrophes only.";
                    response.Results.Add(rowResult);
                    continue;
                }

                if (!System.Net.Mail.MailAddress.TryCreate(email, out _))
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Invalid email format.";
                    response.Results.Add(rowResult);
                    continue;
                }

                if (!seenEmpIds.Add(empId))
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Duplicate employee ID in request.";
                    response.Results.Add(rowResult);
                    continue;
                }

                if (!seenEmails.Add(email))
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Duplicate email in request.";
                    response.Results.Add(rowResult);
                    continue;
                }

                bool emailExistsInDb = await _employeeRepository.Exist(e =>
                    e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                if (emailExistsInDb)
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Duplicate email.";
                    response.Results.Add(rowResult);
                    continue;
                }

                bool employeeIdExistsInDb = await _employeeRepository.Exist(e =>
                    e.CompanyId == companyId && e.EmployeeId.Equals(empId, StringComparison.OrdinalIgnoreCase));
                if (employeeIdExistsInDb)
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Duplicate employee ID.";
                    response.Results.Add(rowResult);
                    continue;
                }

                try
                {
                    Roles? role = availableRoles.FirstOrDefault(r =>
                        string.Equals(NormalizeText(r.Titles), NormalizeText(roleName), StringComparison.OrdinalIgnoreCase)
                        || string.Equals(r.RolesId, roleName, StringComparison.OrdinalIgnoreCase));
                    if (role == null)
                    {
                        rowResult.Status = "Failed";
                        rowResult.Message = "Role not found in company.";
                        response.Results.Add(rowResult);
                        continue;
                    }

                    Department? department = availableDepartments.FirstOrDefault(d =>
                        string.Equals(d.DepartmentId, departmentName, StringComparison.OrdinalIgnoreCase)
                        || d.Titles.Any(t => string.Equals(NormalizeText(t.Label), NormalizeText(departmentName), StringComparison.OrdinalIgnoreCase)));
                    if (department == null)
                    {
                        _logger.LogWarning(
                            "Department not found in company. companyId={CompanyId}, requestedDepartment={RequestedDepartment}, availableDepartments={AvailableDepartments}",
                            companyId,
                            departmentName,
                            string.Join(", ",
                                availableDepartments
                                    .SelectMany(d => d.Titles ?? [])
                                    .Select(t => NormalizeText(t.Label))
                                    .Where(label => !string.IsNullOrWhiteSpace(label))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .Take(10)));

                        rowResult.Status = "Failed";
                        rowResult.Message = "Department not found in company.";
                        response.Results.Add(rowResult);
                        continue;
                    }

                    JobTitles? jobTitle = availableJobTitles.FirstOrDefault(j =>
                        string.Equals(j.JobTitleId, jobRoleName, StringComparison.OrdinalIgnoreCase)
                        || j.Titles.Any(t => string.Equals(NormalizeText(t.Label), NormalizeText(jobRoleName), StringComparison.OrdinalIgnoreCase)));
                    if (jobTitle == null)
                    {
                        jobTitle = new JobTitles()
                        {
                            CompanyId = companyId,
                            DepartmentId = department.DepartmentId,
                            IsActive = true,
                            CreatedBy = currentUserId,
                            CreatedDate = DateTime.UtcNow,
                            Titles =
                            [
                                new MultilingualModel
                                {
                                    Language = defaultLanguage,
                                    Label = jobRoleName
                                }
                            ]
                        };

                        Result jobTitleCreateResult = await _jobTitlesRepository.AddOne(jobTitle);
                        if (!jobTitleCreateResult.Success)
                        {
                            rowResult.Status = "Failed";
                            rowResult.Message = "Unable to create new job role.";
                            response.Results.Add(rowResult);
                            continue;
                        }
                        availableJobTitles.Add(jobTitle);
                    }
                    else if (jobTitle.DepartmentId != department.DepartmentId)
                    {
                        rowResult.Status = "Failed";
                        rowResult.Message = "Job role does not belong to the selected department.";
                        response.Results.Add(rowResult);
                        continue;
                    }

                    InviteEmployeeDto inviteModel = new()
                    {
                        EmployeeId = empId,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        RoleId = role.RolesId,
                        DepartmentId = department.DepartmentId,
                        JobRoleId = jobTitle.JobTitleId,
                        Gender = gender
                    };

                    Result<InviteEmployeeDto> inviteResult = await InviteNewEmployee(inviteModel, currentUserId);
                    if (inviteResult.Success)
                    {
                        rowResult.Status = "Created";
                        rowResult.Message = "Employee invited successfully.";
                        rowResult.CreatedUserId = inviteResult.MethodResult?.UserId;
                    }
                    else
                    {
                        rowResult.Status = "Failed";
                        rowResult.Message = inviteResult.StatusCode switch
                        {
                            CustomStatusCode.EmployeeAlreadyExist => "Duplicate email.",
                            CustomStatusCode.EmployeeIdAlreadyExist => "Duplicate employee ID.",
                            _ => inviteResult.Message ?? "Unable to create employee."
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bulk employee import failed for row {RowNumber}. EmpId: {EmpId}, Email: {Email}", rowNumber, empId, email);
                    rowResult.Status = "Failed";
                    rowResult.Message = "Unexpected error while creating employee.";
                }

                response.Results.Add(rowResult);
            }

            response.CreatedCount = response.Results.Count(r => r.Status == "Created");
            response.FailedCount = response.Total - response.CreatedCount;
            result.MethodResult = response;
            return result;
        }

        private static string NormalizeText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return string.Join(" ", input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string NormalizeEmployeeName(string? input) =>
            Regex.Replace(input?.Trim() ?? string.Empty, @"\s+", " ");

        private static bool TryNormalizeEmployeeName(string? input, string fieldName, out string normalized, out string error)
        {
            normalized = NormalizeEmployeeName(input);
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = $"{fieldName} is required.";
                return false;
            }
            if (!EmployeeNamePattern.IsMatch(normalized))
            {
                error = $"{fieldName} contains unsupported characters.";
                return false;
            }
            return true;
        }

        private async Task SendInvitationLink(string currentUserId, EmpUser employee)
        {
            UserModel? currentUser = await _middlewareService.GetUserById(currentUserId);
            if (currentUser is null)
            {
                return;
            }

            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == currentUser.CompanyId);

            // generate password creation token for newly added employee
            string token = TokenHelper.GenerateToken();
            string tokenHash = TokenHelper.ComputeSha256Hash(token);
            int tokenExpiryTime = 24;
            UserSecurityToken securityToken = new()
            {
                UserId = employee.UserId,
                TokenHash = tokenHash,
                IsUsed = false,
                Expiry = DateTime.UtcNow.AddHours(tokenExpiryTime),
                Type = EnumsHelper.SecurityTokenType.Invite
            };
            await _userSecurityTokenRepository.AddOne(securityToken);

            //Acknowledgement Email Logic 
            RepositoryEmailTemplate.TryGet(EnumsHelper.MailType.EmployeeWelcomeMail, out var templateSubject, out var templateBody);
            string replacedBody = HtmlTemplate.Render(string.IsNullOrWhiteSpace(templateBody) ? "<p>Welcome, [EmployeeName]!</p><p><a href=\"[PasswordCreationLink]\">Create your password</a></p>" : templateBody, new
            {
                EmployeeName = employee.FirstName + " " + employee.LastName,
                PasswordCreationLink = $"{ConfigManager.AppSettings.AppUrl.TrimEnd('/')}/auth/createpassword?token={Uri.EscapeDataString(token)}&uid={employee.UserId}",
                CompanyName = company != null ? company.CompanyName : string.Empty,
                // CompanyLogo = company.CompanyLogo != null ? _middlewareService.GetCompanyLogoAsDataUrl(Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "CompanyLogo", company.CompanyLogo)) : string.Empty,
                CompanyLogo = company.CompanyLogo != null ? Common.GetCompanyLogoUrl(company.CompanyLogo) : string.Empty,
                Year = DateTime.UtcNow.Year,
                LinkExpiryTime = tokenExpiryTime,
            });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                await _middlewareService.EmailSendAndSave(new EmpEmailLogs()
                {
                    UserTo = employee.UserId,
                    Subject = HtmlTemplate.Render(string.IsNullOrWhiteSpace(templateSubject) ? "Welcome to [CompanyName]" : templateSubject, new { CompanyName = company?.CompanyName ?? string.Empty }),
                    Body = replacedBody,
                    EmailLogType = EnumsHelper.MailType.EmployeeWelcomeMail,
                    Email = employee.Email,
                    UserFrom = currentUser.UserId,
                });
            }, priority: 1);
        }

        public async Task<Result<UserModel>> EditEmployee(EmployeePersonalInfo user, string userId)
        {
            if (!TryNormalizeEmployeeName(user.FirstName, "First name", out string firstName, out string firstNameError))
                return new Result<UserModel> { Success = false, Message = firstNameError };
            if (!TryNormalizeEmployeeName(user.LastName, "Last name", out string lastName, out string lastNameError))
                return new Result<UserModel> { Success = false, Message = lastNameError };
            user.FirstName = firstName;
            user.LastName = lastName;

            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            EmpUser? employeeToUpdate = await _employeeRepository.FirstOrDefault(x =>
                x.UserId == userId && x.CompanyId == companyId && !x.IsDeleted);
            if (employeeToUpdate is null)
            {
                return new Result<UserModel>
                {
                    Success = false,
                    Message = "Employee does not exist in the current company."
                };
            }

            if (!string.IsNullOrWhiteSpace(user.RoleId))
            {
                Roles? assignedRole = await _rolesRepository.FirstOrDefault(x =>
                    x.RolesId == user.RoleId && x.CompanyId == companyId && !x.IsDeleted);
                if (assignedRole is null)
                {
                    return new Result<UserModel>
                    {
                        Success = false,
                        Message = "Selected system role is not available in the current company."
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(user.Department) && !await _departmentRepository.Exist(x =>
                x.DepartmentId == user.Department && x.CompanyId == companyId && !x.IsDeleted))
            {
                return new Result<UserModel>
                {
                    Success = false,
                    Message = "Selected department is not available in the current company."
                };
            }

            if (!string.IsNullOrWhiteSpace(user.JobRole))
            {
                JobTitles? jobRole = await _jobTitlesRepository.FirstOrDefault(x =>
                    x.JobTitleId == user.JobRole && x.CompanyId == companyId && x.IsActive && !x.IsDeleted);
                if (jobRole is null)
                {
                    return new Result<UserModel>
                    {
                        Success = false,
                        Message = "Selected job role is not available in the current company."
                    };
                }

                bool isUnchangedLegacyRole = string.IsNullOrWhiteSpace(jobRole.DepartmentId)
                    && employeeToUpdate.JobRole == user.JobRole
                    && employeeToUpdate.Department == user.Department;
                if (!isUnchangedLegacyRole &&
                    (string.IsNullOrWhiteSpace(user.Department) || jobRole.DepartmentId != user.Department))
                {
                    return new Result<UserModel>
                    {
                        Success = false,
                        Message = "Selected job role does not belong to the selected department."
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(user.ReportingManager))
            {
                if (string.Equals(user.ReportingManager, userId, StringComparison.Ordinal))
                    return new Result<UserModel> { Success = false, Message = "An employee cannot be their own reporting manager." };

                var manager = await _employeeRepository.FirstOrDefault(x =>
                    x.UserId == user.ReportingManager && x.CompanyId == companyId && x.Status && !x.IsDeleted);
                if (manager is null)
                    return new Result<UserModel> { Success = false, Message = "Reporting manager must be an active employee in the same company." };
            }
            UpdateDefinitionBuilder<EmpUser> update = Builders<EmpUser>.Update;
            List<UpdateDefinition<EmpUser>> updateDefinition = new();
            foreach (PropertyInfo property in user.GetType().GetProperties().Where(x => x.GetValue(user) != null))
            {
                object value = property.GetValue(user, null);
                if (value != null && value?.ToString() != "")
                {
                    updateDefinition.Add(update.Set(property.Name, property.GetValue(user)));
                }
            }

            updateDefinition.Add(update.Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, userId));
            UpdateDefinition<EmpUser> data = update.Combine(updateDefinition);
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == userId && x.CompanyId == companyId && !x.IsDeleted;
            Result result = await _employeeRepository.UpdateMany(whereCondition, data, true);
            UserModel updatedUser = await GetEmployeeById(userId);
            return new Result<UserModel>()
            {
                Success = true,
                MethodResult = updatedUser,
            };
        }
        public async Task<UserModel> GetEmployeeById(string userId)
        {
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
            UserModel userModel = _mapper.Map<UserModel>(user);
            if (userModel.JobRole != null)
            {
                JobTitles? jobTitles = await _jobTitlesRepository.FirstOrDefault(jt => jt.JobTitleId == userModel.JobRole);
                userModel.JobRoleTitle = jobTitles?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label);
            }
            if (userModel.ReportingManager != null)
            {
                EmpUser? reportingManager = await _employeeRepository.FirstOrDefault(x => x.UserId == user.ReportingManager);
                userModel.ReportingManagerName = reportingManager != null ? $"{reportingManager?.FirstName} {reportingManager?.LastName}" : null;
            }
            if (userModel.Department != null)
            {
                Department? department = await _departmentRepository.FirstOrDefault(x => x.DepartmentId == user.Department);
                userModel.DepartmentTitle = department?.Titles.ToDictionary(keySelector: d => d.Language, elementSelector: d => d.Label);
            }
            userModel.ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl);
            userModel.TotalWorkExperience = GetEmployeeTotalExperience(userId);
            if (user.CustomAttributeList.Count != 0)
            {
                List<UserCustomAttribute> customAttributeList = [];
                foreach (EmpUserCustomAttribute attribute in user.CustomAttributeList)
                {
                    var customAttribute = await _customAttributeRepository.FirstOrDefault(ca => ca.CustomAttributeId == attribute.CustomAttributeId);
                    var customAttributeValue = await _customAttributeValueRepository.FirstOrDefault(cav => cav.CustomAttributeValueId == attribute.CustomAttributeValueId && cav.CustomAttributeId == attribute.CustomAttributeId);
                    if (customAttribute != null && customAttributeValue != null)
                    {
                        customAttributeList.Add(new UserCustomAttribute()
                        {
                            CustomAttributeId = customAttribute.CustomAttributeId,
                            CustomAttributeTitle = customAttribute.CustomAttributeTitle.ToDictionary(t => t.Language, t => t.Label),
                            CustomAttributeValueId = customAttributeValue.CustomAttributeValueId,
                            CustomAttributeValueTitle = customAttributeValue.Titles.ToDictionary(t => t.Language, t => t.Label)
                        });
                    }
                }
                userModel.CustomAttributes = customAttributeList;
            }
            return userModel;
        }
        public async Task<string> GetEmployeeNameById(string employeeId)
        {
            EmpUser? empUser = await _employeeRepository.FirstOrDefault(emp => emp.UserId == employeeId);
            if (empUser == null) return string.Empty;
            return $"{empUser.FirstName} {empUser.LastName}";
        }
        public async Task<List<EmployeeSearchResponseDTO>> SearchEmployeeByName(string name)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => (x.FirstName + " " + x.LastName).Contains(name, StringComparison.CurrentCultureIgnoreCase);
            var empUsers = await _employeeRepository.GetAll(whereCondition);
            List<string> jobTitleIds = empUsers.Select(emp => emp.JobRole).Distinct().ToList();
            var jobTitleList = await _jobTitlesRepository.GetAll(jt => jobTitleIds.Contains(jt.JobTitleId));

            var result = (from emp in empUsers
                          join jobRole in jobTitleList
                          on emp.JobRole equals jobRole.JobTitleId into empJobTitleGroup
                          from jobTitle in empJobTitleGroup.DefaultIfEmpty()
                          select new EmployeeSearchResponseDTO()
                          {
                              UserId = emp.UserId,
                              FirstName = emp.FirstName,
                              LastName = emp.LastName,
                              EmpId = emp.EmployeeId,
                              JobRole = jobTitle?.Titles.ToDictionary(jt => jt.Language, jt => jt.Label),
                              FullProfileUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl)
                          }).ToList();
            return result;
        }
        public async Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(GetAllEmployeeRequestModel? filters)
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);
            string currentUserId = CurrentContext.UserId(_httpContextAccessor);
            EmpUser? currentUser = await _employeeRepository.FirstOrDefault(x =>
                x.UserId == currentUserId && x.CompanyId == companyId && !x.IsDeleted);
            string currentRoleId = currentUser?.RoleId ?? string.Empty;
            bool canViewEmployeePhoneNumbers =
                await _roleService.IsRoleTypeMatch(currentRoleId, EnumsHelper.Roles.Administrator, companyId)
                || await _roleService.IsRoleTypeMatch(currentRoleId, EnumsHelper.Roles.HR, companyId)
                || await _roleService.IsRoleTypeMatch(currentRoleId, EnumsHelper.Roles.HRExecutive, companyId);
            List<EmpUser> employeeList = [];
            int totalRecords = 0;
            if (filters == null)
            {
                employeeList = (await _employeeRepository.GetAll(x => x.CompanyId == companyId)).ToList();
                totalRecords = employeeList.Count;
            }
            else
            {
                Expression<Func<EmpUser, bool>> whereCondition = x =>
                x.CompanyId == companyId &&
                ((filters.DepartmentId == null || filters.DepartmentId.Count == 0) || filters.DepartmentId.Contains(x.Department)) &&
                ((filters.Gender == null || filters.Gender.Count == 0) || filters.Gender.Contains(x.Gender)) &&
                (string.IsNullOrEmpty(filters.Name)
                || (x.FirstName + " " + x.LastName).Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase));

                employeeList = (await _employeeRepository.GetAggregateDataAsync<EmpUser>(whereCondition, pageNo: filters.PageNo, pageSize: filters.Records)).ToList();
                totalRecords = await _employeeRepository.Count(whereCondition);
            }
            if (employeeList.Count == 0)
            {
                return new Result<GetAllEmployeeResponseModel>()
                {
                    Success = true,
                    MethodResults = [],
                    TotalRecords = totalRecords,
                };
            }
            string[] depId = employeeList
                .Select(x => x.Department)
                .Where(departmentId => !string.IsNullOrWhiteSpace(departmentId))
                .Distinct()
                .ToArray();
            // Materialize immediately. GetAll returns a cursor-backed IEnumerable;
            // letting the in-memory join below force the enumeration triggers a
            // Mongo LINQ3 + .NET 10 reflection failure inside PartialEvaluator.
            var deptList = (await _departmentRepository.GetAll(x => depId.Contains(x.DepartmentId))).ToList();

            string[] jobRoleId = employeeList.Select(e => e.JobRole).Distinct().ToArray();
            var jobRoleList = (await _jobTitlesRepository.GetAll(jt => jobRoleId.Contains(jt.JobTitleId))).ToList();

            var data = (from emp in employeeList
                        join dept in deptList
                        on emp.Department equals dept.DepartmentId into empDepartmentGrp
                        from department in empDepartmentGrp.DefaultIfEmpty()
                        join jt in jobRoleList
                        on emp.JobRole equals jt.JobTitleId into empJobTitleGrp
                        from jobTitle in empJobTitleGrp.DefaultIfEmpty()
                        select new GetAllEmployeeResponseModel
                        {
                            UserId = emp.UserId,
                            FullName = $"{emp?.FirstName} {emp?.LastName ?? ""}",
                            Email = emp.Email,
                            Gender = emp.Gender,
                            EmployeeId = emp.EmployeeId,
                            JobRole = jobTitle != null ? jobTitle.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label) : null,
                            Department = department?.Titles.ToDictionary(keySelector: d => d.Language, elementSelector: d => d.Label),
                            PhoneNumber = canViewEmployeePhoneNumbers ? emp.PhoneNumber : null,
                            DateOfBirth = emp.DateOfBirth,
                            DateOfJoining = emp.DateOfJoining,
                            FullProfileUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl),
                            IsVerified = emp.IsEmailVerified,
                        }).ToList();
            return new Result<GetAllEmployeeResponseModel>()
            {
                Success = true,
                MethodResults = data,
                TotalRecords = totalRecords
            };
        }

        public async Task<Result<DepartmentEmpResponseDto>> GetDirectoryDepartments()
        {
            string companyId = CurrentContext.CompanyId(_httpContextAccessor);

            // Keep these options aligned with the directory rows: both are resolved from
            // the authenticated tenant, never from a request-provided CompanyId.
            var employees = (await _employeeRepository.GetAll(x => x.CompanyId == companyId)).ToList();
            var employeeCountsByDepartment = employees
                .Where(x => !string.IsNullOrWhiteSpace(x.Department))
                .GroupBy(x => x.Department)
                .ToDictionary(x => x.Key, x => x.Count());

            var departmentIds = employeeCountsByDepartment.Keys.ToArray();
            if (departmentIds.Length == 0)
            {
                return new Result<DepartmentEmpResponseDto>
                {
                    Success = true,
                    MethodResults = []
                };
            }

            var departments = (await _departmentRepository.GetAll(x =>
                x.CompanyId == companyId &&
                !x.IsDeleted &&
                x.IsActive &&
                departmentIds.Contains(x.DepartmentId))).ToList();

            return new Result<DepartmentEmpResponseDto>
            {
                Success = true,
                MethodResults = departments
                    .OrderBy(x => x.Titles.FirstOrDefault(t => t.Language == "en")?.Label ?? x.DepartmentId)
                    .Select(x => new DepartmentEmpResponseDto
                    {
                        DepartmentId = x.DepartmentId,
                        Label = x.Titles.ToDictionary(t => t.Language, t => t.Label),
                        EmployeeCount = employeeCountsByDepartment[x.DepartmentId]
                    })
                    .ToList()
            };
        }
        public async Task<bool> IsEmailExist(string email)
        {
            bool IsEmailExist = await _employeeRepository.Exist(x => x.Email == email);
            return IsEmailExist;

        }
        public async Task<bool> IsEmpExistAndActive(string email)
        {
            return await _employeeRepository.Exist(x => x.Email == email && x.Status && x.Password != null);
        }

        public async Task<LoginUserViewModel?> GetSignedUserDetails(string userId, string roleId, string companyId)
        {
            LoginUserViewModel returnModel = new();
            UserModel? user = await GetEmployeeById(userId);
            if (user is null)
            {
                return null;
            }

            // The JWT role claim can be stale after an administrator changes an
            // employee's system role. The employee record is the canonical
            // assignment; JobRole remains a separate designation field.
            Roles? role = await _rolesRepository.FirstOrDefault(x => x.RolesId == user.RoleId && x.CompanyId == companyId && !x.IsDeleted);
            Company? companyDetails = await _companyRepository.FirstOrDefault(x => x.CompanyId == companyId);
            if (role is null || companyDetails is null)
            {
                return null;
            }

            string[] allowedModulePermission = await _roleService.GetRolePermissionOfuser(role.RolesId, companyId);
            returnModel.UserId = user.UserId;
            returnModel.RoleType = role.RoleType;
            returnModel.FirstName = user.FirstName;
            returnModel.LastName = user.LastName;
            returnModel.CompanyId = companyDetails.CompanyId;
            returnModel.ModulePermission = allowedModulePermission;
            returnModel.CompanyName = companyDetails.CompanyName;
            returnModel.DefaultLanguage = companyDetails.DefaultLanguage;
            returnModel.ApplicationLanguage = companyDetails.ApplicationLanguage;
            returnModel.ProfileImage = user.ProfileUrl;
            returnModel.CompanyLogo = Common.GetCompanyLogoUrl(companyDetails.CompanyLogo);
            // Pick the localized job title: Accept-Language → company default → "en" → first available.
            if (user.JobRoleTitle != null && user.JobRoleTitle.Count > 0)
            {
                string requestLang = CurrentContext.GetLanguage(_httpContextAccessor);
                returnModel.JobTitle =
                    (!string.IsNullOrEmpty(requestLang) && user.JobRoleTitle.TryGetValue(requestLang, out var fromHeader) ? fromHeader : null)
                    ?? (!string.IsNullOrEmpty(companyDetails.DefaultLanguage) && user.JobRoleTitle.TryGetValue(companyDetails.DefaultLanguage, out var fromDefault) ? fromDefault : null)
                    ?? (user.JobRoleTitle.TryGetValue("en", out var fromEn) ? fromEn : null)
                    ?? user.JobRoleTitle.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
            }
            return returnModel;
        }
        public async Task<Result> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId)
        {
            Expression<Func<EmpSummary, bool>> whereCondition = x => userId == x.UserId && x.Id == userSummary.SummaryId;
            EmpSummary? employeesummary = await _employeeSummaryRepo.FirstOrDefault(whereCondition);

            if (employeesummary == null)
            {
                EmpSummary summary = new EmpSummary()
                {
                    UserId = userId,
                    Summary = userSummary.Summary
                };
                return await _employeeSummaryRepo.AddOne(summary);
            }
            else
            {
                employeesummary.Summary = userSummary.Summary;
                return await _employeeSummaryRepo.Update(whereCondition, employeesummary);
            }
        }
        public async Task<Result> AddEditEmployeeSkills(SkillsRequestModel skillsModel, string userId)
        {
            Expression<Func<EmpSkills, bool>> whereCondition = x => userId == x.UserId;
            EmpSkills? employeSkills = await _employeeSkillsRepository.FirstOrDefault(whereCondition);
            if (employeSkills == null)
            {
                EmpSkills newSkill = new EmpSkills();
                {
                    newSkill.UserId = userId;
                    newSkill.Skills = skillsModel.Skills;
                }
                return await _employeeSkillsRepository.AddOne(newSkill);
            }
            else
            {
                employeSkills.Skills = skillsModel.Skills;
                return await _employeeSkillsRepository.Update(whereCondition, employeSkills);
            }
        }
        public async Task<Result> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId)
        {
            EmpEducationDetails educationDetail = new EmpEducationDetails()
            {
                UserId = userId,
                EducationTitle = educationDetails.EducationTitle,
                CollegeName = educationDetails.CollegeName,
                StartDate = educationDetails.StartDate,
                EndDate = educationDetails.EndDate,
                Type = educationDetails.Type,
            };
            return await _educationDetailsRepo.AddOne(educationDetail);
        }

        public async Task<Result> EditEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId)
        {
            Result result = new();
            Expression<Func<EmpEducationDetails, bool>> whereCondition = x => x.UserId == userId && x.EducationId == educationDetails.EducationId;
            EmpEducationDetails? check = await _educationDetailsRepo.FirstOrDefault(whereCondition);
            if (check == null) return result;
            EmpEducationDetails data = new EmpEducationDetails()
            {
                EducationId = educationDetails.EducationId,
                UserId = userId,
                EducationTitle = educationDetails.EducationTitle,
                CollegeName = educationDetails.CollegeName,
                EndDate = educationDetails.EndDate,
                StartDate = educationDetails.StartDate,
                Type = educationDetails.Type
            };
            result = await _educationDetailsRepo.Update(whereCondition, data);
            return result;
        }

        public async Task<Result> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId)
        {
            EmpCertificationDetails certificatinDetail = new EmpCertificationDetails()
            {
                UserId = userId,
                CertificationTitle = cerificationDetails.CertificationTitle,
                OrganisationName = cerificationDetails.OrganisationName,
                StartDate = cerificationDetails.StartDate,
                EndDate = cerificationDetails.EndDate,
                Mode = cerificationDetails.Mode,
            };
            return await _certificationDetailsRepo.AddOne(certificatinDetail);
        }

        public async Task<Result> EditEmployeeCertification(EmployeeCertificationRequestModel certificationDetails, string userId)
        {
            Result result = new();
            Expression<Func<EmpCertificationDetails, bool>> whereCondition = x => x.UserId == userId && x.CertificationId == certificationDetails.CertificationId;
            EmpCertificationDetails? check = await _certificationDetailsRepo.FirstOrDefault(whereCondition);
            if (check == null) return result;
            EmpCertificationDetails data = new EmpCertificationDetails()
            {
                CertificationId = certificationDetails.CertificationId,
                UserId = userId,
                CertificationTitle = certificationDetails.CertificationTitle,
                OrganisationName = certificationDetails.OrganisationName,
                EndDate = certificationDetails.EndDate,
                StartDate = certificationDetails.StartDate,
                Mode = certificationDetails.Mode
            };
            result = await _certificationDetailsRepo.Update(whereCondition, data);
            return result;
        }

        public async Task<List<EmpEducationDetails>> GetEmployeeEducationDetails(string userId)
        {
            IEnumerable<EmpEducationDetails> list = await _educationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmpEducationDetails>>(list);
        }

        public async Task<List<EmpCertificationDetails>> GetEmployeeCertificationDetails(string userId)
        {
            IEnumerable<EmpCertificationDetails> list = await _certificationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmpCertificationDetails>>(list);
        }

        public async Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId)
        {
            EmpSummary summary = await _employeeSummaryRepo.FirstOrDefault(x => x.UserId == userId);
            if (summary == null)
            {
                return new EmployeeSummaryRequestModel()
                {
                    Summary = ""
                };
            }
            return new EmployeeSummaryRequestModel()
            {
                SummaryId = summary.Id,
                Summary = summary.Summary
            };
        }

        public async Task<EmployeeSkillsDTO> GetEmployeeSkills(string userId)
        {
            EmployeeSkillsDTO EmpSkills = new();
            EmpSkills? employeeSkills = await _employeeSkillsRepository.FirstOrDefault(x => x.UserId == userId);
            if (employeeSkills == null)
            {
                EmpSkills.Skills = [];
            }
            else
            {
                IEnumerable<Skills> skills = await _skillsRepository.GetAll(x => employeeSkills.Skills.Contains(x.Id));
                EmpSkills.Id = employeeSkills.Id;
                EmpSkills.Skills = _mapper.Map<List<SkillsDTO>>(skills);
                EmpSkills.UserId = employeeSkills.UserId;
            }
            return EmpSkills;
        }

        public async Task<string> GetUserExistingProfile(string userId)
        {
            EmpUser? res = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
            return res?.ProfileUrl ?? string.Empty;
        }
        public async Task<string> AddUserProfileImage(string fileName, string userId, string filePath)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == userId;
            EmpUser profile = await _employeeRepository.FirstOrDefault(whereCondition);
            if (profile == null)
            {
                return "User Not Found";
            }
            profile.ProfileUrl = fileName;

            Result res = await _employeeRepository.Update(whereCondition, profile);
            string fullProfileUrl = Common.GetEmployeeImageUrl(fileName) ?? "";
            return fullProfileUrl;
        }

        public async Task<Result> DeleteEducationDetails(string educationId, string userId)
        {
            Expression<Func<EmpEducationDetails, bool>> wherCondition = x => x.EducationId == educationId && x.UserId == userId;
            Result data = await _educationDetailsRepo.Delete(wherCondition);
            return data;
        }

        public async Task<Result> DeleteCertificationDetails(string certificationId, string userId)
        {
            Expression<Func<EmpCertificationDetails, bool>> whereCondition = x => x.CertificationId == certificationId && x.UserId == userId;
            Result data = await _certificationDetailsRepo.Delete(whereCondition);
            return data;
        }

        public async Task<Result> DeleteEmployee(string employeeId)
        {
            //Update for Education Details
            Expression<Func<EmpCertificationDetails, bool>> certificateWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpCertificationDetails> empCerUpdateDefinition = Builders<EmpCertificationDetails>.Update;
            UpdateDefinition<EmpCertificationDetails> empCerUpdate = empCerUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _certificationDetailsRepo.UpdateMany(certificateWhereCondition, empCerUpdate);
            //Update for Education Details
            Expression<Func<EmpEducationDetails, bool>> educationWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpEducationDetails> empEduUpdateDefinition = Builders<EmpEducationDetails>.Update;
            UpdateDefinition<EmpEducationDetails> empEduUpdate = empEduUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _educationDetailsRepo.UpdateMany(educationWhereCondition, empEduUpdate);
            //Update for Employee Skills
            Expression<Func<EmpSkills, bool>> skillsWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpSkills> empSkillUpdateDefinition = Builders<EmpSkills>.Update;
            UpdateDefinition<EmpSkills> empSkillUpdate = empSkillUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _employeeSkillsRepository.UpdateMany(skillsWhereCondition, empSkillUpdate);


            Expression<Func<EmpSummary, bool>> summaryWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpSummary> empSumUpdateDefinition = Builders<EmpSummary>.Update;
            UpdateDefinition<EmpSummary> empSumUpdate = empSumUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _employeeSummaryRepo.UpdateMany(summaryWhereCondition, empSumUpdate);



            Expression<Func<EmpUser, bool>> employeeWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpUser> empUpdateDefinition = Builders<EmpUser>.Update;
            UpdateDefinition<EmpUser> empUpdate = empUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            Result data = await _employeeRepository.UpdateMany(employeeWhereCondition, empUpdate);

            return data;
        }

        public async Task<bool> IsUserActive(string userId)
        {
            bool IsUserActive = await _employeeRepository.Exist(x => x.UserId == userId && x.Status);
            return IsUserActive;
        }

        public async Task<Result> AddSkill(string skill)
        {
            var exist = await _skillsRepository.Exist(x => x.Name.Equals(skill, StringComparison.OrdinalIgnoreCase));
            if (exist)
            {
                return new Result()
                {
                    StatusCode = CustomStatusCode.SkillsAlreadyExist,
                    Success = false,
                };
            }
            Skills skills = new()
            {
                Name = skill,
            };
            return await _skillsRepository.AddOne(skills);
        }

        public async Task<Result<Skills>> GetSuggestedSkills(string query)
        {
            var skills = await _skillsRepository.GetAll(x => x.Name.Contains(query));
            return new Result<Skills>()
            {
                MethodResults = skills == null ? [] : [.. skills],
                StatusCode = 200,
                TotalRecords = skills == null ? 0 : skills.Count(),
                Success = true,
            };
        }

        public async Task<Result> AddUpdateWorkHistory(EmployeeWorkHistoryModel model)
        {
            EmpWorkHistory empWorkHistory = _mapper.Map<EmpWorkHistory>(model);
            Result result = new();
            string currentUser = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrEmpty(empWorkHistory.WorkHistoryId))
            {
                empWorkHistory.CreatedDate = DateTime.UtcNow;
                empWorkHistory.CreatedBy = currentUser;
                result = await _empWorkHistoryRepository.AddOne(empWorkHistory);
            }
            else
            {
                Expression<Func<EmpWorkHistory, bool>> whereCondition = x => x.UserId == model.UserId && x.WorkHistoryId == empWorkHistory.WorkHistoryId;
                EmpWorkHistory? empWorkHistoryExist = await _empWorkHistoryRepository.FirstOrDefault(whereCondition);
                if (empWorkHistoryExist is null)
                {
                    return result;
                }
                empWorkHistory.UpdatedBy = currentUser;
                empWorkHistory.UpdatedDate = DateTime.UtcNow;
                empWorkHistory.CreatedBy = empWorkHistoryExist.CreatedBy;
                empWorkHistory.CreatedDate = empWorkHistoryExist.CreatedDate;
                result = await _empWorkHistoryRepository.Update(whereCondition, empWorkHistory);
            }
            return result;
        }

        public async Task<List<EmployeeWorkHistoryModel>> GetEmpWorkHistory(string userId)
        {
            var list = (await _empWorkHistoryRepository.GetAll(x => x.UserId.Equals(userId))).OrderByDescending(x => x.EndDate);
            if (list.Any()) return _mapper.Map<List<EmployeeWorkHistoryModel>>(list);
            return [];
        }

        private int GetEmployeeTotalExperience(string userId)
        {
            int totalWorkExperienceInMonths = 0;
            IEnumerable<EmpWorkHistory> empWorkHistories = _empWorkHistoryRepository.GetAll(w => w.UserId == userId).Result;
            if (!empWorkHistories.Any()) return totalWorkExperienceInMonths;

            // sort work history by start date
            var sortedList = empWorkHistories.OrderBy(wh => wh.StartDate).ToList();
            List<(DateTime Start, DateTime End)> mergedDates = new List<(DateTime, DateTime)>();
            var currentStart = sortedList[0].StartDate;
            var currentEnd = sortedList[0].EndDate ?? DateTime.UtcNow;

            foreach (var period in sortedList.Skip(1))
            {
                DateTime periodEnd = period.EndDate ?? DateTime.UtcNow;
                if (period.StartDate <= currentEnd)
                {
                    period.EndDate = period.EndDate == null ? DateTime.UtcNow : period.EndDate;
                    currentEnd = periodEnd > currentEnd ? periodEnd : currentEnd;
                }
                else
                {
                    mergedDates.Add((currentStart, currentEnd));
                    currentStart = period.StartDate;
                    currentEnd = periodEnd;
                }
            }
            mergedDates.Add((currentStart, currentEnd));

            // Calculate total months
            foreach (var range in mergedDates)
            {
                int months = ((range.End.Year - range.Start.Year) * 12) + (range.End.Month - range.Start.Month);
                if (range.End.Day >= range.Start.Day) months++;
                totalWorkExperienceInMonths += months;
            }
            return totalWorkExperienceInMonths;
        }

        public async Task<Result> DeleteWorkHistory(string workId, string userId)
        {
            bool exits = await _empWorkHistoryRepository.Exist(x => x.WorkHistoryId.Equals(workId));
            if (!exits) return new Result();
            Expression<Func<EmpWorkHistory, bool>> whereCondition = x => x.WorkHistoryId == workId;
            return await _empWorkHistoryRepository.UpdateMany(whereCondition, Builders<EmpWorkHistory>.Update.Set(x => x.IsDeleted, true));
        }
        public async Task<NotificationResponseModel> GetAllNotifications(NotificationRequestDTO model, string userId)
        {
            Expression<Func<UserNotifications, bool>> wherecondition = x => x.UserId == userId;
            int userNotificationsCount = await _userNotificationRepository.Count(wherecondition);
            if (userNotificationsCount == 0)
            {
                return new NotificationResponseModel()
                {
                    NotificationList = [],
                    All = 0,
                    Unread = 0
                };
            }
            if (model.Type != "all")
            {
                wherecondition = x => x.UserId == userId && !x.IsRead;
            }
            int unReadNotificationCount = await _userNotificationRepository.Count(x => x.UserId == userId && !x.IsRead);
            List<UserNotifications> userNotifications = (await _userNotificationRepository.GetAggregateDataAsync<UserNotifications>(wherecondition, isAscending: false, orderedKey: "CreatedDateTime", pageNo: model.PageNo, pageSize: model.Records)).ToList();
            string[] notificationsId = userNotifications.Select(x => x.NotificationId).ToArray();
            IEnumerable<Notifications> notifications = await _notificationsRepository.GetAll(x => notificationsId.Contains(x.NotificationId));
            var data = (from usrNft in userNotifications
                        join ntf in notifications on usrNft.NotificationId equals ntf.NotificationId
                        select new NotificationViewModel
                        {
                            UserNotificationId = usrNft.UserNotificationId,
                            IsRead = usrNft.IsRead,
                            Title = ntf.Title,
                            Body = ntf.Body,
                            TargetId = ntf.TargetId,
                            SentDateTime = usrNft.CreatedDateTime,
                            SentBy = ntf.CreatedBy,
                            NotificationTypes = ntf.NotificationType

                        }).ToList();
            return new NotificationResponseModel()
            {
                NotificationList = data,
                All = userNotificationsCount,
                Unread = unReadNotificationCount,
            };
        }

        public async Task<Result> MarkNotificationAsRead(string userId, string userNotificationId)
        {
            bool isExist = await _userNotificationRepository.Exist(x => x.UserNotificationId == userNotificationId);
            if (!isExist)
            {
                return new Result();
            }
            Expression<Func<UserNotifications, bool>> whereCondition = x => x.UserNotificationId == userNotificationId;
            return await _userNotificationRepository.UpdateMany(whereCondition, Builders<UserNotifications>.Update.Set(x => x.IsRead, true));
        }

        public async Task<Result> MarkAllNotificationAsRead(string userId)
        {
            Expression<Func<UserNotifications, bool>> whereCondition = x => x.UserId == userId && !x.IsRead;
            return await _userNotificationRepository.UpdateMany(whereCondition, Builders<UserNotifications>.Update.Set(x => x.IsRead, true));
        }

        public async Task<Result> RemoveProfileImage(string userId)
        {
            Result result = new();
            EmpUser? empUser = await _employeeRepository.FirstOrDefault(e => e.UserId == userId);
            if (empUser is null) return result;
            if (empUser.ProfileUrl != null)
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "ProfileImage");
                string oldPath = Path.Combine(uploadFolder, empUser.ProfileUrl);
                FileInfo fileInfo = new(oldPath);
                fileInfo.Delete();
                Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == userId;
                return await _employeeRepository.UpdateMany(whereCondition, Builders<EmpUser>.Update.Set(x => x.ProfileUrl, null));
            }
            else
            {
                return result;
            }
        }

        public async Task<List<string>> GetCollegeNameSuggestions(string searchValue)
        {
            Expression<Func<EmpEducationDetails, bool>> expression = e => e.CollegeName.ToLower().Contains(searchValue.ToLower());
            List<string> collegeList = (await _educationDetailsRepo.GetAll(expression, true, false)).Select(e => e.CollegeName).Distinct().ToList();
            return collegeList;
        }

        // Get a list of employee
        public async Task SendBirthDayAndAnniversaryNotificationToEmployees()
        {
            var currentDate = DateTime.UtcNow.Date;
            var currentMonthDay = currentDate.ToString("MM-dd");

            Expression<Func<EmpUser, bool>> expression = emp =>
                !string.IsNullOrEmpty(emp.DateOfBirth)
                && emp.DateOfBirth.Substring(5, 5) == currentMonthDay;

            IEnumerable<EmpUser> birthDayEmployeeList = await _employeeRepository.GetAll(expression, withDefaultFilter: false);
            IEnumerable<EmpUser> EmployeeAnniversaryList = await _employeeRepository.GetAll(emp => !string.IsNullOrEmpty(emp.DateOfJoining) && emp.DateOfJoining.Substring(5, 5) == currentMonthDay, withDefaultFilter: false);

            var notificationTasks = new List<Task>();
            List<UserNotifications> userNotifications = [];
            //  check if there are employees having birthday today
            if (birthDayEmployeeList.Any())
            {
                foreach (EmpUser employee in birthDayEmployeeList)
                {
                    Expression<Func<EmpUser, bool>> exp = emp => emp.CompanyId == employee.CompanyId && emp.UserId != employee.UserId && emp.Status;
                    List<EmpUser> allEmployees = _employeeRepository.Get(exp).ToList();
                    List<EmpUser> targetEmployeeList = new();
                    foreach (var emp in allEmployees)
                    {
                        if (await _middlewareService.IsUserNotificationPreferenceEnabled(emp.UserId, EnumsHelper.NotificationPreferenceType.BirthdayNotification))
                        {
                            targetEmployeeList.Add(emp);
                        }
                    }
                    Notifications notification = new()
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        CreatedDateTime = DateTime.UtcNow,
                        NotificationType = EnumsHelper.NotificationTypes.BirthDay,
                        Body = $"{employee.FirstName} {employee.LastName}"
                    };
                    await _notificationsRepository.AddOne(notification);
                    foreach (var emp in targetEmployeeList)
                    {
                        UserNotifications userNotification = new()
                        {
                            UserNotificationId = Guid.NewGuid().ToString(),
                            UserId = emp.UserId,
                            NotificationId = notification.NotificationId,
                            IsRead = false,
                            CreatedDateTime = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        userNotifications.Add(userNotification);
                        notificationTasks.Add(_notificationService.SendNotificationToUser(emp.UserId, new NotificationViewModel()
                        {
                            Title = notification.Title,
                            Body = notification.Body,
                            IsRead = false,
                            SentDateTime = DateTime.UtcNow,
                            TargetId = notification.TargetId,
                            NotificationTypes = notification.NotificationType,
                            UserNotificationId = userNotification.UserNotificationId
                        }));
                    }
                }
            }
            if (EmployeeAnniversaryList.Any())
            {
                foreach (EmpUser employee in EmployeeAnniversaryList)
                {
                    Expression<Func<EmpUser, bool>> exp = emp => emp.CompanyId == employee.CompanyId && emp.UserId != employee.UserId && emp.Status;
                    List<EmpUser> allEmployees = _employeeRepository.Get(exp).ToList();
                    List<EmpUser> targetEmployeeList = new();
                    foreach (var emp in allEmployees)
                    {
                        if (await _middlewareService.IsUserNotificationPreferenceEnabled(emp.UserId, EnumsHelper.NotificationPreferenceType.WorkAnniversaries))
                        {
                            targetEmployeeList.Add(emp);
                        }
                    }
                    Notifications notification = new()
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        CreatedDateTime = DateTime.UtcNow,
                        NotificationType = EnumsHelper.NotificationTypes.WorkAnniversary,
                        Body = $"{employee.FirstName} {employee.LastName}"
                    };
                    await _notificationsRepository.AddOne(notification);
                    foreach (var emp in targetEmployeeList)
                    {
                        UserNotifications userNotification = new()
                        {
                            UserNotificationId = Guid.NewGuid().ToString(),
                            UserId = emp.UserId,
                            NotificationId = notification.NotificationId,
                            IsRead = false,
                            CreatedDateTime = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        userNotifications.Add(userNotification);
                        notificationTasks.Add(_notificationService.SendNotificationToUser(emp.UserId, new NotificationViewModel()
                        {
                            Title = notification.Title,
                            Body = notification.Body,
                            IsRead = false,
                            SentDateTime = DateTime.UtcNow,
                            TargetId = notification.TargetId,
                            NotificationTypes = notification.NotificationType,
                            UserNotificationId = userNotification.UserNotificationId
                        }));
                    }
                }
            }

            if (userNotifications.Count > 0)
            {
                // insert all user notification into db
                // process all notificatios task
                await Task.WhenAll(notificationTasks);
                await _userNotificationRepository.AddMany(userNotifications);
            }
        }

        // notification preference service methods
        public async Task<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> GetNotificationPreferences(string userId)
        {
            Dictionary<EnumsHelper.NotificationPreferenceType, bool> result = new();
            var data = await _notificationPreferenceRepository.FirstOrDefault(n => n.UserId == userId);
            if (data == null)
            {
                result = _middlewareService.GetDefaultNotificationPreferences();
            }
            else
            {
                result = data.Preferences;
            }
            return result;
        }
        public async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>> UpdateNotificationPreferences(string userId, Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences)
        {
            Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> result = new();
            Expression<Func<NotificationPreference, bool>> whereCondition = n => n.UserId == userId;
            var existingPreferences = await _notificationPreferenceRepository.FirstOrDefault(whereCondition);
            if (existingPreferences == null)
            {
                NotificationPreference newPreference = new()
                {
                    UserId = userId,
                    Preferences = preferences,
                };
                await _notificationPreferenceRepository.AddOne(newPreference);
            }
            await _notificationPreferenceRepository.UpdateMany(whereCondition, Builders<NotificationPreference>.Update.Set(n => n.Preferences, preferences));
            result.MethodResult = await GetNotificationPreferences(userId);
            return result;
        }

        public async Task<Result> GetNextEmployeeId(string companyId)
        {
            if (string.IsNullOrWhiteSpace(companyId))
            {
                return new Result { Message = "Company is required." };
            }

            Company? company = await _companyRepository.FirstOrDefault(c => c.CompanyId == companyId);
            return new Result
            {
                Success = true,
                Message = company?.AutoGenerateEmployeeId == false
                    ? null
                    : await GenerateNextEmployeeId(companyId)
            };
        }

        private async Task<string> GenerateNextEmployeeId(string companyId)
        {
            var sequence = await EnsureEmployeeIdSequence(companyId);
            return FormatEmployeeId(sequence.Prefix, sequence.NextNumber);
        }

        public async Task<Result<EmployeeIdSequenceResponseDto>> GetEmployeeIdSequence(string companyId)
        {
            if (string.IsNullOrWhiteSpace(companyId)) return new() { Message = "Company is required." };
            var sequence = await EnsureEmployeeIdSequence(companyId);
            return SequenceResult(sequence);
        }

        public async Task<Result<EmployeeIdSequenceResponseDto>> SkipEmployeeIds(string companyId, string actorUserId, int skipCount)
        {
            if (skipCount <= 0) return new() { Message = "Skip count must be greater than 0." };
            var sequence = await EnsureEmployeeIdSequence(companyId);
            var filter = Builders<EmployeeIdSequence>.Filter.Eq(x => x.SequenceId, sequence.SequenceId);
            var update = Builders<EmployeeIdSequence>.Update.Inc(x => x.NextNumber, skipCount)
                .Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, DateTime.UtcNow);
            sequence = await _employeeIdSequenceRepository.GetCollection().FindOneAndUpdateAsync(filter, update,
                new FindOneAndUpdateOptions<EmployeeIdSequence> { ReturnDocument = ReturnDocument.After });
            var result = SequenceResult(sequence);
            result.Message = $"{skipCount} Employee IDs skipped successfully. Next Employee ID is {result.MethodResult!.CurrentNextEmployeeId}.";
            return result;
        }

        public async Task<Result<EmployeeIdSequenceResponseDto>> StartEmployeeIdSequence(string companyId, string actorUserId, string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId)) return new() { Message = "Employee ID is required." };
            var sequence = await EnsureEmployeeIdSequence(companyId);
            var candidate = employeeId.Trim().ToUpperInvariant();
            var expression = new Regex($"^{Regex.Escape(sequence.Prefix)}(?<number>\\d{{4}})$", RegexOptions.CultureInvariant);
            var match = expression.Match(candidate);
            if (!match.Success) return new() { Message = $"Employee ID must follow the format {FormatEmployeeId(sequence.Prefix, 1)}." };
            var number = int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);
            if (number <= 0) return new() { Message = "Employee ID number must be greater than 0." };
            if (await _employeeRepository.Exist(x => x.CompanyId == companyId && x.EmployeeId.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
                return new() { Message = $"Employee ID {candidate} is already assigned. Please enter another starting ID." };

            var filter = Builders<EmployeeIdSequence>.Filter.Eq(x => x.SequenceId, sequence.SequenceId);
            var update = Builders<EmployeeIdSequence>.Update.Set(x => x.NextNumber, number)
                .Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, DateTime.UtcNow);
            sequence = await _employeeIdSequenceRepository.GetCollection().FindOneAndUpdateAsync(filter, update,
                new FindOneAndUpdateOptions<EmployeeIdSequence> { ReturnDocument = ReturnDocument.After });
            var result = SequenceResult(sequence);
            result.Message = $"Employee ID sequence updated. Automatic generation will continue from {candidate}.";
            return result;
        }

        public async Task<Result<AssignMissingEmployeeIdResponseDto>> AssignMissingEmployeeId(string companyId, string actorUserId, string employeeUserId, string? manualEmployeeId)
        {
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(employeeUserId))
                return new() { Message = "Employee and company are required." };

            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employeeUserId && !x.IsDeleted);
            if (employee is null) return new() { Message = "Employee was not found in your company." };
            if (!string.IsNullOrWhiteSpace(employee.EmployeeId)) return new() { Message = "This employee already has an Employee ID assigned." };

            var company = await _companyRepository.FirstOrDefault(x => x.CompanyId == companyId);
            var automatic = company?.AutoGenerateEmployeeId != false;
            var sequence = await EnsureEmployeeIdSequence(companyId);
            string employeeId;

            if (automatic)
            {
                if (!string.IsNullOrWhiteSpace(manualEmployeeId))
                    return new() { Message = "Manual Employee ID assignment is not enabled for this company." };
                employeeId = await ReserveNextEmployeeId(companyId);
            }
            else
            {
                employeeId = manualEmployeeId?.Trim().ToUpperInvariant() ?? string.Empty;
                var expression = new Regex($"^{Regex.Escape(sequence.Prefix)}\\d{{4}}$", RegexOptions.CultureInvariant);
                if (!expression.IsMatch(employeeId)) return new() { Message = $"Employee ID must follow the format {FormatEmployeeId(sequence.Prefix, 0)}." };
                if (await _employeeRepository.Exist(x => x.CompanyId == companyId && x.EmployeeId.Equals(employeeId, StringComparison.OrdinalIgnoreCase)))
                    return new() { Message = $"Employee ID {employeeId} is already assigned." };
            }

            var filter = Builders<EmpUser>.Filter.Eq(x => x.CompanyId, companyId)
                & Builders<EmpUser>.Filter.Eq(x => x.UserId, employeeUserId)
                & Builders<EmpUser>.Filter.Or(Builders<EmpUser>.Filter.Eq(x => x.EmployeeId, ""), Builders<EmpUser>.Filter.Eq(x => x.EmployeeId, null));
            var update = Builders<EmpUser>.Update.Set(x => x.EmployeeId, employeeId).Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, DateTime.UtcNow);

            try
            {
                var assigned = await _employeeRepository.GetCollection().FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<EmpUser> { ReturnDocument = ReturnDocument.After });
                if (assigned is null) return new() { Message = "This employee already has an Employee ID assigned." };
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return new() { Message = $"Employee ID {employeeId} is already assigned." };
            }

            _logger.LogInformation("Employee ID assigned. CompanyId: {CompanyId}; EmployeeUserId: {EmployeeUserId}; EmployeeId: {EmployeeId}; AssignedBy: {ActorUserId}", companyId, employeeUserId, employeeId, actorUserId);
            return new()
            {
                Success = true,
                Message = "Employee ID assigned successfully.",
                MethodResult = new AssignMissingEmployeeIdResponseDto { EmployeeId = employeeId, ManualEntryAllowed = !automatic, Format = FormatEmployeeId(sequence.Prefix, 0) }
            };
        }

        public async Task<string> ReserveNextEmployeeId(string companyId)
        {
            var sequence = await EnsureEmployeeIdSequence(companyId);
            var updated = await _employeeIdSequenceRepository.GetCollection().FindOneAndUpdateAsync(
                Builders<EmployeeIdSequence>.Filter.Eq(x => x.SequenceId, sequence.SequenceId),
                Builders<EmployeeIdSequence>.Update.Inc(x => x.NextNumber, 1).Set(x => x.UpdatedDate, DateTime.UtcNow),
                new FindOneAndUpdateOptions<EmployeeIdSequence> { ReturnDocument = ReturnDocument.Before });
            return FormatEmployeeId(updated.Prefix, updated.NextNumber);
        }

        private async Task<EmployeeIdSequence> EnsureEmployeeIdSequence(string companyId)
        {
            var existing = await _employeeIdSequenceRepository.FirstOrDefault(x => x.CompanyId == companyId);
            var company = await _companyRepository.FirstOrDefault(c => c.CompanyId == companyId);
            var prefix = BuildEmployeeIdPrefix(company);
            if (existing != null)
            {
                if (string.Equals(existing.Prefix, prefix, StringComparison.OrdinalIgnoreCase)) return existing;

                // The company setting is the source of truth for future IDs. Preserve the
                // counter while also accounting for any IDs already using the new prefix.
                var highestUsingConfiguredPrefix = (await _employeeRepository.GetAll(x => x.CompanyId == companyId)).Select(x => x.EmployeeId)
                    .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(x => int.TryParse(x[prefix.Length..], out var number) ? number : 0).DefaultIfEmpty(0).Max();
                var nextNumber = Math.Max(existing.NextNumber, highestUsingConfiguredPrefix + 1);
                var filter = Builders<EmployeeIdSequence>.Filter.Eq(x => x.SequenceId, existing.SequenceId);
                var update = Builders<EmployeeIdSequence>.Update
                    .Set(x => x.Prefix, prefix)
                    .Set(x => x.NextNumber, nextNumber)
                    .Set(x => x.UpdatedDate, DateTime.UtcNow);
                return await _employeeIdSequenceRepository.GetCollection().FindOneAndUpdateAsync(filter, update,
                    new FindOneAndUpdateOptions<EmployeeIdSequence> { ReturnDocument = ReturnDocument.After }) ?? existing;
            }
            var highest = (await _employeeRepository.GetAll(x => x.CompanyId == companyId)).Select(x => x.EmployeeId)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(x => int.TryParse(x[prefix.Length..], out var number) ? number : 0).DefaultIfEmpty(0).Max();
            var sequence = new EmployeeIdSequence { SequenceId = companyId, CompanyId = companyId, Prefix = prefix, NextNumber = highest + 1 };
            try { await _employeeIdSequenceRepository.AddOne(sequence); }
            catch (MongoWriteException) { }
            return await _employeeIdSequenceRepository.FirstOrDefault(x => x.CompanyId == companyId) ?? sequence;
        }

        private static string BuildEmployeeIdPrefix(Company? company)
        {
            var code = new string((company?.EmployeeIdPrefix ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code)) code = string.Concat((company?.CompanyName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word[0])).ToUpperInvariant();
            return $"{(string.IsNullOrWhiteSpace(code) ? "EMP" : code)}-";
        }

        private static string FormatEmployeeId(string prefix, int number) => $"{prefix}{number:D4}";
        private static Result<EmployeeIdSequenceResponseDto> SequenceResult(EmployeeIdSequence sequence) => new()
        {
            Success = true,
            MethodResult = new EmployeeIdSequenceResponseDto { CurrentNextEmployeeId = FormatEmployeeId(sequence.Prefix, sequence.NextNumber), Format = FormatEmployeeId(sequence.Prefix, 1) }
        };
        public async Task<Result> ResendInviteLink(string userId, string currentUserId)
        {
            Result result = new();
            EmpUser? empUser = await _employeeRepository.FirstOrDefault(e => e.UserId == userId && e.Status);
            if (empUser == null)
            {
                result.StatusCode = CustomStatusCode.EmployeeNotExist;
                return result;
            }
            if (empUser.IsEmailVerified)
            {
                result.StatusCode = CustomStatusCode.EmpAlreadyVerified;
                return result;
            }

            Expression<Func<UserSecurityToken, bool>> expression = t => t.UserId == empUser.UserId && t.Type == EnumsHelper.SecurityTokenType.Invite && t.Expiry > DateTime.UtcNow && t.IsUsed == false;
            int activeTokenCount = await _userSecurityTokenRepository.Count(expression);
            if (activeTokenCount > 0)
            {
                await _userSecurityTokenRepository.UpdateMany(expression, Builders<UserSecurityToken>.Update.Set(t => t.IsUsed, true));
            }
            await SendInvitationLink(currentUserId, empUser);
            result.Success = true;
            return result;
        }
    }
}
