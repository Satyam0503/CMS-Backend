using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services;

public class UserService : IUserService
{
    readonly IMongoDbRepository<User> _employeeRepository;
    readonly IMongoDbRepository<EducationDetails> _educationRepo;
    readonly IMapper _mapper;

    public UserService(IMongoDbRepository<User> employeeRepository, IMapper mapper, IMongoDbRepository<EducationDetails> educationRepo)
    {
        _employeeRepository = employeeRepository;
        _educationRepo = educationRepo;
        _mapper = mapper;
    }
    public async Task<Result<UserModel>> AddEmployee(UserModel user, string companyId)
    {

        User employee = new User()
        {
            UserId = user.UserId,
            CompanyId = companyId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Password = user.Password,
            RoleId = user.RoleId,
            Gender = user.Gender,
            EmployeeId = user.EmployeeId,
            DateOfBirth = user.DateOfBirth,
            Department = user.Department,
            ReportingManager = user.ReportingManager,
            TeamLead = user.TeamLead,
            PhoneNumber = user.PhoneNumber,
            BloodGroup = user.BloodGroup,
            PersonalEmail = user.PersonalEmail,
            EmergencyContact = user.EmergencyContact,
            DateOfJoining = user.DateOfJoining,
            Status = true,
            Address = user.Address
        };

        await _employeeRepository.AddOne(employee);

        return new Result<UserModel>
        {
            MethodResult = user,
            Message = "User added",
            Success = true

        };
    }
    public async Task<Result<UserModel>> EditEmployee(UserModel user)
    {
        User employee = new User()
        {

            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = user.RoleId

        };
        Expression<Func<User, bool>> whereCondition = x => user.UserId == x.UserId;
        await _employeeRepository.Update(whereCondition, employee);
        return new Result<UserModel>
        {
            MethodResult = user,
            Message = "User Updated",
            Success = true

        };
    }
    public async Task<UserModel> GetEmployeeById(string id)
    {
        User? user = await _employeeRepository.FirstOrDefault(x => x.UserId == id);

        return new UserModel()
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            RoleId = user.RoleId
        };

    }
    public async Task<List<UserModel>> GetAllEmployees()
    {
        IEnumerable<User> list = await _employeeRepository.GetAll();
        return _mapper.Map<List<UserModel>>(list);

    }
    public async Task<bool> IsEmailExist(string email)
    {
        //return await _employeeRepository.Exist(x => x.Email.Equals(email));
        User? user = await _employeeRepository.FirstOrDefault(x => x.Email == email);
        if (user is null)
        {
            return false;
        }
        return true;

    }

    public async Task<bool> ResetPassword(string userId, string password, string oldPassword = "")
    {
        User? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
        if (user is null)
            return false;

        if (!string.IsNullOrEmpty(oldPassword))
        {
            if (!AuthenticationHandler.VerifyPassword(oldPassword, user.Password))
                return false;
        }

        user.Password = AuthenticationHandler.HashedPassword(password);
        user.UpdatedDate = DateTime.Now;
        Expression<Func<User, bool>> whereCondition = x => user.UserId == x.UserId;
        await _employeeRepository.Update(whereCondition, user);
        return true;
    }
    // Logic for Login User and Applicant by Email and Password
    public async Task<string> GetVerificationToken(string email, string password)
    {
        User? user = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user != null && AuthenticationHandler.VerifyPassword(password, user.Password))
        {
            List<string> roles = new List<string>() { "admin", "employee" };
            return AuthenticationHandler.GenerateJwtToken(user.UserId, user.CompanyId, user.RoleId, roles);
        }
        return string.Empty;
    }

    public async Task<LoginUserViewModel> GetSignedUserDetails(string userId)
    {
        LoginUserViewModel returnModel = new LoginUserViewModel();
        UserModel? user = await GetEmployeeById(userId);
        if (user is null)
        {
            return null;
        }
        returnModel.UserId = user.UserId;
        returnModel.Role = "admin";
        returnModel.FirstName = user.FirstName;
        returnModel.LastName = user.LastName;
        returnModel.Permissions = [];
        return returnModel;

    }
}
