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
using Codeji.CMS.Repository.Entities.Leave;
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
        readonly IMongoDbRepository<EmployeeExit> _employeeExitRepository;
        readonly IMongoDbRepository<EmployeeExitPolicy>? _employeeExitPolicyRepository;
        readonly IMongoDbRepository<EmployeeExitClearanceTemplate>? _employeeExitClearanceTemplateRepository;
        readonly IMongoDbRepository<EmployeeFinalSettlement>? _employeeFinalSettlementRepository;
        readonly IMongoDbRepository<EmpPayRoll>? _employeePayrollRepository;
        readonly IMongoDbRepository<LeaveRequest>? _leaveRequestRepository;
        readonly IMongoDbRepository<WorkFromHomeRequest>? _workFromHomeRequestRepository;
        readonly IMongoDbRepository<EmployeeExitAttendanceCutoff>? _employeeExitAttendanceCutoffRepository;
        readonly IMongoDbRepository<EmployeeExitOutbox>? _employeeExitOutboxRepository;
        readonly IMongoDbRepository<RefreshToken>? _refreshTokenRepository;
        readonly IMongoClient? _mongoClient;

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
            ILogger<EmployeeService> logger,
            IMongoDbRepository<EmployeeExit>? employeeExitRepository = null,
            IMongoDbRepository<RefreshToken>? refreshTokenRepository = null,
            IMongoDbRepository<EmployeeExitPolicy>? employeeExitPolicyRepository = null
            , IMongoDbRepository<EmployeeExitClearanceTemplate>? employeeExitClearanceTemplateRepository = null
            , IMongoDbRepository<EmployeeFinalSettlement>? employeeFinalSettlementRepository = null
            , IMongoDbRepository<EmpPayRoll>? employeePayrollRepository = null
            , IMongoDbRepository<LeaveRequest>? leaveRequestRepository = null
            , IMongoDbRepository<WorkFromHomeRequest>? workFromHomeRequestRepository = null
            , IMongoDbRepository<EmployeeExitAttendanceCutoff>? employeeExitAttendanceCutoffRepository = null
            , IMongoDbRepository<EmployeeExitOutbox>? employeeExitOutboxRepository = null
            , IMongoClient? mongoClient = null
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
            _employeeExitRepository = employeeExitRepository!;
            _refreshTokenRepository = refreshTokenRepository;
            _employeeExitPolicyRepository = employeeExitPolicyRepository;
            _employeeExitClearanceTemplateRepository = employeeExitClearanceTemplateRepository;
            _employeeFinalSettlementRepository = employeeFinalSettlementRepository;
            _employeePayrollRepository = employeePayrollRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _workFromHomeRequestRepository = workFromHomeRequestRepository;
            _employeeExitAttendanceCutoffRepository = employeeExitAttendanceCutoffRepository;
            _employeeExitOutboxRepository = employeeExitOutboxRepository;
            _mongoClient = mongoClient;
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
            // Employee creation must remain successful if a non-critical
            // notification or invitation delivery dependency is unavailable.
            try
            {
                var notificationPreferences = new NotificationPreference()
                {
                    UserId = userId,
                    Preferences = _middlewareService.GetDefaultNotificationPreferences(),
                };
                await _notificationPreferenceRepository.AddOne(notificationPreferences);
                await SendInvitationLink(currentUserId, employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Employee {UserId} was created, but invitation setup failed.", userId);
            }
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
                employeeList = (await _employeeRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && (x.EmploymentStatus == null || x.EmploymentStatus == "" || x.EmploymentStatus == "Active"))).ToList();
                totalRecords = employeeList.Count;
            }
            else
            {
                var requestedEmploymentStatus = filters.EmploymentStatus;
                var includeAllStatuses = requestedEmploymentStatus == "All";
                var includeNoticePeriod = requestedEmploymentStatus == "NoticePeriod";
                var includeExEmployees = requestedEmploymentStatus == "ExEmployees";
                Expression<Func<EmpUser, bool>> whereCondition = x =>
                x.CompanyId == companyId &&
                !x.IsDeleted &&
                (includeAllStatuses ||
                 (includeNoticePeriod && x.EmploymentStatus == "NoticePeriod") ||
                 (includeExEmployees && (x.EmploymentStatus == "Resigned" || x.EmploymentStatus == "Terminated")) ||
                 (!includeNoticePeriod && !includeExEmployees && (x.EmploymentStatus == null || x.EmploymentStatus == "" || x.EmploymentStatus == "Active"))) &&
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
                            EmploymentStatus = NormalizeEmploymentStatus(emp),
                            LastWorkingDate = emp.LastWorkingDate,
                        }).ToList();
            return new Result<GetAllEmployeeResponseModel>()
            {
                Success = true,
                MethodResults = data,
                TotalRecords = totalRecords
            };
        }

        public async Task<Result> InitiateEmployeeExit(InitiateEmployeeExitRequest request)
            => await InitiateEmployeeExitCore(request, false);

        public async Task<Result> SubmitOwnResignation(InitiateEmployeeExitRequest request)
            => await InitiateEmployeeExitCore(request, true);

        private async Task<Result> InitiateEmployeeExitCore(InitiateEmployeeExitRequest request, bool isSelfSubmission)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeUserId)
                || request.ExitType is not ("Resigned" or "Terminated") || request.LastWorkingDate == default)
                return new Result { Success = false, Message = "EXIT_REQUEST_INVALID" };
            if (isSelfSubmission && request.EmployeeUserId != actorUserId)
                return new Result { Success = false, Message = "SELF_EXIT_ONLY" };
            if (!isSelfSubmission && request.EmployeeUserId == actorUserId)
                return new Result { Success = false, Message = "SELF_EXIT_NOT_ALLOWED" };
            if (isSelfSubmission && request.ExitType != "Resigned")
                return new Result { Success = false, Message = "SELF_SUBMISSION_MUST_BE_RESIGNATION" };

            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            if (NormalizeEmploymentStatus(employee) is "Resigned" or "Terminated" or "NoticePeriod")
                return new Result { Success = false, Message = "EXIT_ALREADY_OPEN_OR_COMPLETED" };
            if (DateTime.TryParse(employee.DateOfJoining, out var joined) && request.LastWorkingDate.Date < joined.Date)
                return new Result { Success = false, Message = "EXIT_BEFORE_JOINING" };
            if (isSelfSubmission && !await MeetsNoticePolicy(employee, request.LastWorkingDate.Date))
                return new Result { Success = false, Message = "EXIT_NOTICE_PERIOD_NOT_MET" };

            var lastWorkingDate = request.LastWorkingDate.Date;
            // Employee resignations must be reviewed before they affect employment;
            // HR-initiated terminations enter notice immediately.
            var status = isSelfSubmission ? "PendingManagerApproval" : "NoticePeriod";
            var exit = new EmployeeExit
            {
                CompanyId = companyId, CreatedBy = actorUserId, EmployeeUserId = employee.UserId,
                ExitType = request.ExitType, Status = status, InitiatedAtUtc = DateTime.UtcNow,
                ReportingManagerUserId = employee.ReportingManager,
                LastWorkingDate = lastWorkingDate, Reason = request.Reason?.Trim(), Notes = request.Notes?.Trim(),
                AuditHistory = [new EmployeeExitAuditEvent { Action = isSelfSubmission ? "ResignationSubmitted" : "ExitInitiated", ActorUserId = actorUserId, Notes = request.Notes?.Trim() }]
            };
            var create = await _employeeExitRepository.AddOne(exit);
            if (!create.Success) return create;
            employee.EmploymentStatus = status == "NoticePeriod" ? status : "Active"; employee.LastWorkingDate = lastWorkingDate;
            employee.ExitDate = lastWorkingDate.ToString("yyyy-MM-dd"); employee.Status = true;
            employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> updateEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted;
            return await _employeeRepository.Update(updateEmployee, employee);
        }

        public async Task<Result> ReviewEmployeeExit(ReviewEmployeeExitRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || request.Action is not ("Approve" or "Reject") || string.IsNullOrWhiteSpace(request.EmployeeExitId))
                return new Result { Success = false, Message = "EXIT_REVIEW_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (exit.EmployeeUserId == actorUserId) return new Result { Success = false, Message = "SELF_EXIT_REVIEW_FORBIDDEN" };
            var actor = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
            var isHr = actor is not null && (await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.Administrator, companyId)
                || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HR, companyId)
                || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HRExecutive, companyId));
            var isAssignedManager = exit.Status == "PendingManagerApproval" && exit.ReportingManagerUserId == actorUserId;
            if (!isHr && !isAssignedManager) return new Result { Success = false, Message = "EXIT_REVIEW_FORBIDDEN" };
            if (exit.Status is not ("PendingManagerApproval" or "PendingHrApproval")) return new Result { Success = false, Message = "EXIT_REVIEW_NOT_ALLOWED_IN_STATE" };
            exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.Version++;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.Action, ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            if (request.Action == "Reject") { exit.Status = "Rejected"; exit.IsActive = false; exit.RejectedAtUtc = DateTime.UtcNow; exit.RejectionReason = request.Notes?.Trim(); }
            else if (exit.Status == "PendingManagerApproval" && !isHr) exit.Status = "PendingHrApproval";
            else
            {
                exit.Status = "NoticePeriod"; exit.ApprovedByUserId = actorUserId; exit.ApprovedAtUtc = DateTime.UtcNow;
                await PopulateExitClearances(exit);
            }
            Expression<Func<EmployeeExit, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
            return await _employeeExitRepository.Update(update, exit);
        }

        public Task<Result> WithdrawOwnEmployeeExit(CancelEmployeeExitRequest request) =>
            CloseEmployeeExit(request, "Withdrawn", true);

        public Task<Result> CancelEmployeeExit(CancelEmployeeExitRequest request) =>
            CloseEmployeeExit(request, "Cancelled", false);

        private async Task<Result> CloseEmployeeExit(CancelEmployeeExitRequest request, string status, bool selfOnly)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId)) return new Result { Success = false, Message = "EXIT_CANCEL_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (selfOnly && exit.EmployeeUserId != actorUserId) return new Result { Success = false, Message = "EXIT_WITHDRAW_FORBIDDEN" };
            if (!selfOnly)
            {
                var actor = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
                var isHr = actor is not null && (await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.Administrator, companyId)
                    || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HR, companyId)
                    || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HRExecutive, companyId));
                if (!isHr) return new Result { Success = false, Message = "EXIT_CANCEL_FORBIDDEN" };
            }
            if (selfOnly && exit.Status is not ("PendingManagerApproval" or "PendingHrApproval")) return new Result { Success = false, Message = "EXIT_WITHDRAW_NOT_ALLOWED_IN_STATE" };
            exit.Status = status; exit.IsActive = false; exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = status, ActorUserId = actorUserId, Notes = request.Reason?.Trim() });
            Expression<Func<EmployeeExit, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
            return await _employeeExitRepository.Update(update, exit);
        }

        private async Task<bool> IsHrExitActor(string companyId, string actorUserId)
        {
            var actor = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
            return actor is not null && (await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.Administrator, companyId)
                || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HR, companyId)
                || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HRExecutive, companyId));
        }

        public async Task<Result> UpdateEmployeeExitDate(UpdateEmployeeExitDateRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_OVERRIDE_FORBIDDEN" };
            if (request is null || request.LastWorkingDate == default) return new Result { Success = false, Message = "EXIT_DATE_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || exit.Status is not ("PendingManagerApproval" or "PendingHrApproval" or "NoticePeriod")) return new Result { Success = false, Message = "EXIT_DATE_CHANGE_NOT_ALLOWED" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null || DateTime.TryParse(employee.DateOfJoining, out var joined) && request.LastWorkingDate.Date < joined.Date) return new Result { Success = false, Message = "EXIT_BEFORE_JOINING" };
            if (!request.OverrideNoticePolicy && !await MeetsNoticePolicy(employee, request.LastWorkingDate.Date))
                return new Result { Success = false, Message = "EXIT_NOTICE_PERIOD_NOT_MET" };
            if (request.OverrideNoticePolicy && string.IsNullOrWhiteSpace(request.Reason))
                return new Result { Success = false, Message = "EXIT_NOTICE_OVERRIDE_REASON_REQUIRED" };
            exit.LastWorkingDate = request.LastWorkingDate.Date; exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.OverrideNoticePolicy ? "LastWorkingDateNoticeOverride" : "LastWorkingDateUpdated", ActorUserId = actorUserId, Notes = request.Reason?.Trim() });
            Expression<Func<EmployeeExit, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
            var result = await _employeeExitRepository.Update(update, exit);
            if (!result.Success) return result;
            employee.LastWorkingDate = exit.LastWorkingDate; employee.ExitDate = exit.LastWorkingDate.ToString("yyyy-MM-dd"); employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> updateEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted;
            return await _employeeRepository.Update(updateEmployee, employee);
        }

        public async Task<Result> UpsertEmployeeExitPolicy(UpsertEmployeeExitPolicyRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeExitPolicyRepository is null || request is null || request.MinimumNoticeDays < 0 || (!string.IsNullOrWhiteSpace(request.DepartmentId) && !string.IsNullOrWhiteSpace(request.RoleId)))
                return new Result { Success = false, Message = "EXIT_POLICY_INVALID" };
            var actor = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
            var isHr = actor is not null && (await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.Administrator, companyId) || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HR, companyId) || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HRExecutive, companyId));
            if (!isHr) return new Result { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };
            var existing = string.IsNullOrWhiteSpace(request.EmployeeExitPolicyId) ? null : await _employeeExitPolicyRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitPolicyId == request.EmployeeExitPolicyId && !x.IsDeleted);
            var policy = existing ?? new EmployeeExitPolicy { CompanyId = companyId, CreatedBy = actorUserId };
            policy.DepartmentId = request.DepartmentId?.Trim(); policy.RoleId = request.RoleId?.Trim(); policy.MinimumNoticeDays = request.MinimumNoticeDays; policy.IsActive = request.IsActive; policy.UpdatedBy = actorUserId; policy.UpdatedDate = DateTime.UtcNow;
            if (existing is null) return await _employeeExitPolicyRepository.AddOne(policy);
            Expression<Func<EmployeeExitPolicy, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitPolicyId == policy.EmployeeExitPolicyId && !x.IsDeleted;
            return await _employeeExitPolicyRepository.Update(update, policy);
        }

        public async Task<Result<EmployeeExitPolicy>> GetEmployeeExitPolicy(string policyId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            if (_employeeExitPolicyRepository is null || string.IsNullOrWhiteSpace(policyId)) return new Result<EmployeeExitPolicy> { Success = false, Message = "EXIT_POLICY_NOT_FOUND" };
            var policy = await _employeeExitPolicyRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitPolicyId == policyId && !x.IsDeleted);
            return policy is null ? new Result<EmployeeExitPolicy> { Success = false, Message = "EXIT_POLICY_NOT_FOUND" } : new Result<EmployeeExitPolicy> { Success = true, MethodResult = policy };
        }

        public async Task<Result<EmployeeExitClearanceTemplate>> GetEmployeeExitClearanceTemplate(string templateId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            if (_employeeExitClearanceTemplateRepository is null || string.IsNullOrWhiteSpace(templateId)) return new Result<EmployeeExitClearanceTemplate> { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_NOT_FOUND" };
            var template = await _employeeExitClearanceTemplateRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == templateId && !x.IsDeleted);
            return template is null ? new Result<EmployeeExitClearanceTemplate> { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_NOT_FOUND" } : new Result<EmployeeExitClearanceTemplate> { Success = true, MethodResult = template };
        }

        public async Task<Result<EmployeeExitPolicy>> GetEmployeeExitPolicies()
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var policies = _employeeExitPolicyRepository is null ? [] : (await _employeeExitPolicyRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
            return new Result<EmployeeExitPolicy> { Success = true, MethodResults = policies };
        }

        public async Task<Result<EmployeeExitClearanceTemplate>> GetEmployeeExitClearanceTemplates()
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var templates = _employeeExitClearanceTemplateRepository is null ? [] : (await _employeeExitClearanceTemplateRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
            return new Result<EmployeeExitClearanceTemplate> { Success = true, MethodResults = templates };
        }

        public async Task<Result> UpsertEmployeeExitClearanceTemplate(UpsertEmployeeExitClearanceTemplateRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeExitClearanceTemplateRepository is null || request is null || string.IsNullOrWhiteSpace(request.Category) || request.DueDaysBeforeLastWorkingDate < 0)
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_INVALID" };
            var actor = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
            var isHr = actor is not null && (await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.Administrator, companyId) || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HR, companyId) || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HRExecutive, companyId));
            if (!isHr) return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_FORBIDDEN" };
            var existing = string.IsNullOrWhiteSpace(request.EmployeeExitClearanceTemplateId) ? null : await _employeeExitClearanceTemplateRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == request.EmployeeExitClearanceTemplateId && !x.IsDeleted);
            var template = existing ?? new EmployeeExitClearanceTemplate { CompanyId = companyId, CreatedBy = actorUserId };
            template.Category = request.Category.Trim(); template.OwnerRole = request.OwnerRole?.Trim(); template.IsMandatory = request.IsMandatory; template.DueDaysBeforeLastWorkingDate = request.DueDaysBeforeLastWorkingDate; template.IsActive = request.IsActive; template.UpdatedBy = actorUserId; template.UpdatedDate = DateTime.UtcNow;
            if (existing is null) return await _employeeExitClearanceTemplateRepository.AddOne(template);
            Expression<Func<EmployeeExitClearanceTemplate, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == template.EmployeeExitClearanceTemplateId && !x.IsDeleted;
            return await _employeeExitClearanceTemplateRepository.Update(update, template);
        }

        public async Task<Result> ApproveEmployeeFinalSettlement(ApproveEmployeeFinalSettlementRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeFinalSettlementRepository is null || request is null) return new Result { Success = false, Message = "FINAL_SETTLEMENT_INVALID" };
            var actor = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
            var isHr = actor is not null && (await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.Administrator, companyId) || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HR, companyId) || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HRExecutive, companyId));
            if (!isHr) return new Result { Success = false, Message = "FINAL_SETTLEMENT_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var settlement = await _employeeFinalSettlementRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && !x.IsDeleted)
                ?? new EmployeeFinalSettlement { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = exit.EmployeeUserId };
            settlement.Status = "Approved"; settlement.ApprovedByUserId = actorUserId; settlement.ApprovedAtUtc = DateTime.UtcNow; settlement.UpdatedBy = actorUserId; settlement.UpdatedDate = DateTime.UtcNow;
            var saved = string.IsNullOrWhiteSpace(settlement.EmployeeFinalSettlementId) ? await _employeeFinalSettlementRepository.AddOne(settlement) : await _employeeFinalSettlementRepository.Update(Builders<EmployeeFinalSettlement>.Filter.Eq(x => x.EmployeeFinalSettlementId, settlement.EmployeeFinalSettlementId), settlement);
            if (!saved.Success) return saved;
            exit.Version++; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FinalSettlementApproved", ActorUserId = actorUserId });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version), exit);
        }

        public async Task<Result> CompleteEmployeeExitClearance(CompleteEmployeeExitClearanceRequest request, bool waive)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_CLEARANCE_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.ClearanceId)) return new Result { Success = false, Message = "EXIT_CLEARANCE_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var clearance = exit.Clearances.SingleOrDefault(x => x.ClearanceId == request.ClearanceId);
            if (clearance is null || clearance.Status != "Pending") return new Result { Success = false, Message = "EXIT_CLEARANCE_NOT_ACTIONABLE" };
            if (waive && string.IsNullOrWhiteSpace(request.Notes)) return new Result { Success = false, Message = "EXIT_CLEARANCE_WAIVER_REASON_REQUIRED" };
            clearance.Status = waive ? "Waived" : "Completed"; clearance.EvidenceUrl = request.EvidenceUrl?.Trim(); clearance.OverrideReason = waive ? request.Notes?.Trim() : null; clearance.CompletedByUserId = actorUserId; clearance.CompletedAtUtc = DateTime.UtcNow;
            exit.Version++; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = waive ? "ClearanceWaived" : "ClearanceCompleted", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> RejoinEmployee(RejoinEmployeeRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "REJOIN_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.Reason)) return new Result { Success = false, Message = "REJOIN_REASON_REQUIRED" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || exit.Status is not ("Resigned" or "Terminated")) return new Result { Success = false, Message = "REJOIN_NOT_ALLOWED" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            employee.Status = true; employee.EmploymentStatus = "Active"; employee.LastWorkingDate = null; employee.ExitDate = null; employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> updateEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && !x.Status;
            var result = await _employeeRepository.Update(updateEmployee, employee);
            if (!result.Success) return result;
            exit.Version++; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "Rejoined", ActorUserId = actorUserId, Notes = request.Reason.Trim() });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version), exit);
        }

        public async Task<Result> ExceptionalCompleteEmployeeExit(ExceptionalCompleteEmployeeExitRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_EXCEPTION_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.Reason)) return new Result { Success = false, Message = "EXIT_EXCEPTION_REASON_REQUIRED" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || DateTime.UtcNow.Date < exit.LastWorkingDate.Date) return new Result { Success = false, Message = "EXIT_EXCEPTION_NOT_ALLOWED" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            exit.Status = exit.ExitType; exit.IsActive = false; exit.CompletedAtUtc = DateTime.UtcNow; exit.Version++; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ExceptionalCompletion", ActorUserId = actorUserId, Notes = request.Reason.Trim() });
            var saved = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
            if (!saved.Success) return saved;
            employee.Status = false; employee.EmploymentStatus = exit.ExitType; employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            return await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Status), employee);
        }

        public async Task<Result<EmployeeExitCaseDto>> GetEmployeeExitCase(string employeeUserId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(employeeUserId)) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_REQUEST_INVALID" };
            // A caller may always see their own case. Other cases retain the controller's
            // Employees.View authorization when this endpoint is later surfaced to managers.
            if (employeeUserId != actorUserId) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeUserId == employeeUserId && !x.IsDeleted);
            if (exit is null) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_NOT_FOUND" };
            return new Result<EmployeeExitCaseDto> { Success = true, MethodResult = new EmployeeExitCaseDto
            {
                EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = exit.EmployeeUserId, ExitType = exit.ExitType, Status = exit.Status,
                LastWorkingDate = exit.LastWorkingDate, Reason = exit.Reason, Notes = exit.Notes, InitiatedAtUtc = exit.InitiatedAtUtc, CompletedAtUtc = exit.CompletedAtUtc, Version = exit.Version,
                Clearances = exit.Clearances.Select(x => new EmployeeExitClearanceDto { ClearanceId = x.ClearanceId, Category = x.Category, Status = x.Status, IsMandatory = x.IsMandatory, DueAtUtc = x.DueAtUtc }).ToList(),
                AuditHistory = exit.AuditHistory.Select(x => new EmployeeExitAuditEventDto { Action = x.Action, ActorUserId = x.ActorUserId, AtUtc = x.AtUtc, Notes = x.Notes }).ToList(),
                PermittedActions = exit.Status is "PendingManagerApproval" or "PendingHrApproval" ? ["Withdraw"] : []
            }};
        }

        public async Task<Result<EmployeeExitCaseDto>> GetEmployeeExitCaseForManagement(string employeeUserId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeUserId == employeeUserId && !x.IsDeleted);
            if (exit is null) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_NOT_FOUND" };
            if (exit.EmployeeUserId == actorUserId) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_FORBIDDEN" };
            var actor = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
            var isHr = actor is not null && (await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.Administrator, companyId)
                || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HR, companyId)
                || await _roleService.IsRoleTypeMatch(actor.RoleId, EnumsHelper.Roles.HRExecutive, companyId));
            var isAssignedManager = exit.Status == "PendingManagerApproval" && exit.ReportingManagerUserId == actorUserId;
            if (!isHr && !isAssignedManager) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_FORBIDDEN" };
            var actions = new List<string>();
            if (isAssignedManager) actions.AddRange(["Approve", "Reject"]);
            if (isHr)
            {
                if (exit.Status is "PendingManagerApproval" or "PendingHrApproval") actions.AddRange(["Approve", "Reject", "Cancel"]);
                if (exit.Status == "NoticePeriod") actions.AddRange(["UpdateLastWorkingDate", "Complete", "ExceptionalComplete"]);
                if (exit.IsActive) actions.Add("WaiveClearance");
                if (exit.Status is "Resigned" or "Terminated") actions.Add("Rejoin");
            }
            return new Result<EmployeeExitCaseDto> { Success = true, MethodResult = new EmployeeExitCaseDto
            {
                EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = exit.EmployeeUserId, ExitType = exit.ExitType, Status = exit.Status, Version = exit.Version,
                LastWorkingDate = exit.LastWorkingDate, Reason = exit.Reason, Notes = exit.Notes, InitiatedAtUtc = exit.InitiatedAtUtc, CompletedAtUtc = exit.CompletedAtUtc,
                Clearances = exit.Clearances.Select(x => new EmployeeExitClearanceDto { ClearanceId = x.ClearanceId, Category = x.Category, Status = x.Status, IsMandatory = x.IsMandatory, DueAtUtc = x.DueAtUtc }).ToList(),
                AuditHistory = exit.AuditHistory.Select(x => new EmployeeExitAuditEventDto { Action = x.Action, ActorUserId = x.ActorUserId, AtUtc = x.AtUtc, Notes = x.Notes }).ToList(),
                PermittedActions = actions
            }};
        }

        public async Task<Result> CompleteEmployeeExit(CompleteEmployeeExitRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_COMPLETION_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeUserId)) return new Result { Success = false, Message = "EXIT_REQUEST_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeUserId == request.EmployeeUserId && x.Status == "NoticePeriod" && !x.IsDeleted);
            if (exit is null) return new Result { Success = false, Message = "OPEN_EXIT_NOT_FOUND" };
            if (DateTime.UtcNow.Date < exit.LastWorkingDate.Date) return new Result { Success = false, Message = "LAST_WORKING_DATE_NOT_REACHED" };
            if (exit.Clearances.Any(x => x.IsMandatory && x.Status is not ("Completed" or "Waived"))) return new Result { Success = false, Message = "MANDATORY_CLEARANCE_PENDING" };
            if (_employeeFinalSettlementRepository is not null)
            {
                var settlement = await _employeeFinalSettlementRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Status == "Approved" && !x.IsDeleted);
                if (settlement is null) return new Result { Success = false, Message = "FINAL_SETTLEMENT_PENDING" };
            }
            if (_employeePayrollRepository is not null)
            {
                var payroll = await _employeePayrollRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && x.PayMonth.Month == exit.LastWorkingDate.Month && x.PayMonth.Year == exit.LastWorkingDate.Year && x.IsProcessed);
                if (payroll is null) return new Result { Success = false, Message = "FINAL_PAYROLL_PENDING" };
            }
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            var completedAt = DateTime.UtcNow;
            exit.Status = exit.ExitType; exit.CompletedAtUtc = completedAt; exit.Notes = request.Notes?.Trim() ?? exit.Notes; exit.UpdatedBy = actorUserId; exit.UpdatedDate = completedAt; exit.Version++;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ExitCompleted", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            Expression<Func<EmployeeExit, bool>> updateExit = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Status == "NoticePeriod" && !x.IsDeleted;
            exit.IsActive = false;
            employee.EmploymentStatus = exit.ExitType; employee.Status = false; employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> completeEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Status;
            var outboxEvent = new EmployeeExitOutbox
            {
                CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId,
                EventType = "EmployeeExitCompleted", RecipientUserId = exit.ReportingManagerUserId ?? actorUserId,
                Title = "Employee exit completed", Body = $"The offboarding of {employee.FirstName} {employee.LastName} has been completed.", NextAttemptAtUtc = completedAt
            };
            var cutoff = new EmployeeExitAttendanceCutoff { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = employee.UserId, LastWorkingDate = exit.LastWorkingDate.Date, LockedAtUtc = completedAt, LockedByUserId = actorUserId };
            var committedTransactionally = false;
            if (_mongoClient is not null)
            {
                try
                {
                    using var session = await _mongoClient.StartSessionAsync();
                    await session.WithTransactionAsync(async (_, cancellationToken) =>
                    {
                        var exitWrite = await _employeeExitRepository.GetCollection().ReplaceOneAsync(session, Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Status == "NoticePeriod" && x.IsActive && !x.IsDeleted), exit, cancellationToken: cancellationToken);
                        if (exitWrite.MatchedCount != 1) throw new InvalidOperationException("EXIT_CASE_CHANGED_OR_NOT_FOUND");
                        var employeeWrite = await _employeeRepository.GetCollection().ReplaceOneAsync(session, Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Status && !x.IsDeleted), employee, cancellationToken: cancellationToken);
                        if (employeeWrite.MatchedCount != 1) throw new InvalidOperationException("EMPLOYEE_COMPLETION_FAILED");
                        if (_refreshTokenRepository is not null) await _refreshTokenRepository.GetCollection().UpdateManyAsync(session, Builders<RefreshToken>.Filter.Where(x => x.UserId == employee.UserId && !x.IsRevoked), Builders<RefreshToken>.Update.Set(x => x.IsRevoked, true).Set(x => x.RevokedAt, completedAt), cancellationToken: cancellationToken);
                        if (_employeeExitOutboxRepository is not null) await _employeeExitOutboxRepository.GetCollection().InsertOneAsync(session, outboxEvent, cancellationToken: cancellationToken);
                        if (_employeeExitAttendanceCutoffRepository is not null) await _employeeExitAttendanceCutoffRepository.GetCollection().InsertOneAsync(session, cutoff, cancellationToken: cancellationToken);
                        if (_leaveRequestRepository is not null) await _leaveRequestRepository.GetCollection().UpdateManyAsync(session, Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId && x.EndDate > exit.LastWorkingDate && (x.Status == EnumsHelper.LeaveRequestStatus.Pending || x.Status == EnumsHelper.LeaveRequestStatus.Accepted)), Builders<LeaveRequest>.Update.Set(x => x.Status, EnumsHelper.LeaveRequestStatus.WithDrawn).Set(x => x.Comment, "Cancelled by employee exit").Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, completedAt), cancellationToken: cancellationToken);
                        if (_workFromHomeRequestRepository is not null) await _workFromHomeRequestRepository.GetCollection().UpdateManyAsync(session, Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.ToDate > exit.LastWorkingDate && (x.Status == "Pending" || x.Status == "Approved")), Builders<WorkFromHomeRequest>.Update.Set(x => x.Status, "Cancelled").Set(x => x.RevokedByUserId, actorUserId).Set(x => x.RevokedAt, completedAt), cancellationToken: cancellationToken);
                        return true;
                    });
                    committedTransactionally = true;
                }
                catch (MongoCommandException ex) when (ex.Code == 20) { }
                catch (InvalidOperationException ex) { return new Result { Success = false, Message = ex.Message }; }
                catch (Exception) { return new Result { Success = false, Message = "EXIT_COMPLETION_TRANSACTION_FAILED" }; }
            }
            if (!committedTransactionally)
            {
                var exitResult = await _employeeExitRepository.Update(updateExit, exit);
                if (!exitResult.Success) return exitResult;
            }
            var completion = committedTransactionally ? new Result { Success = true } : await _employeeRepository.Update(completeEmployee, employee);
            if (completion.Success && _refreshTokenRepository is not null)
            {
                Expression<Func<RefreshToken, bool>> activeSessions = x => x.UserId == employee.UserId && !x.IsRevoked;
                await _refreshTokenRepository.UpdateMany(activeSessions, Builders<RefreshToken>.Update
                    .Set(x => x.IsRevoked, true)
                    .Set(x => x.RevokedAt, DateTime.UtcNow));
            }
            if (completion.Success && !committedTransactionally)
            {
                if (_employeeExitOutboxRepository is not null)
                    await _employeeExitOutboxRepository.AddOne(outboxEvent);
                if (_employeeExitAttendanceCutoffRepository is not null)
                {
                    var existingCutoff = await _employeeExitAttendanceCutoffRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && !x.IsDeleted);
                    if (existingCutoff is null)
                        await _employeeExitAttendanceCutoffRepository.AddOne(cutoff);
                }
                if (_leaveRequestRepository is not null)
                    await _leaveRequestRepository.UpdateMany(Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId && x.EndDate > exit.LastWorkingDate && (x.Status == EnumsHelper.LeaveRequestStatus.Pending || x.Status == EnumsHelper.LeaveRequestStatus.Accepted)),
                        Builders<LeaveRequest>.Update.Set(x => x.Status, EnumsHelper.LeaveRequestStatus.WithDrawn).Set(x => x.Comment, "Cancelled by employee exit").Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, DateTime.UtcNow));
                if (_workFromHomeRequestRepository is not null)
                    await _workFromHomeRequestRepository.UpdateMany(Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.ToDate > exit.LastWorkingDate && (x.Status == "Pending" || x.Status == "Approved")),
                        Builders<WorkFromHomeRequest>.Update.Set(x => x.Status, "Cancelled").Set(x => x.RevokedByUserId, actorUserId).Set(x => x.RevokedAt, DateTime.UtcNow));
            }
            return completion;
        }

        private static string NormalizeEmploymentStatus(EmpUser employee) => employee.EmploymentStatus switch { "NoticePeriod" or "Resigned" or "Terminated" => employee.EmploymentStatus, _ => "Active" };

        private async Task<bool> MeetsNoticePolicy(EmpUser employee, DateTime lastWorkingDate)
        {
            if (_employeeExitPolicyRepository is null) return true;
            var policies = await _employeeExitPolicyRepository.GetAll(x => x.CompanyId == employee.CompanyId && x.IsActive && !x.IsDeleted);
            var policy = policies.FirstOrDefault(x => x.DepartmentId == employee.Department)
                ?? policies.FirstOrDefault(x => x.RoleId == employee.RoleId)
                ?? policies.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.DepartmentId) && string.IsNullOrWhiteSpace(x.RoleId));
            return policy is null || lastWorkingDate >= DateTime.UtcNow.Date.AddDays(policy.MinimumNoticeDays);
        }

        private async Task PopulateExitClearances(EmployeeExit exit)
        {
            if (_employeeExitClearanceTemplateRepository is null || exit.Clearances.Count > 0) return;
            var templates = await _employeeExitClearanceTemplateRepository.GetAll(x => x.CompanyId == exit.CompanyId && x.IsActive && !x.IsDeleted);
            exit.Clearances = templates.Select(template => new EmployeeExitClearance
            {
                Category = template.Category,
                OwnerRole = template.OwnerRole,
                OwnerUserId = template.OwnerRole == "ReportingManager" ? exit.ReportingManagerUserId ?? string.Empty : string.Empty,
                IsMandatory = template.IsMandatory,
                DueAtUtc = exit.LastWorkingDate.Date.AddDays(-template.DueDaysBeforeLastWorkingDate)
            }).ToList();
            if (exit.Clearances.Count > 0)
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ClearancesGenerated", ActorUserId = CurrentContext.UserId(_httpContextAccessor) });
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
