using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.Repository.Entities;
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
            

        }


    }

}

