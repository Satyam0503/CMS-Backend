
using System.Text.RegularExpressions;
using Codeji.CMS.API.App_Start;
using Codeji.CMS.API.Notification;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Attendance;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Account.Interface;
using Codeji.CMS.Services.Attendance;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Constraints;
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : BaseApiController
{
    readonly IAccountServices _accountServices;
    private readonly IEmployeeService _employeeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOfficeScheduleAssignmentService _officeScheduleAssignments;
    private readonly IOfficeScheduleSettingsService _officeScheduleSettings;
    private readonly IEmployeeExitEvidenceStorage _employeeExitEvidenceStorage;
    private readonly IFormerEmployeePortalService _formerEmployeePortalService;
    public UserController(IHttpContextAccessor httpContextAccessor, IEmployeeService employeeService, IAccountServices accountServices, IOfficeScheduleAssignmentService officeScheduleAssignments, IOfficeScheduleSettingsService officeScheduleSettings, IEmployeeExitEvidenceStorage employeeExitEvidenceStorage, IFormerEmployeePortalService formerEmployeePortalService)
    {
        _httpContextAccessor = httpContextAccessor;
        _employeeService = employeeService;
        _accountServices = accountServices;
        _officeScheduleAssignments = officeScheduleAssignments;
        _officeScheduleSettings = officeScheduleSettings;
        _employeeExitEvidenceStorage = employeeExitEvidenceStorage;
        _formerEmployeePortalService = formerEmployeePortalService;
    }

    [Route("InviteNewEmployee")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Create)]
    public async Task<Result<InviteEmployeeDto>> InviteNewEmployee([FromBody] InviteEmployeeDto model)
    {
        Result<InviteEmployeeDto> result = new() { Success = false };
        if (!ModelState.IsValid)
        {
            return result;
        }
        var currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.InviteNewEmployee(model, currentUserId);
    }

    [Route("BulkImportEmployees")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Create)]
    public async Task<Result<BulkImportEmployeesResponseDto>> BulkImportEmployees([FromBody] BulkImportEmployeesRequestDto model)
    {
        Result<BulkImportEmployeesResponseDto> result = new() { Success = false };
        if (model is null || !ModelState.IsValid || model.Employees.Count == 0 || model.Employees.Count > BulkImportEmployeesRequestDto.MaxBatchSize)
        {
            result.Message = $"Invalid request. Employees list must contain 1 to {BulkImportEmployeesRequestDto.MaxBatchSize} records.";
            return result;
        }
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.BulkImportEmployees(model, currentUserId);
    }

    [Route("GetNextEmployeeId")]
    [HttpGet]
    [ModulePermission(AppModule.Employees, Permission.View)]
    public async Task<Result> GetNextEmployeeId()
    {
        string companyId = CurrentContext.CompanyId(_httpContextAccessor);
        return await _employeeService.GetNextEmployeeId(companyId);
    }

    [Route("EditEmployees")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public async Task<Result<UserModel>> EditEmployees(EmployeePersonalInfo user, string userId)
    {
        // A user with Employees.Edit (for example HR) must not gain authority to
        // change their own role, department, employment data or financial credentials.
        // Route self-edits through the restricted mapping below and discard all
        // privileged fields supplied by the client.
        if (string.Equals(userId, CurrentContext.UserId(_httpContextAccessor), StringComparison.Ordinal))
            return await _employeeService.EditEmployee(ToSelfEditRequest(user), userId);

        UserModel isUserExist = await _employeeService.GetEmployeeById(userId);
        if (userId != isUserExist.UserId)
        {
            return new Result<UserModel>
            {
                Success = false,
                StatusCode = CustomStatusCode.EmployeeNotExist
            };
        }
        return await _employeeService.EditEmployee(user, userId);
    }

    // No [ModulePermission] - any authenticated user may edit their own profile regardless of
    // their Employees module permission (that permission governs editing *other* employees).
    // Always targets the caller's own userId from the auth token, never a client-supplied one,
    // and EmployeeSelfEditDto deliberately omits privileged fields (role, job, bank details, etc).
    [Route("EditOwnProfile")]
    [HttpPost]
    public async Task<Result<UserModel>> EditOwnProfile(EmployeeSelfEditDto user)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.EditEmployee(ToSelfEditRequest(user), currentUserId);
    }

    [Route("OfficeScheduleAssignment/{userId}")]
    [HttpGet]
    [ModulePermission(AppModule.Employees, Permission.View)]
    public Task<Result<EmployeeOfficeScheduleAssignmentDto>> GetOfficeScheduleAssignment(string userId, CancellationToken cancellationToken) =>
        _officeScheduleAssignments.GetEmployeeAsync(CurrentContext.CompanyId(_httpContextAccessor), userId, cancellationToken);

    [Route("AvailableOfficeSchedules")]
    [HttpGet]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public Task<Result<OfficeScheduleSettingsDto>> GetAvailableOfficeSchedules(CancellationToken cancellationToken) =>
        _officeScheduleSettings.GetAllAsync(CurrentContext.CompanyId(_httpContextAccessor), cancellationToken);

    [Route("OfficeScheduleAssignment/{userId}")]
    [HttpPut]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public Task<Result<EmployeeOfficeScheduleAssignmentDto>> SetOfficeScheduleAssignment(string userId, SetOfficeScheduleAssignmentRequest request, CancellationToken cancellationToken) =>
        _officeScheduleAssignments.SetEmployeeAsync(CurrentContext.CompanyId(_httpContextAccessor), CurrentContext.UserId(_httpContextAccessor), userId, request, cancellationToken);

    [Route("EmployeeIdSequence")]
    [HttpGet]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public Task<Result<EmployeeIdSequenceResponseDto>> GetEmployeeIdSequence() =>
        _employeeService.GetEmployeeIdSequence(CurrentContext.CompanyId(_httpContextAccessor));

    [Route("EmployeeIdSequence/Skip")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public Task<Result<EmployeeIdSequenceResponseDto>> SkipEmployeeIds([FromBody] SkipEmployeeIdsRequestDto model) =>
        _employeeService.SkipEmployeeIds(CurrentContext.CompanyId(_httpContextAccessor), CurrentContext.UserId(_httpContextAccessor), model.SkipCount);

    [Route("EmployeeIdSequence/StartFrom")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public Task<Result<EmployeeIdSequenceResponseDto>> StartEmployeeIdSequence([FromBody] StartEmployeeIdSequenceRequestDto model) =>
        _employeeService.StartEmployeeIdSequence(CurrentContext.CompanyId(_httpContextAccessor), CurrentContext.UserId(_httpContextAccessor), model.EmployeeId);

    [Route("Employees/{userId}/AssignEmployeeId")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public Task<Result<AssignMissingEmployeeIdResponseDto>> AssignMissingEmployeeId(string userId, [FromBody] AssignMissingEmployeeIdRequestDto model) =>
        _employeeService.AssignMissingEmployeeId(CurrentContext.CompanyId(_httpContextAccessor), CurrentContext.UserId(_httpContextAccessor), userId, model?.EmployeeId);

    private static EmployeePersonalInfo ToSelfEditRequest(EmployeeSelfEditDto user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        RoleId = "",
        Gender = user.Gender,
        DateOfBirth = user.DateOfBirth,
        PhoneNumber = user.PhoneNumber,
        BloodGroup = user.BloodGroup,
        PersonalEmail = user.PersonalEmail,
        EmergencyContact = user.EmergencyContact,
        Address = user.Address,
    };

    private static EmployeePersonalInfo ToSelfEditRequest(EmployeePersonalInfo user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        RoleId = "",
        Gender = user.Gender,
        DateOfBirth = user.DateOfBirth,
        PhoneNumber = user.PhoneNumber,
        BloodGroup = user.BloodGroup,
        PersonalEmail = user.PersonalEmail,
        EmergencyContact = user.EmergencyContact,
        Address = user.Address,
    };

    [Route("GetAllEmployees")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.View)]
    public async Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(GetAllEmployeeRequestModel? filters)
    {
        Result<GetAllEmployeeResponseModel> data = await _employeeService.GetAllEmployees(filters);
        return data;
    }

    [Route("InitiateEmployeeExit")]
    [HttpPost]
    [ModulePermission(AppModule.Employees, Permission.Edit)]
    public Task<Result> InitiateEmployeeExit([FromBody] InitiateEmployeeExitRequest request) =>
        _employeeService.InitiateEmployeeExit(request);

    // Any authenticated employee may submit only their own resignation. This is
    // intentionally separate from the Employees.Edit protected HR action.
    [Route("SubmitOwnResignation")]
    [HttpPost]
    public Task<Result> SubmitOwnResignation([FromBody] InitiateEmployeeExitRequest request) =>
        _employeeService.SubmitOwnResignation(request);

    [Route("EmployeeExit/{employeeUserId}")]
    [HttpGet]
    public Task<Result<EmployeeExitCaseDto>> GetEmployeeExitCase(string employeeUserId) =>
        _employeeService.GetEmployeeExitCase(employeeUserId);

    [Route("EmployeeExitManagement/{employeeUserId}")]
    [HttpGet]
    public Task<Result<EmployeeExitCaseDto>> GetEmployeeExitCaseForManagement(string employeeUserId) =>
        _employeeService.GetEmployeeExitCaseForManagement(employeeUserId);

    [Route("CompleteEmployeeExit")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> CompleteEmployeeExit([FromBody] CompleteEmployeeExitRequest request) =>
        _employeeService.CompleteEmployeeExit(request);

    [Route("ReviewEmployeeExit")]
    [HttpPost]
    public Task<Result> ReviewEmployeeExit([FromBody] ReviewEmployeeExitRequest request) =>
        _employeeService.ReviewEmployeeExit(request);

    [Route("RespondToEmployeeExitDiscussion")]
    [HttpPost]
    public Task<Result> RespondToEmployeeExitDiscussion([FromBody] RespondToEmployeeExitDiscussionRequest request) =>
        _employeeService.RespondToEmployeeExitDiscussion(request);

    [Route("WithdrawOwnEmployeeExit")]
    [HttpPost]
    public Task<Result> WithdrawOwnEmployeeExit([FromBody] CancelEmployeeExitRequest request) =>
        _employeeService.WithdrawOwnEmployeeExit(request);

    [Route("DecideEmployeeExitWithdrawal")]
    [HttpPost]
    public Task<Result> DecideEmployeeExitWithdrawal([FromBody] DecideEmployeeExitWithdrawalRequest request) =>
        _employeeService.DecideEmployeeExitWithdrawal(request);

    [Route("CancelEmployeeExit")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> CancelEmployeeExit([FromBody] CancelEmployeeExitRequest request) =>
        _employeeService.CancelEmployeeExit(request);

    [Route("UpdateEmployeeExitDate")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> UpdateEmployeeExitDate([FromBody] UpdateEmployeeExitDateRequest request) =>
        _employeeService.UpdateEmployeeExitDate(request);

    [Route("RequestEmployeeExitNoticeAdjustment")]
    [HttpPost]
    public Task<Result> RequestEmployeeExitNoticeAdjustment([FromBody] RequestEmployeeExitNoticeAdjustmentRequest request) =>
        _employeeService.RequestEmployeeExitNoticeAdjustment(request);

    [Route("DecideEmployeeExitNoticeAdjustment")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> DecideEmployeeExitNoticeAdjustment([FromBody] DecideEmployeeExitNoticeAdjustmentRequest request) =>
        _employeeService.DecideEmployeeExitNoticeAdjustment(request);

    [Route("EmployeeExitPolicy")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> UpsertEmployeeExitPolicy([FromBody] UpsertEmployeeExitPolicyRequest request) =>
        _employeeService.UpsertEmployeeExitPolicy(request);

    [Route("EmployeeExitPolicy/{policyId}")]
    [HttpGet]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result<EmployeeExitPolicy>> GetEmployeeExitPolicy(string policyId) =>
        _employeeService.GetEmployeeExitPolicy(policyId);

    [Route("EmployeeExitClearanceTemplate/{templateId}")]
    [HttpGet]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result<EmployeeExitClearanceTemplate>> GetEmployeeExitClearanceTemplate(string templateId) =>
        _employeeService.GetEmployeeExitClearanceTemplate(templateId);

    [Route("EmployeeExitPolicies")]
    [HttpGet]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result<EmployeeExitPolicy>> GetEmployeeExitPolicies() =>
        _employeeService.GetEmployeeExitPolicies();

    [Route("EmployeeExitClearanceTemplates")]
    [HttpGet]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result<EmployeeExitClearanceTemplate>> GetEmployeeExitClearanceTemplates() =>
        _employeeService.GetEmployeeExitClearanceTemplates();

    [Route("EmployeeExitClearanceTemplate")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> UpsertEmployeeExitClearanceTemplate([FromBody] UpsertEmployeeExitClearanceTemplateRequest request) =>
        _employeeService.UpsertEmployeeExitClearanceTemplate(request);

    [Route("ApproveEmployeeFinalSettlement")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> ApproveEmployeeFinalSettlement([FromBody] ApproveEmployeeFinalSettlementRequest request) =>
        _employeeService.ApproveEmployeeFinalSettlement(request);

    [Route("UpdateEmployeeFinalSettlementPayment")]
    [HttpPost]
    public Task<Result> UpdateEmployeeFinalSettlementPayment([FromBody] UpdateEmployeeFinalSettlementPaymentRequest request) =>
        _employeeService.UpdateEmployeeFinalSettlementPayment(request);

    [Route("CompleteEmployeeExitClearance")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> CompleteEmployeeExitClearance([FromBody] CompleteEmployeeExitClearanceRequest request) =>
        _employeeService.CompleteEmployeeExitClearance(request, false);

    [Route("WaiveEmployeeExitClearance")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> WaiveEmployeeExitClearance([FromBody] CompleteEmployeeExitClearanceRequest request) =>
        _employeeService.CompleteEmployeeExitClearance(request, true);

    [Route("EmployeeExitHandoverTask")]
    [HttpPost]
    public Task<Result> UpsertEmployeeExitHandoverTask([FromBody] UpsertEmployeeExitHandoverTaskRequest request) =>
        _employeeService.UpsertEmployeeExitHandoverTask(request);

    [Route("ReviewEmployeeExitHandoverTask")]
    [HttpPost]
    public Task<Result> ReviewEmployeeExitHandoverTask([FromBody] ReviewEmployeeExitHandoverTaskRequest request) =>
        _employeeService.ReviewEmployeeExitHandoverTask(request);

    [Route("EmployeeExitAsset")]
    [HttpPost]
    public Task<Result> UpsertEmployeeExitAsset([FromBody] UpsertEmployeeExitAssetRequest request) =>
        _employeeService.UpsertEmployeeExitAsset(request);

    [Route("EmployeeExitAccessItem")]
    [HttpPost]
    public Task<Result> UpsertEmployeeExitAccessItem([FromBody] UpsertEmployeeExitAccessItemRequest request) =>
        _employeeService.UpsertEmployeeExitAccessItem(request);

    [Route("EmployeeExitFinanceItem")]
    [HttpPost]
    public Task<Result> UpsertEmployeeExitFinanceItem([FromBody] UpsertEmployeeExitFinanceItemRequest request) =>
        _employeeService.UpsertEmployeeExitFinanceItem(request);

    [Route("EmployeeExitBenefitItem")]
    [HttpPost]
    public Task<Result> UpsertEmployeeExitBenefitItem([FromBody] UpsertEmployeeExitBenefitItemRequest request) =>
        _employeeService.UpsertEmployeeExitBenefitItem(request);

    [Route("EmployeeExitEvidence")]
    [HttpPost]
    public Task<Result> AddEmployeeExitEvidence([FromBody] AddEmployeeExitEvidenceRequest request) =>
        _employeeService.AddEmployeeExitEvidence(request);

    [Route("EmployeeExitEvidenceUpload")]
    [HttpPost]
    public async Task<Result> UploadEmployeeExitEvidence([FromForm] string employeeExitId, [FromForm] string category, [FromForm] string visibility, [FromForm] IFormFile file)
    {
        var access = await _employeeService.GetEmployeeExitCaseForEvidence(employeeExitId);
        if (!access.Success || access.MethodResult is null) return new Result { Success = false, Message = "EXIT_EVIDENCE_FORBIDDEN" };
        var companyId = CurrentContext.CompanyId(_httpContextAccessor);
        try
        {
            var stored = await _employeeExitEvidenceStorage.SaveAsync(companyId, employeeExitId, file);
            return await _employeeService.AddEmployeeExitEvidence(new AddEmployeeExitEvidenceRequest { EmployeeExitId = employeeExitId, Version = access.MethodResult.Version, Category = category, Type = "UploadedFile", Visibility = visibility, StorageKey = stored.StorageKey, FileName = file.FileName, ContentType = file.ContentType, Sha256 = stored.Sha256 });
        }
        catch (InvalidOperationException ex) { return new Result { Success = false, Message = ex.Message }; }
    }

    [Route("EmployeeExitEvidence/{employeeExitId}/{evidenceId}/download")]
    [HttpGet]
    public async Task<IActionResult> DownloadEmployeeExitEvidence(string employeeExitId, string evidenceId)
    {
        var evidence = await _employeeService.GetEmployeeExitEvidence(employeeExitId, evidenceId);
        if (!evidence.Success || evidence.MethodResult is null || string.IsNullOrWhiteSpace(evidence.MethodResult.StorageKey)) return NotFound();
        var stored = await _employeeExitEvidenceStorage.OpenReadAsync(evidence.MethodResult.StorageKey);
        if (stored is null) return NotFound();
        return File(stored.Value.Content, evidence.MethodResult.ContentType ?? stored.Value.ContentType, evidence.MethodResult.FileName ?? stored.Value.FileName);
    }

    [Route("VerifyEmployeeExitEvidence")]
    [HttpPost]
    public Task<Result> VerifyEmployeeExitEvidence([FromBody] VerifyEmployeeExitEvidenceRequest request) =>
        _employeeService.VerifyEmployeeExitEvidence(request);

    [Route("SupersedeEmployeeExitEvidence")]
    [HttpPost]
    public Task<Result> SupersedeEmployeeExitEvidence([FromBody] SupersedeEmployeeExitEvidenceRequest request) =>
        _employeeService.SupersedeEmployeeExitEvidence(request);

    [Route("EmployeeExitPostExitContact")]
    [HttpPost]
    public Task<Result> UpdateEmployeeExitPostExitContact([FromBody] UpdateEmployeeExitPostExitContactRequest request) =>
        _employeeService.UpdateEmployeeExitPostExitContact(request);

    [Route("AcknowledgeEmployeeExit")]
    [HttpPost]
    public Task<Result> AcknowledgeEmployeeExit([FromBody] AcknowledgeEmployeeExitRequest request) =>
        _employeeService.AcknowledgeEmployeeExit(request);

    [Route("EmployeeExitFormerEmployeeRequest")]
    [HttpPost]
    public Task<Result> CreateEmployeeExitFormerEmployeeRequest([FromBody] CreateEmployeeExitFormerEmployeeRequest request) =>
        _employeeService.CreateEmployeeExitFormerEmployeeRequest(request);

    [Route("EmployeeExitFormerEmployeeRequestResolve")]
    [HttpPost]
    public Task<Result> ResolveEmployeeExitFormerEmployeeRequest([FromBody] ResolveEmployeeExitFormerEmployeeRequest request) =>
        _employeeService.ResolveEmployeeExitFormerEmployeeRequest(request);

    [Route("EmployeeExitFormerEmployeePortalInvite")]
    [HttpPost]
    public Task<Result<FormerEmployeePortalInviteDto>> CreateFormerEmployeePortalInvite([FromBody] CreateFormerEmployeePortalInviteRequest request) =>
        _formerEmployeePortalService.CreateInvite(request);

    [Route("EmployeeExitFormerEmployeePortalRevoke")]
    [HttpPost]
    public Task<Result> RevokeFormerEmployeePortalAccess([FromBody] RevokeFormerEmployeePortalAccessRequest request) =>
        _formerEmployeePortalService.RevokeAccess(request);

    [Route("RejoinEmployee")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> RejoinEmployee([FromBody] RejoinEmployeeRequest request) =>
        _employeeService.RejoinEmployee(request);

    [Route("ExceptionalCompleteEmployeeExit")]
    [HttpPost]
    [Authorize(Policy = "HrExitOverride")]
    public Task<Result> ExceptionalCompleteEmployeeExit([FromBody] ExceptionalCompleteEmployeeExitRequest request) =>
        _employeeService.ExceptionalCompleteEmployeeExit(request);

    // Department options are part of the Employee Directory.  Do not route this through
    // Dashboard.ViewAll: callers only need the same Employees.View permission as the grid.
    [Route("GetDirectoryDepartments")]
    [HttpGet]
    [ModulePermission(AppModule.Employees, Permission.View)]
    public Task<Result<DepartmentEmpResponseDto>> GetDirectoryDepartments() =>
        _employeeService.GetDirectoryDepartments();

    [Route("ChangePassword")]
    [HttpPost]
    public async Task<Result> ChangePassword(ChangePasswordRequest passwordModel)
    {
        Result result = new();
        if (!ModelState.IsValid) return result;
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _accountServices.ResetPassword(userId, passwordModel);
    }


    [Route("GetEmployeeById")]
    [HttpGet]
    public async Task<Result<UserModel>> GetEmployeeById([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        UserModel result = await _employeeService.GetEmployeeById(userId);
        return new Result<UserModel>()
        {
            Success = true,
            MethodResult = result
        };
    }

    [Route("Search")]
    [HttpGet]
    public async Task<Result<EmployeeSearchResponseDTO>> SearchEmployeeByName([FromQuery] string name)
    {
        Result<EmployeeSearchResponseDTO> result = new();
        if (string.IsNullOrEmpty(name))
        {
            result.Success = false;
            return result;
        }
        var empList = await _employeeService.SearchEmployeeByName(name);
        result.MethodResults = empList;
        result.Success = true;
        return result;
    }

    //Employee Details APIs

    [Route("AddEditEmployeeSummary")]
    [HttpPost]
    public async Task<Result> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary)
    {
        string userId = string.IsNullOrEmpty(userSummary.UserId) ? CurrentContext.UserId(_httpContextAccessor) : userSummary.UserId;
        return await _employeeService.AddEditEmployeeSummary(userSummary, userId);
    }

    [Route("AddEmployeeEducation")]
    [HttpPost]
    public async Task<Result> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails)
    {
        string userId = string.IsNullOrEmpty(educationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : educationDetails.UserId;
        return await _employeeService.AddEmployeeEducation(educationDetails, userId);
    }

    [Route("EditEmployeeEducation")]
    [HttpPost]
    public async Task<Result> EditEmployeeEducation(EmployeeEducationRequestModel educationDetails)
    {
        string userId = string.IsNullOrEmpty(educationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : educationDetails.UserId;
        return await _employeeService.EditEmployeeEducation(educationDetails, userId);
    }

    [Route("AddEmployeeCertification")]
    [HttpPost]
    public async Task<Result> AddEmployeeCertification(EmployeeCertificationRequestModel certificationDetails)
    {
        string userId = string.IsNullOrEmpty(certificationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : certificationDetails.UserId;
        return await _employeeService.AddEmployeeCertification(certificationDetails, userId);
    }

    [Route("EditEmployeeCertification")]
    [HttpPost]
    public async Task<Result> EditEmployeeCertification(EmployeeCertificationRequestModel certificationDetails)
    {
        string userId = string.IsNullOrEmpty(certificationDetails.UserId) ? CurrentContext.UserId(_httpContextAccessor) : certificationDetails.UserId;
        return await _employeeService.EditEmployeeCertification(certificationDetails, userId);
    }

    [Route("GetEmployeeSummary")]
    [HttpGet]
    public async Task<Result<EmployeeSummaryRequestModel>> GetEmployeeSummary([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        EmployeeSummaryRequestModel summary = await _employeeService.GetEmployeeSummary(userId);
        return new Result<EmployeeSummaryRequestModel>()
        {
            Success = true,
            MethodResult = summary
        };
    }

    [Route("GetEmployeeEducationDetails")]
    [HttpGet]
    public async Task<Result<EmpEducationDetails>> GetEmployeeEducationDetails([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        List<EmpEducationDetails> data = await _employeeService.GetEmployeeEducationDetails(userId);
        Result<EmpEducationDetails> result = new Result<EmpEducationDetails>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }


    [Route("GetEmployeeCertificationDetails")]
    [HttpGet]
    public async Task<Result<EmpCertificationDetails>> GetEmployeeCertificationDetails([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        List<EmpCertificationDetails> data = await _employeeService.GetEmployeeCertificationDetails(userId);
        Result<EmpCertificationDetails> result = new Result<EmpCertificationDetails>();
        result.Success = true;
        result.MethodResults = data.ToList();
        return result;
    }


    [Route("UploadUserImage")]
    [HttpPost]
    //[FilesExtensions([".jpg", ".jpeg", ".png"])]
    public async Task<Result<string>> UploadUserImage(IFormFile profilePicture)
    {

        string userId = CurrentContext.UserId(_httpContextAccessor);
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "ProfileImage");
        string fileExtension = Path.GetExtension(profilePicture.FileName);
        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }
        string fileName = $"{Guid.NewGuid().ToString()}{fileExtension}";
        string filePath = Path.Combine(uploadFolder, fileName);
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            await profilePicture.CopyToAsync(fileStream);
        }
        ;
        string profile = await _employeeService.GetUserExistingProfile(userId);
        if (string.IsNullOrEmpty(profile))
        {
            string data = await _employeeService.AddUserProfileImage(fileName, userId, filePath);
            Result<string> result = new()
            {
                MethodResult = data,
                Success = true,
                StatusCode = 201

            };
            return result;
        }
        else
        {
            string oldPath = Path.Combine(uploadFolder, profile);
            FileInfo fileInfo = new(oldPath);
            fileInfo.Delete();
            string data = await _employeeService.AddUserProfileImage(fileName, userId, filePath);

            Result<string> result = new()
            {
                MethodResult = data,
                Success = true,
                StatusCode = 200
            };
            return result;
        }
    }

    [Route("AddEditEmpSkills")]
    [HttpPost]
    public async Task<Result> AddEditSkills(SkillsRequestModel skillsModel)
    {
        string userId = string.IsNullOrEmpty(skillsModel.UserId) ? CurrentContext.UserId(_httpContextAccessor) : skillsModel.UserId;
        return await _employeeService.AddEditEmployeeSkills(skillsModel, userId);
    }

    [Route("GetEmployeeSkills")]
    [HttpGet]
    public async Task<Result<EmployeeSkillsDTO>> GetEmployeeSkills([FromQuery] string userId = null)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        var result = await _employeeService.GetEmployeeSkills(userId);
        return new Result<EmployeeSkillsDTO>()
        {
            Success = true,
            MethodResult = result
        };

    }

    [Route("DeleteEducationDetails/{educationId}")]
    [HttpDelete]
    public async Task<Result> DeleteEducationDetails(string educationId, [FromBody] string userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.DeleteEducationDetails(educationId, userId);
    }

    [Route("DeleteCertificationDetails/{certificationId}")]
    [HttpDelete]
    public async Task<Result> DeleteCertificationDetails(string certificationId, [FromBody] string userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.DeleteCertificationDetails(certificationId, userId);
    }

    [Route("DeleteEmployee")]
    [HttpDelete]
    [ModulePermission(AppModule.Employees, Permission.Delete)]
    public async Task<Result> DeleteEmployee([FromQuery] string employeeId)
    {
        Result data = await _employeeService.DeleteEmployee(employeeId);
        if (data == null)
        {
            return new Result()
            {
                Success = false,
                StatusCode = 400
            };
        }
        return new Result()
        {
            Success = true,
            StatusCode = 200
        };
    }

    [HttpGet]
    [Route("GetSuggestedSkills")]
    public async Task<Result<Skills>> GetSuggestedSkills([FromQuery] string query)
    {
        var sanitizedQuery = query.Trim().ToLower();
        if (string.IsNullOrEmpty(sanitizedQuery))
        {
            return new Result<Skills>()
            {
                Success = true,
                MethodResults = [],
                StatusCode = 200,
            };
        }
        return await _employeeService.GetSuggestedSkills(sanitizedQuery);
    }

    [HttpPost]
    [Route("AddSkill")]
    public async Task<Result> AddSkill([FromBody] string skill)
    {
        var sanitizeSkill = skill.Trim().ToLower();
        var regex = new Regex(@"^[a-zA-Z0-9\s\+\#\.\-]{2,30}$");
        var isValid = regex.IsMatch(sanitizeSkill);

        if (string.IsNullOrEmpty(sanitizeSkill) || !isValid)
        {
            return new Result()
            {
                StatusCode = 200,
                Success = false,
                Message = "InValid Skill Name"
            };
        }
        var result = await _employeeService.AddSkill(sanitizeSkill);
        return result;
    }

    [HttpGet]
    [Route("GetAllNotifications")]
    public async Task<Result<NotificationResponseModel>> GetAllNotifications([FromQuery] NotificationRequestDTO model)
    {
        string[] supportedType = ["all", "unread"];
        if (!supportedType.Contains(model.Type))
        {
            return new Result<NotificationResponseModel>();
        }
        string userId = CurrentContext.UserId(_httpContextAccessor);
        var data = await _employeeService.GetAllNotifications(model, userId);
        return new Result<NotificationResponseModel>()
        {
            Success = true,
            MethodResult = data,
        };
    }

    [HttpPatch]
    [Route("Notification/{userNotificationId}/MarkAsRead")]
    public async Task<Result> MarkNotificationAsRead(string userNotificationId)
    {
        if (string.IsNullOrEmpty(userNotificationId))
        {
            return new Result();
        }
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.MarkNotificationAsRead(currentUserId, userNotificationId);
    }

    [HttpPost]
    [Route("AddUpdateWorkHistory")]
    public async Task<Result> AddWorkHistory(EmployeeWorkHistoryModel model)
    {
        if (!ModelState.IsValid)
        {
            return new Result();
        }
        if (string.IsNullOrEmpty(model.UserId)) model.UserId = CurrentContext.UserId(_httpContextAccessor);
        Result result = await _employeeService.AddUpdateWorkHistory(model);
        return result;
    }

    [HttpGet]
    [Route("GetEmpWorkHistory")]
    public async Task<Result<EmployeeWorkHistoryModel>> GetEmpWorkHistory([FromQuery] string? userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        var data = await _employeeService.GetEmpWorkHistory(userId);
        return new Result<EmployeeWorkHistoryModel>()
        {
            MethodResults = data,
            TotalRecords = data.Count
        };
    }

    [HttpDelete]
    [Route("DeleteWorkHistory/{workId}")]
    public async Task<Result> DeleteWorkHistory(string workId, [FromBody] string? userId)
    {
        if (string.IsNullOrEmpty(userId)) userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.DeleteWorkHistory(workId, userId);
    }

    [HttpPost]
    [Route("MarkAllNotificationAsRead")]
    public async Task<Result> MarkAllNotificationAsRead()
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.MarkAllNotificationAsRead(userId);
    }

    [HttpDelete]
    [Route("RemoveProfileImage")]
    public async Task<Result> RemoveProfileImage()
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.RemoveProfileImage(userId);
    }

    [HttpGet]
    [Route("GetCollegeList")]
    public async Task<Result<string>> GetCollegeList([FromQuery] string searchValue)
    {
        List<string> collegeList = [];
        collegeList = await _employeeService.GetCollegeNameSuggestions(searchValue);
        return new Result<string>()
        {
            Success = true,
            MethodResults = collegeList,
            TotalRecords = collegeList.Count,
        };
    }

    [HttpGet]
    [Route("GetNotificationPreferences")]
    public async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>> GetNotificationPreferences()
    {
        Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> result = new();
        string userId = CurrentContext.UserId(_httpContextAccessor);
        result.MethodResult = await _employeeService.GetNotificationPreferences(userId);
        return result;
    }

    [HttpPost]
    [Route("UpdateNotificationPreferences")]
    public async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>> UpdateNotificationPreferences([FromBody] Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences)
    {
        string userId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.UpdateNotificationPreferences(userId, preferences);
    }

    [HttpPost]
    [Route("ResendInvite/{userId}")]
    [ModulePermission(AppModule.Employees, Permission.Create)]
    public async Task<Result> ResendInviteLink(string userId)
    {
        string currentUserId = CurrentContext.UserId(_httpContextAccessor);
        return await _employeeService.ResendInviteLink(userId, currentUserId);
    }
}

