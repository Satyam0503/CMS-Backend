using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Services.PayRoll;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
        readonly IMongoDbRepository<PayrollRun>? _payrollRunsRepository;
        readonly IMongoDbRepository<PayrollRunEmployee>? _payrollRunEmployeesRepository;
        readonly IMongoDbRepository<LeaveRequest>? _leaveRequestRepository;
        readonly IMongoDbRepository<WorkFromHomeRequest>? _workFromHomeRequestRepository;
        readonly IMongoDbRepository<EmployeeExitAttendanceCutoff>? _employeeExitAttendanceCutoffRepository;
        readonly IMongoDbRepository<EmployeeExitOutbox>? _employeeExitOutboxRepository;
        readonly IMongoDbRepository<FormerEmployeeAccessToken>? _formerEmployeeAccessTokenRepository;
        readonly IMongoDbRepository<RefreshToken>? _refreshTokenRepository;
        readonly IMongoClient? _mongoClient;
        readonly IEmployeeExitEvidenceStorage? _employeeExitEvidenceStorage;
        readonly IEmployeeLifecyclePolicyService? _employeeLifecyclePolicyService;
        readonly IAttendanceInitializationService? _attendanceInitializationService;

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
            , IMongoDbRepository<PayrollRun>? payrollRunsRepository = null
            , IMongoDbRepository<PayrollRunEmployee>? payrollRunEmployeesRepository = null
            , IMongoDbRepository<LeaveRequest>? leaveRequestRepository = null
            , IMongoDbRepository<WorkFromHomeRequest>? workFromHomeRequestRepository = null
            , IMongoDbRepository<EmployeeExitAttendanceCutoff>? employeeExitAttendanceCutoffRepository = null
            , IMongoDbRepository<EmployeeExitOutbox>? employeeExitOutboxRepository = null
            , IMongoDbRepository<FormerEmployeeAccessToken>? formerEmployeeAccessTokenRepository = null
            , IMongoClient? mongoClient = null
            , IEmployeeExitEvidenceStorage? employeeExitEvidenceStorage = null
            , IEmployeeLifecyclePolicyService? employeeLifecyclePolicyService = null
            , IAttendanceInitializationService? attendanceInitializationService = null
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
            _payrollRunsRepository = payrollRunsRepository;
            _payrollRunEmployeesRepository = payrollRunEmployeesRepository;
            _leaveRequestRepository = leaveRequestRepository;
            _workFromHomeRequestRepository = workFromHomeRequestRepository;
            _employeeExitAttendanceCutoffRepository = employeeExitAttendanceCutoffRepository;
            _employeeExitOutboxRepository = employeeExitOutboxRepository;
            _formerEmployeeAccessTokenRepository = formerEmployeeAccessTokenRepository;
            _mongoClient = mongoClient;
            _employeeExitEvidenceStorage = employeeExitEvidenceStorage;
            _employeeLifecyclePolicyService = employeeLifecyclePolicyService;
            _attendanceInitializationService = attendanceInitializationService;
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
            string effectiveDoj = !string.IsNullOrWhiteSpace(model.DateOfJoining)
                ? model.DateOfJoining
                : DateTime.UtcNow.ToString("yyyy-MM-dd");

            EmpUser employee = new EmpUser()
            {
                UserId = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                EmailNormalized = model.Email.ToLowerInvariant(),
                EmployeeId = model.EmployeeId,
                CompanyId = companyId,
                RoleId = model.RoleId ?? string.Empty,
                Department = model.DepartmentId ?? string.Empty,
                JobRole = model.JobRoleId ?? string.Empty,
                Gender = model.Gender ?? string.Empty,
                ReportingManager = model.ReportingManager ?? string.Empty,
                DateOfJoining = effectiveDoj,
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

                if (_attendanceInitializationService != null)
                {
                    try
                    {
                        await _attendanceInitializationService.InitializeEmployeeAttendanceAsync(
                            companyId, currentUserId, employee.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to initialize attendance for new employee {UserId}", employee.UserId);
                    }
                }
            }
            // Employee creation must remain successful if a non-critical
            // notification or invitation delivery dependency is unavailable.
            try
            {
               
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
                string dateOfJoining = item.DateOfJoining?.Trim() ?? string.Empty;
                int rowNumber = i + 1;

                BulkImportEmployeeRowResultDto rowResult = new()
                {
                    RowNumber = rowNumber,
                    EmpId = empId,
                    Email = email
                };

                if (string.IsNullOrWhiteSpace(empId) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email)
                    || string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(departmentName) || string.IsNullOrWhiteSpace(jobRoleName)
                    || string.IsNullOrWhiteSpace(dateOfJoining))
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Required fields are missing.";
                    response.Results.Add(rowResult);
                    continue;
                }

                string[] dojFormats = ["yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd", "d MMMM yyyy", "d MMM yyyy", "dd MMM yyyy", "dd MMMM yyyy"];
                if (!DateTime.TryParseExact(dateOfJoining, dojFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDoj)
                    && !DateTime.TryParse(dateOfJoining, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDoj)
                    && !DateTime.TryParse(dateOfJoining, out parsedDoj))
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = "Invalid Date of Joining format.";
                    response.Results.Add(rowResult);
                    continue;
                }
                string normalizedDoj = parsedDoj.ToString("yyyy-MM-dd");

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
                            DepartmentId = null,
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

                    InviteEmployeeDto inviteModel = new()
                    {
                        EmployeeId = empId,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        RoleId = role.RolesId,
                        DepartmentId = department.DepartmentId,
                        JobRoleId = jobTitle.JobTitleId,
                        Gender = gender,
                        DateOfJoining = normalizedDoj
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
            if (!string.IsNullOrWhiteSpace(user.DateOfJoining))
            {
                string[] formats = ["yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd", "d MMMM yyyy", "d MMM yyyy", "dd MMM yyyy", "dd MMMM yyyy"];
                if (DateTime.TryParseExact(user.DateOfJoining.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDoj) ||
                    DateTime.TryParse(user.DateOfJoining, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDoj))
                {
                    user.DateOfJoining = parsedDoj.ToString("yyyy-MM-dd");
                }
            }

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

            if (!string.IsNullOrWhiteSpace(user.DateOfJoining) && _attendanceInitializationService != null)
            {
                try
                {
                    var actorUserId = CurrentContext.UserId(_httpContextAccessor) ?? userId;
                    await _attendanceInitializationService.InitializeEmployeeAttendanceAsync(
                        companyId, actorUserId, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize attendance after updating DateOfJoining for user {UserId}", userId);
                }
            }

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
            if (user == null) return null!;
            UserModel userModel = _mapper.Map<UserModel>(user);
            userModel.EmploymentStatus = user.Lifecycle?.EmploymentStatus ?? userModel.EmploymentStatus;
            userModel.AvailabilityStatus = user.Lifecycle?.AvailabilityStatus ?? EmployeeLifecycleStatuses.Working;
            userModel.AccountStatus = user.Lifecycle?.AccountStatus;
            userModel.PayrollStatus = user.Lifecycle?.PayrollStatus;
            userModel.AlumniStatus = user.Lifecycle?.AlumniStatus;
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
                employeeList = (await _employeeRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted &&
                    (x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended ||
                     x.EmploymentStatus == null || x.EmploymentStatus == "" || x.EmploymentStatus == "Active"))).ToList();
                totalRecords = employeeList.Count;
            }
            else
            {
                var requestedEmploymentStatus = filters.EmploymentStatus;
                var includeAllStatuses = requestedEmploymentStatus == "All";
                var includeAttendanceRoster = requestedEmploymentStatus == "AttendanceRoster";
                var includeNoticePeriod = requestedEmploymentStatus == "NoticePeriod";
                var includeExEmployees = requestedEmploymentStatus == "ExEmployees";
                var requestedAvailabilityStatus = filters.AvailabilityStatus;
                Expression<Func<EmpUser, bool>> whereCondition = x =>
                x.CompanyId == companyId &&
                !x.IsDeleted &&
                (includeAllStatuses ||
                 (includeAttendanceRoster && ((x.Lifecycle == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod) || x.EmploymentStatus == null || x.EmploymentStatus == "" || x.EmploymentStatus == "Active" || x.EmploymentStatus == "NoticePeriod")) ||
                 (includeNoticePeriod && ((x.Lifecycle != null && x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod) || x.EmploymentStatus == "NoticePeriod")) ||
                 (includeExEmployees && ((x.Lifecycle != null && EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Lifecycle.EmploymentStatus)) || EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.EmploymentStatus))) ||
                 (!includeAttendanceRoster && !includeNoticePeriod && !includeExEmployees && ((x.Lifecycle == null || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Active || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.NoticePeriod || x.Lifecycle.EmploymentStatus == EmployeeLifecycleStatuses.Suspended) || x.EmploymentStatus == null || x.EmploymentStatus == "" || x.EmploymentStatus == "Active"))) &&
                (string.IsNullOrWhiteSpace(requestedAvailabilityStatus) || (x.Lifecycle != null && x.Lifecycle.AvailabilityStatus == requestedAvailabilityStatus)) &&
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
                            AvailabilityStatus = emp.Lifecycle is null ? EmployeeLifecycleStatuses.Working : EmploymentPeriod.ResolveAvailabilityOn(emp, DateTime.UtcNow),
                            AccountStatus = emp.Lifecycle?.AccountStatus ?? (emp.Status ? EmployeeLifecycleStatuses.AccountActive : EmployeeLifecycleStatuses.AccountClosed),
                            PayrollStatus = emp.Lifecycle?.PayrollStatus ?? (emp.Status ? EmployeeLifecycleStatuses.PayrollActive : EmployeeLifecycleStatuses.PayrollClosed),
                            OffboardingStatus = emp.Lifecycle?.OffboardingStatus ?? "None",
                            FormerPortalStatus = emp.Lifecycle?.FormerPortalStatus ?? "NotEligible",
                            AlumniStatus = emp.Lifecycle?.AlumniStatus ?? EmployeeLifecycleStatuses.AlumniNotApplicable,
                            LifecycleVersion = emp.Lifecycle?.Version ?? 0,
                        }).ToList();
            return new Result<GetAllEmployeeResponseModel>()
            {
                Success = true,
                MethodResults = data,
                TotalRecords = totalRecords
            };
        }

        public async Task<Result> ChangeEmployeeAvailability(ChangeEmployeeAvailabilityRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId))
                return new Result { Success = false, Message = "LIFECYCLE_CONTEXT_REQUIRED" };
            if (!await _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding,
                [Codeji.CMS.Utility.Constraints.Permission.CaseManage, Codeji.CMS.Utility.Constraints.Permission.Edit, Codeji.CMS.Utility.Constraints.Permission.Override],
                actorUserId, companyId))
                return new Result { Success = false, Message = "LIFECYCLE_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeUserId))
                return new Result { Success = false, Message = "LIFECYCLE_EMPLOYEE_REQUIRED" };

            var employee = await _employeeRepository.FirstOrDefault(x =>
                x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            var requestedAvailability = request.AvailabilityStatus?.Trim() ?? string.Empty;
            var lifecycleLeaveError = await ValidateLeaveDrivenAvailabilityAsync(requestedAvailability, request.EmployeeUserId, request.EffectiveFromUtc, companyId);
            if (lifecycleLeaveError is not null) return new Result { Success = false, Message = lifecycleLeaveError };

            if (_employeeLifecyclePolicyService is not null)
            {
                var policyError = await _employeeLifecyclePolicyService.ValidateAvailabilityAsync(
                    requestedAvailability,
                    request.ExpectedReturnDate,
                    request.Reason?.Trim(),
                    companyId);
                if (policyError is not null) return new Result { Success = false, Message = policyError };
            }

            var state = employee.Lifecycle ?? new EmployeeLifecycleState();
            if (!EmployeeLifecycleTransitionService.TryChangeAvailability(
                state, requestedAvailability, actorUserId,
                request.Version, request.EffectiveFromUtc == default ? DateTime.UtcNow : request.EffectiveFromUtc,
                request.ExpectedReturnDate, request.ReasonCode?.Trim(), request.Reason?.Trim(), out var error))
                return new Result { Success = false, Message = error };

            employee.Lifecycle = state;
            employee.UpdatedBy = actorUserId;
            employee.UpdatedDate = DateTime.UtcNow;
            var updated = await _employeeRepository.Update(
                Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted
                    && x.Lifecycle.Version == request.Version),
                employee);
                return updated.Success
                ? updated
                : new Result { Success = false, Message = "LIFECYCLE_VERSION_CONFLICT" };
        }


private async Task<string?> ValidateSpecialLeaveAsync(
    string availability,
    string employeeUserId,
    DateTime effectiveAtUtc,
    string companyId)
{
    var policy = await _employeeLifecyclePolicyService!
        .GetPolicyAsync(availability, companyId);

    // Not a configured special leave
    if (policy is null)
        return null;

    if (string.IsNullOrWhiteSpace(policy.LeavePolicyId))
        return "SPECIAL_LEAVE_POLICY_NOT_LINKED";

    var effectiveDate = effectiveAtUtc == default
        ? DateTime.UtcNow.Date
        : effectiveAtUtc.Date;

    var approvedLeave = await _leaveRequestRepository!.FirstOrDefault(x =>
        x.CompanyId == companyId &&
        x.EmployeeId == employeeUserId &&
        x.LeavePolicyId == policy.LeavePolicyId &&
        x.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
        x.StartDate.Date <= effectiveDate &&
        x.EndDate.Date >= effectiveDate);

    return approvedLeave is null
        ? "SPECIAL_LEAVE_APPROVED_REQUEST_REQUIRED"
        : null;
}

        public async Task<Result> UpdateEmployeeContract(UpdateEmployeeContractRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId) || !await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.Edit)) return new Result { Success = false, Message = "CONTRACT_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeUserId)) return new Result { Success = false, Message = "CONTRACT_REQUEST_INVALID" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            if (employee.Contract.Version != request.Version) return new Result { Success = false, Message = "CONTRACT_CHANGED_OR_NOT_FOUND" };
            var action = request.Action.Trim();
            if (!action.Equals("Extend", StringComparison.OrdinalIgnoreCase) && !action.Equals("End", StringComparison.OrdinalIgnoreCase)) return new Result { Success = false, Message = "CONTRACT_ACTION_INVALID" };
            var isExtend = action.Equals("Extend", StringComparison.OrdinalIgnoreCase);
            var hasActiveExit = !isExtend && await _employeeExitRepository.Exist(x => x.CompanyId == companyId && x.EmployeeUserId == employee.UserId && x.IsActive && !x.IsDeleted);
            var contractError = EmployeeContractLifecycleRules.Validate(action, employee.Contract, request.StartDate, request.EndDate, hasActiveExit);
            if (contractError is not null) return new Result { Success = false, Message = contractError };
            var previousEndDate = employee.Contract.EndDate;
            var previousStartDate = employee.Contract.StartDate;
            var previousStatus = employee.Contract.Status;
            var previousVersion = employee.Contract.Version;
            var previousHistoryCount = employee.Contract.History.Count;
            employee.Contract.StartDate ??= request.StartDate?.Date;
            employee.Contract.EndDate = request.EndDate?.Date ?? employee.Contract.EndDate;
            employee.Contract.Status = isExtend ? "Extended" : "Ended";
            employee.Contract.Version++;
            employee.Contract.History.Add(new EmployeeContractChange { Action = isExtend ? "Extended" : "Ended", PreviousEndDate = previousEndDate, NewEndDate = employee.Contract.EndDate, ActorUserId = actorUserId, Reason = request.Reason?.Trim() });
            var saved = await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && x.Contract.Version == request.Version), employee);
            if (!saved.Success) return new Result { Success = false, Message = "CONTRACT_CHANGED_OR_NOT_FOUND" };
            if (!isExtend)
            {
                var exitResult = await InitiateEmployeeExitCore(new InitiateEmployeeExitRequest { EmployeeUserId = employee.UserId, ExitType = EmployeeLifecycleStatuses.ContractEnded, LastWorkingDate = (employee.Contract.EndDate ?? DateTime.UtcNow.Date).Date, Reason = request.Reason }, false);
                if (!exitResult.Success)
                {
                    // Contract end and the resulting exit case are one business operation.
                    // Compensate the contract write when exit creation is rejected (for
                    // example by a concurrent active exit or policy rule).
                    employee.Contract.StartDate = previousStartDate;
                    employee.Contract.EndDate = previousEndDate;
                    employee.Contract.Status = previousStatus;
                    employee.Contract.Version = previousVersion;
                    if (employee.Contract.History.Count > previousHistoryCount)
                        employee.Contract.History.RemoveRange(previousHistoryCount, employee.Contract.History.Count - previousHistoryCount);
                    await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && x.Contract.Version == previousVersion + 1), employee);
                }
                return exitResult;
            }
            return new Result { Success = true, Message = "CONTRACT_UPDATED" };
        }

        public async Task<Result> ChangeEmployeeLifecycle(ChangeEmployeeLifecycleRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId)) return new Result { Success = false, Message = "LIFECYCLE_CONTEXT_REQUIRED" };
            if (!await _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [Codeji.CMS.Utility.Constraints.Permission.CaseManage, Codeji.CMS.Utility.Constraints.Permission.Edit, Codeji.CMS.Utility.Constraints.Permission.Override], actorUserId, companyId)) return new Result { Success = false, Message = "LIFECYCLE_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeUserId)) return new Result { Success = false, Message = "LIFECYCLE_EMPLOYEE_REQUIRED" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            var effective = request.EffectiveFromUtc == default ? DateTime.UtcNow : request.EffectiveFromUtc;
            var state = employee.Lifecycle ?? new EmployeeLifecycleState();
            var requestedAvailability = request.AvailabilityStatus?.Trim() ?? string.Empty;
        var specialLeaveError = await ValidateSpecialLeaveAsync(
    requestedAvailability,
    request.EmployeeUserId,
    effective,
    companyId);

if (specialLeaveError is not null)
{
    return new Result
    {
        Success = false,
        Message = specialLeaveError
    };
}




            var currentLeaveDrivenStatus = state.AvailabilityStatus is EmployeeLifecycleStatuses.MedicalLeave
                or EmployeeLifecycleStatuses.MaternityLeave
                or EmployeeLifecycleStatuses.PaternityLeave
                or EmployeeLifecycleStatuses.LongTermLeave
                or EmployeeLifecycleStatuses.Sabbatical
                or EmployeeLifecycleStatuses.AdministrativeLeave;
            if (currentLeaveDrivenStatus && !string.Equals(state.AvailabilityStatus, requestedAvailability, StringComparison.Ordinal))
                return new Result { Success = false, Message = "LEAVE_MANAGEMENT_OWNS_LIFECYCLE_STATUS" };
            var lifecycleLeaveError = await ValidateLeaveDrivenAvailabilityAsync(requestedAvailability, request.EmployeeUserId, effective, companyId);
            if (lifecycleLeaveError is not null) return new Result { Success = false, Message = lifecycleLeaveError };
            var availabilityPolicy = _employeeLifecyclePolicyService is null ? null : await _employeeLifecyclePolicyService.GetPolicyAsync(requestedAvailability, companyId);
            if (availabilityPolicy?.RequiresHrApproval == true)
            {
                var roleId = CurrentContext.UserRoleId(_httpContextAccessor);
                var isHrApprover = await _roleService.IsRoleTypeMatch(roleId, EnumsHelper.Roles.HR, companyId)
                    || await _roleService.IsRoleTypeMatch(roleId, EnumsHelper.Roles.HRExecutive, companyId)
                    || await _roleService.IsRoleTypeMatch(roleId, EnumsHelper.Roles.Administrator, companyId);
                if (!isHrApprover) return new Result { Success = false, Message = "LIFECYCLE_HR_APPROVAL_REQUIRED" };
            }
            var policyError = _employeeLifecyclePolicyService is null ? null : await _employeeLifecyclePolicyService.ValidateAvailabilityAsync(request.AvailabilityStatus?.Trim() ?? string.Empty, request.ExpectedReturnDate, request.Reason?.Trim(), companyId);
            if (policyError is not null) return new Result { Success = false, Message = policyError };
            var requestedReturnDate = request.ExpectedReturnDate?.Date;
            if (currentLeaveDrivenStatus && state.AvailabilityExpectedReturnDate?.Date != requestedReturnDate)
                return new Result { Success = false, Message = "LEAVE_MANAGEMENT_OWNS_RETURN_DATE" };
            var availabilityChanged = !string.Equals(state.AvailabilityStatus, requestedAvailability, StringComparison.Ordinal)
                || state.AvailabilityExpectedReturnDate?.Date != requestedReturnDate;
            string error = string.Empty;
            if (availabilityChanged && !EmployeeLifecycleTransitionService.TryChangeAvailability(state, requestedAvailability, actorUserId, request.Version, effective, requestedReturnDate, request.ReasonCode?.Trim(), request.Reason?.Trim(), out error)) return new Result { Success = false, Message = error };
            var nextVersion = request.Version + (availabilityChanged ? 1 : 0);
            if (!string.Equals(state.AccountStatus, request.AccountStatus?.Trim(), StringComparison.Ordinal) && !EmployeeLifecycleTransitionService.TryChangeAccountStatus(state, request.AccountStatus?.Trim() ?? string.Empty, actorUserId, nextVersion++, effective, request.Reason?.Trim(), out error)) return new Result { Success = false, Message = error };
            if (!string.Equals(state.PayrollStatus, request.PayrollStatus?.Trim(), StringComparison.Ordinal) && !EmployeeLifecycleTransitionService.TryChangePayrollStatus(state, request.PayrollStatus?.Trim() ?? string.Empty, actorUserId, nextVersion++, effective, request.Reason?.Trim(), out error)) return new Result { Success = false, Message = error };
            if (!string.IsNullOrWhiteSpace(request.AlumniStatus) && state.IsFormerEmployee() && request.AlumniStatus != state.AlumniStatus && !EmployeeLifecycleTransitionService.TryChangeAlumniStatus(state, request.AlumniStatus.Trim(), actorUserId, nextVersion, effective, request.Reason?.Trim(), out error)) return new Result { Success = false, Message = error };
            employee.Lifecycle = state;
            employee.UpdatedBy = actorUserId;
            employee.UpdatedDate = DateTime.UtcNow;
            var updated = await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted && x.Lifecycle.Version == request.Version), employee);
            return updated.Success ? updated : new Result { Success = false, Message = "LIFECYCLE_VERSION_CONFLICT" };
        }

        private async Task<string?> ValidateLeaveDrivenAvailabilityAsync(string availability, string employeeUserId, DateTime effectiveAtUtc, string companyId)
        {
            if (availability is not (EmployeeLifecycleStatuses.MedicalLeave or EmployeeLifecycleStatuses.MaternityLeave or
                EmployeeLifecycleStatuses.PaternityLeave or EmployeeLifecycleStatuses.LongTermLeave or EmployeeLifecycleStatuses.Sabbatical or EmployeeLifecycleStatuses.AdministrativeLeave))
                return null;
            if (_leaveRequestRepository is null || _employeeLifecyclePolicyService is null)
                return "LIFECYCLE_LEAVE_WORKFLOW_UNAVAILABLE";

            var lifecyclePolicy = await _employeeLifecyclePolicyService.GetPolicyAsync(availability, companyId);
            if (string.IsNullOrWhiteSpace(lifecyclePolicy?.LeavePolicyId))
                return "LIFECYCLE_LEAVE_POLICY_NOT_CONFIGURED";

            var effectiveDate = effectiveAtUtc == default ? DateTime.UtcNow.Date : effectiveAtUtc.Date;
            var approvedRequest = await _leaveRequestRepository.FirstOrDefault(x =>
                x.CompanyId == companyId && x.EmployeeId == employeeUserId &&
                x.LeavePolicyId == lifecyclePolicy.LeavePolicyId &&
                x.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
                x.StartDate.Date <= effectiveDate && x.EndDate.Date >= effectiveDate);
            return approvedRequest is null ? "LIFECYCLE_APPROVED_LEAVE_REQUEST_REQUIRED" : null;
        }

        public async Task<Result> ChangeEmployeeLifecycleDimension(ChangeEmployeeLifecycleDimensionRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId)) return new Result { Success = false, Message = "LIFECYCLE_CONTEXT_REQUIRED" };
            if (!await _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [Codeji.CMS.Utility.Constraints.Permission.CaseManage, Codeji.CMS.Utility.Constraints.Permission.Edit, Codeji.CMS.Utility.Constraints.Permission.Override], actorUserId, companyId)) return new Result { Success = false, Message = "LIFECYCLE_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeUserId)) return new Result { Success = false, Message = "LIFECYCLE_EMPLOYEE_REQUIRED" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            var state = employee.Lifecycle ?? new EmployeeLifecycleState();
            var effective = request.EffectiveFromUtc == default ? DateTime.UtcNow : request.EffectiveFromUtc;
            var dimension = request.Dimension?.Trim();
            var value = request.Value?.Trim();
            string transitionError;
            var changed = dimension switch
            {
                nameof(EmployeeLifecycleState.AccountStatus) => EmployeeLifecycleTransitionService.TryChangeAccountStatus(state, value, actorUserId, request.Version, effective, request.Reason?.Trim(), out transitionError),
                nameof(EmployeeLifecycleState.PayrollStatus) => EmployeeLifecycleTransitionService.TryChangePayrollStatus(state, value, actorUserId, request.Version, effective, request.Reason?.Trim(), out transitionError),
                nameof(EmployeeLifecycleState.AlumniStatus) => EmployeeLifecycleTransitionService.TryChangeAlumniStatus(state, value, actorUserId, request.Version, effective, request.Reason?.Trim(), out transitionError),
                _ => InvalidDimension(out transitionError)
            };
            if (!changed) return new Result { Success = false, Message = string.IsNullOrWhiteSpace(transitionError) ? "LIFECYCLE_STATUS_INVALID" : transitionError };
            employee.Lifecycle = state;
            employee.UpdatedBy = actorUserId;
            employee.UpdatedDate = DateTime.UtcNow;
            var updated = await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted && x.Lifecycle.Version == request.Version), employee);
            return updated.Success ? updated : new Result { Success = false, Message = "LIFECYCLE_VERSION_CONFLICT" };

            static bool InvalidDimension(out string error) { error = "LIFECYCLE_DIMENSION_INVALID"; return false; }
        }

        public async Task<Result<EmployeeLifecycleStatusHistory>> GetEmployeeLifecycleHistory(string employeeUserId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(actorUserId))
                return new Result<EmployeeLifecycleStatusHistory> { Success = false, Message = "LIFECYCLE_CONTEXT_REQUIRED" };
            if (!await _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding,
                [Codeji.CMS.Utility.Constraints.Permission.CaseManage, Codeji.CMS.Utility.Constraints.Permission.Edit, Codeji.CMS.Utility.Constraints.Permission.Override],
                actorUserId, companyId))
                return new Result<EmployeeLifecycleStatusHistory> { Success = false, Message = "LIFECYCLE_FORBIDDEN" };
            if (string.IsNullOrWhiteSpace(employeeUserId))
                return new Result<EmployeeLifecycleStatusHistory> { Success = false, Message = "LIFECYCLE_EMPLOYEE_REQUIRED" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == employeeUserId && !x.IsDeleted);
            if (employee is null)
                return new Result<EmployeeLifecycleStatusHistory> { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            return new Result<EmployeeLifecycleStatusHistory> { Success = true, MethodResults = employee.Lifecycle?.History ?? [] };
        }

        public Task<Result<GetAllEmployeeResponseModel>> GetExEmployees(GetAllEmployeeRequestModel? filters)
        {
            filters ??= new GetAllEmployeeRequestModel();
            filters.EmploymentStatus = "ExEmployees";
            return GetAllEmployees(filters);
        }

        public async Task<Result> InitiateEmployeeExit(InitiateEmployeeExitRequest request)
            => await InitiateEmployeeExitCore(request, false);

        public async Task<Result> SubmitOwnResignation(InitiateEmployeeExitRequest request)
            => await InitiateEmployeeExitCore(request, true);

        private async Task<Result> InitiateEmployeeExitCore(InitiateEmployeeExitRequest request, bool isSelfSubmission)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            var exitType = EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(request?.ExitType);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeUserId)
                || exitType is null || request.LastWorkingDate == default)
                return new Result { Success = false, Message = "EXIT_REQUEST_INVALID" };
            if (isSelfSubmission && request.EmployeeUserId != actorUserId)
                return new Result { Success = false, Message = "SELF_EXIT_ONLY" };
            if (!isSelfSubmission && request.EmployeeUserId == actorUserId)
                return new Result { Success = false, Message = "SELF_EXIT_NOT_ALLOWED" };
            if (isSelfSubmission && exitType != EmployeeLifecycleStatuses.Resigned)
                return new Result { Success = false, Message = "SELF_SUBMISSION_MUST_BE_RESIGNATION" };

            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            if (EmploymentExitRules.IsFormerEmployee(employee) || NormalizeEmploymentStatus(employee) == EmployeeLifecycleStatuses.NoticePeriod)
                return new Result { Success = false, Message = "EXIT_ALREADY_OPEN_OR_COMPLETED" };
            // Employment status can lag behind a case while it is awaiting review.  The
            // active-case check is the authoritative guard and also mirrors the database
            // unique index used to protect concurrent submissions.
            if (await _employeeExitRepository.Exist(x => x.CompanyId == companyId && x.EmployeeUserId == request.EmployeeUserId && x.IsActive && !x.IsDeleted))
                return new Result { Success = false, Message = "EXIT_ALREADY_OPEN_OR_COMPLETED" };
            if (request.LastWorkingDate.Date < DateTime.UtcNow.Date)
                return new Result { Success = false, Message = "EXIT_LAST_WORKING_DATE_IN_PAST" };
            if (DateTime.TryParse(employee.DateOfJoining, out var joined) && request.LastWorkingDate.Date < joined.Date)
                return new Result { Success = false, Message = "EXIT_BEFORE_JOINING" };
            var resolvedPolicy = await GetResolvedExitPolicy(employee, exitType);
            var policy = resolvedPolicy?.Policy;
            var entryFeature = policy?.Features.FirstOrDefault(x => x.Key == (isSelfSubmission ? "SelfResignation" : "HrInitiatedExit"));
            if (policy is not null && !(entryFeature?.Enabled ?? (isSelfSubmission ? policy.AllowEmployeeSelfResignation : policy.EnableOffboarding)))
                return new Result { Success = false, Message = isSelfSubmission ? "SELF_RESIGNATION_DISABLED_BY_POLICY" : "HR_INITIATED_EXIT_DISABLED_BY_POLICY" };
            if (policy?.AllowExitDuringActiveLeave != true && _leaveRequestRepository is not null && await _leaveRequestRepository.Exist(x =>
                x.CompanyId == companyId && x.EmployeeId == employee.UserId &&
                x.Status == EnumsHelper.LeaveRequestStatus.Accepted &&
                x.StartDate.Date <= request.LastWorkingDate.Date && x.EndDate.Date >= DateTime.UtcNow.Date))
                return new Result { Success = false, Message = "ACTIVE_LEAVE_BLOCKS_EXIT" };
            if (isSelfSubmission && policy?.AllowEmployeeSelfResignation == false)
                return new Result { Success = false, Message = "SELF_RESIGNATION_DISABLED_BY_POLICY" };
            if (policy?.EnableOffboarding == false)
                return new Result { Success = false, Message = "OFFBOARDING_DISABLED_FOR_COMPANY" };
            // The requested date is not silently rejected when it is shorter than the
            // policy notice period.  The case starts with the normal policy LWD and a
            // pending, auditable early-release/settlement decision for HR to resolve.
            var requestedLastWorkingDate = request.LastWorkingDate.Date;
            var normalLastWorkingDate = DateTime.UtcNow.Date.AddDays(policy?.MinimumNoticeDays ?? 0);
            var requiresNoticeDecision = isSelfSubmission && requestedLastWorkingDate < normalLastWorkingDate;
            var lastWorkingDate = requiresNoticeDecision ? normalLastWorkingDate : requestedLastWorkingDate;
            var hasActiveReportingManager = !string.IsNullOrWhiteSpace(employee.ReportingManager)
                && await _employeeRepository.Exist(x => x.CompanyId == companyId && x.UserId == employee.ReportingManager && x.Status && !x.IsDeleted);
            // Employee resignations must be reviewed before they affect employment;
            // HR-initiated terminations enter notice immediately.
            var status = !isSelfSubmission ? "NoticePeriod"
                : (policy is null || policy.RequireManagerReview) && hasActiveReportingManager ? "PendingManagerApproval"
                : (policy is null || policy.RequireManagerReview) ? "PendingHrApproval"
                : policy.RequireHrReview ? "PendingHrApproval"
                : "NoticePeriod";
            var exit = new EmployeeExit
            {
                CompanyId = companyId, CreatedBy = actorUserId, EmployeeUserId = employee.UserId, EmployeeDepartmentId = employee.Department,
                ExitType = exitType, Status = status, InitiatedAtUtc = DateTime.UtcNow,
                ReportingManagerUserId = employee.ReportingManager,
                LastWorkingDate = lastWorkingDate, ProposedLastWorkingDate = requiresNoticeDecision ? requestedLastWorkingDate : null, Reason = request.Reason?.Trim(), Notes = request.Notes?.Trim(),
                PolicyVersionId = policy?.EmployeeExitPolicyId, PolicyVersion = policy?.Version,
                PolicySourceScope = resolvedPolicy?.SourceScope, PolicyExitType = resolvedPolicy?.ExitType,
                PolicySnapshot = policy is null ? null : new EmployeeExitPolicySnapshot { SourceScope = resolvedPolicy?.SourceScope, ExitType = resolvedPolicy?.ExitType, AllowEmployeeSelfResignation = policy.AllowEmployeeSelfResignation, AllowRejoin = policy.AllowRejoin, AllowExitDuringActiveLeave = policy.AllowExitDuringActiveLeave, MinimumNoticeDays = policy.MinimumNoticeDays, NoticePaySalaryBasis = policy.NoticePaySalaryBasis, NoticePayDailyRateDivisor = policy.NoticePayDailyRateDivisor, EnableOffboarding = policy.EnableOffboarding, RequireManagerReview = policy.RequireManagerReview, RequireHrReview = policy.RequireHrReview, AllowJointDiscussion = policy.AllowJointDiscussion, RequireResignationEvidence = policy.RequireResignationEvidence, RequireEmployeeAcknowledgement = policy.RequireEmployeeAcknowledgement, EnableFormerEmployeePortal = policy.EnableFormerEmployeePortal, EnableNoticePeriodReminders = policy.EnableNoticePeriodReminders, NoticePeriodReminderEveryDays = policy.NoticePeriodReminderEveryDays, NoticePeriodReminderOffsets = policy.NoticePeriodReminderOffsets.ToList(), NotifyNoticePeriodEmployee = policy.NotifyNoticePeriodEmployee, NotifyNoticePeriodReportingManager = policy.NotifyNoticePeriodReportingManager, NotifyNoticePeriodHr = policy.NotifyNoticePeriodHr, NotifyNoticePeriodAdmins = policy.NotifyNoticePeriodAdmins, NoticePeriodReminderEmail = policy.NoticePeriodReminderEmail, RequireHandover = policy.RequireHandover, RequireAssetClearance = policy.RequireAssetClearance, RequireAccessRevocation = policy.RequireAccessRevocation, RequireFinanceClearance = policy.RequireFinanceClearance, RequireBenefitClearance = policy.RequireBenefitClearance, RequireFinalSettlement = policy.RequireFinalSettlement, PresetLevel = policy.PresetLevel, Features = policy.Features.Select(CloneFeature).ToList() },
                AuditHistory = [new EmployeeExitAuditEvent { Action = isSelfSubmission ? "ResignationSubmitted" : "ExitInitiated", ActorUserId = actorUserId, Notes = request.Notes?.Trim() }]
            };
            if (requiresNoticeDecision)
            {
                var servedDays = Math.Max(0, (requestedLastWorkingDate - DateTime.UtcNow.Date).Days);
                exit.NoticeAdjustments.Add(new EmployeeExitNoticeAdjustment
                {
                    Type = "EmployeeEarlyRelease",
                    RequestedByUserId = actorUserId,
                    CurrentLastWorkingDate = normalLastWorkingDate,
                    RequestedLastWorkingDate = requestedLastWorkingDate,
                    RequiredNoticeDays = policy?.MinimumNoticeDays ?? 0,
                    ServedNoticeDays = servedDays,
                    ShortfallDays = Math.Max(0, (policy?.MinimumNoticeDays ?? 0) - servedDays),
                    Reason = request.Reason?.Trim()
                });
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "EmployeeEarlyReleaseRequested", ActorUserId = actorUserId, Notes = $"Requested LWD {requestedLastWorkingDate:yyyy-MM-dd}; normal policy LWD {normalLastWorkingDate:yyyy-MM-dd}." });
            }
            if (isSelfSubmission && (policy is null || policy.RequireManagerReview) && !hasActiveReportingManager)
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ManagerReviewRoutedToHr", ActorUserId = actorUserId, Notes = "No active reporting manager was assigned." });
            RescheduleExitReminders(exit, actorUserId);
            var create = await _employeeExitRepository.AddOne(exit);
            if (!create.Success) return create;
            employee.EmploymentStatus = status == "NoticePeriod" ? status : "Active"; employee.LastWorkingDate = lastWorkingDate;
            employee.ExitDate = lastWorkingDate.ToString("yyyy-MM-dd"); employee.Status = true;
            if (status == "NoticePeriod")
            {
                employee.Lifecycle ??= new EmployeeLifecycleState();
                var previousLifecycleEmployment = employee.Lifecycle.EmploymentStatus;
                employee.Lifecycle.EmploymentStatus = EmployeeLifecycleStatuses.NoticePeriod;
                employee.Lifecycle.OffboardingStatus = status switch
                {
                    "PendingManagerApproval" => EmployeeLifecycleStatuses.OffboardingPendingManagerApproval,
                    "PendingHrApproval" => EmployeeLifecycleStatuses.OffboardingPendingHrApproval,
                    _ => EmployeeLifecycleStatuses.OffboardingNoticePeriod
                };
                employee.Lifecycle.AvailabilityEffectiveFrom = DateTime.UtcNow;
                employee.Lifecycle.Version++;
                employee.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory
                {
                    Dimension = nameof(EmployeeLifecycleState.EmploymentStatus),
                    PreviousValue = previousLifecycleEmployment,
                    NewValue = EmployeeLifecycleStatuses.NoticePeriod,
                    ReasonCode = exitType,
                    Reason = request.Reason?.Trim(),
                    EffectiveFromUtc = DateTime.UtcNow,
                    ChangedBy = actorUserId,
                    ChangedAtUtc = DateTime.UtcNow
                });
            }
            employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> updateEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted;
            var updateResult = await _employeeRepository.Update(updateEmployee, employee);
            if (updateResult.Success && isSelfSubmission && exit.Status == "PendingHrApproval" && !hasActiveReportingManager)
                await QueueExitReviewNotifications(companyId, actorUserId, exit, employee, "Resignation awaiting HR review");
            if (!updateResult.Success || !isSelfSubmission || exit.Status != "PendingManagerApproval" || string.IsNullOrWhiteSpace(exit.ReportingManagerUserId)) return updateResult;

            var manager = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId
                && x.UserId == exit.ReportingManagerUserId && x.Status && !x.IsDeleted);
            if (manager is null) return updateResult;

            var employeeName = $"{employee.FirstName} {employee.LastName}".Trim();
            const string notificationTitle = "Resignation awaiting your review";
            var notificationBody = $"{employeeName} submitted a resignation. Proposed last working date: {lastWorkingDate:dd MMM yyyy}.";
            try
            {
                if (_employeeExitOutboxRepository is not null)
                {
                    var outboxResult = await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox
                    {
                        CompanyId = companyId,
                        CreatedBy = actorUserId,
                        EmployeeExitId = exit.EmployeeExitId,
                        EventType = "ResignationSubmitted",
                        RecipientUserId = manager.UserId,
                        Title = notificationTitle,
                        Body = notificationBody,
                        TargetId = $"employee-exit-{employee.UserId}",
                        NextAttemptAtUtc = DateTime.UtcNow
                    });
                    if (!outboxResult.Success)
                        _logger.LogError("Unable to queue the manager notification for employee exit case {EmployeeExitId}: {Message}", exit.EmployeeExitId, outboxResult.Message);
                }
                else
                {
                    _logger.LogWarning("Employee-exit notification outbox is not registered; manager {ManagerUserId} was not notified about exit case {EmployeeExitId}.", manager.UserId, exit.EmployeeExitId);
                }
            }
            catch (Exception ex)
            {
                // A notification delivery problem must not make a submitted resignation appear to have failed.
                _logger.LogError(ex, "Unable to queue the manager notification for employee exit case {EmployeeExitId}.", exit.EmployeeExitId);
            }

            if (manager.IsEmailVerified && !string.IsNullOrWhiteSpace(manager.Email))
            {
                var profileUrl = $"{ConfigManager.AppSettings.AppUrl.TrimEnd('/')}/employees/{Uri.EscapeDataString(employee.UserId)}/about";
                var encodedEmployeeName = WebUtility.HtmlEncode(employeeName);
                var encodedProfileUrl = WebUtility.HtmlEncode(profileUrl);
                _priorityTaskQueue.QueueBackgroundWorkItem(_ => _middlewareService.EmailSendAndSave(new EmpEmailLogs
                {
                    CompanyId = companyId,
                    UserTo = manager.UserId,
                    UserFrom = actorUserId,
                    Email = manager.Email,
                    Subject = notificationTitle,
                    Body = $"<p><strong>{encodedEmployeeName}</strong> has submitted a resignation.</p><p>Proposed last working date: <strong>{lastWorkingDate:dd MMM yyyy}</strong>.</p><p><a href=\"{encodedProfileUrl}\">Open the offboarding case</a> to review the request.</p>",
                    EmailLogType = EnumsHelper.MailType.ResignationMail
                }), 1);
            }

            return updateResult;
        }

        public async Task<Result> ReviewEmployeeExit(ReviewEmployeeExitRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || request.Action is not ("Approve" or "Discuss" or "JointDiscussion" or "Reject" or "RequestDiscussion") || string.IsNullOrWhiteSpace(request.EmployeeExitId))
                return new Result { Success = false, Message = "EXIT_REVIEW_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (exit.EmployeeUserId == actorUserId) return new Result { Success = false, Message = "SELF_EXIT_REVIEW_FORBIDDEN" };
            var isHr = await IsHrExitActor(companyId, actorUserId);
            var isDiscussionOwner = exit.Status == "DiscussionRequested" && exit.DiscussionRequestedByUserId == actorUserId;
            var isAssignedManager = exit.ReportingManagerUserId == actorUserId && (exit.Status == "PendingManagerApproval" || isDiscussionOwner);
            var isCombinedManagerHrReviewer = exit.Status == "PendingManagerApproval" && isAssignedManager && isHr;
            if (exit.Status == "PendingManagerApproval" && !isAssignedManager) return new Result { Success = false, Message = "EXIT_MANAGER_REVIEW_FORBIDDEN" };
            if (exit.Status == "PendingHrApproval" && !isHr) return new Result { Success = false, Message = "EXIT_HR_REVIEW_FORBIDDEN" };
            if (exit.Status == "DiscussionRequested" && !isDiscussionOwner) return new Result { Success = false, Message = "EXIT_DISCUSSION_DECISION_FORBIDDEN" };
            if (exit.Status is not ("PendingManagerApproval" or "PendingHrApproval" or "DiscussionRequested")) return new Result { Success = false, Message = "EXIT_REVIEW_NOT_ALLOWED_IN_STATE" };
            if (request.Action == "Approve" && exit.NoticeAdjustments.Any(x => x.Status is "Pending" or "AwaitingEmployeeDecision"))
                return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_DECISION_PENDING" };
            if (request.Action is "Discuss" or "JointDiscussion" or "RequestDiscussion" && exit.PolicySnapshot?.AllowJointDiscussion == false)
                return new Result { Success = false, Message = "EXIT_DISCUSSION_DISABLED_BY_POLICY" };
            if (request.Action is "Discuss" or "JointDiscussion" && string.IsNullOrWhiteSpace(request.Notes))
                return new Result { Success = false, Message = "EXIT_DISCUSSION_NOTE_REQUIRED" };
            var discussionParticipants = request.Action == "JointDiscussion" ? request.DiscussionParticipantUserIds.Where(x => !string.IsNullOrWhiteSpace(x) && x != exit.EmployeeUserId && x != actorUserId).Distinct().Take(10).ToList() : [];
            if (request.Action == "JointDiscussion" && discussionParticipants.Count == 0) return new Result { Success = false, Message = "EXIT_JOINT_DISCUSSION_PARTICIPANT_REQUIRED" };
            if (discussionParticipants.Count > 0)
            {
                var eligible = await _employeeRepository.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted && discussionParticipants.Contains(x.UserId));
                if (eligible.Count() != discussionParticipants.Count) return new Result { Success = false, Message = "EXIT_JOINT_DISCUSSION_PARTICIPANT_INVALID" };
            }
            exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.Version++;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.Action, ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            if (request.Action == "Reject")
            {
                exit.Status = "Rejected"; exit.IsActive = false; exit.RejectedAtUtc = DateTime.UtcNow; exit.RejectionReason = request.Notes?.Trim();
            }
            else if (request.Action is "Discuss" or "JointDiscussion" or "RequestDiscussion")
            {
                if (string.IsNullOrWhiteSpace(request.Notes)) return new Result { Success = false, Message = "EXIT_DISCUSSION_NOTE_REQUIRED" };
                var returnStatus = exit.Status;
                exit.Status = "DiscussionRequested"; exit.DiscussionRequest = request.Notes.Trim(); exit.DiscussionResponse = null;
                exit.DiscussionMode = request.Action == "JointDiscussion" ? "Joint" : "Direct"; exit.DiscussionParticipantUserIds = discussionParticipants;
                exit.DiscussionRequestedByUserId = actorUserId;
                exit.DiscussionReturnStatus = returnStatus;
                exit.DiscussionMessages.Add(new EmployeeExitDiscussionMessage { AuthorUserId = actorUserId, Message = request.Notes.Trim() });
                exit.ProposedLastWorkingDate = request.RecommendedLastWorkingDate?.Date;
            }
            else if (exit.Status == "PendingManagerApproval" || (exit.Status == "DiscussionRequested" && exit.DiscussionReturnStatus == "PendingManagerApproval"))
            {
                exit.ManagerReviewRemarks = request.Notes?.Trim(); exit.ManagerReviewedAtUtc = DateTime.UtcNow;
                if (request.RecommendedLastWorkingDate.HasValue) exit.ProposedLastWorkingDate = request.RecommendedLastWorkingDate.Value.Date;
                if (exit.PolicySnapshot?.RequireHrReview == true && !isCombinedManagerHrReviewer) exit.Status = "PendingHrApproval";
                else { exit.Status = "NoticePeriod"; exit.ApprovedByUserId = actorUserId; exit.ApprovedAtUtc = DateTime.UtcNow; await PopulateExitClearances(exit); }
                if (isCombinedManagerHrReviewer)
                {
                    exit.HrReviewRemarks = request.Notes?.Trim(); exit.HrReviewedAtUtc = DateTime.UtcNow;
                    exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "CombinedManagerHrApproval", ActorUserId = actorUserId, Notes = "Manager and HR review completed by the same authorized user." });
                }
            }
            else
            {
                exit.Status = "NoticePeriod"; exit.ApprovedByUserId = actorUserId; exit.ApprovedAtUtc = DateTime.UtcNow; exit.HrReviewRemarks = request.Notes?.Trim(); exit.HrReviewedAtUtc = DateTime.UtcNow;
                if (request.RecommendedLastWorkingDate.HasValue)
                {
                    var recommendedDate = request.RecommendedLastWorkingDate.Value.Date;
                    // A review can recommend a shorter date, but it cannot bypass the
                    // waiver/buyout/recovery decision that protects the notice policy.
                    if (recommendedDate < exit.LastWorkingDate.Date)
                        exit.ProposedLastWorkingDate = recommendedDate;
                    else
                        exit.LastWorkingDate = recommendedDate;
                }
                await PopulateExitClearances(exit);
            }
            Expression<Func<EmployeeExit, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
            var saved = await _employeeExitRepository.Update(update, exit);
            if (saved.Success && request.Action == "Approve" && exit.Status == "NoticePeriod")
            {
                var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
                if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
                employee.Lifecycle ??= new EmployeeLifecycleState();
                var previousLifecycleEmployment = employee.Lifecycle.EmploymentStatus;
                employee.Lifecycle.EmploymentStatus = EmployeeLifecycleStatuses.NoticePeriod;
                employee.Lifecycle.OffboardingStatus = EmployeeLifecycleStatuses.OffboardingNoticePeriod;
                employee.Lifecycle.Version++;
                employee.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory
                {
                    Dimension = nameof(EmployeeLifecycleState.EmploymentStatus),
                    PreviousValue = previousLifecycleEmployment,
                    NewValue = EmployeeLifecycleStatuses.NoticePeriod,
                    ReasonCode = exit.ExitType,
                    Reason = request.Notes?.Trim(),
                    EffectiveFromUtc = DateTime.UtcNow,
                    ChangedBy = actorUserId,
                    ChangedAtUtc = DateTime.UtcNow
                });
                employee.EmploymentStatus = EmployeeLifecycleStatuses.NoticePeriod;
                employee.Status = true;
                employee.LastWorkingDate = exit.LastWorkingDate;
                employee.UpdatedBy = actorUserId;
                employee.UpdatedDate = DateTime.UtcNow;
                var employeeSaved = await _employeeRepository.Update(
                    Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted),
                    employee);
                if (!employeeSaved.Success) return new Result { Success = false, Message = "EMPLOYEE_LIFECYCLE_UPDATE_FAILED" };
            }
            if (saved.Success && request.Action == "JointDiscussion" && _employeeExitOutboxRepository is not null)
                foreach (var participantId in discussionParticipants)
                    await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EventType = "JointExitDiscussionInvited", RecipientUserId = participantId, Title = "Invitation: offboarding joint discussion", Body = request.Notes!.Trim(), TargetId = $"employee-exit-{exit.EmployeeUserId}" });
            return saved;
        }

        public async Task<Result> RespondToEmployeeExitDiscussion(RespondToEmployeeExitDiscussionRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId)) return new Result { Success = false, Message = "EXIT_DISCUSSION_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (exit.EmployeeUserId != actorUserId || exit.Status != "DiscussionRequested") return new Result { Success = false, Message = "EXIT_DISCUSSION_RESPONSE_FORBIDDEN" };
            if (string.IsNullOrWhiteSpace(request.Notes)) return new Result { Success = false, Message = "EXIT_DISCUSSION_RESPONSE_NOTE_REQUIRED" };
            // A proposed date is recorded for the reviewer.  It must be approved through
            // the notice-adjustment workflow; discussion alone never changes employment dates.
            if (request.ProposedLastWorkingDate.HasValue) exit.ProposedLastWorkingDate = request.ProposedLastWorkingDate.Value.Date;
            exit.DiscussionResponse = request.Notes.Trim();
            exit.DiscussionMessages.Add(new EmployeeExitDiscussionMessage { AuthorUserId = actorUserId, Message = request.Notes.Trim() });
            exit.Status = exit.DiscussionReturnStatus is "PendingManagerApproval" or "PendingHrApproval" ? exit.DiscussionReturnStatus : "PendingManagerApproval";
            var discussionRecipient = exit.DiscussionRequestedByUserId ?? exit.ReportingManagerUserId;
            exit.DiscussionRequestedByUserId = null;
            exit.DiscussionReturnStatus = null;
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "DiscussionResponded", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            Expression<Func<EmployeeExit, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
            var saved = await _employeeExitRepository.Update(update, exit);
            if (saved.Success && _employeeExitOutboxRepository is not null)
            {
                var recipients = exit.DiscussionParticipantUserIds
                    .Append(discussionRecipient ?? string.Empty)
                    .Where(x => !string.IsNullOrWhiteSpace(x) && x != actorUserId)
                    .Distinct();
                foreach (var recipientUserId in recipients)
                    await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EventType = "ExitDiscussionResponded", RecipientUserId = recipientUserId, Title = "Employee responded to the offboarding discussion", Body = request.Notes!.Trim(), TargetId = $"employee-exit-{exit.EmployeeUserId}" });
            }
            return saved;
        }

        public async Task<Result> RespondToEmployeeExitJointDiscussion(RespondToEmployeeExitJointDiscussionRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.Notes))
                return new Result { Success = false, Message = "EXIT_JOINT_DISCUSSION_INVALID" };

            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version)
                return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (exit.Status != "DiscussionRequested" || exit.DiscussionMode != "Joint" || !exit.DiscussionParticipantUserIds.Contains(actorUserId))
                return new Result { Success = false, Message = "EXIT_JOINT_DISCUSSION_FORBIDDEN" };

            var note = request.Notes.Trim();
            exit.DiscussionMessages.Add(new EmployeeExitDiscussionMessage { AuthorUserId = actorUserId, Message = note });
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "JointDiscussionParticipantResponded", ActorUserId = actorUserId, Notes = note });
            var saved = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
            if (saved.Success && _employeeExitOutboxRepository is not null)
            {
                var recipients = exit.DiscussionParticipantUserIds.Append(exit.EmployeeUserId).Append(exit.DiscussionRequestedByUserId ?? string.Empty)
                    .Where(id => !string.IsNullOrWhiteSpace(id) && id != actorUserId).Distinct();
                foreach (var recipientUserId in recipients)
                    await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EventType = "JointExitDiscussionResponded", RecipientUserId = recipientUserId, Title = "New message in offboarding joint discussion", Body = note, TargetId = $"employee-exit-{exit.EmployeeUserId}" });
            }
            return saved;
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
                if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_CANCEL_FORBIDDEN" };
            }
            if (selfOnly && exit.Status == "NoticePeriod")
            {
                if (exit.Withdrawal is not null && exit.Withdrawal.Status == "Pending") return new Result { Success = false, Message = "EXIT_WITHDRAWAL_ALREADY_PENDING" };
                exit.Withdrawal = new EmployeeExitWithdrawal { RequestedByUserId = actorUserId, Reason = request.Reason?.Trim() ?? string.Empty };
                exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "WithdrawalRequested", ActorUserId = actorUserId, Notes = request.Reason?.Trim() });
                Expression<Func<EmployeeExit, bool>> requestUpdate = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
                var requested = await _employeeExitRepository.Update(requestUpdate, exit);
                if (requested.Success) await QueueWithdrawalReviewNotifications(companyId, actorUserId, exit, request.Reason);
                return requested;
            }
            if (selfOnly && exit.Status is not ("PendingManagerApproval" or "PendingHrApproval" or "DiscussionRequested")) return new Result { Success = false, Message = "EXIT_WITHDRAW_NOT_ALLOWED_IN_STATE" };
            exit.Status = status; exit.IsActive = false; exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            CancelPendingExitReminders(exit);
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = status, ActorUserId = actorUserId, Notes = request.Reason?.Trim() });
            Expression<Func<EmployeeExit, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
            var closed = await _employeeExitRepository.Update(update, exit);
            if (closed.Success && status is ("Withdrawn" or "Cancelled"))
            {
                await RevokeFormerEmployeePortalTokens(companyId, exit.EmployeeExitId);
                if (status == "Withdrawn")
                {
                    var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
                    if (employee is not null)
                    {
                        employee.EmploymentStatus = "Active";
                        employee.ExitDate = null;
                        employee.LastWorkingDate = null;
                        employee.UpdatedBy = actorUserId;
                        employee.UpdatedDate = DateTime.UtcNow;
                        await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted), employee);
                    }
                }
            }
            return closed;
        }

        public async Task<Result> DecideEmployeeExitWithdrawal(DecideEmployeeExitWithdrawalRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId)) return new Result { Success = false, Message = "EXIT_WITHDRAWAL_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit?.Withdrawal is null || exit.Withdrawal.Status != "Pending" || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var isManager = exit.ReportingManagerUserId == actorUserId;
            if (!isManager && !await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_WITHDRAWAL_REVIEW_FORBIDDEN" };
            exit.Withdrawal.Status = request.Approve ? "Approved" : "Declined"; exit.Withdrawal.DecidedByUserId = actorUserId; exit.Withdrawal.DecidedAtUtc = DateTime.UtcNow; exit.Withdrawal.DecisionNotes = request.Notes?.Trim();
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.Approve ? "WithdrawalApproved" : "WithdrawalDeclined", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            if (request.Approve)
            {
                exit.Status = "Withdrawn";
                exit.IsActive = false;
                CancelPendingExitReminders(exit);
            }
            var exitResult = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
            if (!exitResult.Success || !request.Approve) return exitResult;
            await RevokeFormerEmployeePortalTokens(companyId, exit.EmployeeExitId);

            // A notice-period resignation updates the employee read-model to NoticePeriod.
            // Restore it when the withdrawal is approved so login, profile actions and
            // directory filters immediately treat the employee as active again.
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND_AFTER_WITHDRAWAL" };
            employee.Status = true;
            employee.EmploymentStatus = "Active";
            employee.Lifecycle ??= new EmployeeLifecycleState();
            employee.Lifecycle.EmploymentStatus = EmployeeLifecycleStatuses.Active;
            employee.Lifecycle.OffboardingStatus = EmployeeLifecycleStatuses.OffboardingWithdrawn;
            employee.Lifecycle.Version++;
            employee.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory
            {
                Dimension = nameof(EmployeeLifecycleState.OffboardingStatus), PreviousValue = EmployeeLifecycleStatuses.OffboardingNoticePeriod,
                NewValue = EmployeeLifecycleStatuses.OffboardingWithdrawn, ReasonCode = "WITHDRAWAL_APPROVED", Reason = request.Notes?.Trim(),
                EffectiveFromUtc = DateTime.UtcNow, ChangedBy = actorUserId, ChangedAtUtc = DateTime.UtcNow
            });
            employee.ExitDate = null;
            employee.LastWorkingDate = null;
            employee.UpdatedBy = actorUserId;
            employee.UpdatedDate = DateTime.UtcNow;
            return await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted), employee);
        }

        private async Task<bool> IsHrExitActor(string companyId, string actorUserId)
        {
            // Keep existing Edit/Override assignments working while companies transition
            // to the more focused CaseManage permission.
            return await _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [Codeji.CMS.Utility.Constraints.Permission.CaseManage, Codeji.CMS.Utility.Constraints.Permission.Edit, Codeji.CMS.Utility.Constraints.Permission.Override], actorUserId, companyId);
        }

        private Task<bool> CanManageExitOperation(string companyId, string actorUserId, string permission)
            => _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [permission, Codeji.CMS.Utility.Constraints.Permission.Edit, Codeji.CMS.Utility.Constraints.Permission.Override], actorUserId, companyId);

        private async Task RevokeFormerEmployeePortalTokens(string companyId, string employeeExitId)
        {
            if (_formerEmployeeAccessTokenRepository is null) return;
            try
            {
                var now = DateTime.UtcNow;
                await _formerEmployeeAccessTokenRepository.GetCollection().UpdateManyAsync(
                    Builders<FormerEmployeeAccessToken>.Filter.And(
                        Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.CompanyId, companyId),
                        Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.EmployeeExitId, employeeExitId),
                        Builders<FormerEmployeeAccessToken>.Filter.Eq(x => x.RevokedAtUtc, null)),
                    Builders<FormerEmployeeAccessToken>.Update.Set(x => x.RevokedAtUtc, now).Set(x => x.SessionRevokedAtUtc, now));
            }
            catch (Exception exception)
            {
                // A withdrawn case is denied by portal session resolution regardless. Token
                // revocation is additional protection and is logged for operational follow-up.
                _logger.LogError(exception, "Unable to revoke former-employee portal tokens for exit {EmployeeExitId}", employeeExitId);
            }
        }

        private async Task QueueExitReviewNotifications(string companyId, string actorUserId, EmployeeExit exit, EmpUser employee, string title)
        {
            if (_employeeExitOutboxRepository is null) return;

            // Do not depend on a literal "HR" role.  Any active company user with the
            // offboarding case-management permission is an eligible fallback reviewer.
            var recipients = new List<string>();
            var employees = await _employeeRepository.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted);
            foreach (var candidate in employees.Where(x => x.UserId != exit.EmployeeUserId))
                if (await IsHrExitActor(companyId, candidate.UserId)) recipients.Add(candidate.UserId);

            var employeeName = string.Join(" ", new[] { employee.FirstName, employee.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            foreach (var recipientUserId in recipients.Distinct())
                await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox
                {
                    CompanyId = companyId,
                    CreatedBy = actorUserId,
                    EmployeeExitId = exit.EmployeeExitId,
                    EventType = "ResignationRoutedToCaseManagers",
                    RecipientUserId = recipientUserId,
                    Title = title,
                    Body = $"{employeeName} submitted a resignation. No active reporting manager is assigned, so this case needs an offboarding review.",
                    TargetId = $"employee-exit-{exit.EmployeeUserId}",
                    NextAttemptAtUtc = DateTime.UtcNow
                });
        }

        private async Task QueueWithdrawalReviewNotifications(string companyId, string actorUserId, EmployeeExit exit, string? reason)
        {
            if (_employeeExitOutboxRepository is null) return;
            var recipients = new List<string>();
            if (!string.IsNullOrWhiteSpace(exit.ReportingManagerUserId) && exit.ReportingManagerUserId != exit.EmployeeUserId) recipients.Add(exit.ReportingManagerUserId);

            var employees = await _employeeRepository.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted);
            foreach (var employee in employees.Where(x => x.UserId != exit.EmployeeUserId))
                if (await IsHrExitActor(companyId, employee.UserId)) recipients.Add(employee.UserId);

            var requester = employees.FirstOrDefault(x => x.UserId == exit.EmployeeUserId)
                ?? await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            var requesterName = requester is null ? "An employee" : string.Join(" ", new[] { requester.FirstName, requester.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var body = string.IsNullOrWhiteSpace(reason)
                ? $"{requesterName} requested to withdraw their resignation. Review and decide the request."
                : $"{requesterName} requested to withdraw their resignation. Reason: {reason.Trim()}";
            foreach (var recipientUserId in recipients.Distinct())
                await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EventType = "EmployeeExitWithdrawalRequested", RecipientUserId = recipientUserId, Title = "Withdrawal request awaiting review", Body = body, TargetId = $"employee-exit-{exit.EmployeeUserId}" });
        }

        private async Task QueueDocumentRequestNotifications(string companyId, string actorUserId, EmployeeExit exit)
        {
            if (_employeeExitOutboxRepository is null) return;

            // A company can name any role as the offboarding case manager. Resolve recipients
            // from the permission model so this remains correct for each tenant's role setup.
            var employees = await _employeeRepository.GetAll(x => x.CompanyId == companyId && x.Status && !x.IsDeleted);
            var recipients = new List<string>();
            foreach (var employee in employees.Where(x => x.UserId != actorUserId))
            {
                if (await IsHrExitActor(companyId, employee.UserId)) recipients.Add(employee.UserId);
            }

            var requester = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            var requesterName = requester is null
                ? "An employee"
                : string.Join(" ", new[] { requester.FirstName, requester.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var request = exit.FormerEmployeeRequests.OrderByDescending(x => x.RequestedAtUtc).First();
            var title = $"Document request: {request.Type}";
            var body = $"{requesterName} requested {request.Type}. Details: {request.Details}";

            foreach (var recipientUserId in recipients.Distinct())
            {
                await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox
                {
                    CompanyId = companyId,
                    CreatedBy = actorUserId,
                    EmployeeExitId = exit.EmployeeExitId,
                    EventType = "EmployeeExitDocumentRequested",
                    RecipientUserId = recipientUserId,
                    Title = title,
                    Body = body,
                    TargetId = $"employee-exit-{exit.EmployeeUserId}"
                });
            }
        }

        private async Task<bool> CanCompleteExitClearance(string companyId, string actorUserId, EmployeeExit exit, EmployeeExitClearance clearance)
        {
            if (await IsHrExitActor(companyId, actorUserId)) return true;
            if (!string.IsNullOrWhiteSpace(clearance.OwnerUserId) && clearance.OwnerUserId == actorUserId) return true;
            if (!string.IsNullOrWhiteSpace(clearance.OwnerPermission)
                && await CanManageExitOperation(companyId, actorUserId, clearance.OwnerPermission)) return true;
            return exit.ReportingManagerUserId == actorUserId
                && string.IsNullOrWhiteSpace(clearance.OwnerPermission)
                && string.IsNullOrWhiteSpace(clearance.OwnerUserId);
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
            var previousLastWorkingDate = exit.LastWorkingDate.Date;
            exit.LastWorkingDate = request.LastWorkingDate.Date; RecalculateOpenClearanceDueDates(exit, actorUserId); exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            RescheduleExitReminders(exit, actorUserId);
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.OverrideNoticePolicy ? "LastWorkingDateNoticeOverride" : "LastWorkingDateUpdated", ActorUserId = actorUserId, Notes = request.Reason?.Trim() });
            Expression<Func<EmployeeExit, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive;
            var result = await _employeeExitRepository.Update(update, exit);
            if (!result.Success) return result;
            employee.LastWorkingDate = exit.LastWorkingDate; employee.ExitDate = exit.LastWorkingDate.ToString("yyyy-MM-dd"); employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> updateEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted;
            var employeeUpdate = await _employeeRepository.Update(updateEmployee, employee);
            if (employeeUpdate.Success && previousLastWorkingDate != exit.LastWorkingDate.Date && _employeeExitOutboxRepository is not null)
            {
                await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox
                {
                    CompanyId = companyId,
                    CreatedBy = actorUserId,
                    EmployeeExitId = exit.EmployeeExitId,
                    EventType = "EmployeeExitLastWorkingDateChanged",
                    RecipientUserId = employee.UserId,
                    RecipientEmail = employee.IsEmailVerified && !string.IsNullOrWhiteSpace(employee.Email) ? employee.Email : null,
                    SendEmail = employee.IsEmailVerified && !string.IsNullOrWhiteSpace(employee.Email),
                    Title = "Your last working day has changed",
                    Body = $"HR changed your last working day from {previousLastWorkingDate:dd MMM yyyy} to {exit.LastWorkingDate:dd MMM yyyy}. Please review your offboarding details.",
                    TargetId = $"employee-exit-{employee.UserId}"
                });
            }
            return employeeUpdate;
        }

        public async Task<Result> RequestEmployeeExitNoticeAdjustment(RequestEmployeeExitNoticeAdjustmentRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.Reason) || request.RequestedLastWorkingDate == default || request.WaivedDays < 0 || request.RecoveryAmount < 0
                || request.Type is not ("EmployeeEarlyRelease" or "CompanyEarlyRelease" or "FullWaiver" or "PartialWaiver" or "Buyout" or "Recovery" or "NoticeExtension")) return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || exit.Status is not ("PendingManagerApproval" or "PendingHrApproval" or "NoticePeriod")) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (exit.EmployeeUserId != actorUserId && !await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_FORBIDDEN" };
            var requestedBusinessDate = DateOnly.FromDateTime(request.RequestedLastWorkingDate.Date);
            var todayBusinessDate = DateOnly.FromDateTime(IndiaTime.Today);
            var initiatedBusinessDate = IndiaTime.ToBusinessDate(exit.InitiatedAtUtc);
            if (requestedBusinessDate < todayBusinessDate || requestedBusinessDate < initiatedBusinessDate)
                return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_DATE_INVALID" };
            // ISSUE #4: Prevent multiple pending adjustments
            if (exit.NoticeAdjustments.Any(x => x.Status is "Pending" or "AwaitingEmployeeDecision")) 
                return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_ALREADY_PENDING" };
            // ISSUE #10: Restrict employees from submitting multiple requests
            if (exit.EmployeeUserId == actorUserId && exit.NoticeAdjustments.Any(x => x.RequestedByUserId == actorUserId && x.Status is ("Pending" or "AwaitingEmployeeDecision")))
                return new Result { Success = false, Message = "EXIT_EMPLOYEE_REQUEST_ALREADY_PENDING" };
            if (exit.EmployeeUserId == actorUserId && request.Type != "EmployeeEarlyRelease")
                return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_DECISION_RESERVED_FOR_HR" };
            var policyNoticeDays = exit.PolicySnapshot?.MinimumNoticeDays ?? 0;
            var servedDays = Math.Max(0, requestedBusinessDate.DayNumber - initiatedBusinessDate.DayNumber);
            var shortfallDays = Math.Max(0, policyNoticeDays - servedDays);
            if (request.WaivedDays > shortfallDays || ((request.Type is "Buyout" or "Recovery") && request.RecoveryAmount <= 0) || ((request.Type is "CompanyEarlyRelease" or "FullWaiver") && request.RecoveryAmount > 0)) return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_TERMS_INVALID" };
            var adjustment = new EmployeeExitNoticeAdjustment { Type = request.Type, RequestedByUserId = actorUserId, CurrentLastWorkingDate = exit.LastWorkingDate, RequestedLastWorkingDate = IndiaTime.ToStoredBusinessDate(requestedBusinessDate), RequiredNoticeDays = policyNoticeDays, ServedNoticeDays = servedDays, ShortfallDays = shortfallDays, WaivedDays = Math.Max(0, request.WaivedDays), RecoveryAmount = request.RecoveryAmount, Reason = request.Reason.Trim() };
            exit.NoticeAdjustments.Add(adjustment); exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "NoticeAdjustmentRequested", ActorUserId = actorUserId, Notes = adjustment.Type });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && !x.IsDeleted), exit);
        }

        public async Task<Result> DecideEmployeeExitNoticeAdjustment(DecideEmployeeExitNoticeAdjustmentRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId) || request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.NoticeAdjustmentId)) return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            var adjustment = exit?.NoticeAdjustments.FirstOrDefault(x => x.NoticeAdjustmentId == request.NoticeAdjustmentId && x.Status == "Pending");
            if (exit is null || adjustment is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var decisionType = request.DecisionType ?? adjustment.Type;
            if (decisionType is not ("CompanyEarlyRelease" or "FullWaiver" or "PartialWaiver" or "Buyout" or "Recovery" or "NoticeExtension") || request.WaivedDays < 0 || request.RecoveryAmount < 0) return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_TERMS_INVALID" };
            var requestedBusinessDate = IndiaTime.ToBusinessDate(adjustment.RequestedLastWorkingDate);
            var currentBusinessDate = IndiaTime.ToBusinessDate(adjustment.CurrentLastWorkingDate);
            if (decisionType == "NoticeExtension" && requestedBusinessDate <= currentBusinessDate) return new Result { Success = false, Message = "EXIT_NOTICE_EXTENSION_DATE_INVALID" };
            if (decisionType != "NoticeExtension" && requestedBusinessDate > currentBusinessDate) return new Result { Success = false, Message = "EXIT_EARLY_RELEASE_DATE_INVALID" };
            // ISSUE #3: Compare notice dates as India business dates, including legacy stored values.
            if (request.Approve && requestedBusinessDate < DateOnly.FromDateTime(IndiaTime.Today)) return new Result { Success = false, Message = "NOTICE_ADJUSTMENT_DATE_IN_PAST" };
            // ISSUE #7: Validate NoticeExtension does not exceed maximum duration (3 months policy)
            if (request.Approve && decisionType == "NoticeExtension")
            {
                var maxExtensionMonths = 3;
                var maxExtensionDate = currentBusinessDate.AddMonths(maxExtensionMonths);
                if (requestedBusinessDate > maxExtensionDate) return new Result { Success = false, Message = "NOTICE_EXTENSION_EXCEEDS_MAXIMUM" };
            }
            var waivedDays = request.WaivedDays ?? adjustment.WaivedDays;
            var recoveryAmount = request.RecoveryAmount ?? adjustment.RecoveryAmount;
            var recoveryDays = 0;
            if (decisionType is "CompanyEarlyRelease" or "FullWaiver") { waivedDays = adjustment.ShortfallDays; recoveryAmount = 0; }
            if (decisionType == "PartialWaiver")
            {
                if (waivedDays <= 0 || waivedDays >= adjustment.ShortfallDays) return new Result { Success = false, Message = "EXIT_NOTICE_PARTIAL_WAIVER_DAYS_INVALID" };
                recoveryDays = adjustment.ShortfallDays - waivedDays;
            }
            if (decisionType is "Buyout" or "Recovery") { waivedDays = 0; recoveryDays = adjustment.ShortfallDays; }
            if (request.Approve && decisionType is ("Buyout" or "Recovery" or "PartialWaiver"))
            {
                var noticePay = await CalculateNoticeRecovery(companyId, exit, recoveryDays);
                if (!noticePay.Success) return new Result { Success = false, Message = "EXIT_NOTICE_PAYROLL_BASIS_NOT_FOUND" };
                // Buyout/recovery is policy-calculated from the employee's processed pay.
                // Do not accept an arbitrary client amount for a department-scoped policy.
                recoveryAmount = noticePay.Amount;
                adjustment.DailyRate = noticePay.DailyRate;
                adjustment.SalaryBasis = exit.PolicySnapshot?.NoticePaySalaryBasis;
                adjustment.SalaryDivisor = exit.PolicySnapshot?.NoticePayDailyRateDivisor ?? 30;
            }
            if (waivedDays > adjustment.ShortfallDays || ((decisionType is "Buyout" or "Recovery" or "PartialWaiver") && recoveryAmount <= 0) || ((decisionType is "CompanyEarlyRelease" or "FullWaiver") && recoveryAmount > 0)) return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_TERMS_INVALID" };
            // A company-funded release/waiver takes effect immediately. Any change that
            // creates a recovery obligation or extends the employee's notice period is an
            // offer, so the employee can accept or retain the original notice terms.
            var requiresEmployeeDecision = decisionType is "Buyout" or "Recovery" or "PartialWaiver" or "NoticeExtension";
            if (request.Approve && requiresEmployeeDecision)
            {
                adjustment.Type = decisionType; adjustment.WaivedDays = waivedDays; adjustment.RecoveryDays = recoveryDays; adjustment.RecoveryAmount = recoveryAmount;
                adjustment.Status = "AwaitingEmployeeDecision"; adjustment.DecisionByUserId = actorUserId; adjustment.DecidedAtUtc = DateTime.UtcNow;
                // ISSUE #9: Set expiry date for offer (7 days from approval)
                adjustment.ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
                // ISSUE #8: Add intermediate status to reflect that case is awaiting employee offer decision
                exit.Status = "AwaitingEmployeeNoticeOffer";
                exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "NoticeAdjustmentOfferCalculated", ActorUserId = actorUserId, Notes = $"{decisionType}; recovery={recoveryAmount}; expires={adjustment.ExpiresAtUtc:yyyy-MM-dd HH:mm:ss}" });
                return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
            }
            adjustment.Status = request.Approve ? "Approved" : "Declined"; adjustment.DecisionByUserId = actorUserId; adjustment.DecidedAtUtc = DateTime.UtcNow;
            if (!request.Approve)
            {
                // ISSUE #6: When declining a non-payable adjustment, add explicit audit trail about retained LWD
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "NoticeAdjustmentDeclined", ActorUserId = actorUserId, Notes = $"Original LWD {exit.LastWorkingDate:yyyy-MM-dd} retained. {request.Reason?.Trim()}" });
            }
            if (request.Approve)
            {
                adjustment.Type = decisionType; adjustment.WaivedDays = waivedDays; adjustment.RecoveryDays = recoveryDays; adjustment.RecoveryAmount = recoveryAmount; exit.LastWorkingDate = IndiaTime.ToStoredBusinessDate(requestedBusinessDate); RecalculateOpenClearanceDueDates(exit, actorUserId);
                // Completing a direct company waiver/release closes any prior discussion
                // and returns the employee to the active notice-period stage.
                exit.Status = "NoticePeriod";
                exit.DiscussionRequestedByUserId = null;
                exit.DiscussionReturnStatus = null;
                if (decisionType is "Buyout" or "Recovery" or "PartialWaiver" && recoveryAmount > 0 && !exit.FinanceItems.Any(x => x.Reference == adjustment.NoticeAdjustmentId))
                    exit.FinanceItems.Add(new EmployeeExitFinanceItem
                    {
                        Category = decisionType == "Buyout" ? "Notice-period buyout" : "Notice-period recovery through final settlement",
                        Direction = "ReceivableFromEmployee",
                        Amount = recoveryAmount,
                        Status = "Pending",
                        Reference = adjustment.NoticeAdjustmentId,
                        Notes = decisionType == "Buyout" ? "Approved notice-period buyout" : $"Approved notice-period recovery for {recoveryDays} shortfall day(s)"
                    });
            }
            if (request.Approve) RescheduleExitReminders(exit, actorUserId);
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.Approve ? "NoticeAdjustmentApproved" : "NoticeAdjustmentDeclined", ActorUserId = actorUserId, Notes = request.Approve ? $"{decisionType}; waived={waivedDays}; recovery={recoveryAmount}; {request.Reason?.Trim()}" : request.Reason?.Trim() });
            var saved = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
            if (!saved.Success || !request.Approve) return saved;
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND_AFTER_NOTICE_ADJUSTMENT" };
            employee.LastWorkingDate = exit.LastWorkingDate;
            employee.ExitDate = exit.LastWorkingDate.ToString("yyyy-MM-dd");
            employee.UpdatedBy = actorUserId;
            employee.UpdatedDate = DateTime.UtcNow;
            return await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted), employee);
        }

        public async Task<Result> DecideOwnEmployeeExitNoticeOffer(DecideOwnEmployeeExitNoticeOfferRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.NoticeAdjustmentId)) return new Result { Success = false, Message = "EXIT_NOTICE_OFFER_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.EmployeeUserId == actorUserId && x.IsActive && !x.IsDeleted);
            var adjustment = exit?.NoticeAdjustments.FirstOrDefault(x => x.NoticeAdjustmentId == request.NoticeAdjustmentId && x.Status == "AwaitingEmployeeDecision");
            if (exit is null || adjustment is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            adjustment.Status = request.Accept ? "Approved" : "EmployeeDeclined";
            if (request.Accept)
            {
                exit.LastWorkingDate = IndiaTime.ToStoredBusinessDate(IndiaTime.ToBusinessDate(adjustment.RequestedLastWorkingDate)); RecalculateOpenClearanceDueDates(exit, actorUserId); RescheduleExitReminders(exit, actorUserId);
                // ISSUE #6: Return to NoticePeriod status when employee accepts
                exit.Status = "NoticePeriod";
                if (adjustment.Type is "Buyout" or "Recovery" or "PartialWaiver" && adjustment.RecoveryAmount > 0 && !exit.FinanceItems.Any(x => x.Reference == adjustment.NoticeAdjustmentId))
                    exit.FinanceItems.Add(new EmployeeExitFinanceItem { Category = adjustment.Type == "Buyout" ? "Notice-period buyout" : "Notice-period recovery through final settlement", Direction = "ReceivableFromEmployee", Amount = adjustment.RecoveryAmount, Status = "Pending", Reference = adjustment.NoticeAdjustmentId, Notes = "Accepted by employee" });
            }
            else
            {
                // ISSUE #6: When declining, revert to NoticePeriod and remove any finance items created for this offer
                exit.Status = "NoticePeriod";
                exit.FinanceItems.RemoveAll(x => x.Reference == adjustment.NoticeAdjustmentId);
            }
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.Accept ? "NoticeAdjustmentOfferAccepted" : "NoticeAdjustmentOfferDeclined", ActorUserId = actorUserId });
            var saved = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
            if (!saved.Success || !request.Accept) return saved;
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == actorUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND_AFTER_NOTICE_ADJUSTMENT" };
            employee.LastWorkingDate = exit.LastWorkingDate; employee.ExitDate = exit.LastWorkingDate.ToString("yyyy-MM-dd"); employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            return await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted), employee);
        }

        /// <summary>
        /// ISSUE #11: Cancel a pending notice adjustment. Employees can cancel their own Pending requests;
        /// HR can cancel any pending adjustment. Cannot cancel Approved, Declined, or AwaitingEmployeeDecision statuses.
        /// </summary>
        public async Task<Result> CancelEmployeeExitNoticeAdjustment(
            string employeeExitId, 
            string noticeAdjustmentId, 
            int version,
            string? reason)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            
            if (string.IsNullOrWhiteSpace(employeeExitId) || string.IsNullOrWhiteSpace(noticeAdjustmentId))
                return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_INVALID" };

            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == employeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };

            var adjustment = exit.NoticeAdjustments.FirstOrDefault(x => x.NoticeAdjustmentId == noticeAdjustmentId);
            if (adjustment is null) return new Result { Success = false, Message = "NOTICE_ADJUSTMENT_NOT_FOUND" };
            
            // Can only cancel Pending adjustments; cannot cancel Approved, Declined, or AwaitingEmployeeDecision
            if (adjustment.Status is not "Pending") 
                return new Result { Success = false, Message = "NOTICE_ADJUSTMENT_CANNOT_CANCEL_IN_STATE" };

            var isEmployee = exit.EmployeeUserId == actorUserId;
            var isHr = await IsHrExitActor(companyId, actorUserId);
            
            if (isEmployee && adjustment.RequestedByUserId != actorUserId)
                return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_CANCEL_FORBIDDEN" };
            
            if (!isEmployee && !isHr)
                return new Result { Success = false, Message = "EXIT_NOTICE_ADJUSTMENT_CANCEL_FORBIDDEN" };

            adjustment.Status = "Cancelled";
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent 
            { 
                Action = "NoticeAdjustmentCancelled", 
                ActorUserId = actorUserId, 
                Notes = $"Cancelled by {(isEmployee ? "employee" : "HR")}. Reason: {reason?.Trim()}" 
            });

            return await _employeeExitRepository.Update(
                Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == version && x.IsActive), 
                exit);
        }

        public async Task<Result> UpsertEmployeeExitPolicy(UpsertEmployeeExitPolicyRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request?.Features is not null && request.Features.Count == 0 && request.PresetLevel != "Custom")
                request.Features = BuildPresetFeatures(request.PresetLevel);
            if (request?.PresetLevel == "Disabled") request.EnableOffboarding = false;
            if (_employeeExitPolicyRepository is null || request is null || request.MinimumNoticeDays < 0 || request.NoticePayDailyRateDivisor is < 1 or > 31 || request.NoticePaySalaryBasis is not ("BasicMonthly" or "GrossMonthly") || (!string.IsNullOrWhiteSpace(request.DepartmentId) && !string.IsNullOrWhiteSpace(request.RoleId)) || !IsValidFeatureConfiguration(request.Features) || !IsValidPresetLevel(request.PresetLevel))
                return new Result { Success = false, Message = "EXIT_POLICY_INVALID" };
            if (!await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PolicyManage)) return new Result { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };
            var departmentIds = (request.DepartmentIds ?? []).Append(request.DepartmentId ?? string.Empty)
                .Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToList();
            var departmentId = departmentIds.Count == 1 ? departmentIds[0] : null;
            var roleId = request.RoleId?.Trim();
            var policyExitType = string.IsNullOrWhiteSpace(request.ExitType)
                ? null
                : EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(request.ExitType);
            if (!string.IsNullOrWhiteSpace(request.ExitType) && policyExitType is null)
                return new Result { Success = false, Message = "EXIT_POLICY_EXIT_TYPE_INVALID" };
            if (departmentIds.Count > 0 && !string.IsNullOrWhiteSpace(roleId))
                return new Result { Success = false, Message = "EXIT_POLICY_SCOPE_MUST_BE_COMPANY_DEPARTMENT_OR_ROLE" };
            var scopeKey = BuildExitPolicyScopeKey(departmentIds, roleId, policyExitType);
            foreach (var selectedDepartmentId in departmentIds)
                if (!await _departmentRepository.Exist(x => x.CompanyId == companyId && x.DepartmentId == selectedDepartmentId && x.IsActive && !x.IsDeleted))
                    return new Result { Success = false, Message = "EXIT_POLICY_DEPARTMENT_NOT_FOUND" };
            if (!string.IsNullOrWhiteSpace(roleId) && !await _rolesRepository.Exist(x => x.CompanyId == companyId && x.RolesId == roleId && !x.IsDeleted))
                return new Result { Success = false, Message = "EXIT_POLICY_ROLE_NOT_FOUND" };

            // A policy is unique within a tenant by its scope. Resolve by scope first so a
            // department save cannot accidentally replace the company default when a stale
            // browser payload includes the wrong policy id.
            var requestedExisting = string.IsNullOrWhiteSpace(request.EmployeeExitPolicyId) ? null : await _employeeExitPolicyRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitPolicyId == request.EmployeeExitPolicyId && !x.IsDeleted);
            // ScopeKey was introduced after some policies had already been stored and
            // older consolidation writes could leave it inconsistent with the actual
            // scope fields. Compare canonical fields so a same-scope edit can repair
            // that legacy key, while still rejecting a real scope replacement.
            var requestedExistingScopeKey = requestedExisting is null ? null : BuildExitPolicyScopeKey(GetPolicyDepartmentIds(requestedExisting), requestedExisting.RoleId, EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(requestedExisting.ExitType));
            if (requestedExisting is not null && !string.Equals(requestedExistingScopeKey, scopeKey, StringComparison.Ordinal))
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "EXIT_POLICY_SCOPE_CONFLICT" };
            var scopedExisting = requestedExisting ?? await _employeeExitPolicyRepository.FirstOrDefault(x => x.CompanyId == companyId
                && x.ScopeKey == scopeKey && !x.IsDeleted);
            // Compatibility for policies saved before ScopeKey was introduced.
            if (scopedExisting is null)
            {
                var legacyScopeCandidates = await _employeeExitPolicyRepository.GetAll(x => x.CompanyId == companyId
                    && x.DepartmentId == departmentId && x.RoleId == roleId && !x.IsDeleted);
                scopedExisting = legacyScopeCandidates.FirstOrDefault(x =>
                    EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(x.ExitType) == policyExitType);
            }
            var existing = scopedExisting;
            if (existing is not null && request.Version != existing.Version)
                return new Result { Success = false, Message = "EXIT_POLICY_VERSION_CONFLICT" };
            var allPolicies = (await _employeeExitPolicyRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
            var conflicts = departmentIds.Count == 0 ? [] : allPolicies.Where(x => x.EmployeeExitPolicyId != existing?.EmployeeExitPolicyId
                && EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(x.ExitType) == policyExitType
                && GetPolicyDepartmentIds(x).Intersect(departmentIds, StringComparer.Ordinal).Any()).ToList();
            if (conflicts.Count > 0 && !request.ConsolidateDepartmentPolicies)
                return new Result { Success = false, Message = "EXIT_POLICY_DEPARTMENT_ALREADY_CONFIGURED" };
            if (conflicts.Any(conflict => !GetPolicyDepartmentIds(conflict).All(departmentIds.Contains)))
                return new Result { Success = false, Message = "EXIT_POLICY_DEPARTMENT_GROUP_CONFLICT" };
            if (conflicts.Any(conflict => !HasSameExitPolicyConfiguration(conflict, request)))
                return new Result { Success = false, Message = "EXIT_POLICY_DEPARTMENT_CONFIGURATION_DIFFERS" };
            var policy = existing ?? new EmployeeExitPolicy { CompanyId = companyId, CreatedBy = actorUserId };
            policy.ScopeKey = scopeKey; policy.ExitType = policyExitType; policy.DepartmentId = departmentId; policy.DepartmentIds = departmentIds; policy.RoleId = roleId; policy.MinimumNoticeDays = request.MinimumNoticeDays; policy.NoticePaySalaryBasis = request.NoticePaySalaryBasis; policy.NoticePayDailyRateDivisor = request.NoticePayDailyRateDivisor;
            policy.EnableOffboarding = request.EnableOffboarding; policy.AllowEmployeeSelfResignation = request.AllowEmployeeSelfResignation; policy.RequireManagerReview = request.RequireManagerReview; policy.RequireHrReview = request.RequireHrReview; policy.AllowJointDiscussion = request.AllowJointDiscussion;
            policy.RequireResignationEvidence = request.RequireResignationEvidence; policy.RequireEmployeeAcknowledgement = request.RequireEmployeeAcknowledgement; policy.EnableFormerEmployeePortal = request.EnableFormerEmployeePortal; policy.AllowRejoin = request.AllowRejoin; policy.AllowExitDuringActiveLeave = request.AllowExitDuringActiveLeave;
            policy.EnableNoticePeriodReminders = request.EnableNoticePeriodReminders; policy.NoticePeriodReminderEveryDays = Math.Clamp(request.NoticePeriodReminderEveryDays, 1, 365); policy.NoticePeriodReminderOffsets = request.NoticePeriodReminderOffsets.Where(x => int.TryParse(x, out var days) && days >= 0 && days <= 365).Distinct().ToList(); policy.NotifyNoticePeriodEmployee = request.NotifyNoticePeriodEmployee; policy.NotifyNoticePeriodReportingManager = request.NotifyNoticePeriodReportingManager; policy.NotifyNoticePeriodHr = request.NotifyNoticePeriodHr; policy.NotifyNoticePeriodAdmins = request.NotifyNoticePeriodAdmins; policy.NoticePeriodReminderEmail = request.NoticePeriodReminderEmail;
            policy.RequireHandover = request.RequireHandover; policy.RequireAssetClearance = request.RequireAssetClearance; policy.RequireAccessRevocation = request.RequireAccessRevocation; policy.RequireFinanceClearance = request.RequireFinanceClearance; policy.RequireBenefitClearance = request.RequireBenefitClearance; policy.RequireFinalSettlement = request.RequireFinalSettlement;
            policy.PresetLevel = request.PresetLevel; policy.Features = request.Features.Select(ToFeature).ToList();
            if (existing is not null) policy.Version++; policy.IsActive = request.IsActive; policy.UpdatedBy = actorUserId; policy.UpdatedDate = DateTime.UtcNow;
            Result saved;
            if (existing is null) saved = await _employeeExitPolicyRepository.AddOne(policy);
            else
            {
                Expression<Func<EmployeeExitPolicy, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitPolicyId == policy.EmployeeExitPolicyId && x.Version == request.Version && !x.IsDeleted;
                saved = await _employeeExitPolicyRepository.Update(update, policy);
            }
            if (!saved.Success) return saved;
            foreach (var conflict in conflicts)
            {
                conflict.IsActive = false; conflict.IsDeleted = true; conflict.UpdatedBy = actorUserId; conflict.UpdatedDate = DateTime.UtcNow;
                var retired = await _employeeExitPolicyRepository.Update(Builders<EmployeeExitPolicy>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitPolicyId == conflict.EmployeeExitPolicyId && !x.IsDeleted), conflict);
                if (!retired.Success) return new Result { Success = false, Message = "EXIT_POLICY_CONSOLIDATION_PARTIAL" };
            }
            return saved;
        }

        public async Task<Result<EmployeeExitPolicy>> GetEmployeeExitPolicy(string policyId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeExitPolicyRepository is null || string.IsNullOrWhiteSpace(policyId)) return new Result<EmployeeExitPolicy> { Success = false, Message = "EXIT_POLICY_NOT_FOUND" };
            if (!await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PolicyManage)) return new Result<EmployeeExitPolicy> { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };
            var policy = await _employeeExitPolicyRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitPolicyId == policyId && !x.IsDeleted);
            return policy is null ? new Result<EmployeeExitPolicy> { Success = false, Message = "EXIT_POLICY_NOT_FOUND" } : new Result<EmployeeExitPolicy> { Success = true, MethodResult = policy };
        }

        public async Task<Result<EmployeeExitClearanceTemplate>> GetEmployeeExitClearanceTemplate(string templateId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeExitClearanceTemplateRepository is null || string.IsNullOrWhiteSpace(templateId)) return new Result<EmployeeExitClearanceTemplate> { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_NOT_FOUND" };
            if (!await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PolicyManage)) return new Result<EmployeeExitClearanceTemplate> { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };
            var template = await _employeeExitClearanceTemplateRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == templateId && !x.IsDeleted);
            return template is null ? new Result<EmployeeExitClearanceTemplate> { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_NOT_FOUND" } : new Result<EmployeeExitClearanceTemplate> { Success = true, MethodResult = template };
        }

        public async Task<Result<EmployeeExitPolicy>> GetEmployeeExitPolicies()
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PolicyManage)) return new Result<EmployeeExitPolicy> { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };
            var policies = _employeeExitPolicyRepository is null ? [] : (await _employeeExitPolicyRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
            return new Result<EmployeeExitPolicy> { Success = true, MethodResults = policies };
        }

        public async Task<Result<EmployeeExitClearanceTemplate>> GetEmployeeExitClearanceTemplates()
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PolicyManage)) return new Result<EmployeeExitClearanceTemplate> { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };
            var templates = _employeeExitClearanceTemplateRepository is null ? [] : (await _employeeExitClearanceTemplateRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted)).ToList();
            return new Result<EmployeeExitClearanceTemplate> { Success = true, MethodResults = templates };
        }

        public async Task<Result> UpsertEmployeeExitClearanceTemplate(UpsertEmployeeExitClearanceTemplateRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeExitClearanceTemplateRepository is null || request is null || string.IsNullOrWhiteSpace(request.Category) || request.DueDaysBeforeLastWorkingDate < 0)
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_INVALID" };
            if (!await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PolicyManage)) return new Result { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };
            var existing = string.IsNullOrWhiteSpace(request.EmployeeExitClearanceTemplateId) ? null : await _employeeExitClearanceTemplateRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == request.EmployeeExitClearanceTemplateId && !x.IsDeleted);
            var requiredGate = string.IsNullOrWhiteSpace(request.RequiredPolicyGate) ? "General" : request.RequiredPolicyGate.Trim();
            if (requiredGate is not ("General" or "Handover" or "AssetClearance" or "AccessRevocation" or "FinanceClearance" or "BenefitClearance"))
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_GATE_INVALID" };
            var scope = string.IsNullOrWhiteSpace(request.Scope) ? "Global" : request.Scope.Trim();
            if (scope is not ("Global" or "Department")) return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_SCOPE_INVALID" };
            if (request.EffectiveFromUtc.HasValue && request.EffectiveToUtc.HasValue && request.EffectiveToUtc <= request.EffectiveFromUtc)
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_EFFECTIVE_RANGE_INVALID" };
            var departmentId = scope == "Department" ? request.DepartmentId?.Trim() : null;
            if (scope == "Department" && (string.IsNullOrWhiteSpace(departmentId) || !await _departmentRepository.Exist(x => x.CompanyId == companyId && x.DepartmentId == departmentId && x.IsActive && !x.IsDeleted)))
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_DEPARTMENT_INVALID" };
            var templateKey = NormalizeClearanceTemplateKey(request.TemplateKey, request.Category);
            if (string.IsNullOrWhiteSpace(templateKey)) return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_KEY_INVALID" };
            var scopeKey = BuildClearanceTemplateScopeKey(scope, departmentId, templateKey);
            var existingTemplateId = existing?.EmployeeExitClearanceTemplateId ?? string.Empty;
            var duplicates = await _employeeExitClearanceTemplateRepository.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted && x.EmployeeExitClearanceTemplateId != existingTemplateId);
            if (request.IsActive && duplicates.Any(x => NormalizeClearanceTemplateKey(x.TemplateKey, x.Category) == templateKey && (string.IsNullOrWhiteSpace(x.Scope) ? "Global" : x.Scope) == scope && x.DepartmentId == departmentId))
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_KEY_ALREADY_EXISTS" };
            if (existing is not null && request.Version != existing.Version) return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_VERSION_CONFLICT" };
            var template = existing ?? new EmployeeExitClearanceTemplate { CompanyId = companyId, CreatedBy = actorUserId };
            template.ScopeKey = scopeKey; template.TemplateKey = templateKey; template.Scope = scope; template.DepartmentId = departmentId; template.Category = request.Category.Trim(); template.Description = request.Description?.Trim(); template.OwnerRole = request.OwnerRole?.Trim(); template.OwnerPermission = request.OwnerPermission?.Trim(); template.RequiredPolicyGate = requiredGate; template.IsMandatory = request.IsMandatory; template.DueDaysBeforeLastWorkingDate = request.DueDaysBeforeLastWorkingDate; template.IsActive = request.IsActive; template.EffectiveFromUtc = request.EffectiveFromUtc; template.EffectiveToUtc = request.EffectiveToUtc; template.Version = existing is null ? 1 : existing.Version + 1; template.UpdatedBy = actorUserId; template.UpdatedDate = DateTime.UtcNow;
            if (existing is null) return await _employeeExitClearanceTemplateRepository.AddOne(template);
            Expression<Func<EmployeeExitClearanceTemplate, bool>> update = x => x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == template.EmployeeExitClearanceTemplateId && x.Version == request.Version && !x.IsDeleted;
            return await _employeeExitClearanceTemplateRepository.Update(update, template);
        }

        public async Task<Result> DeleteEmployeeExitClearanceTemplate(string templateId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeExitClearanceTemplateRepository is null || string.IsNullOrWhiteSpace(templateId))
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_NOT_FOUND" };
            if (!await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PolicyManage))
                return new Result { Success = false, Message = "EXIT_POLICY_FORBIDDEN" };

            var template = await _employeeExitClearanceTemplateRepository.FirstOrDefault(x =>
                x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == templateId && !x.IsDeleted);
            if (template is null)
                return new Result { Success = false, Message = "EXIT_CLEARANCE_TEMPLATE_NOT_FOUND" };

            var version = template.Version;
            template.IsActive = false;
            template.IsDeleted = true;
            template.UpdatedBy = actorUserId;
            template.UpdatedDate = DateTime.UtcNow;
            template.Version++;

            Expression<Func<EmployeeExitClearanceTemplate, bool>> update = x =>
                x.CompanyId == companyId && x.EmployeeExitClearanceTemplateId == templateId && x.Version == version && !x.IsDeleted;
            return await _employeeExitClearanceTemplateRepository.Update(update, template);
        }

        public async Task<Result> ApproveEmployeeFinalSettlement(ApproveEmployeeFinalSettlementRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeFinalSettlementRepository is null || request is null) return new Result { Success = false, Message = "FINAL_SETTLEMENT_INVALID" };
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "FINAL_SETTLEMENT_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && (x.Status == "NoticePeriod" || EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status)));
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (DateTime.UtcNow.Date < exit.LastWorkingDate.Date) return new Result { Success = false, Message = "LAST_WORKING_DATE_NOT_REACHED" };
            var settlement = await _employeeFinalSettlementRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && !x.IsDeleted);
            if (settlement is null || string.IsNullOrWhiteSpace(settlement.EmployeeFinalSettlementId) || settlement.Status != "Calculated")
                return new Result { Success = false, Message = "FINAL_SETTLEMENT_NOT_CALCULATED" };

            var expectedSettlementVersion = settlement.Version;
            settlement.Status = "Approved";
            settlement.ApprovedByUserId = actorUserId;
            settlement.ApprovedAtUtc = DateTime.UtcNow;
            settlement.UpdatedBy = actorUserId;
            settlement.UpdatedDate = DateTime.UtcNow;
            settlement.Version++;
            exit.Version++;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FinalSettlementApproved", ActorUserId = actorUserId });
            return await PersistFinalSettlementAndExitAsync(settlement, exit, companyId, actorUserId, request.Version, expectedSettlementVersion, isNewSettlement: false);
        }

        public async Task<Result> UpdateEmployeeFinalSettlementPayment(UpdateEmployeeFinalSettlementPaymentRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (_employeeFinalSettlementRepository is null || request is null || request.Status is not ("PaymentInitiated" or "Paid" or "PaymentFailed" or "Disputed") || request.PaidAmount < 0 || !await IsHrExitActor(companyId, actorUserId))
                return new Result { Success = false, Message = "FINAL_SETTLEMENT_PAYMENT_FORBIDDEN" };

            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            var settlement = await _employeeFinalSettlementRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            if (exit is null || settlement is null || settlement.Status is not ("Approved" or "PaymentInitiated" or "PaymentFailed" or "Disputed"))
                return new Result { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = "FINAL_SETTLEMENT_NOT_APPROVED" };
            if (request.Status == "Paid" && request.PaidAmount != settlement.NetPayable)
                return new Result { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = "FINAL_SETTLEMENT_PAID_AMOUNT_MISMATCH" };

            var expectedSettlementVersion = settlement.Version;
            var expectedExitVersion = exit.Version;
            settlement.Status = request.Status;
            settlement.PaidAmount = request.PaidAmount;
            settlement.PaymentReference = request.PaymentReference?.Trim();
            settlement.PaymentMethod = request.PaymentMethod?.Trim();
            settlement.UpdatedBy = actorUserId;
            settlement.UpdatedDate = DateTime.UtcNow;
            settlement.Version++;
            if (request.Status == "PaymentInitiated") settlement.PaymentInitiatedAtUtc = DateTime.UtcNow;
            if (request.Status == "Paid") settlement.PaidAtUtc = DateTime.UtcNow;
            if (request.Status == "PaymentFailed") settlement.PaymentFailureReason = request.Reason?.Trim();
            if (request.Status == "Disputed") settlement.DisputeReason = request.Reason?.Trim();

            EmpPayRoll? sourcePayroll = null;
            if (request.Status == "Paid" && settlement.SalaryPayable + settlement.BonusesAndIncentives > 0 && !string.IsNullOrWhiteSpace(settlement.SourcePayrollRunId))
            {
                if (!await IsPayrollRunSourceAvailableForPayment(companyId, settlement))
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "FINAL_SETTLEMENT_SOURCE_PAYROLL_CHANGED_RECALCULATE" };
            }
            else if (request.Status == "Paid" && settlement.SalaryPayable + settlement.BonusesAndIncentives > 0)
            {
                if (_employeePayrollRepository is null || string.IsNullOrWhiteSpace(settlement.SourcePayrollId))
                    return new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "FINAL_SETTLEMENT_SOURCE_PAYROLL_NOT_AVAILABLE" };
                sourcePayroll = await _employeePayrollRepository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == settlement.SourcePayrollId);
                if (sourcePayroll is null || IsPayrollAlreadyProcessedForSettlement(sourcePayroll))
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "FINAL_SETTLEMENT_SOURCE_PAYROLL_CHANGED_RECALCULATE" };
                MarkPayrollPaidViaFinalSettlement(sourcePayroll, actorUserId, request.EmployeeExitId, settlement);
            }

            exit.Version++;
            exit.UpdatedBy = actorUserId;
            exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = $"FinalSettlement{request.Status}", ActorUserId = actorUserId, Notes = request.Status == "Paid" ? $"amount={request.PaidAmount}; payroll={settlement.SourcePayrollId ?? settlement.SourcePayrollRunId ?? "none"}" : request.Reason?.Trim() });
            return await PersistFinalSettlementAndExitAsync(settlement, exit, companyId, actorUserId, expectedExitVersion, expectedSettlementVersion, isNewSettlement: false, sourcePayroll: sourcePayroll);
        }
        public async Task<Result> CalculateEmployeeFinalSettlement(CalculateEmployeeFinalSettlementRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId) || request is null) return new Result { Success = false, Message = "FINAL_SETTLEMENT_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && (x.Status == "NoticePeriod" || EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status)));
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (DateTime.UtcNow.Date < exit.LastWorkingDate.Date) return new Result { Success = false, Message = "LAST_WORKING_DATE_NOT_REACHED" };
            if (_employeeFinalSettlementRepository is null || (_employeePayrollRepository is null && (_payrollRunsRepository is null || _payrollRunEmployeesRepository is null))) return new Result { Success = false, Message = "FINAL_PAYROLL_NOT_CONFIGURED" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            // HR may calculate before month-end publication. Payroll remains the source snapshot;
            // tenant and stable employee identifiers avoid missing historical rows with stale user links.
            var lastWorkingMonthStart = new DateTime(exit.LastWorkingDate.Year, exit.LastWorkingDate.Month, 1);
            EmpPayRoll? payroll = null;
            if (_employeePayrollRepository is not null)
            {
                var payrolls = await _employeePayrollRepository.GetAll(x => x.CompanyId == companyId
                    && (x.UserId == exit.EmployeeUserId || x.EmployeeId == employee.EmployeeId)
                    && x.PayMonth == lastWorkingMonthStart);
                payroll = payrolls
                    .OrderByDescending(x => x.IsProcessed)
                    .ThenByDescending(x => x.ProcessedAt ?? x.PaidDate)
                    .ThenByDescending(x => x.CreatedAt)
                    .FirstOrDefault();
            }

            PayrollFinalSettlementSource? payrollRunSource = null;
            if (payroll is null && _payrollRunsRepository is not null && _payrollRunEmployeesRepository is not null)
                payrollRunSource = await new PayrollFinalSettlementSourceResolver(_payrollRunsRepository, _payrollRunEmployeesRepository)
                    .Resolve(companyId, employee.EmployeeId, lastWorkingMonthStart);
            if (payroll is null && payrollRunSource is null)
                return new Result { Success = false, Message = "FINAL_MONTH_PAYROLL_NOT_AVAILABLE" };
            var settlement = await _employeeFinalSettlementRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && !x.IsDeleted)
                ?? new EmployeeFinalSettlement { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = exit.EmployeeUserId };
            var isNewSettlement = string.IsNullOrWhiteSpace(settlement.EmployeeFinalSettlementId);
            if (isNewSettlement) settlement.EmployeeFinalSettlementId = $"final-settlement-{companyId}-{exit.EmployeeExitId}";
            if (!isNewSettlement && settlement.Status is not ("Pending" or "Calculated"))
                return new Result { Success = false, Message = "FINAL_SETTLEMENT_CANNOT_BE_RECALCULATED_AFTER_APPROVAL_OR_PAYMENT" };
            var expectedSettlementVersion = settlement.Version;
            var approvedNoticeRecovery = exit.NoticeAdjustments.Where(x => x.Status == "Approved" && (x.Type is "Recovery" or "PartialWaiver")).Sum(x => x.RecoveryAmount);
            // The regular last-month payroll is authoritative once published: its earnings,
            // bonus and deductions must not be added a second time to the F&F amount.
            var payrollAlreadyProcessed = payroll is not null
                ? IsPayrollAlreadyProcessedForSettlement(payroll)
                : payrollRunSource!.AlreadyProcessed;
            var salaryPayable = payrollAlreadyProcessed
                ? 0m
                : payroll is not null
                    ? payroll.BasicPay + payroll.Allowance.HRA + payroll.Allowance.LTA + payroll.Allowance.OtherAllowance
                    : payrollRunSource!.GrossAmount;
            var payrollBonus = payrollAlreadyProcessed || payroll is null ? 0m : payroll.Bonus;
            var deductions = payrollAlreadyProcessed
                ? 0m
                : payroll is not null
                    ? payroll.Deduction.LossOfPay + payroll.Deduction.IncomeTax + payroll.Deduction.HealthInsurance + payroll.Deduction.EPF + payroll.Deduction.ESIC + PayrollDeductionLineTotals.AdditionalAmount(payroll.DeductionLines)
                    : payrollRunSource!.DeductionAmount;
            settlement.SalaryPayable = salaryPayable; settlement.Reimbursements = 0; settlement.LeaveEncashment = 0; settlement.BonusesAndIncentives = payrollBonus; settlement.Deductions = deductions; settlement.NoticeRecovery = approvedNoticeRecovery; settlement.OutstandingDues = 0;
            settlement.SourcePayrollId = payroll?.Id;
            settlement.SourcePayrollMonth = payroll?.PayMonth ?? payrollRunSource?.PayrollMonth;
            settlement.SourcePayrollRunId = payrollRunSource?.PayrollRunId;
            settlement.SourcePayrollRunEmployeeId = payrollRunSource?.PayrollRunEmployeeId;
            settlement.NetPayable = settlement.SalaryPayable + settlement.Reimbursements + settlement.LeaveEncashment + settlement.BonusesAndIncentives - settlement.Deductions - settlement.NoticeRecovery - settlement.OutstandingDues; settlement.Status = "Calculated"; settlement.CalculatedAtUtc = DateTime.UtcNow; settlement.UpdatedBy = actorUserId; settlement.UpdatedDate = DateTime.UtcNow;
            settlement.Version++;
            exit.Version++;
            var sourceNote = payroll is not null ? $"payroll={payroll.Id}" : $"payrollRun={payrollRunSource!.PayrollRunId}; line={payrollRunSource.PayrollRunEmployeeId}";
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FinalSettlementCalculated", ActorUserId = actorUserId, Notes = $"net={settlement.NetPayable}; noticeRecovery={approvedNoticeRecovery}; {sourceNote}" });
            return await PersistFinalSettlementAndExitAsync(settlement, exit, companyId, actorUserId, request.Version, expectedSettlementVersion, isNewSettlement);
        }

        public async Task<Result> MarkEmployeeFinalSettlementPaid(MarkEmployeeFinalSettlementPaidRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId) || request is null)
                return new Result { Success = false, Message = "FINAL_SETTLEMENT_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && (x.Status == "NoticePeriod" || EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status)));
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var settlement = await _employeeFinalSettlementRepository!.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && !x.IsDeleted);
            if (settlement is null || settlement.Status != "Approved") return new Result { Success = false, Message = "FINAL_SETTLEMENT_NOT_APPROVED" };
            if (settlement.NetPayable < 0) return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "FINAL_SETTLEMENT_HAS_RECOVERY_BALANCE" };
            var expectedSettlementVersion = settlement.Version;
            settlement.Status = "Paid";
            settlement.PaidAmount = Math.Max(0m, settlement.NetPayable);
            settlement.PaidAtUtc = DateTime.UtcNow;
            settlement.UpdatedBy = actorUserId;
            settlement.UpdatedDate = DateTime.UtcNow;
            settlement.Version++;

            EmpPayRoll? sourcePayroll = null;
            if (settlement.SalaryPayable + settlement.BonusesAndIncentives > 0 && !string.IsNullOrWhiteSpace(settlement.SourcePayrollRunId))
            {
                if (!await IsPayrollRunSourceAvailableForPayment(companyId, settlement))
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "FINAL_SETTLEMENT_SOURCE_PAYROLL_CHANGED_RECALCULATE" };
            }
            else if (settlement.SalaryPayable + settlement.BonusesAndIncentives > 0)
            {
                if (_employeePayrollRepository is null || string.IsNullOrWhiteSpace(settlement.SourcePayrollId))
                    return new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "FINAL_SETTLEMENT_SOURCE_PAYROLL_NOT_AVAILABLE" };
                sourcePayroll = await _employeePayrollRepository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == settlement.SourcePayrollId);
                if (sourcePayroll is null || IsPayrollAlreadyProcessedForSettlement(sourcePayroll))
                    return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "FINAL_SETTLEMENT_SOURCE_PAYROLL_CHANGED_RECALCULATE" };
                MarkPayrollPaidViaFinalSettlement(sourcePayroll, actorUserId, request.EmployeeExitId, settlement);
            }

            var expectedExitVersion = exit.Version;
            exit.Version++;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FinalSettlementPaid", ActorUserId = actorUserId, Notes = $"amount={settlement.PaidAmount}; payroll={settlement.SourcePayrollId ?? settlement.SourcePayrollRunId ?? "none"}" });
            return await PersistFinalSettlementAndExitAsync(settlement, exit, companyId, actorUserId, expectedExitVersion, expectedSettlementVersion, isNewSettlement: false, sourcePayroll: sourcePayroll);
        }
        private async Task<bool> IsPayrollRunSourceAvailableForPayment(string companyId, EmployeeFinalSettlement settlement)
        {
            if (_payrollRunsRepository is null || _payrollRunEmployeesRepository is null || string.IsNullOrWhiteSpace(settlement.SourcePayrollRunId) || string.IsNullOrWhiteSpace(settlement.SourcePayrollRunEmployeeId)) return false;
            var run = await _payrollRunsRepository.FirstOrDefault(x => x.CompanyId == companyId && x.Id == settlement.SourcePayrollRunId && x.RunType == "FINAL_SETTLEMENT");
            if (run is null || run.Status is "PROCESSED" or "PAID" or "CLOSED") return false;
            var line = await _payrollRunEmployeesRepository.FirstOrDefault(x => x.CompanyId == companyId && x.PayrollRunId == run.Id && x.Id == settlement.SourcePayrollRunEmployeeId);
            return line is not null && line.Status is not "PROCESSED" and not "PAID" and not "CLOSED";
        }
        private static bool IsPayrollAlreadyProcessedForSettlement(EmpPayRoll payroll) =>
            payroll.IsProcessed ||
            string.Equals(payroll.WorkflowStatus, "PROCESSED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(payroll.WorkflowStatus, "PAID", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(payroll.WorkflowStatus, "CLOSED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(payroll.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(payroll.PaymentStatus, "CLOSED", StringComparison.OrdinalIgnoreCase);
        private static void MarkPayrollPaidViaFinalSettlement(EmpPayRoll payroll, string actorUserId, string employeeExitId, EmployeeFinalSettlement settlement)
        {
            var now = DateTime.UtcNow;
            payroll.IsProcessed = true;
            payroll.WorkflowStatus = "PAID";
            payroll.PaymentStatus = "PAID";
            payroll.ProcessedAt = now;
            payroll.ProcessedBy = actorUserId;
            payroll.PaymentDate = now;
            payroll.PaymentRecordedBy = actorUserId;
            payroll.PaymentReference = settlement.PaymentReference;
            payroll.PaymentMethod = settlement.PaymentMethod ?? "FINAL_SETTLEMENT";
            payroll.AuditHistory.Add(new PayrollAuditEvent
            {
                Action = "PaidViaFinalSettlement",
                ActorId = actorUserId,
                OccurredAtUtc = now,
                Notes = $"EmployeeExitId={employeeExitId}; settlement={settlement.EmployeeFinalSettlementId}"
            });
        }
        private async Task<Result> PersistFinalSettlementAndExitAsync(
            EmployeeFinalSettlement settlement,
            EmployeeExit exit,
            string companyId,
            string actorUserId,
            int expectedExitVersion,
            int expectedSettlementVersion,
            bool isNewSettlement,
            EmpPayRoll? sourcePayroll = null)
        {
            if (_mongoClient is null)
                return new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "FINAL_SETTLEMENT_TRANSACTION_NOT_CONFIGURED" };

            var now = DateTime.UtcNow;
            settlement.CompanyId = companyId;
            settlement.UpdatedBy = actorUserId;
            settlement.UpdatedDate = now;
            exit.CompanyId = companyId;
            exit.UpdatedBy = actorUserId;
            exit.UpdatedDate = now;
            var settlementFilter = Builders<EmployeeFinalSettlement>.Filter.Where(x =>
                x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId &&
                x.EmployeeFinalSettlementId == settlement.EmployeeFinalSettlementId &&
                x.Version == expectedSettlementVersion && !x.IsDeleted);
            var exitFilter = Builders<EmployeeExit>.Filter.Where(x =>
                x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId &&
                x.Version == expectedExitVersion && !x.IsDeleted);

            try
            {
                using var session = await _mongoClient.StartSessionAsync(cancellationToken: CancellationToken.None);
                await session.WithTransactionAsync(async (transactionSession, cancellationToken) =>
                {
                    if (isNewSettlement)
                    {
                        settlement.CreatedBy = actorUserId;
                        settlement.CreatedDate ??= now;
                        await _employeeFinalSettlementRepository!.GetCollection().InsertOneAsync(transactionSession, settlement, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var settlementWrite = await _employeeFinalSettlementRepository!.GetCollection().ReplaceOneAsync(
                            transactionSession, settlementFilter, settlement, cancellationToken: cancellationToken);
                        if (settlementWrite.MatchedCount != 1) throw new FinalSettlementConcurrencyException();
                    }

                    if (sourcePayroll is not null)
                    {
                        var processedStatusFilter = Builders<EmpPayRoll>.Filter.Or(Builders<EmpPayRoll>.Filter.Regex(x => x.WorkflowStatus, new BsonRegularExpression("^(PROCESSED|PAID|CLOSED)$", "i")), Builders<EmpPayRoll>.Filter.Regex(x => x.PaymentStatus, new BsonRegularExpression("^(PAID|CLOSED)$", "i")));
                        var payrollFilter = Builders<EmpPayRoll>.Filter.Eq(x => x.CompanyId, companyId) & Builders<EmpPayRoll>.Filter.Eq(x => x.Id, sourcePayroll.Id) & Builders<EmpPayRoll>.Filter.Eq(x => x.IsProcessed, false) & Builders<EmpPayRoll>.Filter.Not(processedStatusFilter);
                        var payrollWrite = await _employeePayrollRepository!.GetCollection().ReplaceOneAsync(transactionSession, payrollFilter, sourcePayroll, cancellationToken: cancellationToken);
                        if (payrollWrite.MatchedCount != 1) throw new FinalSettlementConcurrencyException();
                    }

                    var exitWrite = await _employeeExitRepository.GetCollection().ReplaceOneAsync(
                        transactionSession, exitFilter, exit, cancellationToken: cancellationToken);
                    if (exitWrite.MatchedCount != 1) throw new FinalSettlementConcurrencyException();
                    return true;
                });
                return new Result { Success = true, StatusCode = StatusCodes.Status200OK, Message = "OK" };
            }
            catch (FinalSettlementConcurrencyException)
            {
                return new Result { Success = false, StatusCode = StatusCodes.Status409Conflict, Message = "FINAL_SETTLEMENT_CHANGED_RELOAD_AND_RETRY" };
            }
            catch (MongoCommandException ex) when (ex.Code == 20)
            {
                _logger.LogError(ex, "MongoDB transactions are unavailable for final settlement {EmployeeExitId}", exit.EmployeeExitId);
                return new Result { Success = false, StatusCode = StatusCodes.Status503ServiceUnavailable, Message = "FINAL_SETTLEMENT_REQUIRES_MONGODB_TRANSACTIONS" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final settlement transaction failed for exit {EmployeeExitId}", exit.EmployeeExitId);
                return new Result { Success = false, StatusCode = StatusCodes.Status500InternalServerError, Message = "FINAL_SETTLEMENT_TRANSACTION_FAILED" };
            }
        }

        private sealed class FinalSettlementConcurrencyException : Exception { }
        public async Task<Result> GenerateEmployeeExitDocument(GenerateEmployeeExitDocumentRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId) || request is null || request.Type is not ("RelievingLetter" or "ExperienceLetter" or "FinalSettlementStatement") || _employeeExitEvidenceStorage is null) return new Result { Success = false, Message = "EXIT_DOCUMENT_INVALID_OR_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || !EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(exit.Status)) return new Result { Success = false, Message = "EXIT_DOCUMENT_NOT_AVAILABLE" };
            var documentRequest = string.IsNullOrWhiteSpace(request.RequestId) ? null : exit.FormerEmployeeRequests.FirstOrDefault(x => x.RequestId == request.RequestId);
            if (documentRequest is null || documentRequest.Status != "InProgress" || !GeneratedDocumentMatchesRequest(request.Type, documentRequest.Type)) return new Result { Success = false, Message = "FORMER_DOCUMENT_NOT_READY" };
            if (exit.Evidence.Any(x => x.FormerEmployeeRequestId == documentRequest.RequestId && x.Status is not ("Superseded" or "Archived"))) return new Result { Success = false, Message = "FORMER_DOCUMENT_DRAFT_EXISTS" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted, withDefaultFilter: false);
            var fileName = $"{request.Type}-{employee?.EmployeeId ?? exit.EmployeeUserId}.pdf";
            try
            {
                await using var pdf = new MemoryStream(BuildExitDocumentPdf(request.Type, employee, exit));
                var stored = await _employeeExitEvidenceStorage.SaveAsync(companyId, exit.EmployeeExitId, fileName, "application/pdf", pdf);
                exit.Evidence.Add(new EmployeeExitEvidence { Category = request.Type, Type = "GeneratedDocument", Status = "Verified", Visibility = "EmployeeOnly", StorageKey = stored.StorageKey, FileName = fileName, ContentType = "application/pdf", Sha256 = stored.Sha256, UploadedByUserId = actorUserId, VerifiedByUserId = actorUserId, VerifiedAtUtc = DateTime.UtcNow, FormerEmployeeRequestId = documentRequest?.RequestId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store generated employee-exit document for {EmployeeExitId}", exit.EmployeeExitId);
                return new Result { Success = false, Message = "EXIT_DOCUMENT_GENERATION_FAILED" };
            }
            var document = exit.Documents.SingleOrDefault(x => x.Type == request.Type) ?? new EmployeeExitDocument { Type = request.Type };
            if (!exit.Documents.Contains(document)) exit.Documents.Add(document);
            document.Status = "Generated"; document.Url = request.Url?.Trim(); document.GeneratedAtUtc = DateTime.UtcNow;
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ExitDocumentGenerated", ActorUserId = actorUserId, Notes = request.Type });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version), exit);
        }

        public async Task<Result> IssueEmployeeExitDocument(IssueEmployeeExitDocumentRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.EvidenceId) || string.IsNullOrWhiteSpace(request.RequestId) || !await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "FORMER_DOCUMENT_ISSUE_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status));
            var evidence = exit?.Evidence.FirstOrDefault(x => x.EvidenceId == request.EvidenceId);
            var documentRequest = string.IsNullOrWhiteSpace(request.RequestId) ? null : exit?.FormerEmployeeRequests.FirstOrDefault(x => x.RequestId == request.RequestId);
            if (exit is null || evidence is null || documentRequest is null || evidence.FormerEmployeeRequestId != documentRequest.RequestId || exit.Version != request.Version || evidence.Status != "Verified" || evidence.Visibility != "EmployeeOnly" || string.IsNullOrWhiteSpace(evidence.StorageKey)) return new Result { Success = false, Message = "FORMER_DOCUMENT_NOT_READY" };
            if (string.IsNullOrWhiteSpace(exit.PostExitAccess?.PersonalEmail) || exit.PostExitAccess.PersonalEmailVerified != true) return new Result { Success = false, Message = "POST_EXIT_CONTACT_VERIFICATION_REQUIRED" };
            if (evidence.IssuedToFormerEmployeeAtUtc.HasValue) return new Result { Success = false, Message = "FORMER_DOCUMENT_ALREADY_ISSUED" };
            var now = DateTime.UtcNow;
            evidence.FormerEmployeeRequestId = documentRequest?.RequestId; evidence.IssuedToFormerEmployeeAtUtc = now; evidence.IssuedByUserId = actorUserId;
            if (documentRequest is not null)
            {
                // A request cannot jump from Pending to Issued: HR must explicitly take it
                // into progress, then attach a verified private file to this exact request.
                if (documentRequest.Status != "InProgress")
                    return new Result { Success = false, Message = "FORMER_DOCUMENT_NOT_READY" };
                documentRequest.Status = "Issued";
                documentRequest.EvidenceId = evidence.EvidenceId;
                documentRequest.IssuedAtUtc = now;
                documentRequest.ResolvedByUserId = actorUserId;
                documentRequest.ResolvedAtUtc = now;
                documentRequest.ResolutionNotes = request.Message?.Trim();
            }
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = now;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeeDocumentIssued", ActorUserId = actorUserId, Notes = documentRequest?.Type ?? evidence.Category });
            var saved = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && !x.IsDeleted), exit);
            if (!saved.Success) return saved;
            if (_employeeExitOutboxRepository is not null)
                await _employeeExitOutboxRepository.AddOne(new EmployeeExitOutbox { CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EventType = "FormerEmployeeDocumentIssued", RecipientUserId = exit.EmployeeUserId, RecipientEmail = exit.PostExitAccess.PersonalEmail, SendEmail = true, Title = $"Your {documentRequest?.Type ?? evidence.Category} is ready", Body = "A document is available in your former-employee portal.", TargetId = $"employee-exit-{exit.EmployeeUserId}" });
            return saved;
        }

        public async Task<Result> CompleteEmployeeExitClearance(CompleteEmployeeExitClearanceRequest request, bool waive)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.ClearanceId)) return new Result { Success = false, Message = "EXIT_CLEARANCE_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var clearance = exit.Clearances.SingleOrDefault(x => x.ClearanceId == request.ClearanceId);
            if (clearance is null || clearance.Status != "Pending") return new Result { Success = false, Message = "EXIT_CLEARANCE_NOT_ACTIONABLE" };
            if (!await CanCompleteExitClearance(companyId, actorUserId, exit, clearance)) return new Result { Success = false, Message = "EXIT_CLEARANCE_FORBIDDEN" };
            if (waive && string.IsNullOrWhiteSpace(request.Notes)) return new Result { Success = false, Message = "EXIT_CLEARANCE_WAIVER_REASON_REQUIRED" };
            clearance.Status = waive ? "Waived" : "Completed"; clearance.EvidenceUrl = request.EvidenceUrl?.Trim(); clearance.OverrideReason = waive ? request.Notes?.Trim() : null; clearance.CompletedByUserId = actorUserId; clearance.CompletedAtUtc = DateTime.UtcNow;
            if (waive) { clearance.WaivedByUserId = actorUserId; clearance.WaivedAtUtc = clearance.CompletedAtUtc; }
            exit.Version++; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = waive ? "ClearanceWaived" : "ClearanceCompleted", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            // Former-employee documents are deliberately issued after an exit has been completed.
            // Do not reintroduce an IsActive requirement here: a completed Resigned/Terminated
            // case is still an authorised evidence record, while deleted cases remain excluded.
            try
            {
                return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x =>
                    x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version &&
                    !x.IsDeleted && (x.IsActive || EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status))), exit);
            }
            catch
            {
                return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
        }

        public async Task<Result> UpsertEmployeeExitHandoverTask(UpsertEmployeeExitHandoverTaskRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.Title)) return new Result { Success = false, Message = "EXIT_HANDOVER_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && (x.IsActive || EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status)));
            if (exit is null || exit.Version != request.Version || exit.Status != "NoticePeriod") return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (exit.ReportingManagerUserId != actorUserId && !await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_HANDOVER_FORBIDDEN" };
            var task = string.IsNullOrWhiteSpace(request.HandoverTaskId) ? null : exit.HandoverTasks.FirstOrDefault(x => x.HandoverTaskId == request.HandoverTaskId);
            if (task is null) { task = new EmployeeExitHandoverTask(); exit.HandoverTasks.Add(task); }
            task.Title = request.Title.Trim(); task.Details = request.Details?.Trim(); task.AssigneeUserId = request.AssigneeUserId?.Trim() ?? exit.EmployeeUserId; task.DueAtUtc = request.DueAtUtc;
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "HandoverTaskUpdated", ActorUserId = actorUserId, Notes = task.Title });
            try
            {
                return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x =>
                    x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version &&
                    !x.IsDeleted && (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated")), exit);
            }
            catch
            {
                return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
        }

        public async Task<Result> ReviewEmployeeExitHandoverTask(ReviewEmployeeExitHandoverTaskRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || request.Action is not ("Submit" or "Approve" or "ChangesRequested" or "Blocked" or "Waived")) return new Result { Success = false, Message = "EXIT_HANDOVER_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && (x.IsActive || EmployeeLifecycleStatuses.CompletedEmploymentStatuses.Contains(x.Status)));
            var task = exit?.HandoverTasks.FirstOrDefault(x => x.HandoverTaskId == request.HandoverTaskId);
            if (exit is null || task is null || !exit.IsActive || exit.IsDeleted) return new Result { Success = false, Message = "EXIT_HANDOVER_NOT_FOUND" };
            if (exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (request.Action == "Submit") { if (task.AssigneeUserId != actorUserId) return new Result { Success = false, Message = "EXIT_HANDOVER_FORBIDDEN" }; task.Status = "Submitted"; task.SubmittedByUserId = actorUserId; task.SubmittedAtUtc = DateTime.UtcNow; }
            else { if (exit.ReportingManagerUserId != actorUserId && !await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_HANDOVER_FORBIDDEN" }; task.Status = request.Action == "Approve" ? "Approved" : request.Action; task.ReviewNotes = request.Notes?.Trim(); if (request.Action == "Approve") { task.ApprovedByUserId = actorUserId; task.ApprovedAtUtc = DateTime.UtcNow; } }
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "Handover" + request.Action, ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            try
            {
                return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x =>
                    x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version &&
                    !x.IsDeleted && (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated")), exit);
            }
            catch
            {
                return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
        }

        public async Task<Result> UpsertEmployeeExitAsset(UpsertEmployeeExitAssetRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.AssetType) || request.RecoveryAmount < 0) return new Result { Success = false, Message = "EXIT_ASSET_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || exit.Status != "NoticePeriod" || !await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.AssetManage)) return new Result { Success = false, Message = "EXIT_ASSET_FORBIDDEN" };
            var asset = string.IsNullOrWhiteSpace(request.AssetExitId) ? null : exit.Assets.FirstOrDefault(x => x.AssetExitId == request.AssetExitId);
            if (asset is null) { asset = new EmployeeExitAsset(); exit.Assets.Add(asset); }
            asset.AssetType = request.AssetType.Trim(); asset.AssetIdentifier = request.AssetIdentifier?.Trim(); asset.Status = request.Status; asset.TrackingReference = request.TrackingReference?.Trim(); asset.RecoveryAmount = request.RecoveryAmount; asset.Notes = request.Notes?.Trim();
            if (request.Status == "Verified") { asset.VerifiedByUserId = actorUserId; asset.VerifiedAtUtc = DateTime.UtcNow; }
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "AssetUpdated", ActorUserId = actorUserId, Notes = asset.AssetType });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> UpsertEmployeeExitAccessItem(UpsertEmployeeExitAccessItemRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.System) || request.Mode is not ("Immediate" or "Scheduled" or "AtLwd" or "Manual" or "Phased")) return new Result { Success = false, Message = "EXIT_ACCESS_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || !await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.AccessManage)) return new Result { Success = false, Message = "EXIT_ACCESS_FORBIDDEN" };
            var item = string.IsNullOrWhiteSpace(request.AccessItemId) ? null : exit.AccessItems.FirstOrDefault(x => x.AccessItemId == request.AccessItemId);
            if (item is null) { item = new EmployeeExitAccessItem(); exit.AccessItems.Add(item); }
            item.System = request.System.Trim(); item.Mode = request.Mode; item.Status = request.Status; item.ScheduledAtUtc = request.ScheduledAtUtc; item.ExternalReference = request.ExternalReference?.Trim(); item.Notes = request.Notes?.Trim(); if (request.Status == "Revoked") item.CompletedAtUtc = DateTime.UtcNow;
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "AccessItemUpdated", ActorUserId = actorUserId, Notes = item.System });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> UpsertEmployeeExitFinanceItem(UpsertEmployeeExitFinanceItemRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.Category) || request.Amount < 0 || request.Direction is not ("PayableToEmployee" or "ReceivableFromEmployee" or "Informational")) return new Result { Success = false, Message = "EXIT_FINANCE_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || !await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.FinanceManage)) return new Result { Success = false, Message = "EXIT_FINANCE_FORBIDDEN" };
            var item = string.IsNullOrWhiteSpace(request.FinanceItemId) ? null : exit.FinanceItems.FirstOrDefault(x => x.FinanceItemId == request.FinanceItemId);
            if (item is null) { item = new EmployeeExitFinanceItem(); exit.FinanceItems.Add(item); }
            item.Category = request.Category.Trim(); item.Direction = request.Direction; item.Amount = request.Amount; item.Status = request.Status; item.Reference = request.Reference?.Trim(); item.Notes = request.Notes?.Trim();
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FinanceItemUpdated", ActorUserId = actorUserId, Notes = item.Category });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> UpsertEmployeeExitBenefitItem(UpsertEmployeeExitBenefitItemRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.Category)) return new Result { Success = false, Message = "EXIT_BENEFIT_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || !await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.BenefitsManage)) return new Result { Success = false, Message = "EXIT_BENEFIT_FORBIDDEN" };
            var item = string.IsNullOrWhiteSpace(request.BenefitItemId) ? null : exit.BenefitItems.FirstOrDefault(x => x.BenefitItemId == request.BenefitItemId);
            if (item is null) { item = new EmployeeExitBenefitItem(); exit.BenefitItems.Add(item); }
            item.Category = request.Category.Trim(); item.Status = request.Status; item.EffectiveEndDate = request.EffectiveEndDate; item.Reference = request.Reference?.Trim(); item.Notes = request.Notes?.Trim();
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "BenefitItemUpdated", ActorUserId = actorUserId, Notes = item.Category });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> AddEmployeeExitEvidence(AddEmployeeExitEvidenceRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.Category) || request.Type is not ("UploadedFile" or "Email" or "SystemRecord" or "ExternalReference" or "GeneratedDocument" or "SignedAcknowledgement")) return new Result { Success = false, Message = "EXIT_EVIDENCE_INVALID" };
            if (request.Visibility is not ("EmployeeOnly" or "EmployeeManagerHR" or "HROnly" or "HRAndFinance" or "HRAndIT")) return new Result { Success = false, Message = "EXIT_EVIDENCE_VISIBILITY_INVALID" };
            if (request.Type == "Email" && (string.IsNullOrWhiteSpace(request.EmailMessageId) || string.IsNullOrWhiteSpace(request.EmailSubject) || !request.EmailSentAtUtc.HasValue || request.EmailSentAtUtc > DateTime.UtcNow.AddMinutes(5))) return new Result { Success = false, Message = "EXIT_EVIDENCE_EMAIL_METADATA_REQUIRED" };
            if (request.Type is "SystemRecord" or "ExternalReference" && string.IsNullOrWhiteSpace(request.ExternalReference)) return new Result { Success = false, Message = "EXIT_EVIDENCE_REFERENCE_REQUIRED" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            var isHr = await IsHrExitActor(companyId, actorUserId);
            if (exit is null || exit.Version != request.Version || (exit.EmployeeUserId != actorUserId && !isHr) || (!exit.IsActive && !isHr)) return new Result { Success = false, Message = "EXIT_EVIDENCE_FORBIDDEN" };
            var documentRequest = string.IsNullOrWhiteSpace(request.FormerEmployeeRequestId) ? null : exit.FormerEmployeeRequests.FirstOrDefault(x => x.RequestId == request.FormerEmployeeRequestId);
            if (!string.IsNullOrWhiteSpace(request.FormerEmployeeRequestId) && (!isHr || request.Visibility != "EmployeeOnly" || documentRequest is null || documentRequest.Status != "InProgress" || exit.Evidence.Any(x => x.FormerEmployeeRequestId == documentRequest.RequestId && x.Status is not ("Superseded" or "Archived")))) return new Result { Success = false, Message = "FORMER_DOCUMENT_NOT_READY" };
            if (request.Type == "UploadedFile" && (string.IsNullOrWhiteSpace(request.StorageKey) || string.IsNullOrWhiteSpace(request.FileName) || string.IsNullOrWhiteSpace(request.ContentType))) return new Result { Success = false, Message = "EXIT_EVIDENCE_FILE_METADATA_REQUIRED" };
            if (!string.IsNullOrWhiteSpace(request.Sha256) && exit.Evidence.Any(x => x.Sha256 == request.Sha256.Trim() && x.Status != "Archived")) return new Result { Success = false, Message = "EXIT_EVIDENCE_DUPLICATE" };
            exit.Evidence.Add(new EmployeeExitEvidence { Category = request.Category.Trim(), Type = request.Type, Visibility = request.Visibility, StorageKey = request.StorageKey?.Trim(), FileName = request.FileName?.Trim(), ContentType = request.ContentType?.Trim(), Sha256 = request.Sha256?.Trim(), ExternalReference = request.ExternalReference?.Trim(), EmailMessageId = request.EmailMessageId?.Trim(), EmailSubject = request.EmailSubject?.Trim(), EmailSentAtUtc = request.EmailSentAtUtc, FormerEmployeeRequestId = documentRequest?.RequestId, UploadedByUserId = actorUserId });
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "EvidenceAdded", ActorUserId = actorUserId, Notes = request.Category });
            try
            {
                // Former-employee documents are issued after an exit is completed. A completed
                // Resigned/Terminated case remains writable for authorised HR evidence work.
                return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x =>
                    x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version &&
                    !x.IsDeleted && (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated")), exit);
            }
            catch
            {
                return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
        }

        public async Task<Result> VerifyEmployeeExitEvidence(VerifyEmployeeExitEvidenceRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.EvidenceId) || !await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.EvidenceManage)) return new Result { Success = false, Message = "EXIT_EVIDENCE_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated"));
            var evidence = exit?.Evidence.FirstOrDefault(x => x.EvidenceId == request.EvidenceId);
            if (exit is null || evidence is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            evidence.Status = request.Verified ? "Verified" : "NeedsCorrection"; evidence.VerifiedByUserId = actorUserId; evidence.VerifiedAtUtc = DateTime.UtcNow;
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = request.Verified ? "EvidenceVerified" : "EvidenceNeedsCorrection", ActorUserId = actorUserId, Notes = evidence.Category });
            try
            {
                return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x =>
                    x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version &&
                    !x.IsDeleted && (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated")), exit);
            }
            catch
            {
                return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
        }

        public async Task<Result> SupersedeEmployeeExitEvidence(SupersedeEmployeeExitEvidenceRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || !await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.EvidenceManage)) return new Result { Success = false, Message = "EXIT_EVIDENCE_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted && (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated"));
            var evidence = exit?.Evidence.FirstOrDefault(x => x.EvidenceId == request.EvidenceId);
            if (exit is null || evidence is null || exit.Version != request.Version || evidence.Status is "Archived" or "Superseded" || evidence.IssuedToFormerEmployeeAtUtc.HasValue) return new Result { Success = false, Message = "EXIT_EVIDENCE_NOT_ACTIONABLE" };
            evidence.Status = "Superseded"; exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "EvidenceSuperseded", ActorUserId = actorUserId, Notes = evidence.Category });
            try
            {
                var saved = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x =>
                    x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version &&
                    !x.IsDeleted && (x.IsActive || x.Status == "Resigned" || x.Status == "Terminated")), exit);
                if (saved.Success && !string.IsNullOrWhiteSpace(evidence.StorageKey) && _employeeExitEvidenceStorage is not null)
                    await _employeeExitEvidenceStorage.DeleteAsync(evidence.StorageKey);
                return saved;
            }
            catch
            {
                return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
        }

        public async Task<Result> UpdateEmployeeExitPostExitContact(UpdateEmployeeExitPostExitContactRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.PersonalEmail) || !request.PersonalEmail.Contains('@')) return new Result { Success = false, Message = "EXIT_POST_EXIT_CONTACT_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || !exit.IsActive || exit.IsDeleted) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (exit.EmployeeUserId != actorUserId) return new Result { Success = false, Message = "EXIT_POST_EXIT_CONTACT_FORBIDDEN" };
            if (exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var personalEmail = request.PersonalEmail.Trim().ToLowerInvariant();
            exit.PostExitAccess ??= new EmployeeExitPostExitAccess();
            var emailChanged = !string.Equals(exit.PostExitAccess.PersonalEmail, personalEmail, StringComparison.OrdinalIgnoreCase);
            exit.PostExitAccess.PersonalEmail = personalEmail; exit.PostExitAccess.Mobile = request.Mobile?.Trim();
            if (emailChanged || !exit.PostExitAccess.PersonalEmailVerified)
            {
                var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                exit.PostExitAccess.PersonalEmailVerified = false;
                exit.PostExitAccess.PersonalEmailVerifiedAtUtc = null;
                exit.PostExitAccess.PersonalEmailVerificationTokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
                exit.PostExitAccess.PersonalEmailVerificationExpiresAtUtc = DateTime.UtcNow.AddHours(48);
                var verificationUrl = $"{ConfigManager.AppSettings.AppUrl.TrimEnd('/')}/former-employee?verify-contact=true&token={Uri.EscapeDataString(rawToken)}";
                var safeUrl = WebUtility.HtmlEncode(verificationUrl);
                _priorityTaskQueue.QueueBackgroundWorkItem(_ => _middlewareService.EmailSendAndSave(new EmpEmailLogs
                {
                    CompanyId = companyId, UserTo = actorUserId, UserFrom = actorUserId, Email = personalEmail,
                    Subject = "Verify your post-exit contact email",
                    Body = $"<p>Confirm this email address before your offboarding can be completed.</p><p><a href=\"{safeUrl}\">Verify post-exit contact</a></p>",
                    EmailLogType = EnumsHelper.MailType.ResignationMail
                }), 1);
            }
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "PostExitContactUpdated", ActorUserId = actorUserId });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> AcknowledgeEmployeeExit(AcknowledgeEmployeeExitRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            var exit = request is null ? null : await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            if (exit is null || exit.EmployeeUserId != actorUserId || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_ACKNOWLEDGEMENT_FORBIDDEN" };
            exit.PostExitAccess ??= new EmployeeExitPostExitAccess(); exit.PostExitAccess.AcknowledgementStatus = "Acknowledged"; exit.PostExitAccess.AcknowledgedAtUtc = DateTime.UtcNow; exit.PostExitAccess.AcknowledgementNotes = request.Notes?.Trim();
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ExitAcknowledged", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version), exit);
        }

        public async Task<Result> CreateEmployeeExitFormerEmployeeRequest(CreateEmployeeExitFormerEmployeeRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.Details) || request.Type is not ("RelievingLetter" or "ExperienceLetter" or "FnFStatement" or "TaxDocument" or "Correction" or "Other")) return new Result { Success = false, Message = "FORMER_EMPLOYEE_REQUEST_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            if (exit is null || exit.EmployeeUserId != actorUserId || exit.Version != request.Version) return new Result { Success = false, Message = "FORMER_EMPLOYEE_REQUEST_FORBIDDEN" };
            exit.FormerEmployeeRequests.Add(new EmployeeExitFormerEmployeeRequest { Type = request.Type, Details = request.Details.Trim() }); exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "EmployeeExitDocumentRequested", ActorUserId = actorUserId, Notes = request.Type });
            var saved = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version), exit);
            if (saved.Success) await QueueDocumentRequestNotifications(companyId, actorUserId, exit);
            return saved;
        }

        public async Task<Result> ResolveEmployeeExitFormerEmployeeRequest(ResolveEmployeeExitFormerEmployeeRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || string.IsNullOrWhiteSpace(request.EmployeeExitId) || string.IsNullOrWhiteSpace(request.RequestId) || request.Status is not ("InProgress" or "Issued" or "Closed"))
                return new Result { Success = false, Message = "FORMER_EMPLOYEE_REQUEST_RESOLUTION_INVALID" };
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "FORMER_EMPLOYEE_REQUEST_RESOLUTION_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            var formerEmployeeRequest = exit?.FormerEmployeeRequests.FirstOrDefault(x => x.RequestId == request.RequestId);
            if (exit is null || formerEmployeeRequest is null || exit.Version != request.Version)
                return new Result { Success = false, Message = "FORMER_EMPLOYEE_REQUEST_CHANGED_OR_NOT_FOUND" };
            if (formerEmployeeRequest.Status is "Issued" or "Closed") return new Result { Success = false, Message = "FORMER_EMPLOYEE_REQUEST_ALREADY_FINALIZED" };

            if (request.Status == "InProgress" && formerEmployeeRequest.Status != "Pending")
                return new Result { Success = false, Message = "FORMER_EMPLOYEE_REQUEST_NOT_PENDING" };
            formerEmployeeRequest.Status = request.Status;
            formerEmployeeRequest.ResolvedByUserId = actorUserId;
            formerEmployeeRequest.ResolvedAtUtc = DateTime.UtcNow;
            formerEmployeeRequest.ResolutionNotes = request.Notes?.Trim();
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FormerEmployeeRequest" + request.Status, ActorUserId = actorUserId, Notes = formerEmployeeRequest.Type });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version), exit);
        }

        public async Task<Result> UpdateEmployeeExitClearanceStatus(UpdateEmployeeExitClearanceStatusRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (request is null || request.Status is not ("Completed" or "Issue")) return new Result { Success = false, Message = "EXIT_CLEARANCE_STATUS_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var clearance = exit.Clearances.SingleOrDefault(x => x.ClearanceId == request.ClearanceId);
            if (clearance is null || clearance.Status is "Completed" or "Waived") return new Result { Success = false, Message = "EXIT_CLEARANCE_NOT_ACTIONABLE" };
            if (!await CanCompleteExitClearance(companyId, actorUserId, exit, clearance)) return new Result { Success = false, Message = "EXIT_CLEARANCE_FORBIDDEN" };
            clearance.Status = request.Status; clearance.EvidenceUrl = request.EvidenceUrl?.Trim(); clearance.OverrideReason = request.Notes?.Trim();
            if (request.Status == "Completed") { clearance.CompletedByUserId = actorUserId; clearance.CompletedAtUtc = DateTime.UtcNow; }
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = $"Clearance{request.Status}", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> RecordEmployeeExitInterview(RecordEmployeeExitInterviewRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId) || request is null) return new Result { Success = false, Message = "EXIT_INTERVIEW_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            exit.ExitInterview = new EmployeeExitInterview { EmployeeFeedback = request.EmployeeFeedback?.Trim(), ManagerFeedback = request.ManagerFeedback?.Trim(), HrNotes = request.HrNotes?.Trim(), IsRehireEligible = request.IsRehireEligible, ConductedByUserId = actorUserId, ConductedAtUtc = DateTime.UtcNow };
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ExitInterviewRecorded", ActorUserId = actorUserId });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> UpsertEmployeeExitReassignment(UpsertEmployeeExitReassignmentRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId) || request is null || string.IsNullOrWhiteSpace(request.Responsibility) || string.IsNullOrWhiteSpace(request.NewOwnerUserId)) return new Result { Success = false, Message = "EXIT_REASSIGNMENT_INVALID_OR_FORBIDDEN" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.IsActive && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            var newOwner = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == request.NewOwnerUserId && x.Status && !x.IsDeleted);
            if (newOwner is null || newOwner.UserId == exit.EmployeeUserId) return new Result { Success = false, Message = "EXIT_REASSIGNMENT_OWNER_INVALID" };
            exit.ResponsibilityReassignments.Add(new EmployeeExitResponsibilityReassignment { Responsibility = request.Responsibility.Trim(), NewOwnerUserId = request.NewOwnerUserId, Notes = request.Notes?.Trim() });
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ResponsibilityReassigned", ActorUserId = actorUserId, Notes = request.Responsibility.Trim() });
            return await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.IsActive), exit);
        }

        public async Task<Result> RejoinEmployee(RejoinEmployeeRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "REJOIN_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.Reason)) return new Result { Success = false, Message = "REJOIN_REASON_REQUIRED" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && !x.IsDeleted);
            if (exit is null || exit.Version != request.Version || exit.Status == EmployeeLifecycleStatuses.Deceased || !EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(exit.Status)) return new Result { Success = false, Message = "REJOIN_NOT_ALLOWED" };
            if (exit.PolicySnapshot?.AllowRejoin == false || exit.ExitInterview?.IsRehireEligible != true) return new Result { Success = false, Message = "REJOIN_NOT_ELIGIBLE" };
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            if (!EmploymentExitRules.CanRejoin(employee)) return new Result { Success = false, Message = "REJOIN_NOT_ALLOWED" };
            var priorStatus = employee.Status;
            var priorEmploymentStatus = employee.EmploymentStatus;
            var priorLastWorkingDate = employee.LastWorkingDate;
            var priorExitDate = employee.ExitDate;
            var priorLifecycle = employee.Lifecycle;
            employee.Status = true; employee.EmploymentStatus = "Active"; employee.LastWorkingDate = null; employee.ExitDate = null;
            employee.Lifecycle = new EmployeeLifecycleState
            {
                EmploymentStatus = EmployeeLifecycleStatuses.Active,
                AvailabilityStatus = EmployeeLifecycleStatuses.Working,
                AccountStatus = EmployeeLifecycleStatuses.AccountActive,
                PayrollStatus = EmployeeLifecycleStatuses.PayrollActive,
                OffboardingStatus = "None",
                FormerPortalStatus = "NotEligible",
                AlumniStatus = employee.Lifecycle?.AlumniStatus ?? "NotMember",
                Version = (employee.Lifecycle?.Version ?? 0) + 1,
                History = employee.Lifecycle?.History ?? []
            };
            employee.Lifecycle.History.Add(new EmployeeLifecycleStatusHistory
            {
                Dimension = nameof(EmployeeLifecycleState.EmploymentStatus), PreviousValue = priorEmploymentStatus ?? "Terminated",
                NewValue = EmployeeLifecycleStatuses.Active, ReasonCode = "REJOIN", Reason = request.Reason.Trim(),
                EffectiveFromUtc = DateTime.UtcNow, ChangedBy = actorUserId, ChangedAtUtc = DateTime.UtcNow
            });
            employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> updateEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && !x.Status;
            var result = await _employeeRepository.Update(updateEmployee, employee);
            if (!result.Success) return result;
            exit.Status = "Rejoined"; exit.IsActive = false;
            if (exit.PostExitAccess is not null) exit.PostExitAccess.Enabled = false;
            exit.Version++; exit.UpdatedBy = actorUserId; exit.UpdatedDate = DateTime.UtcNow; exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "Rejoined", ActorUserId = actorUserId, Notes = request.Reason.Trim() });
            var exitResult = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version), exit);
            if (exitResult.Success) return exitResult;

            // The generic repository does not expose a shared Mongo session. Restore the
            // employee when the optimistic exit-case write loses its race or fails.
            employee.Status = priorStatus;
            employee.EmploymentStatus = priorEmploymentStatus;
            employee.LastWorkingDate = priorLastWorkingDate;
            employee.ExitDate = priorExitDate;
            employee.Lifecycle = priorLifecycle;
            employee.UpdatedBy = actorUserId;
            employee.UpdatedDate = DateTime.UtcNow;
            await _employeeRepository.Update(Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && !x.IsDeleted && x.Status), employee);
            return exitResult;
        }

        public async Task<Result> ExceptionalCompleteEmployeeExit(ExceptionalCompleteEmployeeExitRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [Codeji.CMS.Utility.Constraints.Permission.Override], actorUserId, companyId)) return new Result { Success = false, Message = "EXIT_EXCEPTION_FORBIDDEN" };
            if (request is null || string.IsNullOrWhiteSpace(request.Reason)) return new Result { Success = false, Message = "EXIT_EXCEPTION_REASON_REQUIRED" };
            // Exceptional completion is deliberately routed through the same atomic completion path.
            // It can no longer bypass clearance, session-revocation, payroll, or audit safeguards.
            return await CompleteEmployeeExit(new CompleteEmployeeExitRequest
            {
                EmployeeExitId = request.EmployeeExitId,
                Version = request.Version,
                Notes = $"Exceptional completion request: {request.Reason.Trim()}"
            });
        }

        public async Task<Result<EmployeeExitCaseDto>> GetEmployeeExitCase(string employeeUserId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(employeeUserId)) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_REQUEST_INVALID" };
            // A caller may always see their own case. Other cases retain the controller's
            // Employees.View authorization when this endpoint is later surfaced to managers.
            if (employeeUserId != actorUserId) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_FORBIDDEN" };
            // The employee-facing endpoint is for the current workflow only. Historical
            // completed, cancelled and withdrawn records stay in the audit trail but do
            // not block a fresh resignation or render post-exit contact controls.
            var exits = await _employeeExitRepository.GetAll(x => x.CompanyId == companyId && x.EmployeeUserId == employeeUserId && !x.IsDeleted);
            var exit = exits.Where(x => x.IsActive).OrderByDescending(x => x.InitiatedAtUtc).FirstOrDefault()
                ?? exits.Where(x => x.Status == "Withdrawn").OrderByDescending(x => x.InitiatedAtUtc).FirstOrDefault();
            if (exit is null) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_NOT_FOUND" };
            // Compatibility repair for the earlier direct-waiver path. That path changed
            // the LWD but could leave the parent case in DiscussionRequested. Restrict the
            // repair to a completed company waiver/release whose latest action is the
            // approval itself, so an active, later discussion is never overwritten.
            var legacyDirectApproval = exit.NoticeAdjustments.LastOrDefault(x =>
                x.Status == "Approved" && x.Type is "CompanyEarlyRelease" or "FullWaiver");
            if (exit.Status == "DiscussionRequested"
                && legacyDirectApproval is not null
                && IndiaTime.ToBusinessDate(exit.LastWorkingDate) == IndiaTime.ToBusinessDate(legacyDirectApproval.RequestedLastWorkingDate)
                && exit.AuditHistory.LastOrDefault()?.Action == "NoticeAdjustmentApproved")
            {
                var version = exit.Version;
                exit.Status = "NoticePeriod";
                exit.DiscussionRequestedByUserId = null;
                exit.DiscussionReturnStatus = null;
                exit.Version++;
                exit.UpdatedBy = actorUserId;
                exit.UpdatedDate = DateTime.UtcNow;
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "LegacyDirectNoticeApprovalRepaired", ActorUserId = actorUserId, Notes = "Restored NoticePeriod after a completed company waiver/release." });
                var repaired = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == version && x.IsActive), exit);
                if (!repaired.Success)
                    return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
            var employeeCase = new EmployeeExitCaseDto
            {
                EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = exit.EmployeeUserId, ExitType = exit.ExitType, Status = exit.Status,
                LastWorkingDate = exit.LastWorkingDate, Reason = exit.Reason, Notes = exit.Notes, InitiatedAtUtc = exit.InitiatedAtUtc, CompletedAtUtc = exit.CompletedAtUtc, Version = exit.Version,
                PresetLevel = exit.PolicySnapshot?.PresetLevel ?? "Custom", Features = exit.PolicySnapshot?.Features.Select(ToFeatureRequest).ToList() ?? [],
                PostExitContact = exit.PostExitAccess is null ? null : new EmployeeExitPostExitContactDto { PersonalEmail = exit.PostExitAccess.PersonalEmail, Mobile = exit.PostExitAccess.Mobile, PersonalEmailVerified = exit.PostExitAccess.PersonalEmailVerified, PortalEnabled = exit.PostExitAccess.Enabled, PersonalEmailVerificationExpiresAtUtc = exit.PostExitAccess.PersonalEmailVerificationExpiresAtUtc },
                Clearances = exit.Clearances.Select(x => new EmployeeExitClearanceDto { ClearanceId = x.ClearanceId, TemplateKey = x.TemplateKey, Description = x.Description, Category = x.Category, Status = x.Status, IsMandatory = x.IsMandatory, DueAtUtc = x.DueAtUtc }).ToList(),
                HandoverTasks = exit.HandoverTasks.Select(x => new EmployeeExitHandoverTaskDto { HandoverTaskId = x.HandoverTaskId, Title = x.Title, Details = x.Details, AssigneeUserId = x.AssigneeUserId, DueAtUtc = x.DueAtUtc, Status = x.Status == "Approve" ? "Approved" : x.Status }).ToList(), Assets = exit.Assets.Select(x => new EmployeeExitAssetDto { AssetExitId = x.AssetExitId, AssetType = x.AssetType, AssetIdentifier = x.AssetIdentifier, Status = x.Status, TrackingReference = x.TrackingReference, RecoveryAmount = x.RecoveryAmount, Notes = x.Notes }).ToList(), AccessItems = exit.AccessItems.Select(x => new EmployeeExitAccessItemDto { AccessItemId = x.AccessItemId, System = x.System, Mode = x.Mode, Status = x.Status, ScheduledAtUtc = x.ScheduledAtUtc, ExternalReference = x.ExternalReference, Notes = x.Notes }).ToList(), FinanceItems = exit.FinanceItems.Select(x => new EmployeeExitFinanceItemDto { FinanceItemId = x.FinanceItemId, Category = x.Category, Direction = x.Direction, Amount = x.Amount, Status = x.Status, Reference = x.Reference, Notes = x.Notes }).ToList(), BenefitItems = exit.BenefitItems.Select(x => new EmployeeExitBenefitItemDto { BenefitItemId = x.BenefitItemId, Category = x.Category, Status = x.Status, EffectiveEndDate = x.EffectiveEndDate, Reference = x.Reference, Notes = x.Notes }).ToList(), Evidence = exit.Evidence.Select(x => new EmployeeExitEvidenceDetailDto { EvidenceId = x.EvidenceId, Category = x.Category, Type = x.Type, Status = x.Status, Visibility = x.Visibility, FileName = x.FileName, ContentType = x.ContentType, UploadedAtUtc = x.UploadedAtUtc, FormerEmployeeRequestId = x.FormerEmployeeRequestId, IssuedToFormerEmployeeAtUtc = x.IssuedToFormerEmployeeAtUtc, SeenByFormerEmployeeAtUtc = x.SeenByFormerEmployeeAtUtc }).ToList(),
                FormerEmployeeRequests = exit.FormerEmployeeRequests.Select(x => new FormerEmployeePortalRequestDto { RequestId = x.RequestId, Type = x.Type, Status = x.Status, Details = x.Details, RequestedAtUtc = x.RequestedAtUtc, EvidenceId = x.EvidenceId, IssuedAtUtc = x.IssuedAtUtc }).ToList(),
                ResponsibilityReassignments = exit.ResponsibilityReassignments.Select(x => new EmployeeExitResponsibilityReassignmentDto { Responsibility = x.Responsibility, NewOwnerUserId = x.NewOwnerUserId, Notes = x.Notes }).ToList(),
                ExitInterview = exit.ExitInterview is null ? null : new EmployeeExitInterviewDto { EmployeeFeedback = exit.ExitInterview.EmployeeFeedback, ManagerFeedback = exit.ExitInterview.ManagerFeedback, HrNotes = exit.ExitInterview.HrNotes, IsRehireEligible = exit.ExitInterview.IsRehireEligible, ConductedAtUtc = exit.ExitInterview.ConductedAtUtc },
                FinalSettlement = await BuildFinalSettlementDto(exit),
                Documents = exit.Documents.Select(x => new EmployeeExitDocumentDto { Type = x.Type, Status = x.Status, Url = x.Url }).ToList(),
                Withdrawal = exit.Withdrawal is null ? null : new EmployeeExitWithdrawalDto { Status = exit.Withdrawal.Status, Reason = exit.Withdrawal.Reason },
                NoticeAdjustments = exit.NoticeAdjustments.Select(x => new EmployeeExitNoticeAdjustmentDto { NoticeAdjustmentId = x.NoticeAdjustmentId, Type = x.Type, Status = x.Status, RequestedLastWorkingDate = x.RequestedLastWorkingDate, RequiredNoticeDays = x.RequiredNoticeDays, ServedNoticeDays = x.ServedNoticeDays, ShortfallDays = x.ShortfallDays, WaivedDays = x.WaivedDays, RecoveryAmount = x.RecoveryAmount, Reason = x.Reason }).ToList(),
                AuditHistory = exit.AuditHistory.Select(x => new EmployeeExitAuditEventDto { Action = x.Action, ActorUserId = x.ActorUserId, AtUtc = x.AtUtc, Notes = x.Notes }).ToList(),
                DiscussionRequest = exit.DiscussionRequest, DiscussionResponse = exit.DiscussionResponse, DiscussionMode = exit.DiscussionMode,
                DiscussionParticipantUserIds = exit.DiscussionParticipantUserIds, DiscussionMessages = exit.DiscussionMessages.Select(x => new EmployeeExitDiscussionMessageDto { MessageId = x.MessageId, AuthorUserId = x.AuthorUserId, Message = x.Message, CreatedAtUtc = x.CreatedAtUtc }).ToList(),
                PermittedActions = exit.Status switch { "PendingManagerApproval" or "PendingHrApproval" => ["Withdraw"], "DiscussionRequested" => ["RespondDiscussion", "Withdraw"], "NoticePeriod" => ["Withdraw"], _ => [] },
                PendingHandoverCount = exit.HandoverTasks.Count(x => x.Status is not ("Approve" or "Approved" or "Waived")), PendingAssetCount = exit.Assets.Count(x => x.Status is not ("Verified" or "Waived" or "RecoveredFinancially")), PendingAccessCount = exit.AccessItems.Count(x => x.Status is not ("Revoked" or "Waived")), PendingFinanceCount = exit.FinanceItems.Count(x => x.Status is not ("Completed" or "Waived")), PendingBenefitCount = exit.BenefitItems.Count(x => x.Status is not ("Completed" or "Waived"))
            };
            if (exit.Status == "NoticePeriod" && exit.HandoverTasks.Any(x => x.AssigneeUserId == actorUserId && x.Status is not ("Submitted" or "Approve" or "Approved" or "Waived"))) employeeCase.PermittedActions.Add("SubmitHandover");
            return new Result<EmployeeExitCaseDto> { Success = true, MethodResult = employeeCase };
        }

        public async Task<Result<List<EmployeeExitHistoryDto>>> GetOwnEmployeeExitHistory(string employeeUserId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrWhiteSpace(employeeUserId)) return new Result<List<EmployeeExitHistoryDto>> { Success = false, Message = "EXIT_REQUEST_INVALID" };
            if (employeeUserId != actorUserId) return new Result<List<EmployeeExitHistoryDto>> { Success = false, Message = "EXIT_CASE_FORBIDDEN" };

            var exits = await _employeeExitRepository.GetAll(x => x.CompanyId == companyId && x.EmployeeUserId == employeeUserId && x.Status == "Withdrawn" && !x.IsDeleted);
            return new Result<List<EmployeeExitHistoryDto>>
            {
                Success = true,
                MethodResult = exits.OrderByDescending(x => x.InitiatedAtUtc).Select(x => new EmployeeExitHistoryDto
                {
                    EmployeeExitId = x.EmployeeExitId,
                    Status = x.Status,
                    LastWorkingDate = x.LastWorkingDate,
                    InitiatedAtUtc = x.InitiatedAtUtc,
                    WithdrawalReason = x.Withdrawal?.Reason
                }).ToList()
            };
        }

        public async Task<Result<EmployeeExitCaseDto>> GetEmployeeExitCaseForEvidence(string employeeExitId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor); var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == employeeExitId && !x.IsDeleted);
            if (exit is null) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_NOT_FOUND" };
            return exit.EmployeeUserId == actorUserId ? await GetEmployeeExitCase(exit.EmployeeUserId) : await GetEmployeeExitCaseForManagement(exit.EmployeeUserId);
        }

        public async Task<Result<EmployeeExitEvidenceDto>> GetEmployeeExitEvidence(string employeeExitId, string evidenceId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == employeeExitId && !x.IsDeleted);
            var evidence = exit?.Evidence.FirstOrDefault(x => x.EvidenceId == evidenceId && x.Status != "Archived");
            if (exit is null || evidence is null) return new Result<EmployeeExitEvidenceDto> { Success = false, Message = "EXIT_EVIDENCE_NOT_FOUND" };

            var isEmployee = exit.EmployeeUserId == actorUserId;
            var isHr = await IsHrExitActor(companyId, actorUserId);
            var isAssignedManager = exit.ReportingManagerUserId == actorUserId;
            var canManageFinance = await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.FinanceManage);
            var canManageAccess = await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.AccessManage);

            // The evidence visibility label is the authoritative audience.  A finance or IT
            // operator must not be forced through the HR-only management-case endpoint, and
            // must not receive unrelated evidence merely because they have a broad download right.
            var audienceAllowed = evidence.Visibility switch
            {
                // "EmployeeOnly" limits the employee-facing audience; authorised HR still
                // needs to verify, preview and issue the file through the case workspace.
                "EmployeeOnly" => isEmployee || isHr,
                "EmployeeManagerHR" => isEmployee || isAssignedManager || isHr,
                "HROnly" => isHr,
                "HRAndFinance" => isHr || canManageFinance,
                "HRAndIT" => isHr || canManageAccess,
                _ => false
            };
            if (!audienceAllowed) return new Result<EmployeeExitEvidenceDto> { Success = false, Message = "EXIT_EVIDENCE_FORBIDDEN" };

            var hasDownloadPermission = isEmployee || isHr
                || await _roleService.VerifyUserAccess(Codeji.CMS.Utility.Constraints.AppModule.Offboarding, [Codeji.CMS.Utility.Constraints.Permission.Download], actorUserId, companyId);
            if (!hasDownloadPermission && !(evidence.Visibility == "HRAndFinance" && canManageFinance) && !(evidence.Visibility == "HRAndIT" && canManageAccess))
                return new Result<EmployeeExitEvidenceDto> { Success = false, Message = "EXIT_EVIDENCE_DOWNLOAD_FORBIDDEN" };

            return new Result<EmployeeExitEvidenceDto>
            {
                Success = true,
                MethodResult = new EmployeeExitEvidenceDto { EvidenceId = evidence.EvidenceId, StorageKey = evidence.StorageKey, FileName = evidence.FileName, ContentType = evidence.ContentType }
            };
        }

        public async Task<Result<EmployeeExitCaseDto>> GetEmployeeExitCaseForManagement(string employeeUserId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            // Employees can have earlier withdrawn records as well as one active resignation.
            // Always surface the active workflow to a manager/HR first; history is read-only
            // fallback only when there is no active case.
            var employeeExits = await _employeeExitRepository.GetAll(x => x.CompanyId == companyId && x.EmployeeUserId == employeeUserId && !x.IsDeleted);
            var exit = employeeExits.Where(x => x.IsActive).OrderByDescending(x => x.InitiatedAtUtc).FirstOrDefault()
                ?? employeeExits.Where(x => EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(x.Status)).OrderByDescending(x => x.CompletedAtUtc ?? x.InitiatedAtUtc).FirstOrDefault()
                ?? employeeExits.Where(x => x.Status == "Withdrawn").OrderByDescending(x => x.InitiatedAtUtc).FirstOrDefault();
            if (exit is null) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_NOT_FOUND" };
            if (exit.EmployeeUserId == actorUserId) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_FORBIDDEN" };
            var isHr = await IsHrExitActor(companyId, actorUserId);
            var isAssignedManager = exit.ReportingManagerUserId == actorUserId;
            var legacyDirectApproval = exit.NoticeAdjustments.LastOrDefault(x =>
                x.Status == "Approved" && x.Type is "CompanyEarlyRelease" or "FullWaiver");
            if ((isHr || isAssignedManager)
                && exit.Status == "DiscussionRequested"
                && legacyDirectApproval is not null
                && IndiaTime.ToBusinessDate(exit.LastWorkingDate) == IndiaTime.ToBusinessDate(legacyDirectApproval.RequestedLastWorkingDate)
                && exit.AuditHistory.LastOrDefault()?.Action == "NoticeAdjustmentApproved")
            {
                var version = exit.Version;
                exit.Status = "NoticePeriod";
                exit.DiscussionRequestedByUserId = null;
                exit.DiscussionReturnStatus = null;
                exit.Version++;
                exit.UpdatedBy = actorUserId;
                exit.UpdatedDate = DateTime.UtcNow;
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "LegacyDirectNoticeApprovalRepaired", ActorUserId = actorUserId, Notes = "Restored NoticePeriod after a completed company waiver/release." });
                var repaired = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == version && x.IsActive), exit);
                if (!repaired.Success)
                    return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            }
            var isDiscussionParticipant = exit.Status == "DiscussionRequested" && exit.DiscussionMode == "Joint" && exit.DiscussionParticipantUserIds.Contains(actorUserId);
            if (!isHr && !isAssignedManager && !isDiscussionParticipant) return new Result<EmployeeExitCaseDto> { Success = false, Message = "EXIT_CASE_FORBIDDEN" };
            if (isDiscussionParticipant && !isHr && !isAssignedManager)
                return new Result<EmployeeExitCaseDto>
                {
                    Success = true,
                    MethodResult = new EmployeeExitCaseDto
                    {
                        EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = exit.EmployeeUserId, ExitType = exit.ExitType, Status = exit.Status, LastWorkingDate = exit.LastWorkingDate, Version = exit.Version,
                        DiscussionRequest = exit.DiscussionRequest, DiscussionResponse = exit.DiscussionResponse, DiscussionMode = exit.DiscussionMode,
                        DiscussionParticipantUserIds = exit.DiscussionParticipantUserIds,
                        DiscussionMessages = exit.DiscussionMessages.Select(x => new EmployeeExitDiscussionMessageDto { MessageId = x.MessageId, AuthorUserId = x.AuthorUserId, Message = x.Message, CreatedAtUtc = x.CreatedAtUtc }).ToList(),
                        PermittedActions = ["RespondJointDiscussion"]
                    }
                };
            // Repair active cases created before templates/default operational tasks were
            // introduced. This is deliberately HR-only and optimistic-concurrency guarded.
            if (isHr && exit.Status == "NoticePeriod")
            {
                var beforeCount = exit.Clearances.Count + exit.HandoverTasks.Count + exit.Assets.Count + exit.AccessItems.Count + exit.FinanceItems.Count + exit.BenefitItems.Count;
                var expectedVersion = exit.Version;
                await PopulateExitClearances(exit);
                var afterCount = exit.Clearances.Count + exit.HandoverTasks.Count + exit.Assets.Count + exit.AccessItems.Count + exit.FinanceItems.Count + exit.BenefitItems.Count;
                if (afterCount > beforeCount)
                {
                    exit.Version++;
                    var repaired = await _employeeExitRepository.Update(Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == expectedVersion && x.IsActive), exit);
                    if (!repaired.Success)
                        exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.IsActive && !x.IsDeleted) ?? exit;
                }
            }
            var actions = new List<string>();
            // A reporting manager can act only while their review is pending.  Keeping these
            // actions on a withdrawn or HR-owned case produced invalid buttons in the UI.
            var isDiscussionDecisionOwner = exit.Status == "DiscussionRequested" && exit.DiscussionRequestedByUserId == actorUserId;
            if (isAssignedManager && (exit.Status == "PendingManagerApproval" || isDiscussionDecisionOwner)) actions.AddRange(["Approve", "Discuss", "JointDiscussion", "Reject", "RequestDiscussion"]);
            if (exit.Withdrawal?.Status == "Pending" && (isAssignedManager || isHr)) actions.Add("DecideWithdrawal");
            if (exit.Status == "NoticePeriod" && (isAssignedManager || isHr)) actions.AddRange(["ManageHandover", "ApproveHandover"]);
            if (isHr)
            {
                // HR may not bypass an assigned manager's review. Once the case reaches
                // the HR stage, or HR owns the discussion, expose the HR review actions.
                if (exit.Status == "PendingHrApproval" || isDiscussionDecisionOwner) actions.AddRange(["Approve", "Discuss", "JointDiscussion", "Reject", "RequestDiscussion", "Cancel"]);
                if (exit.Status == "NoticePeriod") actions.AddRange(["UpdateLastWorkingDate", "Complete", "ExceptionalComplete", "ManageClearance", "RecordInterview", "CalculateSettlement", "ApproveSettlement", "MarkSettlementPaid"]);
                // The former employee stays in the final-month payroll run through
                // EmploymentPeriod. F&F actions therefore remain available after the
                // employment record has been completed, without reopening the case.
                if (EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(exit.Status)) actions.AddRange(["CalculateSettlement", "ApproveSettlement", "MarkSettlementPaid"]);
                if ((exit.Status is "PendingManagerApproval" or "PendingHrApproval" or "NoticePeriod") &&
                    exit.NoticeAdjustments.Any(x => x.Status == "Pending"))
                    actions.Add("DecideNoticeAdjustment");
                if (exit.FormerEmployeeRequests.Any(x => x.Status is "Pending" or "InProgress")) actions.Add("ManageDocumentRequests");
                if (exit.IsActive) actions.Add("WaiveClearance");
                if (exit.Status != EmployeeLifecycleStatuses.Deceased && EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(exit.Status)) actions.Add("Rejoin");
            }
            if (exit.Status != EmployeeLifecycleStatuses.Deceased && EmployeeLifecycleStatuses.IsCompletedEmploymentStatus(exit.Status) && exit.PolicySnapshot?.EnableFormerEmployeePortal == true && await CanManageExitOperation(companyId, actorUserId, Codeji.CMS.Utility.Constraints.Permission.PortalManage)) actions.Add("ManageFormerPortal");
            var managementCase = new EmployeeExitCaseDto
            {
                EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = exit.EmployeeUserId, ExitType = exit.ExitType, Status = exit.Status, Version = exit.Version,
                LastWorkingDate = exit.LastWorkingDate, Reason = exit.Reason, Notes = exit.Notes, InitiatedAtUtc = exit.InitiatedAtUtc, CompletedAtUtc = exit.CompletedAtUtc,
                PresetLevel = exit.PolicySnapshot?.PresetLevel ?? "Custom", Features = exit.PolicySnapshot?.Features.Select(ToFeatureRequest).ToList() ?? [],
                PostExitContact = exit.PostExitAccess is null ? null : new EmployeeExitPostExitContactDto { PersonalEmail = exit.PostExitAccess.PersonalEmail, Mobile = exit.PostExitAccess.Mobile, PersonalEmailVerified = exit.PostExitAccess.PersonalEmailVerified, PortalEnabled = exit.PostExitAccess.Enabled, PersonalEmailVerificationExpiresAtUtc = exit.PostExitAccess.PersonalEmailVerificationExpiresAtUtc },
                Clearances = exit.Clearances.Select(x => new EmployeeExitClearanceDto { ClearanceId = x.ClearanceId, TemplateKey = x.TemplateKey, Description = x.Description, Category = x.Category, Status = x.Status, IsMandatory = x.IsMandatory, DueAtUtc = x.DueAtUtc }).ToList(),
                HandoverTasks = exit.HandoverTasks.Select(x => new EmployeeExitHandoverTaskDto { HandoverTaskId = x.HandoverTaskId, Title = x.Title, Details = x.Details, AssigneeUserId = x.AssigneeUserId, DueAtUtc = x.DueAtUtc, Status = x.Status == "Approve" ? "Approved" : x.Status }).ToList(), Assets = exit.Assets.Select(x => new EmployeeExitAssetDto { AssetExitId = x.AssetExitId, AssetType = x.AssetType, AssetIdentifier = x.AssetIdentifier, Status = x.Status, TrackingReference = x.TrackingReference, RecoveryAmount = x.RecoveryAmount, Notes = x.Notes }).ToList(), AccessItems = exit.AccessItems.Select(x => new EmployeeExitAccessItemDto { AccessItemId = x.AccessItemId, System = x.System, Mode = x.Mode, Status = x.Status, ScheduledAtUtc = x.ScheduledAtUtc, ExternalReference = x.ExternalReference, Notes = x.Notes }).ToList(), FinanceItems = exit.FinanceItems.Select(x => new EmployeeExitFinanceItemDto { FinanceItemId = x.FinanceItemId, Category = x.Category, Direction = x.Direction, Amount = x.Amount, Status = x.Status, Reference = x.Reference, Notes = x.Notes }).ToList(), BenefitItems = exit.BenefitItems.Select(x => new EmployeeExitBenefitItemDto { BenefitItemId = x.BenefitItemId, Category = x.Category, Status = x.Status, EffectiveEndDate = x.EffectiveEndDate, Reference = x.Reference, Notes = x.Notes }).ToList(), Evidence = exit.Evidence.Select(x => new EmployeeExitEvidenceDetailDto { EvidenceId = x.EvidenceId, Category = x.Category, Type = x.Type, Status = x.Status, Visibility = x.Visibility, FileName = x.FileName, ContentType = x.ContentType, UploadedAtUtc = x.UploadedAtUtc, FormerEmployeeRequestId = x.FormerEmployeeRequestId, IssuedToFormerEmployeeAtUtc = x.IssuedToFormerEmployeeAtUtc, SeenByFormerEmployeeAtUtc = x.SeenByFormerEmployeeAtUtc }).ToList(),
                FormerEmployeeRequests = exit.FormerEmployeeRequests.Select(x => new FormerEmployeePortalRequestDto { RequestId = x.RequestId, Type = x.Type, Status = x.Status, Details = x.Details, RequestedAtUtc = x.RequestedAtUtc, EvidenceId = x.EvidenceId, IssuedAtUtc = x.IssuedAtUtc }).ToList(),
                ResponsibilityReassignments = exit.ResponsibilityReassignments.Select(x => new EmployeeExitResponsibilityReassignmentDto { Responsibility = x.Responsibility, NewOwnerUserId = x.NewOwnerUserId, Notes = x.Notes }).ToList(),
                ExitInterview = exit.ExitInterview is null ? null : new EmployeeExitInterviewDto { EmployeeFeedback = exit.ExitInterview.EmployeeFeedback, ManagerFeedback = exit.ExitInterview.ManagerFeedback, HrNotes = exit.ExitInterview.HrNotes, IsRehireEligible = exit.ExitInterview.IsRehireEligible, ConductedAtUtc = exit.ExitInterview.ConductedAtUtc },
                FinalSettlement = await BuildFinalSettlementDto(exit),
                Documents = exit.Documents.Select(x => new EmployeeExitDocumentDto { Type = x.Type, Status = x.Status, Url = x.Url }).ToList(),
                Withdrawal = exit.Withdrawal is null ? null : new EmployeeExitWithdrawalDto { Status = exit.Withdrawal.Status, Reason = exit.Withdrawal.Reason },
                NoticeAdjustments = exit.NoticeAdjustments.Select(x => new EmployeeExitNoticeAdjustmentDto { NoticeAdjustmentId = x.NoticeAdjustmentId, Type = x.Type, Status = x.Status, RequestedLastWorkingDate = x.RequestedLastWorkingDate, RequiredNoticeDays = x.RequiredNoticeDays, ServedNoticeDays = x.ServedNoticeDays, ShortfallDays = x.ShortfallDays, WaivedDays = x.WaivedDays, RecoveryAmount = x.RecoveryAmount, Reason = x.Reason }).ToList(),
                AuditHistory = exit.AuditHistory.Select(x => new EmployeeExitAuditEventDto { Action = x.Action, ActorUserId = x.ActorUserId, AtUtc = x.AtUtc, Notes = x.Notes }).ToList(),
                DiscussionRequest = exit.DiscussionRequest, DiscussionResponse = exit.DiscussionResponse, DiscussionMode = exit.DiscussionMode,
                DiscussionParticipantUserIds = exit.DiscussionParticipantUserIds, DiscussionMessages = exit.DiscussionMessages.Select(x => new EmployeeExitDiscussionMessageDto { MessageId = x.MessageId, AuthorUserId = x.AuthorUserId, Message = x.Message, CreatedAtUtc = x.CreatedAtUtc }).ToList(),
                PermittedActions = actions,
                IsAssignedManagerReviewer = isAssignedManager,
                IsHrReviewer = isHr,
                PendingHandoverCount = exit.HandoverTasks.Count(x => x.Status is not ("Approve" or "Approved" or "Waived")), PendingAssetCount = exit.Assets.Count(x => x.Status is not ("Verified" or "Waived" or "RecoveredFinancially")), PendingAccessCount = exit.AccessItems.Count(x => x.Status is not ("Revoked" or "Waived")), PendingFinanceCount = exit.FinanceItems.Count(x => x.Status is not ("Completed" or "Waived")), PendingBenefitCount = exit.BenefitItems.Count(x => x.Status is not ("Completed" or "Waived"))
            };
            if (!isHr)
            {
                managementCase.Assets = [];
                managementCase.AccessItems = [];
                managementCase.FinanceItems = [];
                managementCase.BenefitItems = [];
                managementCase.FormerEmployeeRequests = [];
                managementCase.ResponsibilityReassignments = [];
                managementCase.ExitInterview = null;
                managementCase.Documents = [];
                managementCase.NoticeAdjustments = [];
                managementCase.Evidence = managementCase.Evidence.Where(x => x.Visibility == "EmployeeManagerHR").ToList();
                managementCase.AuditHistory = managementCase.AuditHistory.Select(x => new EmployeeExitAuditEventDto { Action = x.Action, AtUtc = x.AtUtc }).ToList();
            }
            return new Result<EmployeeExitCaseDto> { Success = true, MethodResult = managementCase };
        }

        public async Task<Result<List<EmployeeExitQueueItemDto>>> GetEmployeeExitQueue(int page = 1, int pageSize = 25, string? search = null, string? status = null)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            var isHr = await IsHrExitActor(companyId, actorUserId);
            var activeCases = (await _employeeExitRepository.GetAll(x => x.CompanyId == companyId && x.IsActive && !x.IsDeleted &&
                x.Status != "Withdrawn" && x.Status != "Cancelled" && x.Status != "Rejoined"))
                .Where(x => isHr || x.ReportingManagerUserId == actorUserId)
                .OrderBy(x => x.LastWorkingDate).ThenByDescending(x => x.InitiatedAtUtc).ToList();
            if (!isHr && activeCases.Count == 0)
                return new Result<List<EmployeeExitQueueItemDto>> { Success = true, MethodResult = [] };

            var employees = (await _employeeRepository.GetAll(x => x.CompanyId == companyId && !x.IsDeleted && activeCases.Select(c => c.EmployeeUserId).Contains(x.UserId)))
                .ToDictionary(x => x.UserId);
            var items = activeCases.Select(exit =>
            {
                employees.TryGetValue(exit.EmployeeUserId, out var employee);
                var total = exit.Clearances.Count + exit.HandoverTasks.Count + exit.Assets.Count + exit.AccessItems.Count + exit.FinanceItems.Count + exit.BenefitItems.Count;
                var pending = exit.Clearances.Count(x => x.Status is not ("Completed" or "Waived")) + exit.HandoverTasks.Count(x => x.Status is not ("Approved" or "Waived")) + exit.Assets.Count(x => x.Status is not ("Verified" or "Waived" or "RecoveredFinancially")) + exit.AccessItems.Count(x => x.Status is not ("Revoked" or "Waived")) + exit.FinanceItems.Count(x => x.Status is not ("Completed" or "Waived")) + exit.BenefitItems.Count(x => x.Status is not ("Completed" or "Waived"));
                return new EmployeeExitQueueItemDto { EmployeeUserId = exit.EmployeeUserId, EmployeeExitId = exit.EmployeeExitId, EmployeeName = employee is null ? "Employee" : $"{employee.FirstName} {employee.LastName}".Trim(), EmployeeId = employee?.EmployeeId ?? string.Empty, DepartmentId = exit.EmployeeDepartmentId, Status = exit.Status, LastWorkingDate = exit.LastWorkingDate, InitiatedAtUtc = exit.InitiatedAtUtc, ProgressPercent = total == 0 ? 0 : (int)Math.Round(100d * (total - pending) / total) };
            }).Where(item => string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase) || item.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
              .Where(item => string.IsNullOrWhiteSpace(search) || $"{item.EmployeeName} {item.EmployeeId} {item.DepartmentId}".Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
              .ToList();
            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 100);
            return new Result<List<EmployeeExitQueueItemDto>> { Success = true, TotalRecords = items.Count, MethodResult = items.Skip((safePage - 1) * safePageSize).Take(safePageSize).ToList() };
        }

        public async Task<Result> CompleteEmployeeExit(CompleteEmployeeExitRequest request)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var actorUserId = CurrentContext.UserId(_httpContextAccessor);
            if (!await IsHrExitActor(companyId, actorUserId)) return new Result { Success = false, Message = "EXIT_COMPLETION_FORBIDDEN" };
            if (request is null || (string.IsNullOrWhiteSpace(request.EmployeeExitId) && string.IsNullOrWhiteSpace(request.EmployeeUserId))) return new Result { Success = false, Message = "EXIT_REQUEST_INVALID" };
            var exit = await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeExitId == request.EmployeeExitId && x.Status == "NoticePeriod" && x.IsActive && !x.IsDeleted)
                ?? await _employeeExitRepository.FirstOrDefault(x => x.CompanyId == companyId && x.EmployeeUserId == request.EmployeeUserId && x.Status == "NoticePeriod" && x.IsActive && !x.IsDeleted);
            if (exit is null) return new Result { Success = false, Message = "OPEN_EXIT_NOT_FOUND" };
            if (request.Version != exit.Version) return new Result { Success = false, Message = "EXIT_CASE_CHANGED_OR_NOT_FOUND" };
            if (DateTime.UtcNow.Date < exit.LastWorkingDate.Date) return new Result { Success = false, Message = "LAST_WORKING_DATE_NOT_REACHED" };
            var completedExitType = EmployeeLifecycleStatuses.NormalizeCompletedEmploymentStatus(exit.ExitType)
                ?? (exit.PolicySnapshot?.RequireResignationEvidence == true ? EmployeeLifecycleStatuses.Resigned : EmployeeLifecycleStatuses.Terminated);
            if (completedExitType == EmployeeLifecycleStatuses.Resigned && exit.PolicySnapshot?.RequireResignationEvidence == true && !exit.Evidence.Any(x => x.Category == "Resignation" && x.Status == "Verified")) return new Result { Success = false, Message = "RESIGNATION_EVIDENCE_PENDING" };
            if (completedExitType != EmployeeLifecycleStatuses.Deceased && exit.PolicySnapshot?.RequireEmployeeAcknowledgement == true && exit.PostExitAccess?.AcknowledgementStatus != "Acknowledged") return new Result { Success = false, Message = "EMPLOYEE_ACKNOWLEDGEMENT_PENDING" };
            if (completedExitType != EmployeeLifecycleStatuses.Deceased && (string.IsNullOrWhiteSpace(exit.PostExitAccess?.PersonalEmail) || exit.PostExitAccess.PersonalEmailVerified != true)) return new Result { Success = false, Message = "POST_EXIT_CONTACT_VERIFICATION_REQUIRED" };
            if (exit.Clearances.Any(x => x.IsMandatory && x.Status is not ("Completed" or "Waived"))) return new Result { Success = false, Message = "MANDATORY_CLEARANCE_PENDING" };
            if (BlocksCompletion(exit.PolicySnapshot, "Handover", exit.PolicySnapshot?.RequireHandover == true) && (!exit.HandoverTasks.Any() || exit.HandoverTasks.Any(x => x.Status is not ("Approve" or "Approved" or "Waived")))) return new Result { Success = false, Message = "HANDOVER_PENDING_OR_NOT_INITIALIZED" };
            if (BlocksCompletion(exit.PolicySnapshot, "Assets", exit.PolicySnapshot?.RequireAssetClearance == true) && (!exit.Assets.Any() || exit.Assets.Any(x => x.Status is not ("Verified" or "Waived" or "RecoveredFinancially")))) return new Result { Success = false, Message = "ASSET_CLEARANCE_PENDING_OR_NOT_INITIALIZED" };
            if (BlocksCompletion(exit.PolicySnapshot, "ItAccess", exit.PolicySnapshot?.RequireAccessRevocation == true) && (!exit.AccessItems.Any() || exit.AccessItems.Any(x => x.Status is not ("Revoked" or "Waived")))) return new Result { Success = false, Message = "ACCESS_DEACTIVATION_PENDING_OR_NOT_INITIALIZED" };
            if (BlocksCompletion(exit.PolicySnapshot, "Finance", exit.PolicySnapshot?.RequireFinanceClearance == true) && (!exit.FinanceItems.Any() || exit.FinanceItems.Any(x => x.Status is not ("Completed" or "Waived")))) return new Result { Success = false, Message = "FINANCE_CLEARANCE_PENDING_OR_NOT_INITIALIZED" };
            if (BlocksCompletion(exit.PolicySnapshot, "Benefits", exit.PolicySnapshot?.RequireBenefitClearance == true) && (!exit.BenefitItems.Any() || exit.BenefitItems.Any(x => x.Status is not ("Completed" or "Waived")))) return new Result { Success = false, Message = "BENEFIT_CLEARANCE_PENDING_OR_NOT_INITIALIZED" };
            if (exit.FinanceItems.Any(x => x.Direction == "ReceivableFromEmployee" && x.Status is not ("Completed" or "Waived"))) return new Result { Success = false, Message = "NOTICE_RECOVERY_PENDING" };
            // ISSUE #9: Auto-decline expired offers before checking for pending decisions
            foreach (var adj in exit.NoticeAdjustments.Where(x => x.Status == "AwaitingEmployeeDecision" && x.ExpiresAtUtc < DateTime.UtcNow))
            {
                adj.Status = "EmployeeDeclined";
                exit.FinanceItems.RemoveAll(x => x.Reference == adj.NoticeAdjustmentId);
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "NoticeAdjustmentOfferExpired", ActorUserId = "system", Notes = $"Offer expired on {adj.ExpiresAtUtc:yyyy-MM-dd HH:mm:ss}; auto-declined." });
            }
            // ISSUE #1: Block completion if a payable offer is awaiting employee decision
            if (exit.NoticeAdjustments.Any(x => x.Status == "AwaitingEmployeeDecision")) return new Result { Success = false, Message = "NOTICE_ADJUSTMENT_AWAITING_EMPLOYEE_DECISION" };
            // ISSUE #2: Block completion if there are unresolved declined notice adjustments
            if (exit.NoticeAdjustments.Any(x => x.Status == "Declined")) return new Result { Success = false, Message = "DECLINED_NOTICE_ADJUSTMENT_UNRESOLVED" };
            // A final-month payroll cannot be published while that calendar month is still
            // in progress. Blocking employment completion on it therefore makes a same-day
            // exit impossible. Complete the employment record now; EmploymentPeriod keeps
            // this former employee eligible for the normal final-month payroll run, after
            // which HR calculates, approves and pays the auditable F&F settlement.
            if (exit.PolicySnapshot?.RequireFinalSettlement == true)
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "FinalSettlementPendingPostExitPayroll", ActorUserId = actorUserId, Notes = $"Final-month payroll for {exit.LastWorkingDate:MMMM yyyy} must be processed before F&F calculation." });
            // EmployeeExitId is the authoritative completion identifier. Older clients may
            // still send EmployeeUserId, but it must never be required once a case was found.
            var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
            if (employee is null) return new Result { Success = false, Message = "EMPLOYEE_NOT_FOUND" };
            var completedAt = DateTime.UtcNow;
            exit.Status = exit.ExitType; exit.CompletedAtUtc = completedAt; exit.Notes = request.Notes?.Trim() ?? exit.Notes; exit.UpdatedBy = actorUserId; exit.UpdatedDate = completedAt; exit.Version++;
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ExitCompleted", ActorUserId = actorUserId, Notes = request.Notes?.Trim() });
            Expression<Func<EmployeeExit, bool>> updateExit = x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.Status == "NoticePeriod" && x.IsActive && !x.IsDeleted;
            exit.IsActive = false;
            CancelPendingExitReminders(exit);
            employee.Lifecycle ??= new EmployeeLifecycleState();
            EmployeeLifecycleTransitionService.ApplyCompletedExit(employee.Lifecycle, exit.ExitType, exit.Notes, actorUserId, completedAt);
            employee.EmploymentStatus = exit.ExitType; employee.Status = false; employee.UpdatedBy = actorUserId; employee.UpdatedDate = DateTime.UtcNow;
            Expression<Func<EmpUser, bool>> completeEmployee = x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Status;
            var outboxEvent = new EmployeeExitOutbox
            {
                CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId,
                EventType = "EmployeeExitCompleted", RecipientUserId = exit.ReportingManagerUserId ?? actorUserId,
                Title = "Employee exit completed", Body = $"The offboarding of {employee.FirstName} {employee.LastName} has been completed.", NextAttemptAtUtc = completedAt
            };
            var cutoff = new EmployeeExitAttendanceCutoff { EmployeeExitAttendanceCutoffId = Guid.NewGuid().ToString(), CompanyId = companyId, CreatedBy = actorUserId, EmployeeExitId = exit.EmployeeExitId, EmployeeUserId = employee.UserId, LastWorkingDate = exit.LastWorkingDate.Date, LockedAtUtc = completedAt, LockedByUserId = actorUserId };
            var committedTransactionally = false;
            if (_mongoClient is not null)
            {
                var transactionOperation = "start-session";
                try
                {
                    using var session = await _mongoClient.StartSessionAsync();
                    await session.WithTransactionAsync(async (_, cancellationToken) =>
                    {
                        transactionOperation = "exit-case";
                        var exitWrite = await _employeeExitRepository.GetCollection().ReplaceOneAsync(session, Builders<EmployeeExit>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && x.Version == request.Version && x.Status == "NoticePeriod" && x.IsActive && !x.IsDeleted), exit, cancellationToken: cancellationToken);
                        if (exitWrite.MatchedCount != 1) throw new InvalidOperationException("EXIT_CASE_CHANGED_OR_NOT_FOUND");
                        transactionOperation = "employee";
                        var employeeWrite = await _employeeRepository.GetCollection().ReplaceOneAsync(session, Builders<EmpUser>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.Status && !x.IsDeleted), employee, cancellationToken: cancellationToken);
                        if (employeeWrite.MatchedCount != 1) throw new InvalidOperationException("EMPLOYEE_COMPLETION_FAILED");
                        transactionOperation = "refresh-tokens";
                        if (_refreshTokenRepository is not null) await _refreshTokenRepository.GetCollection().UpdateManyAsync(session, Builders<RefreshToken>.Filter.Where(x => x.UserId == employee.UserId && !x.IsRevoked), Builders<RefreshToken>.Update.Set(x => x.IsRevoked, true).Set(x => x.RevokedAt, completedAt), cancellationToken: cancellationToken);
                        transactionOperation = "outbox";
                        if (_employeeExitOutboxRepository is not null) await _employeeExitOutboxRepository.GetCollection().InsertOneAsync(session, outboxEvent, cancellationToken: cancellationToken);
                        transactionOperation = "attendance-cutoff";
                        if (_employeeExitAttendanceCutoffRepository is not null)
                            await _employeeExitAttendanceCutoffRepository.GetCollection().UpdateOneAsync(session,
                                Builders<EmployeeExitAttendanceCutoff>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeExitId == exit.EmployeeExitId && !x.IsDeleted),
                                Builders<EmployeeExitAttendanceCutoff>.Update
                                    .SetOnInsert(x => x.EmployeeExitAttendanceCutoffId, cutoff.EmployeeExitAttendanceCutoffId)
                                    .SetOnInsert(x => x.CompanyId, cutoff.CompanyId)
                                    .SetOnInsert(x => x.CreatedBy, cutoff.CreatedBy)
                                    .SetOnInsert(x => x.CreatedDate, cutoff.CreatedDate)
                                    .SetOnInsert(x => x.EmployeeExitId, cutoff.EmployeeExitId)
                                    .SetOnInsert(x => x.EmployeeUserId, cutoff.EmployeeUserId)
                                    .SetOnInsert(x => x.LastWorkingDate, cutoff.LastWorkingDate)
                                    .SetOnInsert(x => x.LockedAtUtc, cutoff.LockedAtUtc)
                                    .SetOnInsert(x => x.LockedByUserId, cutoff.LockedByUserId),
                                new UpdateOptions { IsUpsert = true }, cancellationToken);
                        transactionOperation = "future-leave";
                        if (_leaveRequestRepository is not null) await _leaveRequestRepository.GetCollection().UpdateManyAsync(session, Builders<LeaveRequest>.Filter.Where(x => x.CompanyId == companyId && x.EmployeeId == employee.EmployeeId && x.EndDate > exit.LastWorkingDate && (x.Status == EnumsHelper.LeaveRequestStatus.Pending || x.Status == EnumsHelper.LeaveRequestStatus.Accepted)), Builders<LeaveRequest>.Update.Set(x => x.Status, EnumsHelper.LeaveRequestStatus.WithDrawn).Set(x => x.Comment, "Cancelled by employee exit").Set(x => x.UpdatedBy, actorUserId).Set(x => x.UpdatedDate, completedAt), cancellationToken: cancellationToken);
                        transactionOperation = "future-wfh";
                        if (_workFromHomeRequestRepository is not null) await _workFromHomeRequestRepository.GetCollection().UpdateManyAsync(session, Builders<WorkFromHomeRequest>.Filter.Where(x => x.CompanyId == companyId && x.UserId == employee.UserId && x.ToDate > exit.LastWorkingDate && (x.Status == "Pending" || x.Status == "Approved")), Builders<WorkFromHomeRequest>.Update.Set(x => x.Status, "Cancelled").Set(x => x.RevokedByUserId, actorUserId).Set(x => x.RevokedAt, completedAt), cancellationToken: cancellationToken);
                        return true;
                    });
                    committedTransactionally = true;
                }
                // Standalone MongoDB deployments cannot start multi-document transactions.
                // Fall back to the compatibility path below; its writes retain the same
                // optimistic filters and are individually idempotent.
                catch (MongoCommandException ex) when (ex.Code == 20)
                {
                    _logger.LogWarning(ex, "MongoDB transactions are unavailable while completing employee exit {EmployeeExitId}; using the compatibility completion path.", exit.EmployeeExitId);
                }
                catch (InvalidOperationException ex) { return new Result { Success = false, Message = ex.Message }; }
                catch (Exception ex) when (transactionOperation == "start-session")
                {
                    _logger.LogWarning(ex, "MongoDB session creation failed while completing employee exit {EmployeeExitId}; using the compatibility completion path.", exit.EmployeeExitId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Employee exit completion transaction failed for exit {EmployeeExitId} during {Operation}", exit.EmployeeExitId, transactionOperation);
                    return new Result { Success = false, Message = $"EXIT_COMPLETION_{transactionOperation.Replace('-', '_').ToUpperInvariant()}_FAILED" };
                }
            }
            Result completion;
            if (!committedTransactionally)
            {
                var exitResult = await _employeeExitRepository.Update(updateExit, exit);
                if (!exitResult.Success) return exitResult;
                completion = await _employeeRepository.Update(completeEmployee, employee);
                if (!completion.Success) return completion;
            }
            else
                completion = new Result { Success = true };
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

        private static string NormalizeEmploymentStatus(EmpUser employee)
        {
            return EmploymentExitRules.GetEffectiveEmploymentStatus(employee);
        }

        private async Task<(bool Success, decimal DailyRate, decimal Amount)> CalculateNoticeRecovery(string companyId, EmployeeExit exit, int shortfallDays)
        {
            if (_employeePayrollRepository is null || shortfallDays <= 0) return (false, 0, 0);
            var payrolls = await _employeePayrollRepository.GetAll(x => x.CompanyId == companyId && x.UserId == exit.EmployeeUserId && x.IsProcessed);
            var payroll = payrolls.OrderByDescending(x => x.PayMonth).FirstOrDefault();
            if (payroll is null) return (false, 0, 0);

            var basis = exit.PolicySnapshot?.NoticePaySalaryBasis ?? "GrossMonthly";
            var divisor = exit.PolicySnapshot?.NoticePayDailyRateDivisor ?? 30;
            if (divisor is < 1 or > 31) return (false, 0, 0);
            var monthlyAmount = basis == "BasicMonthly"
                ? payroll.BasicPay
                : payroll.BasicPay + payroll.Allowance.HRA + payroll.Allowance.LTA + payroll.Allowance.OtherAllowance;
            if (monthlyAmount <= 0) return (false, 0, 0);

            var dailyRate = decimal.Round(monthlyAmount / divisor, 2, MidpointRounding.AwayFromZero);
            return (true, dailyRate, decimal.Round(dailyRate * shortfallDays, 2, MidpointRounding.AwayFromZero));
        }

        private async Task<bool> MeetsNoticePolicy(EmpUser employee, DateTime lastWorkingDate)
        {
            var policy = await GetApplicableExitPolicy(employee);
            return policy is null || lastWorkingDate >= DateTime.UtcNow.Date.AddDays(policy.MinimumNoticeDays);
        }

        private async Task<EmployeeExitPolicy?> GetApplicableExitPolicy(EmpUser employee, string? exitType = null)
            => (await GetResolvedExitPolicy(employee, exitType))?.Policy;

        private async Task<ResolvedOffboardingPolicy?> GetResolvedExitPolicy(EmpUser employee, string? exitType)
        {
            if (_employeeExitPolicyRepository is null) return null;
            var policies = (await _employeeExitPolicyRepository.GetAll(x => x.CompanyId == employee.CompanyId && x.IsActive && !x.IsDeleted))
                .ToList();
            return OffboardingPolicyResolver.Resolve(policies, new OffboardingPolicyContext
            {
                DepartmentId = employee.Department,
                RoleId = employee.RoleId,
                ExitType = exitType ?? string.Empty
            });
        }

        private static void RescheduleExitReminders(EmployeeExit exit, string actorUserId)
        {
            foreach (var reminder in exit.Reminders.Where(x => x.Status == "Pending")) reminder.Status = "Cancelled";
            if (exit.PolicySnapshot?.EnableNoticePeriodReminders == false) return;
            var offsets = (exit.PolicySnapshot?.NoticePeriodReminderOffsets ?? []).Select(x => int.TryParse(x, out var days) ? days : -1).Where(x => x >= 0).Distinct().ToList();
            if (offsets.Count == 0) offsets = [30, 15, 7, 3, 1, 0];
            var cadence = Math.Clamp(exit.PolicySnapshot?.NoticePeriodReminderEveryDays ?? 3, 1, 365);
            for (var due = exit.InitiatedAtUtc.Date.AddDays(cadence); due < exit.LastWorkingDate.Date; due = due.AddDays(cadence))
                if (!exit.Reminders.Any(x => x.Status == "Pending" && x.DueAtUtc.Date == due))
                    exit.Reminders.Add(new EmployeeExitReminder { Trigger = "NoticePeriodCadence", DueAtUtc = due, RecipientUserId = exit.ReportingManagerUserId });
            foreach (var days in offsets.Where(days => exit.LastWorkingDate.Date.AddDays(-days) >= DateTime.UtcNow.Date))
                if (!exit.Reminders.Any(x => x.Status == "Pending" && x.DueAtUtc.Date == exit.LastWorkingDate.Date.AddDays(-days)))
                    exit.Reminders.Add(new EmployeeExitReminder { Trigger = days == 0 ? "LastWorkingDay" : $"LastWorkingDayMinus{days}", DueAtUtc = exit.LastWorkingDate.Date.AddDays(-days), RecipientUserId = exit.ReportingManagerUserId });
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "RemindersRescheduled", ActorUserId = actorUserId });
        }

        private void CancelPendingExitReminders(EmployeeExit exit)
        {
            foreach (var reminder in exit.Reminders.Where(x => x.Status is "Pending" or "Queued"))
                reminder.Status = "Cancelled";
            if (_employeeExitOutboxRepository is null) return;
            _ = _employeeExitOutboxRepository.GetCollection().UpdateManyAsync(
                Builders<EmployeeExitOutbox>.Filter.And(
                    Builders<EmployeeExitOutbox>.Filter.Eq(x => x.CompanyId, exit.CompanyId),
                    Builders<EmployeeExitOutbox>.Filter.Eq(x => x.EmployeeExitId, exit.EmployeeExitId),
                    Builders<EmployeeExitOutbox>.Filter.Eq(x => x.EventType, "ExitReminder"),
                    Builders<EmployeeExitOutbox>.Filter.In(x => x.Status, new[] { "Pending", "Failed" }),
                    Builders<EmployeeExitOutbox>.Filter.Eq(x => x.IsDeleted, false)),
                Builders<EmployeeExitOutbox>.Update.Set(x => x.Status, "Cancelled").Set(x => x.LastError, "Exit case is no longer active."));
        }

        private static string NormalizeClearanceTemplateKey(string? templateKey, string? category)
        {
            var source = string.IsNullOrWhiteSpace(templateKey) ? category : templateKey;
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            var normalized = Regex.Replace(source.Trim().ToUpperInvariant(), "[^A-Z0-9]+", "_").Trim('_');
            return normalized.Length is > 0 and <= 80 ? normalized : string.Empty;
        }

        private static string BuildExitPolicyScopeKey(string? departmentId, string? roleId, string? exitType = null) =>
            BuildExitPolicyScopeKey(string.IsNullOrWhiteSpace(departmentId) ? [] : [departmentId], roleId, exitType);

        private static string BuildExitPolicyScopeKey(IEnumerable<string> departmentIds, string? roleId, string? exitType = null)
        {
            var normalizedDepartmentIds = departmentIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            var scope = normalizedDepartmentIds.Count switch
            {
                1 => $"Department:{normalizedDepartmentIds[0]}",
                > 1 => $"Departments:{string.Join("|", normalizedDepartmentIds)}",
                _ when !string.IsNullOrWhiteSpace(roleId) => $"Role:{roleId}",
                _ => "Company"
            };
            return string.IsNullOrWhiteSpace(exitType) ? scope : $"{scope}|ExitType:{exitType}";
        }

        private static List<string> GetPolicyDepartmentIds(EmployeeExitPolicy policy)
        {
            var departmentIds = policy.DepartmentIds ?? [];
            if (!string.IsNullOrWhiteSpace(policy.DepartmentId)) departmentIds = departmentIds.Append(policy.DepartmentId).ToList();
            return departmentIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
        }

        private static bool HasSameExitPolicyConfiguration(EmployeeExitPolicy policy, UpsertEmployeeExitPolicyRequest request) =>
            policy.MinimumNoticeDays == request.MinimumNoticeDays
            && policy.NoticePaySalaryBasis == request.NoticePaySalaryBasis
            && policy.NoticePayDailyRateDivisor == request.NoticePayDailyRateDivisor
            && policy.EnableOffboarding == request.EnableOffboarding
            && policy.RequireManagerReview == request.RequireManagerReview
            && policy.RequireHrReview == request.RequireHrReview
            && policy.AllowJointDiscussion == request.AllowJointDiscussion
            && policy.RequireResignationEvidence == request.RequireResignationEvidence
            && policy.RequireEmployeeAcknowledgement == request.RequireEmployeeAcknowledgement
            && policy.EnableFormerEmployeePortal == request.EnableFormerEmployeePortal
            && policy.RequireHandover == request.RequireHandover
            && policy.RequireAssetClearance == request.RequireAssetClearance
            && policy.RequireAccessRevocation == request.RequireAccessRevocation
            && policy.RequireFinanceClearance == request.RequireFinanceClearance
            && policy.RequireBenefitClearance == request.RequireBenefitClearance
            && policy.RequireFinalSettlement == request.RequireFinalSettlement
            && policy.PresetLevel == request.PresetLevel
            && policy.Features.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Enabled}:{x.BlocksCompletion}:{x.Required}:{x.OwnerPermission}:{x.AllowWaiver}:{x.WaiverReasonRequired}:{x.DueDaysBeforeLastWorkingDate}").SequenceEqual(request.Features.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Enabled}:{x.BlocksCompletion}:{x.Required}:{x.OwnerPermission}:{x.AllowWaiver}:{x.WaiverReasonRequired}:{x.DueDaysBeforeLastWorkingDate}"))
            && policy.IsActive == request.IsActive;

        private static string BuildClearanceTemplateScopeKey(string scope, string? departmentId, string templateKey) =>
            scope == "Department" ? $"Department:{departmentId}:{templateKey}" : $"Global:{templateKey}";

        private async Task PopulateExitClearances(EmployeeExit exit)
        {
            if (_employeeExitClearanceTemplateRepository is null) return;
            var now = DateTime.UtcNow;
            var templates = (await _employeeExitClearanceTemplateRepository.GetAll(x => x.CompanyId == exit.CompanyId && x.IsActive && !x.IsDeleted))
                .Where(x => (!x.EffectiveFromUtc.HasValue || x.EffectiveFromUtc <= now) && (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc >= now))
                .ToList();
            var globalByKey = templates.Where(x => string.IsNullOrWhiteSpace(x.Scope) || x.Scope == "Global")
                .GroupBy(x => NormalizeClearanceTemplateKey(x.TemplateKey, x.Category)).Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key, x => x.OrderByDescending(t => t.Version).ThenByDescending(t => t.UpdatedDate).First());
            var employeeDepartmentId = exit.EmployeeDepartmentId;
            if (string.IsNullOrWhiteSpace(employeeDepartmentId))
            {
                // Backward-compatible fallback for exits created before department snapshots existed.
                var employee = await _employeeRepository.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.UserId == exit.EmployeeUserId && !x.IsDeleted);
                employeeDepartmentId = employee?.Department;
            }
            var departmentByKey = templates.Where(x => x.Scope == "Department" && x.DepartmentId == employeeDepartmentId)
                .GroupBy(x => NormalizeClearanceTemplateKey(x.TemplateKey, x.Category)).Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key, x => x.OrderByDescending(t => t.Version).ThenByDescending(t => t.UpdatedDate).First());
            var effective = globalByKey.Keys.Union(departmentByKey.Keys).Select(key => departmentByKey.TryGetValue(key, out var departmentTemplate) ? departmentTemplate : globalByKey[key]);
            var generatedGates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var template in effective.Where(template => IsClearanceTemplateEnabled(exit.PolicySnapshot, template.RequiredPolicyGate)))
            {
                var key = NormalizeClearanceTemplateKey(template.TemplateKey, template.Category);
                if (exit.Clearances.Any(x => NormalizeClearanceTemplateKey(x.TemplateKey, x.Category) == key)) continue;
                if (!string.IsNullOrWhiteSpace(template.RequiredPolicyGate)) generatedGates.Add(template.RequiredPolicyGate);
                exit.Clearances.Add(new EmployeeExitClearance
                {
                    TemplateId = template.EmployeeExitClearanceTemplateId, TemplateVersion = template.Version, TemplateKey = key,
                    TemplateScope = string.IsNullOrWhiteSpace(template.Scope) ? "Global" : template.Scope, SourceDepartmentId = template.DepartmentId,
                    Category = template.Category, Description = template.Description, RequiredPolicyGate = template.RequiredPolicyGate,
                    OwnerRole = template.OwnerRole, OwnerPermission = template.OwnerPermission,
                    // No owner permission means the reporting manager is the documented fallback;
                    // no role-name matching is used for authorization.
                    OwnerUserId = string.Empty,
                    IsMandatory = template.IsMandatory, DaysBeforeLastWorkingDay = template.DueDaysBeforeLastWorkingDate, DueAtUtc = exit.LastWorkingDate.Date.AddDays(-template.DueDaysBeforeLastWorkingDate)
                });
            }
            EnsureDefaultOperationalTasks(exit, generatedGates);
            if (!exit.AuditHistory.Any(x => x.Action == "ClearancesGenerated"))
                exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ClearancesGenerated", ActorUserId = CurrentContext.UserId(_httpContextAccessor), Notes = $"{exit.Clearances.Count} scoped template/default task(s)" });
        }

        // A selected completion gate must always produce actionable work.  Templates can
        // enrich this list, but an empty template catalogue must not leave HR with a
        // misleading zero-count workflow or allow an uninitialised gate to block exit.
        private static void EnsureDefaultOperationalTasks(EmployeeExit exit, ISet<string> configuredGates)
        {
            var policy = exit.PolicySnapshot;
            var dueAt = exit.LastWorkingDate.Date;
            void AddClearance(string gate, string category, string description)
            {
                if (configuredGates.Contains(gate) || exit.Clearances.Any(x => string.Equals(x.RequiredPolicyGate, gate, StringComparison.OrdinalIgnoreCase))) return;
                exit.Clearances.Add(new EmployeeExitClearance { TemplateKey = $"SYSTEM_{gate.ToUpperInvariant()}", Category = category, Description = description, RequiredPolicyGate = gate, IsMandatory = true, DueAtUtc = dueAt, DaysBeforeLastWorkingDay = 0 });
            }
            if (IsFeatureEnabled(policy, "Handover", policy?.RequireHandover == true))
            {
                AddClearance("Handover", "Handover confirmation", "Confirm that owned work is transferred or formally waived.");
                if (!exit.HandoverTasks.Any()) exit.HandoverTasks.Add(new EmployeeExitHandoverTask { Title = "Confirm work handover", Details = "Document transferred responsibilities or record a waiver when no handover applies.", AssigneeUserId = exit.EmployeeUserId, DueAtUtc = dueAt });
            }
            if (IsFeatureEnabled(policy, "Assets", policy?.RequireAssetClearance == true))
            {
                AddClearance("AssetClearance", "Asset confirmation", "Confirm returned assets or record that no company asset was allocated.");
                if (!exit.Assets.Any()) exit.Assets.Add(new EmployeeExitAsset { AssetType = "Asset inventory confirmation", Notes = "Verify allocated assets or waive when none apply." });
            }
            if (IsFeatureEnabled(policy, "ItAccess", policy?.RequireAccessRevocation == true))
            {
                AddClearance("AccessRevocation", "Access confirmation", "Confirm company access is revoked or record that no access applies.");
                if (!exit.AccessItems.Any()) exit.AccessItems.Add(new EmployeeExitAccessItem { System = "Access inventory confirmation", Mode = "AtLwd", Notes = "Revoke applicable access or waive when none apply." });
            }
            if (IsFeatureEnabled(policy, "Finance", policy?.RequireFinanceClearance == true))
            {
                AddClearance("FinanceClearance", "Finance confirmation", "Confirm outstanding finance items are settled or waived.");
                if (!exit.FinanceItems.Any()) exit.FinanceItems.Add(new EmployeeExitFinanceItem { Category = "Finance clearance confirmation", Notes = "Confirm no outstanding finance items or record settlement." });
            }
            if (IsFeatureEnabled(policy, "Benefits", policy?.RequireBenefitClearance == true))
            {
                AddClearance("BenefitClearance", "Benefits confirmation", "Confirm benefit closure or record that no benefits apply.");
                if (!exit.BenefitItems.Any()) exit.BenefitItems.Add(new EmployeeExitBenefitItem { Category = "Benefits closure confirmation", Notes = "Confirm benefit closure or waive when none apply." });
            }
        }

        private async Task<EmployeeFinalSettlementDto?> BuildFinalSettlementDto(EmployeeExit exit)
        {
            if (_employeeFinalSettlementRepository is null) return null;
            var settlement = await _employeeFinalSettlementRepository.FirstOrDefault(x => x.CompanyId == exit.CompanyId && x.EmployeeExitId == exit.EmployeeExitId && !x.IsDeleted);
            return settlement is null ? null : new EmployeeFinalSettlementDto
            {
                Status = settlement.Status, SalaryPayable = settlement.SalaryPayable, Deductions = settlement.Deductions,
                NoticeRecovery = settlement.NoticeRecovery, NetPayable = settlement.NetPayable, SourcePayrollId = settlement.SourcePayrollId, SourcePayrollRunId = settlement.SourcePayrollRunId, SourcePayrollRunEmployeeId = settlement.SourcePayrollRunEmployeeId,
                SourcePayrollMonth = settlement.SourcePayrollMonth, CalculatedAtUtc = settlement.CalculatedAtUtc,
                ApprovedAtUtc = settlement.ApprovedAtUtc, PaidAtUtc = settlement.PaidAtUtc
            };
        }

        private static void RecalculateOpenClearanceDueDates(EmployeeExit exit, string actorUserId)
        {
            foreach (var clearance in exit.Clearances.Where(x => x.Status == "Pending" && x.DaysBeforeLastWorkingDay.HasValue))
                clearance.DueAtUtc = exit.LastWorkingDate.Date.AddDays(-Math.Max(0, clearance.DaysBeforeLastWorkingDay.Value));
            exit.AuditHistory.Add(new EmployeeExitAuditEvent { Action = "ClearanceDueDatesRecalculated", ActorUserId = actorUserId });
        }

        private static bool IsClearanceTemplateEnabled(EmployeeExitPolicySnapshot? policy, string? gate) => gate switch
        {
            "Handover" => IsFeatureEnabled(policy, "Handover", policy?.RequireHandover == true),
            "AssetClearance" => IsFeatureEnabled(policy, "Assets", policy?.RequireAssetClearance == true),
            "AccessRevocation" => IsFeatureEnabled(policy, "ItAccess", policy?.RequireAccessRevocation == true),
            "FinanceClearance" => IsFeatureEnabled(policy, "Finance", policy?.RequireFinanceClearance == true),
            "BenefitClearance" => IsFeatureEnabled(policy, "Benefits", policy?.RequireBenefitClearance == true),
            _ => policy is not null && (IsFeatureEnabled(policy, "Handover", policy.RequireHandover) || IsFeatureEnabled(policy, "Assets", policy.RequireAssetClearance) || IsFeatureEnabled(policy, "ItAccess", policy.RequireAccessRevocation) || IsFeatureEnabled(policy, "Finance", policy.RequireFinanceClearance) || IsFeatureEnabled(policy, "Benefits", policy.RequireBenefitClearance))
        };

        private static readonly HashSet<string> OffboardingFeatureKeys = new(StringComparer.Ordinal) { "SelfResignation", "HrInitiatedExit", "ManagerApproval", "HrApproval", "Discussion", "NoticePeriod", "NoticeAdjustments", "Withdrawal", "Handover", "Clearance", "Assets", "ItAccess", "Finance", "Benefits", "Evidence", "FinalSettlement", "ExitInterview", "Documents", "FormerEmployeePortal" };
        private static bool IsValidPresetLevel(string? value) => value is "Disabled" or "Basic" or "Structured" or "Operational" or "Advanced" or "Enterprise" or "Custom";
        private static List<EmployeeExitFeatureConfigurationRequest> BuildPresetFeatures(string? level)
        {
            var order = new[] { "SelfResignation", "HrInitiatedExit", "ManagerApproval", "HrApproval", "NoticePeriod", "Withdrawal", "Handover", "Clearance", "Assets", "ItAccess", "Finance", "Benefits", "Evidence", "FinalSettlement", "ExitInterview", "Documents", "FormerEmployeePortal" };
            var count = level switch { "Disabled" => 0, "Basic" => 5, "Structured" => 6, "Operational" => 10, "Advanced" => 16, "Enterprise" => order.Length, _ => 0 };
            return order.Take(count).Select(key => new EmployeeExitFeatureConfigurationRequest { Key = key, Enabled = true, BlocksCompletion = key is "Handover" or "Clearance" or "Assets" or "ItAccess" or "Finance" or "Benefits" or "FinalSettlement", Required = key is "HrApproval" or "NoticePeriod" }).ToList();
        }

        private static bool GeneratedDocumentMatchesRequest(string generatedType, string requestType) =>
            (generatedType == "RelievingLetter" && requestType == "RelievingLetter") ||
            (generatedType == "ExperienceLetter" && requestType == "ExperienceLetter") ||
            (generatedType == "FinalSettlementStatement" && requestType == "FnFStatement");

        private static byte[] BuildExitDocumentPdf(string type, EmpUser? employee, EmployeeExit exit)
        {
            var name = string.Join(" ", new[] { employee?.FirstName, employee?.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            var lines = new[] { type.Replace("FinalSettlementStatement", "Final Settlement Statement").Replace("Letter", " Letter"), string.Empty, $"Employee: {name}", $"Employee ID: {employee?.EmployeeId ?? exit.EmployeeUserId}", $"Last working date: {exit.LastWorkingDate:dd MMM yyyy}", string.Empty, "This document was generated by the HRMS and requires HR review before issue." };
            static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            var content = new StringBuilder("BT /F1 16 Tf 54 760 Td ");
            for (var index = 0; index < lines.Length; index++) { if (index > 0) content.Append("0 -24 Td "); content.Append('(').Append(Escape(lines[index])).Append(") Tj "); }
            content.Append("ET");
            var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", $"<< /Length {Encoding.ASCII.GetByteCount(content.ToString())} >>\nstream\n{content}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
            using var output = new MemoryStream(); using var writer = new StreamWriter(output, Encoding.ASCII, 1024, true);
            writer.Write("%PDF-1.4\n"); var offsets = new List<long> { 0 };
            for (var index = 0; index < objects.Length; index++) { writer.Flush(); offsets.Add(output.Position); writer.Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n"); }
            writer.Flush(); var xref = output.Position; writer.Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n"); for (var index = 1; index < offsets.Count; index++) writer.Write($"{offsets[index]:D10} 00000 n \n"); writer.Write($"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); writer.Flush(); return output.ToArray();
        }
        private static bool IsValidFeatureConfiguration(IEnumerable<EmployeeExitFeatureConfigurationRequest>? features) => features is not null && features.All(x => !string.IsNullOrWhiteSpace(x.Key) && OffboardingFeatureKeys.Contains(x.Key) && (x.DueDaysBeforeLastWorkingDate is null or >= 0) && (!x.BlocksCompletion || x.Enabled) && (!x.Required || x.Enabled) && (!x.AllowWaiver || x.Enabled) && (!x.WaiverReasonRequired || x.AllowWaiver)) && features.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() == features.Count();
        private static EmployeeExitFeatureConfiguration ToFeature(EmployeeExitFeatureConfigurationRequest x) => new() { Key = x.Key, Enabled = x.Enabled, BlocksCompletion = x.BlocksCompletion, Required = x.Required, OwnerPermission = x.OwnerPermission?.Trim(), AllowWaiver = x.AllowWaiver, WaiverReasonRequired = x.WaiverReasonRequired, DueDaysBeforeLastWorkingDate = x.DueDaysBeforeLastWorkingDate };
        private static EmployeeExitFeatureConfiguration CloneFeature(EmployeeExitFeatureConfiguration x) => new() { Key = x.Key, Enabled = x.Enabled, BlocksCompletion = x.BlocksCompletion, Required = x.Required, OwnerPermission = x.OwnerPermission, AllowWaiver = x.AllowWaiver, WaiverReasonRequired = x.WaiverReasonRequired, DueDaysBeforeLastWorkingDate = x.DueDaysBeforeLastWorkingDate };
        private static EmployeeExitFeatureConfigurationRequest ToFeatureRequest(EmployeeExitFeatureConfiguration x) => new() { Key = x.Key, Enabled = x.Enabled, BlocksCompletion = x.BlocksCompletion, Required = x.Required, OwnerPermission = x.OwnerPermission, AllowWaiver = x.AllowWaiver, WaiverReasonRequired = x.WaiverReasonRequired, DueDaysBeforeLastWorkingDate = x.DueDaysBeforeLastWorkingDate };
        private static bool IsFeatureEnabled(EmployeeExitPolicySnapshot? policy, string key, bool legacy) => policy?.Features.FirstOrDefault(x => x.Key == key) is { } feature ? feature.Enabled : legacy;
        private static bool BlocksCompletion(EmployeeExitPolicySnapshot? policy, string key, bool legacy) => policy?.Features.FirstOrDefault(x => x.Key == key) is { } feature ? feature.Enabled && feature.BlocksCompletion : legacy;

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
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            var employee = await _employeeRepository.FirstOrDefault(x => x.UserId == employeeId && x.CompanyId == companyId && !x.IsDeleted);
            if (employee == null)
            {
                return new Result { Success = false, Message = "Employee not found." };
            }

            //Update for Certification Details
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

            //Update for Work History
            Expression<Func<EmpWorkHistory, bool>> workWhereCondition = x => x.UserId == employeeId;
            await _empWorkHistoryRepository.UpdateMany(workWhereCondition, Builders<EmpWorkHistory>.Update.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId));

            Expression<Func<EmpUser, bool>> employeeWhereCondition = x => x.UserId == employeeId && x.CompanyId == companyId;
            UpdateDefinitionBuilder<EmpUser> empUpdateDefinition = Builders<EmpUser>.Update;
            UpdateDefinition<EmpUser> empUpdate = empUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            Result data = await _employeeRepository.UpdateMany(employeeWhereCondition, empUpdate);

            return data;
        }

        public async Task<bool> IsUserActive(string userId)
        {
            var companyId = CurrentContext.CompanyId(_httpContextAccessor);
            bool IsUserActive = await _employeeRepository.Exist(x => x.UserId == userId && x.CompanyId == companyId && x.Status && !x.IsDeleted &&
                (x.EmploymentStatus == null || x.EmploymentStatus == "" || x.EmploymentStatus == "Active" || x.EmploymentStatus == "NoticePeriod") &&
                (x.Lifecycle == null || x.Lifecycle.AccountStatus == null || x.Lifecycle.AccountStatus == "" || x.Lifecycle.AccountStatus == EmployeeLifecycleStatuses.AccountActive));
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
            bool exits = await _empWorkHistoryRepository.Exist(x => x.WorkHistoryId == workId && x.UserId == userId && !x.IsDeleted);
            if (!exits) return new Result { Success = false, Message = "Work history not found." };
            Expression<Func<EmpWorkHistory, bool>> whereCondition = x => x.WorkHistoryId == workId && x.UserId == userId;
            return await _empWorkHistoryRepository.UpdateMany(whereCondition, Builders<EmpWorkHistory>.Update.Set(x => x.IsDeleted, true));
        }
        public async Task<NotificationResponseModel> GetAllNotifications(NotificationRequestDTO model, string userId)
        {
            Expression<Func<UserNotifications, bool>> wherecondition = x => x.UserId == userId && !x.IsDismissed;
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
            int unReadNotificationCount = await _userNotificationRepository.Count(x => x.UserId == userId && !x.IsRead && !x.IsDismissed && (x.SnoozedUntilUtc == null || x.SnoozedUntilUtc <= DateTime.UtcNow));
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
            Expression<Func<UserNotifications, bool>> whereCondition = x => x.UserId == userId && x.UserNotificationId == userNotificationId;
            return await _userNotificationRepository.UpdateMany(whereCondition, Builders<UserNotifications>.Update.Set(x => x.IsRead, true));
        }

        public Task<Result> SnoozeNotification(string userId, string userNotificationId, DateTime untilUtc) =>
            _userNotificationRepository.UpdateMany(Builders<UserNotifications>.Filter.Where(x => x.UserId == userId && x.UserNotificationId == userNotificationId),
                Builders<UserNotifications>.Update.Set(x => x.SnoozedUntilUtc, untilUtc).Set(x => x.IsRead, true));

        public Task<Result> DismissNotification(string userId, string userNotificationId) =>
            _userNotificationRepository.UpdateMany(Builders<UserNotifications>.Filter.Where(x => x.UserId == userId && x.UserNotificationId == userNotificationId),
                Builders<UserNotifications>.Update.Set(x => x.IsDismissed, true).Set(x => x.IsRead, true));

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
                && emp.DateOfBirth.Length >= 10
                && emp.DateOfBirth.Substring(5, 5) == currentMonthDay
                && emp.Status
                && !emp.IsDeleted;

            IEnumerable<EmpUser> birthDayEmployeeList = await _employeeRepository.GetAll(expression, withDefaultFilter: false);
            IEnumerable<EmpUser> EmployeeAnniversaryList = await _employeeRepository.GetAll(emp =>
                !string.IsNullOrEmpty(emp.DateOfJoining)
                && emp.DateOfJoining.Length >= 10
                && emp.DateOfJoining.Substring(5, 5) == currentMonthDay
                && emp.Status
                && !emp.IsDeleted, withDefaultFilter: false);

            var notificationTasks = new List<Task>();
            List<UserNotifications> userNotifications = [];
            //  check if there are employees having birthday today
            if (birthDayEmployeeList.Any())
            {
                foreach (EmpUser employee in birthDayEmployeeList)
                {
                    Expression<Func<EmpUser, bool>> exp = emp => emp.CompanyId == employee.CompanyId && emp.UserId != employee.UserId && emp.Status && !emp.IsDeleted;
                    List<EmpUser> allEmployees = (await _employeeRepository.GetAll(exp, withDefaultFilter: false)).ToList();
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
                    Expression<Func<EmpUser, bool>> exp = emp => emp.CompanyId == employee.CompanyId && emp.UserId != employee.UserId && emp.Status && !emp.IsDeleted;
                    List<EmpUser> allEmployees = (await _employeeRepository.GetAll(exp, withDefaultFilter: false)).ToList();
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
        public async Task<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> GetNotificationPreferences(string userId,string companyId)
        {
           var userPreferences = await _notificationPreferenceRepository.FirstOrDefault(n => n.UserId == userId&& n.CompanyId == companyId);
            if (userPreferences != null)
            {
                return userPreferences.Preferences;
            }
            var company=await _companyRepository.FirstOrDefault(c => c.CompanyId == companyId && !c.IsDeleted);
            if (company?.NotificationPreferences?.Count > 0)
            {
 return new Dictionary<
            EnumsHelper.NotificationPreferenceType,
            bool>(company.NotificationPreferences);            }
            else
            {
    return _middlewareService.GetDefaultNotificationPreferences();
            }

        }
        public async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>> UpdateNotificationPreferences(string userId,string companyId, Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences)
        {
            Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> result = new();
            Expression<Func<NotificationPreference, bool>> whereCondition = n => n.UserId == userId && n.CompanyId == companyId;
            var existingPreferences = await _notificationPreferenceRepository.FirstOrDefault(whereCondition);
            if (existingPreferences == null)
            {
                NotificationPreference newPreference = new()
                {
                    UserId = userId,
                    CompanyId =companyId,
                    Preferences = preferences,
                };
                var createResult=await _notificationPreferenceRepository.AddOne(newPreference);
                if (!createResult.Success)
                {
                    result.Success=false;
                    result.Message="Unable to save Prefreneces";
                    return result ;
                }
            }
            else
            {
await _notificationPreferenceRepository.UpdateMany(
    whereCondition,
    Builders<NotificationPreference>.Update
        .Set(n => n.Preferences, preferences)
        .Set(n => n.UpdatedDate, DateTime.UtcNow)
);
            }

            result.Success = true;
            result.MethodResult =
        await GetNotificationPreferences(
            userId,
            companyId);            return result;
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
