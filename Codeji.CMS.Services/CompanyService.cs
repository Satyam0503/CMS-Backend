using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.RolePermissions;
using Codeji.CMS.Services.Employees.Interface;
using Codeji.CMS.Services.Interface;
using Codeji.CMS.Utility.Helpers;

namespace Codeji.CMS.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IMongoDbRepository<Company> _companyRepo;
        private readonly IMongoDbRepository<EmpUser> _userRepo;
        private readonly IMongoDbRepository<Roles> _companyRoleRepo;
        private readonly IMongoDbRepository<ModulePermission> _modulePermissisonRepo;
        private readonly IMongoDbRepository<RolePermission> _rolePermissionRepo;
        private readonly IMapper _mapper;
        private readonly IRoleService _roleService;
        private readonly IMongoDbRepository<Department> _departmentRepository;

        public CompanyService(
            IMongoDbRepository<Company> companyRepo,
            IMapper mapper,
            IMongoDbRepository<EmpUser> userRepo,
            IMongoDbRepository<Roles> companyRoleRepo,
            IMongoDbRepository<ModulePermission> modulePermissisonRepo,
            IMongoDbRepository<RolePermission> rolePermissionRepo,
            IRoleService roleService,
            IMongoDbRepository<Department> departmentRepository

            )
        {
            _roleService = roleService;
            _companyRepo = companyRepo;
            _userRepo = userRepo;
            _companyRoleRepo = companyRoleRepo;
            _modulePermissisonRepo = modulePermissisonRepo;
            _rolePermissionRepo = rolePermissionRepo;
            _mapper = mapper;
            _departmentRepository = departmentRepository;

        }



        public async Task<Result> Register(CompanyRequestModel companyModel)
        {
            Result result = new Result();
            string companyId = Guid.NewGuid().ToString();
            //Add Default Role
            List<Roles> adminRole = await _roleService.AddDefaultRole(companyId);

            EmpUser user = new EmpUser()
            {
                UserId = Guid.NewGuid().ToString(),
                Email = companyModel.Email,
                FirstName = companyModel.FirstName,
                LastName = companyModel.LastName,
                CompanyId = companyId,
                Password = AuthenticationHandler.HashedPassword(companyModel.Password),
                RoleId = adminRole.FirstOrDefault(x => x.RoleType == 1)?.RolesId ?? "",
            };
            //Company Creation and Addition in DB
            Company company = new Company()
            {
                CompanyId = companyId,
                PrimaryContact = user.UserId,
                CompanyName = companyModel.CompanyName,
                Status = true,
            };

            await _companyRepo.AddOne(company);

            Result addedUser = await _userRepo.AddOne(user);

            result.Success = true;
            return result;
        }
        public async Task<List<Company>> GetAllCompanyList()
        {
            IEnumerable<Company> list = await _companyRepo.GetAll();
            return _mapper.Map<List<Company>>(list);
        }

        public async Task<bool> IsActiveCompanyExist(string companyId)
        {
            bool exist = await _companyRepo.Exist(x => x.CompanyId == companyId && x.Status);
            return exist;
        }

        public async Task<Result> AddDepartment(DepartmentRequestModel model)
        {
            Result result = new Result();
            Department department = new Department()
            {
                DepartmentName = model.DepartmentName,
                DepartmentDescription = model.DepartmentDescription,
            };
            await _departmentRepository.AddOne(department);
            result.Success = true;
            result.Message = "Department Added Successfully";
            return result;
        }

        public async Task<Result<Department>> GetDepartmentList()
        {
            Result<Department> result = new Result<Department>();
            IEnumerable<Department> departments = await _departmentRepository.GetAll();
            if (departments == null || !departments.Any())
            {
                result.Success = false;
                result.Message = "No Department Found";
                result.StatusCode = 404;
                return result;
            }
            result.MethodResults = departments.ToList();
            result.TotalRecords = departments.Count();
            result.Success = true;
            result.StatusCode = 200;
            result.Message = "List Of Departments";
            return result;
        }
    }
}
