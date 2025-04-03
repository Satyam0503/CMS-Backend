

using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;

namespace Codeji.CMS.Services.Companies;

public class CompanyMasterService
{
  private readonly IMongoDbRepository<Department> _departmentRepository;
  public CompanyMasterService(IMongoDbRepository<Department> departmentRepository)
  {
    _departmentRepository = departmentRepository;
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

