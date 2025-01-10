using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.Registration
{
    public class AutoMapperObjects : AutoMapper.Profile
    {
        private IMapper _mapper;
        public AutoMapperObjects()
        {
            CreateMap<User, UserModel>().ReverseMap();
            CreateMap<Company, CompanyRequestModel>().ReverseMap();
            CreateMap<User, CompanyRequestModel>().ReverseMap();

        }


    }

}

