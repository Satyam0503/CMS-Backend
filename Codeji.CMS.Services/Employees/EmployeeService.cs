using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using BCrypt.Net;
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
using Codeji.CMS.Utility.Enums;
using Codeji.CMS.Utility.Helpers;
using Codeji.CMS.Utility.middlewares;
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
        readonly IRoleService _roleService;
        readonly IMongoDbRepository<RolePermission> _rolePermissionRepository;
        readonly IMongoDbRepository<EmpSkills> _employeeSkillsRepository;
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
        readonly IMongoDbRepository<JobVacancy> _jobVacancy;
        private readonly IMiddlewareService _middlewareService;
        readonly IMongoDbRepository<Department> _departmentRepository;

        readonly IMongoDbRepository<Skills> _skillsRepository;
        readonly IMongoDbRepository<PasswordResetTokens> _passwordResetTokens;
        readonly IMongoDbRepository<EmpWorkHistory> _empWorkHistoryRepository;
        readonly IMongoDbRepository<UserNotifications> _userNotificationRepository;
        readonly IMongoDbRepository<Notifications> _notificationsRepository;
        readonly IHttpContextAccessor _httpContextAccessor;

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
            IMongoDbRepository<Skills> skillsRepository,
            IMongoDbRepository<PasswordResetTokens> passwordResetTokens,
            IMongoDbRepository<EmpWorkHistory> empWorkHistoryRepository,
            IMongoDbRepository<UserNotifications> userNotificationRepository,
            IMongoDbRepository<Notifications> notificationsRepository
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
            _passwordResetTokens = passwordResetTokens;
            _httpContextAccessor = httpContextAccessor;
            _empWorkHistoryRepository = empWorkHistoryRepository;
            _userNotificationRepository = userNotificationRepository;
            _notificationsRepository = notificationsRepository;
        }

        public async Task<Result<UserModel>> AddEmployee(UserModel user, string currentUserId)
        {
            Result<UserModel> result = new();
            var userId = Guid.NewGuid().ToString();
            EmpUser employee = new EmpUser()
            {
                UserId = userId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
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
                IsEmailVerified = false,
                Address = user.Address
            };

            Result result1 = await _employeeRepository.AddOne(employee);
            if (!result1.Success)
            {
                result.Success = false;
                return result;
            }
            UserModel currentUser = _middlewareService.GetUserById(currentUserId);
            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == currentUser.CompanyId);

            // generate password creation token for newly added employee
            string token = TokenHelper.GenerateToken();
            string tokenHash = TokenHelper.ComputeSha256Hash(token);
            PasswordResetTokens passwordResetTokens = new()
            {
                UserId = userId,
                TokenHash = tokenHash,
                IsUsed = false,
                Expiry = DateTime.UtcNow.AddMinutes(15),
            };
            var result2 = await _passwordResetTokens.AddOne(passwordResetTokens);

            //Acknowledgement Email Logic 
            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.CreateNewPasswordMail);
            HtmlTemplate htmlTemplate = new HtmlTemplate();
            string replacedBody = htmlTemplate.Render(emailContent.body, new
            {
                RecipientName = employee.FirstName + " " + employee.LastName,
                PasswordCreationLink = $"{ConfigManager.AppSettings.AppUrl}auth/createpassword?token={Uri.EscapeDataString(token)}&uid={userId}",
                CompanyName = company != null ? company.CompanyName : "",
            });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
          {
              _middlewareService.EmailSendAndSave(new EmpEmailLogs()
              {
                  UserTo = employee.Email,
                  Subject = emailContent.subject,
                  Body = replacedBody,
                  EmailLogType = EnumsHelper.MailType.CreateNewPasswordMail,
                  Email = employee.Email,
                  UserFrom = currentUser.Email,
              });
          }, priority: 1);

            result.MethodResult = user;
            result.Message = "User added";
            return result;
        }
        public async Task<Result<UserModel>> EditEmployee(EmployeePersonalInfo user, string userId)
        {
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
            Result result = await _employeeRepository.UpdateMany(whereCondition, data, true);
            UserModel updatedUser = await GetEmployeeById(userId);
            return new Result<UserModel>()
            {
                Success = true,
                MethodResult = updatedUser,
                Message = "Updated Successfully",
            };
        }
        public async Task<UserModel> GetEmployeeById(string userId)
        {
            string acceptLanguage = CurrentContext.GetLanguage(_httpContextAccessor);
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
            EmpUser? teamLead = await _employeeRepository.FirstOrDefault(x => x.UserId == user.TeamLead);
            EmpUser? reportingManager = await _employeeRepository.FirstOrDefault(x => x.UserId == user.ReportingManager);
            Department? department = await _departmentRepository.FirstOrDefault(x => x.DepartmentId == user.Department);
            UserModel userModel = _mapper.Map<UserModel>(user);
            userModel.DepartmentName = department?.Titles?.FirstOrDefault(x => x.Language == acceptLanguage)?.Label;
            userModel.TeamLeadName = teamLead != null ? $"{teamLead.FirstName} {teamLead.LastName}" : null;
            userModel.ReportingManagerName = reportingManager != null ? $"{reportingManager?.FirstName} {reportingManager?.LastName}" : null;
            userModel.FullProfileUrl = string.IsNullOrEmpty(user.ProfileUrl) ? Common.GetEmployeeImageUrl(null) : Common.GetEmployeeImageUrl(user.ProfileUrl);
            return userModel;
        }

        public async Task<List<EmployeeSearchResponseDTO>> SearchEmployeeByName(string name)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => x.FirstName.Contains(name, StringComparison.CurrentCultureIgnoreCase) || x.LastName.Contains(name, StringComparison.CurrentCultureIgnoreCase);
            var data = await _employeeRepository.GetAll(whereCondition);
            return _mapper.Map<List<EmployeeSearchResponseDTO>>(data);
        }

        public async Task<Result<GetAllEmployeeResponseModel>> GetAllEmployees(GetAllEmployeeRequestModel? filters)
        {
            List<EmpUser> employeeList = [];
            int totalRecords = 0;
            if (filters == null)
            {
                employeeList = (await _employeeRepository.GetAll()).ToList();
                totalRecords = employeeList.Count;
            }
            else
            {
                Expression<Func<EmpUser, bool>> whereCondition = x =>
                ((filters.DepartmentId.Count == 0) || filters.DepartmentId.Contains(x.Department)) &&
                ((filters.Gender.Count == 0) || filters.Gender.Contains(x.Gender)) &&
                (string.IsNullOrEmpty(filters.Name)
                || (x.FirstName + " " + x.LastName).Contains(filters.Name, StringComparison.CurrentCultureIgnoreCase));

                employeeList = (await _employeeRepository.GetAggregateDataAsync<EmpUser>(whereCondition, pageNo: filters.PageNo, pageSize: filters.Records)).ToList();
                totalRecords = await _employeeRepository.Count(whereCondition);
            }
            if (employeeList.Count == 0)
            {
                return new Result<GetAllEmployeeResponseModel>()
                {
                    Success = true,
                    MethodResults = [],
                    TotalRecords = totalRecords,
                };
            }
            string acceptLanguage = CurrentContext.GetLanguage(_httpContextAccessor);
            string[] depId = employeeList.Select(x => x.Department).Distinct().ToArray();
            var deptList = await _departmentRepository.GetAll(x => depId.Contains(x.DepartmentId));

            var data = (from emp in employeeList
                        join dept in deptList
                        on emp.Department equals dept.DepartmentId into empDepartmentGrp
                        from department in empDepartmentGrp.DefaultIfEmpty()
                        select new GetAllEmployeeResponseModel
                        {
                            UserId = emp.UserId,
                            FullName = $"{emp?.FirstName} {emp?.LastName ?? ""}",
                            Email = emp.Email,
                            EmployeeId = emp.EmployeeId,
                            JobRole = emp.JobRole,
                            Department = department?.Titles?.FirstOrDefault(x => x.Language == acceptLanguage)?.Label,
                            PhoneNumber = emp.PhoneNumber,
                            DateOfBirth = emp.DateOfBirth,
                            FullProfileUrl = string.IsNullOrEmpty(emp.ProfileUrl) ? Common.GetEmployeeImageUrl(null) : Common.GetEmployeeImageUrl(emp.ProfileUrl),
                        }).ToList();
            return new Result<GetAllEmployeeResponseModel>()
            {
                Success = true,
                MethodResults = data,
                TotalRecords = totalRecords
            };
        }
        public async Task<bool> IsEmailExist(string email)
        {
            bool IsEmailExist = await _employeeRepository.Exist(x => x.Email == email);
            return IsEmailExist;

        }
        public async Task<bool> IsEmpExistAndActive(string email)
        {
            return await _employeeRepository.Exist(x => x.Email == email && x.Status && x.Password != null);
        }
        public async Task<Result> GenerateTokenAndSendEmail(string email)
        {
            Result result = new();
            var emp = await _employeeRepository.FirstOrDefault(x => x.Email.Equals(email));
            if (emp is null) return result;
            var token = TokenHelper.GenerateToken();
            var hashedToken = TokenHelper.ComputeSha256Hash(token);

            PasswordResetTokens passwordResetTokens = new()
            {
                UserId = emp.UserId,
                TokenHash = hashedToken,
                IsUsed = false,
                Expiry = DateTime.UtcNow.AddMinutes(10),
            };
            var result2 = await _passwordResetTokens.AddOne(passwordResetTokens);
            if (!result2.Success)
            {
                return result;
            }
            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == emp.CompanyId);
            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.ResetPassword);
            HtmlTemplate htmlTemplate = new HtmlTemplate();
            string replacedBody = htmlTemplate.Render(emailContent?.body, new
            {
                EmployeeName = emp.FirstName + " " + emp.LastName,
                PasswordResetLink = $"{ConfigManager.AppSettings.AppUrl}auth/createpassword?token={Uri.EscapeDataString(token)}&uid={emp.UserId}",
                CompanyName = company != null ? company.CompanyName : "",
            });
            string replacedSubject = htmlTemplate.Render(emailContent.subject, new
            {
                CompanyName = company != null ? company.CompanyName : ""
            });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                _middlewareService.EmailSendAndSave(new EmpEmailLogs()
                {
                    UserTo = emp.Email,
                    Subject = replacedSubject,
                    Body = replacedBody,
                    EmailLogType = EnumsHelper.MailType.ResetPassword,
                    Email = emp.Email,
                    UserFrom = "",
                });
            }, priority: 1);

            result.Success = true;
            result.Message = "Password reset link has been sent. Please check your mail";
            return result;
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

        public async Task<Result> CreateNewPassword(CreateNewPasswordRequest model)
        {
            Result result = new();
            var tokenHash = TokenHelper.ComputeSha256Hash(model.Token);
            PasswordResetTokens? token = await _passwordResetTokens.FirstOrDefault(x => x.UserId == model.Uid && x.TokenHash == tokenHash && !x.IsUsed);
            if (token is null)
            {
                result.Message = "Invalid or expired token";
                return result;
            }
            if (token?.Expiry < DateTime.UtcNow)
            {
                result.Message = "This password reset link has expired. Please request a new one.";
                return result;
            }

            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == model.Uid);
            if (user is null)
            {
                return result;
            }
            user.Password = AuthenticationHandler.HashedPassword(model.NewPassword);
            user.IsEmailVerified = true;
            Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == user.UserId;
            await _employeeRepository.Update(whereCondition, user);
            Expression<Func<PasswordResetTokens, bool>> whereCondition2 = x => x.Id == token.Id;
            var result2 = await _passwordResetTokens.Delete(whereCondition2);
            result.Success = true;
            result.Message = "Password has been reset";
            return result;
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

        public async Task<LoginUserViewModel> GetSignedUserDetails(string userId, string roleId, string companyId)
        {
            LoginUserViewModel returnModel = new();
            UserModel? user = await GetEmployeeById(userId);
            Roles? role = await _rolesRepository.FirstOrDefault(x => x.RolesId == roleId);
            Company? companyDetails = await _companyRepository.FirstOrDefault(x => x.CompanyId == companyId);
            if (user is null)
            {
                return null;
            }
            string[] allowedModulePermission = await _roleService.GetRolePermissionOfuser(role.RolesId);
            returnModel.UserId = user.UserId;
            returnModel.FirstName = user.FirstName;
            returnModel.LastName = user.LastName;
            returnModel.CompanyId = companyDetails.CompanyId;
            returnModel.modulePermission = allowedModulePermission;
            returnModel.CompanyName = companyDetails.CompanyName;
            returnModel.DefaultLanguage = companyDetails.DefaultLanguage;
            returnModel.ApplicationLanguage = companyDetails.ApplicationLanguage;
            returnModel.ProfileImage = user.FullProfileUrl;
            returnModel.CompanyLogo = string.IsNullOrEmpty(companyDetails.CompanyLogo) ? Common.GetCompanyLogoUrl(null) : Common.GetCompanyLogoUrl(companyDetails.CompanyLogo);
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

        public async Task<Result> AddUpdateWorkHistory(EmployeeWorkHistoryModel model)
        {
            EmpWorkHistory empWorkHistory = _mapper.Map<EmpWorkHistory>(model);
            Result result = new();
            string currentUser = CurrentContext.UserId(_httpContextAccessor);
            if (string.IsNullOrEmpty(empWorkHistory.WorkHistoryId))
            {
                empWorkHistory.CreatedDate = DateTime.UtcNow;
                empWorkHistory.CreatedBy = currentUser;
                result = await _empWorkHistoryRepository.AddOne(empWorkHistory);
            }
            else
            {
                Expression<Func<EmpWorkHistory, bool>> whereCondition = x => x.UserId == model.UserId && x.WorkHistoryId == empWorkHistory.WorkHistoryId;
                EmpWorkHistory? empWorkHistoryExist = await _empWorkHistoryRepository.FirstOrDefault(whereCondition);
                if (empWorkHistoryExist is null)
                {
                    return result;
                }
                empWorkHistory.UpdatedBy = currentUser;
                empWorkHistory.UpdatedDate = DateTime.UtcNow;
                empWorkHistory.CreatedBy = empWorkHistoryExist.CreatedBy;
                empWorkHistory.CreatedDate = empWorkHistoryExist.CreatedDate;
                result = await _empWorkHistoryRepository.Update(whereCondition, empWorkHistory);
            }
            return result;
        }

        public async Task<List<EmployeeWorkHistoryModel>> GetEmpWorkHistory(string userId)
        {
            var list = (await _empWorkHistoryRepository.GetAll(x => x.UserId.Equals(userId))).OrderByDescending(x => x.EndDate);
            if (list.Any()) return _mapper.Map<List<EmployeeWorkHistoryModel>>(list);
            return [];
        }

        public async Task<Result> DeleteWorkHistory(string workId, string userId)
        {
            bool exits = await _empWorkHistoryRepository.Exist(x => x.WorkHistoryId.Equals(workId));
            if (!exits) return new Result();
            Expression<Func<EmpWorkHistory, bool>> whereCondition = x => x.WorkHistoryId == workId;
            return await _empWorkHistoryRepository.UpdateMany(whereCondition, Builders<EmpWorkHistory>.Update.Set(x => x.IsDeleted, true));
        }
        public async Task<NotificationResponseModel> GetAllNotifications(NotificationRequestDTO model, string userId)
        {
            Expression<Func<UserNotifications, bool>> wherecondition = x => x.UserId == userId;
            int userNotificationsCount = await _userNotificationRepository.Count(wherecondition);
            if (userNotificationsCount == 0)
            {
                return new NotificationResponseModel()
                {
                    NotificationList = [],
                    All = 0,
                    Unread = 0
                };
            }
            if (model.Type != "all")
            {
                wherecondition = x => x.UserId == userId && !x.IsRead;
            }
            int unReadNotificationCount = await _userNotificationRepository.Count(x => x.UserId == userId && !x.IsRead);
            List<UserNotifications> userNotifications = (await _userNotificationRepository.GetAggregateDataAsync<UserNotifications>(wherecondition, isAscending: false, orderedKey: "CreatedDateTime", pageNo: model.PageNo, pageSize: model.Records)).ToList();
            string[] notificationsId = userNotifications.Select(x => x.NotificationId).ToArray();
            IEnumerable<Notifications> notifications = await _notificationsRepository.GetAll(x => notificationsId.Contains(x.NotificationId));
            var data = (from usrNft in userNotifications
                        join ntf in notifications on usrNft.NotificationId equals ntf.NotificationId
                        select new NotificationViewModel
                        {
                            UserNotificationId = usrNft.UserNotificationId,
                            IsRead = usrNft.IsRead,
                            Title = ntf.Title,
                            Body = ntf.Body,
                            SentDateTime = usrNft.CreatedDateTime,
                            SentBy = ntf.CreatedBy,
                            NotificationTypes = ntf.NotificationType
                        }).ToList();
            return new NotificationResponseModel()
            {
                NotificationList = data,
                All = userNotificationsCount,
                Unread = unReadNotificationCount,
            };
        }

        public async Task<Result> MarkNotificationAsRead(string userId, string userNotificationId)
        {
            bool isExist = await _userNotificationRepository.Exist(x => x.UserNotificationId == userNotificationId);
            if (!isExist)
            {
                return new Result();
            }
            Expression<Func<UserNotifications, bool>> whereCondition = x => x.UserNotificationId == userNotificationId;
            return await _userNotificationRepository.UpdateMany(whereCondition, Builders<UserNotifications>.Update.Set(x => x.IsRead, true));
        }

        public async Task<Result> MarkAllNotificationAsRead(string userId)
        {
            Expression<Func<UserNotifications, bool>> whereCondition = x => x.UserId == userId && !x.IsRead;
            return await _userNotificationRepository.UpdateMany(whereCondition, Builders<UserNotifications>.Update.Set(x => x.IsRead, true));
        }
    }
}
