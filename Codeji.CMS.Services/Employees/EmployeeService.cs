using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
using Codeji.CMS.DTO.RolePermissions;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility;
using Codeji.CMS.Utility.Helpers;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees
{
    public class EmployeeService : IEmployeeService
    {
        readonly IMongoDbRepository<EmpEducationDetails> _educationDetailsRepo;
        readonly IMongoDbRepository<EmpCertificationDetails> _certificationDetailsRepo;
        readonly IMongoDbRepository<EmpSummary> _employeeSummaryRepo;
        readonly IMapper _mapper;
        private readonly IPriorityTaskQueue _priorityTaskQueue;
        readonly IMongoDbRepository<EmpUser> _employeeRepository;
        readonly IMongoDbRepository<Roles> _rolesRepository;
        private readonly IRoleService _roleService;
        readonly IMongoDbRepository<RolePermission> _rolePermissionRepository;
        readonly IMongoDbRepository<EmpSkills> _employeeSkillsRepository;
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
        readonly IMongoDbRepository<JobVacancy> _jobVacancy;
        private readonly IMiddlewareService _middlewareService;
        readonly IMongoDbRepository<Department> _departmentRepository;

        readonly IMongoDbRepository<Skills> _skillsRepository;

        public EmployeeService(IMongoDbRepository<EmpEducationDetails> educationDetailsRepo,
            IMapper mapper, IMongoDbRepository<EmpCertificationDetails> certificationDetailsRepo,
            IMongoDbRepository<EmpSummary> userSummary,
            IMongoDbRepository<EmpUser> employeeRepository,
            IMongoDbRepository<Roles> rolesRepository,
            IRoleService roleService,
            IMongoDbRepository<RolePermission> rolePermissionRepository,
            IMongoDbRepository<EmpSkills> employeeSkillsRepository,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IMongoDbRepository<JobVacancy> jobVacancy,
            IPriorityTaskQueue priorityTaskQueue,
            IMiddlewareService middlewareService,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbRepository<Department> departmentRepository,
            IMongoDbRepository<Skills> skillsRepository
            )
        {
            _employeeRepository = employeeRepository;
            _rolesRepository = rolesRepository;
            _roleService = roleService;
            _educationDetailsRepo = educationDetailsRepo;
            _mapper = mapper;
            _certificationDetailsRepo = certificationDetailsRepo;
            _employeeSummaryRepo = userSummary;
            _rolePermissionRepository = rolePermissionRepository;
            _employeeSkillsRepository = employeeSkillsRepository;
            _companyRepository = companyRepository;
            _mailTemplateRepository = mailTemplateRepository;
            _jobVacancy = jobVacancy;
            _priorityTaskQueue = priorityTaskQueue;
            _middlewareService = middlewareService;
            _departmentRepository = departmentRepository;
            _skillsRepository = skillsRepository;
        }

        public async Task<Result<UserModel>> AddEmployee(UserModel user, string currentUserId)
        {
            EmpUser employee = new EmpUser()
            {
                UserId = user.UserId,
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

            await _employeeRepository.AddOne(employee);

            UserModel currentUser = _middlewareService.GetUserById(currentUserId);
            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == currentUser.CompanyId);

            //Acknowledgement Email Logic 
            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == 0);
            HtmlTemplate htmlTemplate = new HtmlTemplate();
            string replacedBody = htmlTemplate.Render(emailContent.body, new
            {
                RecipientName = employee.FirstName + " " + employee.LastName,
                PasswordCreationLink = ConfigManager.AppSettings.AppUrl + "auth/createpassword",
                statusNumber = employee.StatusNumber,
                CompanyName = company != null ? company.CompanyName : "",
            });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
          {
              _middlewareService.EmailSendAndSave(new Repository.Entities.EmpEmailLogs()
              {
                  UserTo = employee.Email,
                  Subject = emailContent.subject,
                  Body = replacedBody,
                  EmailLogType = Utility.Enums.EnumsHelper.MailType.CreateNewPasswordMail,
                  Email = employee.Email,
                  UserFrom = currentUser.Email,
              });
          }, priority: 1);

            return new Result<UserModel>
            {
                MethodResult = user,
                Message = "User added",
                Success = true
            };
        }
        public async Task<Result<UserModel>> EditEmployee(UserModel user, string userId)
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
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == userId;
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
        public async Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(int pageNo, int records)
        {
            pageNo = pageNo == 0 ? 1 : pageNo;
            records = records == 0 ? 10 : records;
            var count = _employeeRepository.Count();
            var empList = (await _employeeRepository.GetAggregateDataAsync<EmpUser>(pageNo: pageNo, pageSize: records)).ToList();
            string[] departmentList = empList.Select(x => x.Department).Distinct().ToArray();
            IEnumerable<Department> depList = await _departmentRepository.GetAll(x => departmentList.Contains(x.DepartmentId));
            var data = (from emp in empList
                        join department in depList
                        on emp.Department equals department.DepartmentId into deptGroup
                        from dept in deptGroup.DefaultIfEmpty()
                        select new GetAllEmployeeResponseModel
                        {
                            UserId = emp.UserId,
                            FullName = $"{emp.FirstName} {emp.LastName ?? ""}",
                            Email = emp.Email,
                            EmployeeId = emp.EmployeeId,
                            JobRole = emp.JobRole,
                            Department = dept?.DepartmentName,
                            PhoneNumber = emp.PhoneNumber,
                            DateOfBirth = emp.DateOfBirth,
                            FullProfileUrl = string.IsNullOrEmpty(emp.ProfileUrl) ? null : Common.GetEmployeeImageUrl(emp.ProfileUrl),
                        }).ToList();

            return new Result<GetAllEmployeeResponseModel>()
            {
                Success = true,
                TotalRecords = await count,
                MethodResults = data,
            };
        }
        public async Task<bool> IsEmailExist(string email)
        {
            bool IsEmailExist = await _employeeRepository.Exist(x => x.Email == email);
            return IsEmailExist;

        }

        public async Task<bool> ResetPassword(string userId, string password, string oldPassword)
        {
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
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
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == user.UserId;
            await _employeeRepository.Update(whereCondition, user);
            return true;
        }
        // Logic for Login User and Employee by Email and Password
        public async Task<string> GetVerificationToken(string email, string password)
        {
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            Roles? role = await _rolesRepository.FirstOrDefault(x => x.RolesId == user.RoleId);
            if (role.HasAppAccess)
            {
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
            return "No Access";
        }

        public async Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId)
        {

            LoginUserViewModel returnModel = new LoginUserViewModel();
            UserModel? user = await GetEmployeeById(userId);
            Roles? role = await _rolesRepository.FirstOrDefault(x => x.RolesId == roleId);
            Company? companyDetails = await _companyRepository.FirstOrDefault(x => true);
            if (user is null)
            {
                return null;
            }
            string[] allowedModulePermission = await _roleService.GetRolePermissionOfuser(role.RolesId);
            returnModel.UserId = user.UserId;
            // returnModel.Role = role.Titles;
            returnModel.FirstName = user.FirstName;
            returnModel.LastName = user.LastName;
            // returnModel.Permissions = [];
            returnModel.modulePermission = allowedModulePermission;
            // returnModel.RoleId = role.RolesId;
            // returnModel.CompanyId = role.CompanyId;
            returnModel.CompanyName = companyDetails.CompanyName;
            returnModel.ProfileImage = string.IsNullOrEmpty(user.FullProfileUrl) ? null : user.FullProfileUrl;
            return returnModel;

        }
        public async Task<Result<EmployeeSummaryRequestModel>> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId)
        {
            Expression<Func<EmpSummary, bool>> whereCondition = x => userId == x.UserId && x.Id == userSummary.SummaryId;
            EmpSummary? employeesummary = await _employeeSummaryRepo.FirstOrDefault(whereCondition);

            if (employeesummary == null)
            {
                EmpSummary summary = new EmpSummary()
                {
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
        public async Task<Result> AddEditEmployeeSkills(SkillsRequestModel skillsModel, string userId)
        {
            Expression<Func<EmpSkills, bool>> whereCondition = x => userId == x.UserId;
            EmpSkills? employeSkills = await _employeeSkillsRepository.FirstOrDefault(whereCondition);
            if (employeSkills == null)
            {
                EmpSkills newSkill = new EmpSkills();
                {
                    newSkill.UserId = userId;
                    newSkill.Skills = skillsModel.Skills;
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
                employeSkills.Skills = skillsModel.Skills;
                Result result = await _employeeSkillsRepository.Update(whereCondition, employeSkills);
                return new Result
                {
                    Success = true,
                    Message = "Skills Updated Successfully"
                };
            }
        }
        public async Task<Result<EmployeeEducationRequestModel>> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId)
        {
            EmpEducationDetails educationDetail = new EmpEducationDetails()
            {
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

        public async Task<Result> EditEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId)
        {
            Expression<Func<EmpEducationDetails, bool>> whereCondition = x => x.UserId == userId && x.EducationId == educationDetails.EducationId;
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

        public async Task<Result<EmployeeCertificationRequestModel>> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId)
        {
            EmpCertificationDetails certificatinDetail = new EmpCertificationDetails()
            {
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

        public async Task<Result> EditEmployeeCertification(EmployeeCertificationRequestModel certificationDetails, string userId)
        {
            Expression<Func<EmpCertificationDetails, bool>> whereCondition = x => x.UserId == userId && x.CertificationId == certificationDetails.CertificationId;
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

        public async Task<EmployeeSummaryRequestModel> GetEmployeeSummary(string userId)
        {
            EmpSummary summary = await _employeeSummaryRepo.FirstOrDefault(x => x.UserId == userId);
            if (summary == null)
            {
                return new EmployeeSummaryRequestModel()
                {
                    Summary = ""
                };
            }
            return new EmployeeSummaryRequestModel()
            {
                SummaryId = summary.Id,
                Summary = summary.Summary
            };
        }

        public async Task<EmployeeSkillsDTO> GetEmployeeSkills(string userId)
        {
            EmployeeSkillsDTO EmpSkills = new();
            EmpSkills? employeeSkills = await _employeeSkillsRepository.FirstOrDefault(x => x.UserId == userId);
            if (employeeSkills == null)
            {
                EmpSkills.Skills = [];
            }
            else
            {
                IEnumerable<Skills> skills = await _skillsRepository.GetAll(x => employeeSkills.Skills.Contains(x.Id));
                EmpSkills.Id = employeeSkills.Id;
                EmpSkills.Skills = _mapper.Map<List<SkillsDTO>>(skills);
                EmpSkills.UserId = employeeSkills.UserId;
            }
            return EmpSkills;
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

        public async Task<Result> DeleteEducationDetails(string educationId, string userId)
        {
            Expression<Func<EmpEducationDetails, bool>> wherCondition = x => x.EducationId == educationId && x.UserId == userId;
            Result data = await _educationDetailsRepo.Delete(wherCondition);
            return data;
        }

        public async Task<Result> DeleteCertificationDetails(string certificationId, string userId)
        {
            Expression<Func<EmpCertificationDetails, bool>> whereCondition = x => x.CertificationId == certificationId && x.UserId == userId;
            Result data = await _certificationDetailsRepo.Delete(whereCondition);
            return data;
        }

        public async Task<Result> DeleteEmployee(string employeeId)
        {
            //Update for Education Details
            Expression<Func<EmpCertificationDetails, bool>> certificateWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpCertificationDetails> empCerUpdateDefinition = Builders<EmpCertificationDetails>.Update;
            UpdateDefinition<EmpCertificationDetails> empCerUpdate = empCerUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _certificationDetailsRepo.UpdateMany(certificateWhereCondition, empCerUpdate);
            //Update for Education Details
            Expression<Func<EmpEducationDetails, bool>> educationWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpEducationDetails> empEduUpdateDefinition = Builders<EmpEducationDetails>.Update;
            UpdateDefinition<EmpEducationDetails> empEduUpdate = empEduUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _educationDetailsRepo.UpdateMany(educationWhereCondition, empEduUpdate);
            //Update for Employee Skills
            Expression<Func<EmpSkills, bool>> skillsWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpSkills> empSkillUpdateDefinition = Builders<EmpSkills>.Update;
            UpdateDefinition<EmpSkills> empSkillUpdate = empSkillUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _employeeSkillsRepository.UpdateMany(skillsWhereCondition, empSkillUpdate);


            Expression<Func<EmpSummary, bool>> summaryWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpSummary> empSumUpdateDefinition = Builders<EmpSummary>.Update;
            UpdateDefinition<EmpSummary> empSumUpdate = empSumUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            await _employeeSummaryRepo.UpdateMany(summaryWhereCondition, empSumUpdate);



            Expression<Func<EmpUser, bool>> employeeWhereCondition = x => x.UserId == employeeId;
            UpdateDefinitionBuilder<EmpUser> empUpdateDefinition = Builders<EmpUser>.Update;
            UpdateDefinition<EmpUser> empUpdate = empUpdateDefinition.Set(x => x.IsDeleted, true).Set(x => x.UpdatedDate, DateTime.UtcNow).Set(x => x.UpdatedBy, employeeId);
            Result data = await _employeeRepository.UpdateMany(employeeWhereCondition, empUpdate);

            return data;
        }



        public async Task<bool> IsUserActive(string userId)
        {
            bool IsUserActive = await _employeeRepository.Exist(x => x.UserId == userId && x.Status);
            return IsUserActive;
        }

        public async Task<Result> AddSkill(string skill)
        {
            var exist = await _skillsRepository.Exist(x => x.Name.Equals(skill, StringComparison.OrdinalIgnoreCase));
            if (exist)
            {
                return new Result()
                {
                    StatusCode = 200,
                    Success = false,
                    Message = "Skill already exist"
                };
            }
            Skills skills = new()
            {
                Name = skill,
            };
            return await _skillsRepository.AddOne(skills);
        }

        public async Task<Result<Skills>> GetSuggestedSkills(string query)
        {
            var skills = await _skillsRepository.GetAll(x => x.Name.Contains(query));
            return new Result<Skills>()
            {
                MethodResults = skills == null ? [] : [.. skills],
                StatusCode = 200,
                TotalRecords = skills == null ? 0 : skills.Count(),
                Success = true,
            };
        }
    }
}
