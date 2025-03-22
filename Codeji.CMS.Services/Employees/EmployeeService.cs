using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees
{
    public class EmployeeService : IEmployeeService
    {
        readonly IMongoDbRepository<EmpEducationDetails> _educationDetailsRepo;
        readonly IMongoDbRepository<EmpCertificationDetails> _certificationDetailsRepo;
        readonly IMongoDbRepository<EmpSummary> _employeeSummaryRepo;
        readonly IMapper _mapper;
        readonly IMongoDbRepository<EmpUser> _employeeRepository;
        readonly IMongoDbRepository<Roles> _rolesRepository;
        private readonly IRoleService _roleService;
        readonly IMongoDbRepository<Comments> _commentRepository;
        readonly IMongoDbRepository<RolePermission> _rolePermissionRepository;
        readonly IMongoDbRepository<EmpSkills> _employeeSkillsRepository;
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
        readonly IMongoDbRepository<ProcessLogs> _processLogs;
        readonly IMongoDbRepository<JobVacancy> _jobVacancy;
        public EmployeeService(IMongoDbRepository<EmpEducationDetails> educationDetailsRepo,
            IMapper mapper, IMongoDbRepository<EmpCertificationDetails> certificationDetailsRepo,
            IMongoDbRepository<EmpSummary> userSummary,
            IMongoDbRepository<EmpUser> employeeRepository,
            IMongoDbRepository<Roles> rolesRepository,
            IMongoDbRepository<Comments> commentRepository,
            IRoleService roleService,
            IMongoDbRepository<RolePermission> rolePermissionRepository,
            IMongoDbRepository<EmpSkills> employeeSkillsRepository,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IMongoDbRepository<ProcessLogs> processLogs,
            IMongoDbRepository<JobVacancy> jobVacancy
            )
        {
            _employeeRepository = employeeRepository;
            _rolesRepository = rolesRepository;
            _roleService = roleService;
            _educationDetailsRepo = educationDetailsRepo;
            _mapper = mapper;
            _certificationDetailsRepo = certificationDetailsRepo;
            _employeeSummaryRepo = userSummary;
            _commentRepository = commentRepository;
            _rolePermissionRepository = rolePermissionRepository;
            _employeeSkillsRepository = employeeSkillsRepository;
            _companyRepository = companyRepository;
            _mailTemplateRepository = mailTemplateRepository;
            _processLogs = processLogs;
            _jobVacancy = jobVacancy;

        }

        public async Task<Result<UserModel>> AddEmployee(UserModel user, string companyId)
        {
            EmpUser employee = new EmpUser()
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
                StatusNumber = Guid.NewGuid().ToString(),
                IsEmailVerified = false,
                Address = user.Address
            };
            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == employee.CompanyId);
            await _employeeRepository.AddOne(employee);

            //Acknowledgement Email Logic 
            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == 0);
            HtmlTemplate htmlTemplate = new HtmlTemplate();
            string replacedBody = htmlTemplate.Render(emailContent.body, new
            {
                RecipientName = employee.FirstName + " " + employee.LastName,
                PasswordCreationLink = ConfigManager.LocalAuthUrl,
                statusNumber = employee.StatusNumber,
                CompanyName = company.CompanyName,
            });
            await EmailFunctionality.SendEmailFromAPI(employee.Email, emailContent.subject, replacedBody);

            return new Result<UserModel>
            {
                MethodResult = user,
                Message = "User added",
                Success = true

            };
        }
        public async Task<Result<UserModel>> EditEmployee(UserModel user, string userId, string companyId)
        {
            //User? checkUser = await _employeeRepository.FirstOrDefault(x => x.UserId == id);

            UpdateDefinitionBuilder<EmpUser> update = Builders<EmpUser>.Update;
            List<UpdateDefinition<EmpUser>> updateDefinition = new();
            foreach (PropertyInfo property in user.GetType().GetProperties().Where(x => x.GetValue(user) != null))
            {
                object value = property.GetValue(user, null);
                if (value != null && value?.ToString() != "")
                {
                    updateDefinition.Add(update.Set(property.Name, property.GetValue(user)));
                }
            }

            updateDefinition.Add(update.Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, userId));
            UpdateDefinition<EmpUser> data = update.Combine(updateDefinition);
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == userId && x.CompanyId == companyId;
            Task<Result> model = _employeeRepository.UpdateMany(whereCondition, data, true);
            return new Result<UserModel>
            {
                Message = "User Updated",
                Success = true,
                MethodResult = user,
            };
        }
        public async Task<UserModel> GetEmployeeById(string userId)
        {
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
            UserModel userModel = _mapper.Map<UserModel>(user);
            return userModel;
        }
        public async Task<Result<UserModel>> GetAllEmployees(string companyId, int pageNo, int records)
        {
            Task<List<RoleModel>> roleList = _roleService.GetRoles(companyId);
            RoleModel? adminRole = roleList.Result.FirstOrDefault(role => role.Titles == "Company Administrator");
            IEnumerable<EmpUser> list = await _employeeRepository.GetAll(x => x.CompanyId == companyId && x.RoleId != adminRole.RolesId);
            object pagedList = list.Skip((pageNo - 1) * records).Take(records);
            if (pageNo != 0 && records != 0)
            {
                List<UserModel> data = _mapper.Map<List<UserModel>>(pagedList);
                Result<UserModel> result = new Result<UserModel>
                {
                    Success = true,
                    TotalRecords = list.Count(),
                    MethodResults = data.ToList(),
                };
                return result;
            }
            else
            {
                List<UserModel> data = _mapper.Map<List<UserModel>>(list);
                Result<UserModel> result = new Result<UserModel>
                {
                    Success = true,
                    TotalRecords = list.Count(),
                    MethodResults = data.ToList(),
                };
                return result;
            }


        }
        public async Task<bool> IsEmailExist(string email)
        {
            //return await _employeeRepository.Exist(x => x.Email.Equals(email));
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.Email == email);
            if (user is null)
            {
                return false;
            }
            return true;

        }

        public async Task<bool> ResetPassword(string userId, string companyId, string password, string oldPassword)
        {
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId && x.CompanyId == companyId);
            if (user is null)
                return false;
            if (!string.IsNullOrEmpty(oldPassword))
            {
                if (!AuthenticationHandler.VerifyPassword(oldPassword, user.Password))
                    return false;
            }

            user.Password = AuthenticationHandler.HashedPassword(password);
            user.UpdatedDate = DateTime.Now;
            Expression<Func<EmpUser, bool>> whereCondition = x => user.UserId == x.UserId;
            await _employeeRepository.Update(whereCondition, user);
            return true;
        }

        public async Task<bool> CreateNewPassword(string password, string statusNumber)
        {
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.StatusNumber == statusNumber);
            if (user is null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(user.Password))
            {
                return false;
            }
            user.Password = AuthenticationHandler.HashedPassword(password);
            user.IsEmailVerified = true;
            Expression<Func<EmpUser, bool>> whereCondition = x => x.StatusNumber == user.StatusNumber;
            await _employeeRepository.Update(whereCondition, user);
            return true;
        }
        // Logic for Login User and Employee by Email and Password
        public async Task<string> GetVerificationToken(string email, string password)
        {

            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(user.Password))
            {
                return "false";
            }
            if (user != null && AuthenticationHandler.VerifyPassword(password, user.Password))
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
            Company? companyDetails = await _companyRepository.FirstOrDefault(x => x.CompanyId == role.CompanyId);
            if (user is null)
            {
                return null;
            }
            List<ModuleWithPermissionsModel> modulePermission = await _roleService.GetRoleWithPermissions(role.RolesId, role.CompanyId);
            returnModel.UserId = user.UserId;
            returnModel.Role = role.Titles;
            returnModel.FirstName = user.FirstName;
            returnModel.LastName = user.LastName;
            returnModel.Permissions = [];
            returnModel.modulePermission = modulePermission;
            returnModel.RoleId = role.RolesId;
            returnModel.CompanyId = role.CompanyId;
            returnModel.CompanyName = companyDetails.CompanyName;
            returnModel.ProfileImage = string.IsNullOrEmpty(user.FullProfileUrl) ? null : user.FullProfileUrl;
            return returnModel;

        }
        public async Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId, string companyId)
        {
            Expression<Func<EmpSummary, bool>> whereCondition = x => userId == x.UserId && x.Id == userSummary.SummaryId && x.CompanyId == companyId;
            EmpSummary? employeesummary = await _employeeSummaryRepo.FirstOrDefault(x => x.UserId == userId && x.CompanyId == companyId);
            bool success = false;

            if (employeesummary == null && string.IsNullOrEmpty(userSummary.SummaryId))
            {
                EmpSummary summary = new EmpSummary()
                {

                    CompanyId = companyId,
                    UserId = userId,
                    Summary = userSummary.Summary
                };
                Result result = await _employeeSummaryRepo.AddOne(summary);
                return new Result<EmployeeSummaryRequestModel>
                {
                    Success = true,
                    Message = "Summary Added Successfully"
                };

            }
            else
            {
                employeesummary.Summary = userSummary.Summary;
                Result result = await _employeeSummaryRepo.Update(whereCondition, employeesummary);
                return new Result<EmployeeSummaryRequestModel>
                {
                    Success = true,
                    Message = "Summary Updated Successfully"
                };
            }
        }
        public async Task<Result> AddEditEmployeeSkills(SkillsRequestModel skillsModel, string companyId, string userId)
        {
            Expression<Func<EmpSkills, bool>> whereCondition = x => userId == x.UserId && x.CompanyId == companyId;
            EmpSkills? employeSkills = await _employeeSkillsRepository.FirstOrDefault(whereCondition);
            if (employeSkills == null)
            {
                EmpSkills newSkill = new EmpSkills();
                {
                    newSkill.CompanyId = companyId;
                    newSkill.UserId = userId;
                    newSkill.Skills = skillsModel.TotalSkills;
                }
                Result result = await _employeeSkillsRepository.AddOne(newSkill);
                return new Result
                {
                    Success = true,
                    Message = "Skills Addded Successfully"
                };
            }
            else
            {
                employeSkills.Skills = skillsModel.TotalSkills;
                Result result = await _employeeSkillsRepository.Update(whereCondition, employeSkills);
                return new Result
                {
                    Success = true,
                    Message = "Skills Updated Successfully"

                };
            }
        }
        public async Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId, string companyId)
        {
            EmpEducationDetails educationDetail = new EmpEducationDetails()
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

        public async Task<Result> EditEmployeeEducation(EmpEducationDetails educationDetails, string companyId, string userId)
        {
            Expression<Func<EmpEducationDetails, bool>> whereCondition = x => x.UserId == userId && x.CompanyId == companyId && x.EducationId == educationDetails.EducationId;
            EmpEducationDetails? check = await _educationDetailsRepo.FirstOrDefault(whereCondition);
            if (check == null)
            {
                return new Result()
                {
                    Message = "Education Details Not Updated",
                    Success = false,
                    StatusCode = 400

                };
            }
            EmpEducationDetails data = new EmpEducationDetails()
            {
                EducationId = educationDetails.EducationId,
                UserId = userId,
                CompanyId = companyId,
                EducationTitle = educationDetails.EducationTitle,
                CollegeName = educationDetails.CollegeName,
                EndDate = educationDetails.EndDate,
                StartDate = educationDetails.StartDate,
                Type = educationDetails.Type
            };
            await _educationDetailsRepo.Update(whereCondition, data);
            return new Result()
            {
                Message = "Education Details Updated Sucessfully",
                Success = true,
                StatusCode = 200

            };
        }

        public async Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId, string companyId)
        {
            EmpCertificationDetails certificatinDetail = new EmpCertificationDetails()
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

        public async Task<Result> EditEmployeeCertification(EmpCertificationDetails certificationDetails, string companyId, string userId)
        {
            Expression<Func<EmpCertificationDetails, bool>> whereCondition = x => x.UserId == userId && x.CompanyId == companyId && x.CertificationId == certificationDetails.CertificationId;
            EmpCertificationDetails? check = await _certificationDetailsRepo.FirstOrDefault(whereCondition);
            if (check == null)
            {
                return new Result()
                {
                    Message = "Certification Details Not Updated",
                    Success = false,
                    StatusCode = 400

                };
            }
            EmpCertificationDetails data = new EmpCertificationDetails()
            {
                CertificationId = certificationDetails.CertificationId,
                UserId = userId,
                CompanyId = companyId,
                CertificationTitle = certificationDetails.CertificationTitle,
                OrganisationName = certificationDetails.OrganisationName,
                EndDate = certificationDetails.EndDate,
                StartDate = certificationDetails.StartDate,
                Mode = certificationDetails.Mode
            };
            await _certificationDetailsRepo.Update(whereCondition, data);
            return new Result()
            {
                Message = "Certification Details Updated Sucessfully",
                Success = true,
                StatusCode = 200

            };
        }

        public async Task<List<EmpEducationDetails>> GetEmployeeEducationDetails(string userId)
        {
            IEnumerable<EmpEducationDetails> list = await _educationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmpEducationDetails>>(list);
        }

        public async Task<List<EmpCertificationDetails>> GetEmployeeCertificationDetails(string userId)
        {
            IEnumerable<EmpCertificationDetails> list = await _certificationDetailsRepo.GetAll(x => x.UserId == userId);
            return _mapper.Map<List<EmpCertificationDetails>>(list);
        }

        public async Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId, string companyId)
        {
            EmpSummary summary = await _employeeSummaryRepo.FirstOrDefault(x => x.UserId == userId && x.CompanyId == companyId);
            if (summary == null)
            {
                return new EmployeeSummaryRequestModel()
                {
                    Summary = "No Summary Available"
                };
            }
            return new EmployeeSummaryRequestModel()
            {
                SummaryId = summary.Id,
                Summary = summary.Summary
            };
        }

        public async Task<EmpSkills> GetEmployeeSkills(string companyId, string userId)
        {
            EmpSkills employeeSkills = await _employeeSkillsRepository.FirstOrDefault(x => x.UserId == userId && x.CompanyId == companyId);
            if (employeeSkills == null)
            {
                return new EmpSkills()
                {
                    Skills = ""
                };
            }
            return new EmpSkills()
            {
                Id = employeeSkills.Id,
                Skills = employeeSkills.Skills
            };
        }

        //Logic For Addig Comment on Applicant By Employee (Admin And HR Manager )

        public async Task<Result> AddComment(string companyId, string userId, CommentRequestModel model)
        {
            Comments comments = new Comments()
            {
                CompanyId = companyId,
                UserId = userId,
                ApplicantId = model.ApplicantId,
                ActivityCategory = model.ActivityCategory,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                UserName = model.Username,
            };
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.CompanyId == companyId && x.UserId == model.UserId);
            ProcessLogs processLogs = new ProcessLogs()
            {
                CompanyId = companyId,
                UserId = userId,
                ApplicantName = model.Username,
                CommentedOn = DateTime.UtcNow,
                Comments = model.Description,
                CommentedBy = user.FirstName,
                ActionCategory = model.ActivityCategory,
                JobRole = model.JobTitle

            };
            await _processLogs.AddOne(processLogs);
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
        public async Task<string> GetUserExistingProfile(string userId)
        {
            EmpUser? res = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
            return res?.ProfileUrl ?? string.Empty;
        }
        public async Task<string> AddUserProfileImage(string fileName, string userId, string filePath)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == userId;
            EmpUser profile = await _employeeRepository.FirstOrDefault(whereCondition);
            if (profile == null)
            {
                return "User Not Found";
            }
            profile.ProfileUrl = fileName;

            Result res = await _employeeRepository.Update(whereCondition, profile);
            string fullProfileUrl = Common.GetEmployeeImageUrl(fileName);
            return fullProfileUrl;
        }

        public async Task<Result> DeleteEducationDetails(string educationId, string companyId, string userId)
        {
            Expression<Func<EmpEducationDetails, bool>> wherCondition = x => x.EducationId == educationId && x.CompanyId == companyId && x.UserId == userId;
            Result data = await _educationDetailsRepo.Delete(wherCondition);
            return data;
        }

        public async Task<Result> DeleteCertificationDetails(string certificationId, string companyId, string userId)
        {
            Expression<Func<EmpCertificationDetails, bool>> whereCondition = x => x.CertificationId == certificationId && x.UserId == userId && x.CompanyId == companyId;
            Result data = await _certificationDetailsRepo.Delete(whereCondition);
            return data;
        }

        public async Task<Result> DeleteEmployee(string employeeId, string companyId)
        {
            Expression<Func<EmpUser, bool>> employeeWhereCondition = x => x.UserId == employeeId && x.CompanyId == companyId;
            Expression<Func<EmpCertificationDetails, bool>> certificateWhereCondition = x => x.UserId == employeeId && x.CompanyId == companyId;
            Expression<Func<EmpSkills, bool>> skillsWhereCondition = x => x.UserId == employeeId && x.CompanyId == companyId;
            Expression<Func<EmpSummary, bool>> summaryWhereCondition = x => x.UserId == employeeId && x.CompanyId == companyId;
            Expression<Func<EmpEducationDetails, bool>> educationWhereCondition = x => x.UserId == employeeId && x.CompanyId == companyId;
            await _certificationDetailsRepo.Delete(certificateWhereCondition);
            await _employeeSkillsRepository.Delete(skillsWhereCondition);
            await _employeeSummaryRepo.Delete(summaryWhereCondition);
            await _educationDetailsRepo.Delete(educationWhereCondition);

            Result data = await _employeeRepository.Delete(employeeWhereCondition);

            return data;
        }

        public async Task<List<ProcessLogs>> GetProcessLogData(string companyId)
        {
            Expression<Func<ProcessLogs, bool>> whereCondition = x => x.CompanyId == companyId;
            return (await _processLogs.GetAll(whereCondition)).ToList();
        }
    }
}
