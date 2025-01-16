using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Registration
{
    public class AutoMapperObjects : AutoMapper.Profile
    {
        private readonly IMapper _mapper;
        public AutoMapperObjects()
        {
            CreateMap<User, UserModel>().ReverseMap();
            CreateMap<Company, CompanyRequestModel>().ReverseMap();
            CreateMap<User, CompanyRequestModel>().ReverseMap();
            CreateMap<EducationDetails, EmployeeEducationRequestModel>().ReverseMap();
            CreateMap<CertificationDetails, EmployeeCertificationRequestModel>().ReverseMap();

        }


    }

}

