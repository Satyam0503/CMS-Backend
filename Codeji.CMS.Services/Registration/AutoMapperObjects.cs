using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Calendar;
using Codeji.CMS.DTO.Company;
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

namespace Codeji.CMS.Services.Registration
{
    public class AutoMapperObjects : AutoMapper.Profile
    {
        private readonly IMapper _mapper;
        public AutoMapperObjects()
        {
            // CreateMap<EmpUser, UserModel>().ForMember(dest => dest.Password, opt => opt.Ignore()).ReverseMap();
            CreateMap<EmpUser, UserModel>().ReverseMap();
            CreateMap<EmpUser, GetAllEmployeeResponseModel>().ReverseMap();
            CreateMap<EmpWorkHistory, EmployeeWorkHistoryModel>().ReverseMap();
            CreateMap<Company, CompanyRequestModel>().ReverseMap();
            CreateMap<EmpUser, CompanyRequestModel>().ReverseMap();
            CreateMap<EmpEducationDetails, EmployeeEducationRequestModel>().ReverseMap();
            CreateMap<EmpCertificationDetails, EmployeeCertificationRequestModel>().ReverseMap();
            CreateMap<Roles, RoleModel>().ReverseMap();
            CreateMap<JobVacancy, JobVacancyModel>().ReverseMap();
            CreateMap<Department, DepartmentRequestDto>().ReverseMap();
            CreateMap<Skills, SkillsDTO>().ReverseMap();
            CreateMap<Notice, MyNoticeDTO>().ReverseMap();
            CreateMap<Applicant, ApplicantViewModel>().ForMember(dest => dest.ApplyDate, opt => opt.MapFrom(src => src.CreatedDate)).ReverseMap();
            CreateMap<JobTitles, JobTitleRequestDto>().ReverseMap();
            CreateMap<CustomAttributeValue, CustomAttributeValueRequestDto>().ReverseMap();
            CreateMap<LeaveRequest, MyLeaveRequestResponse>().ReverseMap();
            CreateMap<CalendarEntity, UpComingHolidayEventResponseDto>().ReverseMap();
            CreateMap<CalendarEntity, CalendarResponseDto>().ReverseMap();
            CreateMap<LeavePolicy, LeavePolicyRequest>().ReverseMap();
            CreateMap<LeavePolicy, UpdateLeavePolicyRequest>().ReverseMap();
            CreateMap<Policy, CreatePolicyRequestModel>().ReverseMap();
            CreateMap<Policy, PolicyResponseModel>().ReverseMap();
        }
    }

}

