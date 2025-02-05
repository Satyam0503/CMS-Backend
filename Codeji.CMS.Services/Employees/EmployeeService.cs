using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Employees
{
    public class EmployeeService : IEmployeeService
    {
        readonly IMongoDbRepository<EducationDetails> _educationDetailsRepo;
        readonly IMongoDbRepository<CertificationDetails> _certificationDetailsRepo;
        readonly IMongoDbRepository<EmployeeSummary> _employeeSummaryRepo;
        readonly IMapper _mapper;
        readonly IMongoDbRepository<User> _employeeRepository;
        readonly IMongoDbRepository<Roles> _rolesRepository;
        readonly IMongoDbRepository<Comments> _commentRepository;
        public EmployeeService(IMongoDbRepository<EducationDetails> educationDetailsRepo,
            IMapper mapper, IMongoDbRepository<CertificationDetails> certificationDetailsRepo,
            IMongoDbRepository<EmployeeSummary> userSummary,
            IMongoDbRepository<User> employeeRepository,
            IMongoDbRepository<Roles> rolesRepository,
            IMongoDbRepository<Comments> commentRepository
            )
        {
            _employeeRepository = employeeRepository;
            _rolesRepository = rolesRepository;
            _educationDetailsRepo = educationDetailsRepo;
            _mapper = mapper;
            _certificationDetailsRepo = certificationDetailsRepo;
            _employeeSummaryRepo = userSummary;
            _commentRepository = commentRepository;

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
                JobRole = user.JobRole,
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
                IsEmailVerified = true,
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
        public async Task<Result<UserModel>> EditEmployee(UserModel user, string userId, string companyId, string userPassword)
        {
            //User? checkUser = await _employeeRepository.FirstOrDefault(x => x.UserId == id);
            User employee = new User()
            {
                UserId = userId,
                CompanyId = companyId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleId = user.RoleId,
                Password = userPassword,
                Gender = user.Gender,
                EmployeeId = user.EmployeeId,
                JobRole = user.JobRole,
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
                IsEmailVerified = true,
                Address = user.Address
            };
            Expression<Func<User, bool>> whereCondition = x => x.UserId == userId;
            await _employeeRepository.Update(whereCondition, employee);
            return new Result<UserModel>
            {
                Message = "User Updated",
                Success = true,
                MethodResult = user,
            };
        }
        public async Task<UserModel> GetEmployeeById(string userId)
        {
            User? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);

            return new UserModel()
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                RoleId = user.RoleId,
                CompanyId = user.CompanyId,
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
                IsEmailVerified = true,
                Address = user.Address
            };

        }
        public async Task<List<UserModel>> GetAllEmployees(string companyId)
        {
            IEnumerable<User> list = await _employeeRepository.GetAll(x => x.CompanyId == companyId);
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

        // Logic for Login User and Employee by Email and Password
        public async Task<string> GetVerificationToken(string email, string password)
        {

            User? user = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            //Login User With Any Password need to change in future
            if (user.IsEmailVerified)
            {
                List<string> roles = new List<string>() { "employee" };
                return AuthenticationHandler.GenerateJwtToken(user.UserId, user.CompanyId, user.RoleId, roles);
            }
            else if (user != null && AuthenticationHandler.VerifyPassword(password, user.Password))
            {
                List<string> roles = new List<string>() { "admin", "employee" };
                return AuthenticationHandler.GenerateJwtToken(user.UserId, user.CompanyId, user.RoleId, roles);
            }
            return string.Empty;
        }

        public async Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId)
        {
            LoginUserViewModel returnModel = new LoginUserViewModel();
            UserModel? user = await GetEmployeeById(userId);
            Roles? role = await _rolesRepository.FirstOrDefault(x => x.RolesId == roleId);
            if (user is null)
            {
                return null;
            }
            returnModel.UserId = user.UserId;
            returnModel.Role = role.Titles;
            returnModel.FirstName = user.FirstName;
            returnModel.LastName = user.LastName;
            returnModel.Permissions = [];
            returnModel.CompanyId = role.CompanyId;
            return returnModel;

        }
        public async Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId)
        {
            Expression<Func<EmployeeSummary, bool>> whereCondition = x => userId == x.UserId && x.Id == userSummary.SummaryId && x.CompanyId == companyId;
            EmployeeSummary? employeesummary = await _employeeSummaryRepo.FirstOrDefault(whereCondition);
            bool success = false;

            if (employeesummary == null)
            {
                EmployeeSummary summary = new EmployeeSummary()
                {
                    CompanyId = companyId,
                    UserId = userId,
                    Summary = userSummary.Summary
                };
                Result result = await _employeeSummaryRepo.AddOne(summary);
                success = result.Success;
            }
            else
            {

                employeesummary.Summary = userSummary.Summary;
                Result result = await _employeeSummaryRepo.Update(whereCondition, employeesummary);
                success = result.Success;

            }
            return new Result<EmployeeSummaryRequestModel>
            {
                Message = "Summary Added Successfully",
                Success = success

            };


        }
        public async Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId)
        {
            EducationDetails educationDetail = new EducationDetails()
            {
                CompanyId = companyId,
                UserId = userId,
                EducationTitle = educationDetails.EducationTitle,
                CollegeName = educationDetails.CollegeName,
                StartDate = educationDetails.StartDate,
                EndDate = educationDetails.EndDate,
                Type = educationDetails.Type,
            };
            await _educationDetailsRepo.AddOne(educationDetail);
            return new Result<EmployeeEducationRequestModel>
            {
                MethodResult = educationDetails,
                Message = "Education Details Added",
                Success = true

            };
        }

        public async Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId)
        {
            CertificationDetails certificatinDetail = new CertificationDetails()
            {
                CompanyId = companyId,
                UserId = userId,
                CertificationTitle = cerificationDetails.CertificationTitle,
                OrganisationName = cerificationDetails.OrganisationName,
                StartDate = cerificationDetails.StartDate,
                EndDate = cerificationDetails.EndDate,
                Mode = cerificationDetails.Mode,
            };
            await _certificationDetailsRepo.AddOne(certificatinDetail);
            return new Result<EmployeeCertificationRequestModel>
            {
                MethodResult = cerificationDetails,
                Message = "Certification Details Added",
                Success = true
            };
        }

        public async Task<List<EmployeeEducationRequestModel>> GetEmployeeEducationDetails(string userId)
        {
            IEnumerable<EducationDetails> list = await _educationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmployeeEducationRequestModel>>(list);
        }

        public async Task<List<EmployeeCertificationRequestModel>> GetEmployeeCertificationDetails(string userId)
        {
            IEnumerable<CertificationDetails> list = await _certificationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmployeeCertificationRequestModel>>(list);
        }

        public async Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId)
        {
            EmployeeSummary summary = await _employeeSummaryRepo.FirstOrDefault(x => x.UserId == userId);
            return new EmployeeSummaryRequestModel()
            {
                Summary = summary.Summary
            };
        }

        //Logic For Addig Comment on Applicant By Employee (Admin And HR Manager )

        public async Task<Result> AddComment(string companyId, CommentRequestModel model)
        {
            Comments comments = new Comments()
            {
                CompanyId = companyId,
                UserId = model.UserId,
                ApplicantId = model.ApplicantId,
                ActivityCategory = model.ActivityCategory,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                UserName = model.Username,
            };
            await _commentRepository.AddOne(comments);
            Result result = new Result()
            {
                Success = true,
                Message = "Comment Added Successfully",
                StatusCode = StatusCodes.Status200OK,
            };
            return result;
        }
        public async Task<List<Comments>> GetAllComment(string applicantId)
        {
            IEnumerable<Comments> list = await _commentRepository.GetAll(x => x.ApplicantId == applicantId);
            return _mapper.Map<List<Comments>>(list);
        }
    }
}
