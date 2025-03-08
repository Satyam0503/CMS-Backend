using AutoMapper;
using Codeji.CMS.DTO;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Interface;

namespace Codeji.CMS.Services
{
    public class MiddlewareService : IMiddlewareService
    {
        private readonly IMongoDbRepository<User> _userRepository;
        private readonly IMapper _mapper;
        public MiddlewareService(IMongoDbRepository<User> userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public UserModel GetUserById(string id)
        {
            UserModel userModel = new();
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            User user = _userRepository.FirstOrDefault(x => x.UserId == id).Result;
            return _mapper.Map(user, userModel);
        }

        //public static void ApplyFilter(FilterRequestModel model)
        //{

        //}
    }
}

