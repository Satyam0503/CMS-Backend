using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;

namespace Codeji.CMS.Services.Registration
{
    public class AutoMapperObjects : AutoMapper.Profile
    {
        private readonly IMapper _mapper;
        public AutoMapperObjects()
        {
            CreateMap<User, UserModel>().ForMember(dest => dest.Password, opt => opt.Ignore()).ReverseMap();
            CreateMap<Company, CompanyRequestModel>().ReverseMap();
            CreateMap<User, CompanyRequestModel>().ReverseMap();
            CreateMap<EducationDetails, EmployeeEducationRequestModel>().ReverseMap();
            CreateMap<CertificationDetails, EmployeeCertificationRequestModel>().ReverseMap();
            CreateMap<Roles, RoleModel>().ReverseMap();
            CreateMap<JobVacancy, JobVacancyModel>().ReverseMap();
            CreateMap<Applicant, ApplicantViewModel>().ForMember(dest => dest.ApplyDate, opt => opt.MapFrom(src => src.CreatedDate)).ReverseMap();

        }


    }

}

