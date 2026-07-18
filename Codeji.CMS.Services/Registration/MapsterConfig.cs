using Mapster;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.DTO.Company.CustomAttribute;
using Codeji.CMS.DTO.Company.Department;
using Codeji.CMS.DTO.Company.JobTitle;
using Codeji.CMS.DTO.Company.Policy;
using Codeji.CMS.DTO.Dashboard;
using Codeji.CMS.DTO.LeaveManagement.Leave;
using Codeji.CMS.DTO.LeaveManagement.LeavePolicy;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Calendar;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Repository.Entities.NoticeBoard;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;

namespace Codeji.CMS.Services.Registration;

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<EmpUser, UserModel>().TwoWays();
        config.NewConfig<EmpUser, GetAllEmployeeResponseModel>().TwoWays();
        config.NewConfig<EmpWorkHistory, EmployeeWorkHistoryModel>().TwoWays();
        config.NewConfig<Company, CompanyRequestModel>().TwoWays();
        config.NewConfig<EmpUser, CompanyRequestModel>().TwoWays();
        config.NewConfig<EmpEducationDetails, EmployeeEducationRequestModel>().TwoWays();
        config.NewConfig<EmpCertificationDetails, EmployeeCertificationRequestModel>().TwoWays();
        config.NewConfig<Roles, RoleModel>().TwoWays();
        config.NewConfig<JobVacancy, JobVacancyModel>().TwoWays();
        config.NewConfig<Department, DepartmentRequestDto>().TwoWays();
        config.NewConfig<Skills, SkillsDTO>().TwoWays();
        config.NewConfig<Notice, MyNoticeDTO>().TwoWays();
        config.NewConfig<Applicant, ApplicantViewModel>()
            .Map(dest => dest.ApplyDate, src => src.CreatedDate);
        config.NewConfig<ApplicantViewModel, Applicant>()
            .Map(dest => dest.CreatedDate, src => src.ApplyDate);
        config.NewConfig<JobTitles, JobTitleRequestDto>().TwoWays();
        config.NewConfig<CustomAttributeValue, CustomAttributeValueRequestDto>().TwoWays();
        config.NewConfig<LeaveRequest, MyLeaveRequestResponse>().TwoWays();
        config.NewConfig<CalendarEntity, UpComingHolidayEventResponseDto>().TwoWays();
        config.NewConfig<CalendarEntity, CalendarResponseDto>().TwoWays();
        config.NewConfig<LeavePolicy, LeavePolicyRequest>().TwoWays();
        config.NewConfig<LeavePolicy, UpdateLeavePolicyRequest>().TwoWays();
        config.NewConfig<Policy, CreatePolicyRequestModel>().TwoWays();
        config.NewConfig<Policy, PolicyResponseModel>().TwoWays();
        config.NewConfig<AdminAttendanceCreateDto, AttendanceModel>()
            .Ignore(dest => dest.AttendanceId)
            .Ignore(dest => dest.TotalHours)
            .Ignore(dest => dest.Remarks);
        config.NewConfig<AttendanceModel, AttendanceResponseDto>();
        config.NewConfig<AttendanceUpdateDto, AttendanceModel>()
            .IgnoreNullValues(true);
    }
}
