using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO;
using Codeji.CMS.DTO.Employee;
using Codeji.CMS.DTO.RequestModels;
using Codeji.CMS.DTO.RequestModels.EmployeeData;
using Codeji.CMS.DTO.ResponseModel;
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
using MongoDB.Driver;

namespace Codeji.CMS.Services.Employees
{
    public class EmployeeService : IEmployeeService
    {
        readonly IMongoDbRepository<EmpEducationDetails> _educationDetailsRepo;
        readonly IMongoDbRepository<EmpCertificationDetails> _certificationDetailsRepo;
        readonly IMongoDbRepository<EmpSummary> _employeeSummaryRepo;
        readonly IMapper _mapper;
        readonly IPriorityTaskQueue _priorityTaskQueue;
        readonly IMongoDbRepository<EmpUser> _employeeRepository;
        readonly IMongoDbRepository<Roles> _rolesRepository;
        readonly IRoleService _roleService;
        readonly IMongoDbRepository<EmpSkills> _employeeSkillsRepository;
        readonly IMongoDbRepository<Company> _companyRepository;
        readonly IMongoDbRepository<MailTemplate> _mailTemplateRepository;
        readonly IMiddlewareService _middlewareService;
        readonly IMongoDbRepository<Department> _departmentRepository;
        readonly IMongoDbRepository<Skills> _skillsRepository;
        readonly IMongoDbRepository<PasswordResetTokens> _passwordResetTokens;
        readonly IMongoDbRepository<EmpWorkHistory> _empWorkHistoryRepository;
        readonly IMongoDbRepository<UserNotifications> _userNotificationRepository;
        readonly IMongoDbRepository<Notifications> _notificationsRepository;
        readonly IMongoDbRepository<JobTitles> _jobTitlesRepository;
        readonly IHttpContextAccessor _httpContextAccessor;
        readonly IMongoDbRepository<CustomAttribute> _customAttributeRepository;
        readonly IMongoDbRepository<CustomAttributeValue> _customAttributeValueRepository;
        readonly IMongoDbRepository<NotificationPreference> _notificationPreferenceRepository;
        readonly INotificationService _notificationService;

        public EmployeeService(IMongoDbRepository<EmpEducationDetails> educationDetailsRepo,
            IMapper mapper, IMongoDbRepository<EmpCertificationDetails> certificationDetailsRepo,
            IMongoDbRepository<EmpSummary> userSummary,
            IMongoDbRepository<EmpUser> employeeRepository,
            IMongoDbRepository<Roles> rolesRepository,
            IRoleService roleService,
            IMongoDbRepository<EmpSkills> employeeSkillsRepository,
            IMongoDbRepository<Company> companyRepository,
            IMongoDbRepository<MailTemplate> mailTemplateRepository,
            IPriorityTaskQueue priorityTaskQueue,
            IMiddlewareService middlewareService,
            IHttpContextAccessor httpContextAccessor,
            IMongoDbRepository<Department> departmentRepository,
            IMongoDbRepository<Skills> skillsRepository,
            IMongoDbRepository<PasswordResetTokens> passwordResetTokens,
            IMongoDbRepository<EmpWorkHistory> empWorkHistoryRepository,
            IMongoDbRepository<UserNotifications> userNotificationRepository,
            IMongoDbRepository<Notifications> notificationsRepository,
            IMongoDbRepository<JobTitles> jobTitlesRepository,
            IMongoDbRepository<CustomAttribute> customAttributeRepository,
            IMongoDbRepository<CustomAttributeValue> customAttributeValueRepository,
            INotificationService notificationService,
            IMongoDbRepository<NotificationPreference> notificationPreferenceRepository
            )
        {
            _employeeRepository = employeeRepository;
            _rolesRepository = rolesRepository;
            _roleService = roleService;
            _educationDetailsRepo = educationDetailsRepo;
            _mapper = mapper;
            _certificationDetailsRepo = certificationDetailsRepo;
            _employeeSummaryRepo = userSummary;
            _employeeSkillsRepository = employeeSkillsRepository;
            _companyRepository = companyRepository;
            _mailTemplateRepository = mailTemplateRepository;
            _priorityTaskQueue = priorityTaskQueue;
            _middlewareService = middlewareService;
            _departmentRepository = departmentRepository;
            _skillsRepository = skillsRepository;
            _passwordResetTokens = passwordResetTokens;
            _httpContextAccessor = httpContextAccessor;
            _empWorkHistoryRepository = empWorkHistoryRepository;
            _userNotificationRepository = userNotificationRepository;
            _notificationsRepository = notificationsRepository;
            _jobTitlesRepository = jobTitlesRepository;
            _customAttributeRepository = customAttributeRepository;
            _customAttributeValueRepository = customAttributeValueRepository;
            _notificationService = notificationService;
            _notificationPreferenceRepository = notificationPreferenceRepository;
        }

        public async Task<Result<InviteEmployeeDto>> InviteNewEmployee(InviteEmployeeDto model, string currentUserId)
        {
            Result<InviteEmployeeDto> result = new() { Success = false };
            bool IsEmpIdExist = await _employeeRepository.Exist(e => e.EmployeeId.Equals(model.EmployeeId, StringComparison.OrdinalIgnoreCase) || e.Email == model.Email);
            if (IsEmpIdExist)
            {
                result.StatusCode = CustomStatusCode.EmployeeIdAlreadyExist;
                return result;
            }
            var userId = Guid.NewGuid().ToString();
            EmpUser employee = new EmpUser()
            {
                UserId = userId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                EmployeeId = model.EmployeeId,
                IsEmailVerified = false,
                Status = true
            };
            var res = await _employeeRepository.AddOne(employee);
            if (res.Success)
            {
                model.UserId = employee.UserId;
                result.Success = true;
                result.MethodResult = model;
            }
            // set default notification preferences for new employee
            var notificationPreferences = new NotificationPreference()
            {
                UserId = userId,
                Preferences = _middlewareService.GetDefaultNotificationPreferences(),
            };
            await _notificationPreferenceRepository.AddOne(notificationPreferences);

            UserModel currentUser = _middlewareService.GetUserById(currentUserId);
            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == currentUser.CompanyId);

            // generate password creation token for newly added employee
            string token = TokenHelper.GenerateToken();
            string tokenHash = TokenHelper.ComputeSha256Hash(token);
            int tokenExpiryTime = 24;
            PasswordResetTokens passwordResetTokens = new()
            {
                UserId = userId,
                TokenHash = tokenHash,
                IsUsed = false,
                Expiry = DateTime.UtcNow.AddHours(tokenExpiryTime),
            };
            var result2 = await _passwordResetTokens.AddOne(passwordResetTokens);

            //Acknowledgement Email Logic 
            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.CreateNewPasswordMail);
            string replacedBody = HtmlTemplate.Render(emailContent.body, new
            {
                RecipientName = employee.FirstName + " " + employee.LastName,
                PasswordCreationLink = $"{ConfigManager.AppSettings.AppUrl}auth/createpassword?token={Uri.EscapeDataString(token)}&uid={userId}",
                CompanyName = company != null ? company.CompanyName : "",
            });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                _middlewareService.EmailSendAndSave(new EmpEmailLogs()
                {
                    UserTo = employee.UserId,
                    Subject = emailContent.subject,
                    Body = replacedBody,
                    EmailLogType = EnumsHelper.MailType.CreateNewPasswordMail,
                    Email = employee.Email,
                    UserFrom = currentUser.UserId,
                });
            }, priority: 1);
            return result;
        }
        public async Task<Result> AddEmployee(UserModel user, string currentUserId)
        {
            Result result = new();
            bool IsEmpIdExist = await _employeeRepository.Exist(e => e.EmployeeId.Equals(user.EmployeeId, StringComparison.OrdinalIgnoreCase));
            if (IsEmpIdExist)
            {
                result.Success = false;
                result.StatusCode = CustomStatusCode.EmployeeIdAlreadyExist;
                return result;
            }
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
                EmploymentType = user.EmploymentType,
                DateOfBirth = user.DateOfBirth,
                Department = user.Department,
                ReportingManager = user.ReportingManager,
                PhoneNumber = user.PhoneNumber,
                BloodGroup = user.BloodGroup,
                PersonalEmail = user.PersonalEmail,
                EmergencyContact = user.EmergencyContact,
                DateOfJoining = user.DateOfJoining ?? DateTime.Now.ToString("yyyy-MM-dd"),
                Status = true,
                IsEmailVerified = false,
                Address = user.Address,
                BankAccountNumber = user.BankAccountNumber,
                PanNumber = user.PanNumber,
            };

            result = await _employeeRepository.AddOne(employee);
            if (!result.Success)
            {
                result.Success = false;
                return result;
            }
            // set default notification preferences for new employee
            var notificationPreferences = new NotificationPreference()
            {
                UserId = userId,
                Preferences = _middlewareService.GetDefaultNotificationPreferences(),
            };
            await _notificationPreferenceRepository.AddOne(notificationPreferences);

            UserModel currentUser = _middlewareService.GetUserById(currentUserId);
            Company? company = await _companyRepository.FirstOrDefault(x => x.CompanyId == currentUser.CompanyId);

            // generate password creation token for newly added employee
            string token = TokenHelper.GenerateToken();
            string tokenHash = TokenHelper.ComputeSha256Hash(token);
            int tokenExpiryTime = 24;
            PasswordResetTokens passwordResetTokens = new()
            {
                UserId = userId,
                TokenHash = tokenHash,
                IsUsed = false,
                Expiry = DateTime.UtcNow.AddHours(tokenExpiryTime),
            };
            var result2 = await _passwordResetTokens.AddOne(passwordResetTokens);

            //Acknowledgement Email Logic 
            MailTemplate? emailContent = await _mailTemplateRepository.FirstOrDefault(x => x.mailType == EnumsHelper.MailType.CreateNewPasswordMail);
            string replacedBody = HtmlTemplate.Render(emailContent.body, new
            {
                RecipientName = employee.FirstName + " " + employee.LastName,
                PasswordCreationLink = $"{ConfigManager.AppSettings.AppUrl}auth/createpassword?token={Uri.EscapeDataString(token)}&uid={userId}",
                CompanyName = company != null ? company.CompanyName : "",
            });

            _priorityTaskQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                _middlewareService.EmailSendAndSave(new EmpEmailLogs()
                {
                    UserTo = employee.UserId,
                    Subject = emailContent.subject,
                    Body = replacedBody,
                    EmailLogType = EnumsHelper.MailType.CreateNewPasswordMail,
                    Email = employee.Email,
                    UserFrom = currentUser.UserId,
                });
            }, priority: 1);
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
            };
        }
        public async Task<UserModel> GetEmployeeById(string userId)
        {
            EmpUser? user = await _employeeRepository.FirstOrDefault(x => x.UserId == userId);
            UserModel userModel = _mapper.Map<UserModel>(user);
            if (userModel.JobRole != null)
            {
                JobTitles? jobTitles = await _jobTitlesRepository.FirstOrDefault(jt => jt.JobTitleId == userModel.JobRole);
                userModel.JobRoleTitle = jobTitles?.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label);
            }
            if (userModel.ReportingManager != null)
            {
                EmpUser? reportingManager = await _employeeRepository.FirstOrDefault(x => x.UserId == user.ReportingManager);
                userModel.ReportingManagerName = reportingManager != null ? $"{reportingManager?.FirstName} {reportingManager?.LastName}" : null;
            }
            if (userModel.Department != null)
            {
                Department? department = await _departmentRepository.FirstOrDefault(x => x.DepartmentId == user.Department);
                userModel.DepartmentTitle = department?.Titles.ToDictionary(keySelector: d => d.Language, elementSelector: d => d.Label);
            }
            userModel.ProfileUrl = Common.GetEmployeeImageUrl(user.ProfileUrl);
            userModel.TotalWorkExperience = GetEmployeeTotalExperience(userId);
            if (user.CustomAttributeList.Count != 0)
            {
                List<UserCustomAttribute> customAttributeList = [];
                foreach (EmpUserCustomAttribute attribute in user.CustomAttributeList)
                {
                    var customAttribute = await _customAttributeRepository.FirstOrDefault(ca => ca.CustomAttributeId == attribute.CustomAttributeId);
                    var customAttributeValue = await _customAttributeValueRepository.FirstOrDefault(cav => cav.CustomAttributeValueId == attribute.CustomAttributeValueId && cav.CustomAttributeId == attribute.CustomAttributeId);
                    if (customAttribute != null && customAttributeValue != null)
                    {
                        customAttributeList.Add(new UserCustomAttribute()
                        {
                            CustomAttributeId = customAttribute.CustomAttributeId,
                            CustomAttributeTitle = customAttribute.CustomAttributeTitle.ToDictionary(t => t.Language, t => t.Label),
                            CustomAttributeValueId = customAttributeValue.CustomAttributeValueId,
                            CustomAttributeValueTitle = customAttributeValue.Titles.ToDictionary(t => t.Language, t => t.Label)
                        });
                    }
                }
                userModel.CustomAttributes = customAttributeList;
            }
            return userModel;
        }
        public async Task<string> GetEmployeeNameById(string employeeId)
        {
            EmpUser? empUser = await _employeeRepository.FirstOrDefault(emp => emp.UserId == employeeId);
            if (empUser == null) return string.Empty;
            return $"{empUser.FirstName} {empUser.LastName}";
        }
        public async Task<List<EmployeeSearchResponseDTO>> SearchEmployeeByName(string name)
        {
            Expression<Func<EmpUser, bool>> whereCondition = x => (x.FirstName + " " + x.LastName).Contains(name, StringComparison.CurrentCultureIgnoreCase);
            var empUsers = await _employeeRepository.GetAll(whereCondition);
            List<string> jobTitleIds = empUsers.Select(emp => emp.JobRole).Distinct().ToList();
            var jobTitleList = await _jobTitlesRepository.GetAll(jt => jobTitleIds.Contains(jt.JobTitleId));

            var result = (from emp in empUsers
                          join jobRole in jobTitleList
                          on emp.JobRole equals jobRole.JobTitleId into empJobTitleGroup
                          from jobTitle in empJobTitleGroup.DefaultIfEmpty()
                          select new EmployeeSearchResponseDTO()
                          {
                              UserId = emp.UserId,
                              FirstName = emp.FirstName,
                              LastName = emp.LastName,
                              EmpId = emp.EmployeeId,
                              JobRole = jobTitle?.Titles.ToDictionary(jt => jt.Language, jt => jt.Label),
                              FullProfileUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl)
                          }).ToList();
            return result;
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
            string[] depId = employeeList.Select(x => x.Department).Distinct().ToArray();
            var deptList = await _departmentRepository.GetAll(x => depId.Contains(x.DepartmentId));

            string[] jobRoleId = employeeList.Select(e => e.JobRole).Distinct().ToArray();
            var jobRoleList = await _jobTitlesRepository.GetAll(jt => jobRoleId.Contains(jt.JobTitleId));

            var data = (from emp in employeeList
                        join dept in deptList
                        on emp.Department equals dept.DepartmentId into empDepartmentGrp
                        from department in empDepartmentGrp.DefaultIfEmpty()
                        join jt in jobRoleList
                        on emp.JobRole equals jt.JobTitleId into empJobTitleGrp
                        from jobTitle in empJobTitleGrp.DefaultIfEmpty()
                        select new GetAllEmployeeResponseModel
                        {
                            UserId = emp.UserId,
                            FullName = $"{emp?.FirstName} {emp?.LastName ?? ""}",
                            Email = emp.Email,
                            Gender = emp.Gender,
                            EmployeeId = emp.EmployeeId,
                            JobRole = jobTitle != null ? jobTitle.Titles.ToDictionary(keySelector: jt => jt.Language, elementSelector: jt => jt.Label) : null,
                            Department = department?.Titles.ToDictionary(keySelector: d => d.Language, elementSelector: d => d.Label),
                            PhoneNumber = emp.PhoneNumber,
                            DateOfBirth = emp.DateOfBirth,
                            FullProfileUrl = Common.GetEmployeeImageUrl(emp.ProfileUrl),
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
            returnModel.RoleType = role.RoleType;
            returnModel.FirstName = user.FirstName;
            returnModel.LastName = user.LastName;
            returnModel.CompanyId = companyDetails.CompanyId;
            returnModel.modulePermission = allowedModulePermission;
            returnModel.CompanyName = companyDetails.CompanyName;
            returnModel.DefaultLanguage = companyDetails.DefaultLanguage;
            returnModel.ApplicationLanguage = companyDetails.ApplicationLanguage;
            returnModel.ProfileImage = user.ProfileUrl;
            returnModel.CompanyLogo = Common.GetCompanyLogoUrl(companyDetails.CompanyLogo);
            return returnModel;
        }
        public async Task<Result> AddEditEmployeeSummary(EmployeeSummaryRequestModel userSummary, string userId)
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
                return await _employeeSummaryRepo.AddOne(summary);
            }
            else
            {
                employeesummary.Summary = userSummary.Summary;
                return await _employeeSummaryRepo.Update(whereCondition, employeesummary);
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
                return await _employeeSkillsRepository.AddOne(newSkill);
            }
            else
            {
                employeSkills.Skills = skillsModel.Skills;
                return await _employeeSkillsRepository.Update(whereCondition, employeSkills);
            }
        }
        public async Task<Result> AddEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId)
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
            return await _educationDetailsRepo.AddOne(educationDetail);
        }

        public async Task<Result> EditEmployeeEducation(EmployeeEducationRequestModel educationDetails, string userId)
        {
            Result result = new();
            Expression<Func<EmpEducationDetails, bool>> whereCondition = x => x.UserId == userId && x.EducationId == educationDetails.EducationId;
            EmpEducationDetails? check = await _educationDetailsRepo.FirstOrDefault(whereCondition);
            if (check == null) return result;
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
            result = await _educationDetailsRepo.Update(whereCondition, data);
            return result;
        }

        public async Task<Result> AddEmployeeCertification(EmployeeCertificationRequestModel cerificationDetails, string userId)
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
            return await _certificationDetailsRepo.AddOne(certificatinDetail);
        }

        public async Task<Result> EditEmployeeCertification(EmployeeCertificationRequestModel certificationDetails, string userId)
        {
            Result result = new();
            Expression<Func<EmpCertificationDetails, bool>> whereCondition = x => x.UserId == userId && x.CertificationId == certificationDetails.CertificationId;
            EmpCertificationDetails? check = await _certificationDetailsRepo.FirstOrDefault(whereCondition);
            if (check == null) return result;
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
            result = await _certificationDetailsRepo.Update(whereCondition, data);
            return result;
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
            string fullProfileUrl = Common.GetEmployeeImageUrl(fileName) ?? "";
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
                    StatusCode = CustomStatusCode.SkillsAlreadyExist,
                    Success = false,
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

        private int GetEmployeeTotalExperience(string userId)
        {
            int totalWorkExperienceInMonths = 0;
            IEnumerable<EmpWorkHistory> empWorkHistories = _empWorkHistoryRepository.GetAll(w => w.UserId == userId).Result;
            if (!empWorkHistories.Any()) return totalWorkExperienceInMonths;

            // sort work history by start date
            var sortedList = empWorkHistories.OrderBy(wh => wh.StartDate).ToList();
            List<(DateTime Start, DateTime End)> mergedDates = new List<(DateTime, DateTime)>();
            var currentStart = sortedList[0].StartDate;
            var currentEnd = sortedList[0].EndDate ?? DateTime.UtcNow;

            foreach (var period in sortedList.Skip(1))
            {
                DateTime periodEnd = period.EndDate ?? DateTime.UtcNow;
                if (period.StartDate <= currentEnd)
                {
                    period.EndDate = period.EndDate == null ? DateTime.UtcNow : period.EndDate;
                    currentEnd = periodEnd > currentEnd ? periodEnd : currentEnd;
                }
                else
                {
                    mergedDates.Add((currentStart, currentEnd));
                    currentStart = period.StartDate;
                    currentEnd = periodEnd;
                }
            }
            mergedDates.Add((currentStart, currentEnd));

            // Calculate total months
            foreach (var range in mergedDates)
            {
                int months = ((range.End.Year - range.Start.Year) * 12) + (range.End.Month - range.Start.Month);
                if (range.End.Day >= range.Start.Day) months++;
                totalWorkExperienceInMonths += months;
            }
            return totalWorkExperienceInMonths;
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
                            TargetId = ntf.TargetId,
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

        public async Task<Result> RemoveProfileImage(string userId)
        {
            Result result = new();
            EmpUser? empUser = await _employeeRepository.FirstOrDefault(e => e.UserId == userId);
            if (empUser is null) return result;
            if (empUser.ProfileUrl != null)
            {
                string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads\\ProfileImage\\");
                string oldPath = Path.Combine(uploadFolder, empUser.ProfileUrl);
                FileInfo fileInfo = new(oldPath);
                fileInfo.Delete();
                Expression<Func<EmpUser, bool>> whereCondition = x => x.UserId == userId;
                return await _employeeRepository.UpdateMany(whereCondition, Builders<EmpUser>.Update.Set(x => x.ProfileUrl, null));
            }
            else
            {
                return result;
            }
        }

        public async Task<List<string>> GetCollegeNameSuggestions(string searchValue)
        {
            Expression<Func<EmpEducationDetails, bool>> expression = e => e.CollegeName.ToLower().Contains(searchValue.ToLower());
            List<string> collegeList = (await _educationDetailsRepo.GetAll(expression, true, false)).Select(e => e.CollegeName).Distinct().ToList();
            return collegeList;
        }

        // Get a list of employee
        public async Task SendBirthDayAndAnniversaryNotificationToEmployees()
        {
            var currentDate = DateTime.UtcNow.Date;
            var currentMonthDay = currentDate.ToString("MM-dd");

            Expression<Func<EmpUser, bool>> expression = emp =>
                !string.IsNullOrEmpty(emp.DateOfBirth)
                && emp.DateOfBirth.Substring(5, 5) == currentMonthDay;

            IEnumerable<EmpUser> birthDayEmployeeList = await _employeeRepository.GetAll(expression, withDefaultFilter: false);
            IEnumerable<EmpUser> EmployeeAnniversaryList = await _employeeRepository.GetAll(emp => !string.IsNullOrEmpty(emp.DateOfJoining) && emp.DateOfJoining.Substring(5, 5) == currentMonthDay, withDefaultFilter: false);

            var notificationTasks = new List<Task>();
            List<UserNotifications> userNotifications = [];
            //  check if there are employees having birthday today
            if (birthDayEmployeeList.Any())
            {
                foreach (EmpUser employee in birthDayEmployeeList)
                {
                    Expression<Func<EmpUser, bool>> exp = emp => emp.CompanyId == employee.CompanyId && emp.UserId != employee.UserId && emp.Status;
                    List<EmpUser> targetEmployeeList = _employeeRepository.Get(exp).Where(emp => _middlewareService.IsUserNotificationPreferenceEnabled(emp.UserId, EnumsHelper.NotificationPreferenceType.BirthdayNotification)).ToList();
                    Notifications notification = new()
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        CreatedDateTime = DateTime.UtcNow,
                        NotificationType = EnumsHelper.NotificationTypes.BirthDay,
                        Body = $"{employee.FirstName} {employee.LastName}"
                    };
                    await _notificationsRepository.AddOne(notification);
                    foreach (var emp in targetEmployeeList)
                    {
                        UserNotifications userNotification = new()
                        {
                            UserNotificationId = Guid.NewGuid().ToString(),
                            UserId = emp.UserId,
                            NotificationId = notification.NotificationId,
                            IsRead = false,
                            CreatedDateTime = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        userNotifications.Add(userNotification);
                        notificationTasks.Add(_notificationService.SendNotificationToUser(emp.UserId, new NotificationViewModel()
                        {
                            Title = notification.Title,
                            Body = notification.Body,
                            IsRead = false,
                            SentDateTime = DateTime.UtcNow,
                            TargetId = notification.TargetId,
                            NotificationTypes = notification.NotificationType,
                            UserNotificationId = userNotification.UserNotificationId
                        }));
                    }
                }
            }
            if (EmployeeAnniversaryList.Any())
            {
                foreach (EmpUser employee in EmployeeAnniversaryList)
                {
                    Expression<Func<EmpUser, bool>> exp = emp => emp.CompanyId == employee.CompanyId && emp.UserId != employee.UserId && emp.Status;
                    List<EmpUser> targetEmployeeList = _employeeRepository.Get(exp).Where(emp => _middlewareService.IsUserNotificationPreferenceEnabled(emp.UserId, EnumsHelper.NotificationPreferenceType.WorkAnniversaries)).ToList();
                    Notifications notification = new()
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        CreatedDateTime = DateTime.UtcNow,
                        NotificationType = EnumsHelper.NotificationTypes.WorkAnniversary,
                        Body = $"{employee.FirstName} {employee.LastName}"
                    };
                    await _notificationsRepository.AddOne(notification);
                    foreach (var emp in targetEmployeeList)
                    {
                        UserNotifications userNotification = new()
                        {
                            UserNotificationId = Guid.NewGuid().ToString(),
                            UserId = emp.UserId,
                            NotificationId = notification.NotificationId,
                            IsRead = false,
                            CreatedDateTime = DateTime.UtcNow,
                            IsDeleted = false
                        };
                        userNotifications.Add(userNotification);
                        notificationTasks.Add(_notificationService.SendNotificationToUser(emp.UserId, new NotificationViewModel()
                        {
                            Title = notification.Title,
                            Body = notification.Body,
                            IsRead = false,
                            SentDateTime = DateTime.UtcNow,
                            TargetId = notification.TargetId,
                            NotificationTypes = notification.NotificationType,
                            UserNotificationId = userNotification.UserNotificationId
                        }));
                    }
                }
            }

            if (userNotifications.Count > 0)
            {
                // insert all user notification into db
                // process all notificatios task
                await Task.WhenAll(notificationTasks);
                await _userNotificationRepository.AddMany(userNotifications);
            }
        }

        // notification preference service methods
        public async Task<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> GetNotificationPreferences(string userId)
        {
            Dictionary<EnumsHelper.NotificationPreferenceType, bool> result = new();
            var data = await _notificationPreferenceRepository.FirstOrDefault(n => n.UserId == userId);
            if (data == null)
            {
                result = _middlewareService.GetDefaultNotificationPreferences();
            }
            else
            {
                result = data.Preferences;
            }
            return result;
        }

        public async Task<Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>>> UpdateNotificationPreferences(string userId, Dictionary<EnumsHelper.NotificationPreferenceType, bool> preferences)
        {
            Result<Dictionary<EnumsHelper.NotificationPreferenceType, bool>> result = new();
            Expression<Func<NotificationPreference, bool>> whereCondition = n => n.UserId == userId;
            var existingPreferences = await _notificationPreferenceRepository.FirstOrDefault(whereCondition);
            if (existingPreferences == null)
            {
                NotificationPreference newPreference = new()
                {
                    UserId = userId,
                    Preferences = preferences,
                };
                await _notificationPreferenceRepository.AddOne(newPreference);
            }
            await _notificationPreferenceRepository.UpdateMany(whereCondition, Builders<NotificationPreference>.Update.Set(n => n.Preferences, preferences));
            result.MethodResult = await GetNotificationPreferences(userId);
            return result;
        }
    }
}
