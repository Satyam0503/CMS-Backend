

using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Services.Interface;

namespace Codeji.CMS.Services.Companies;

public class CompanyMasterService: ICompanyMasterService
{
  private readonly IMongoDbRepository<Department> _departmentRepository;
   readonly IMapper _mapper;
  public CompanyMasterService(IMongoDbRepository<Department> departmentRepository, IMapper mapper)
  {
    _departmentRepository = departmentRepository;
    _mapper= mapper;
  }
  public async Task<Result> AddDepartment(DepartmentRequestModel model)
  {
    Result result = new();
    Department department = new()
    {
      DepartmentName = model.DepartmentName,
      DepartmentDescription = model.DepartmentDescription,
    };
    await _departmentRepository.AddOne(department);
    result.Success = true;
    result.Message = "Department Added Successfully";
    return result;
  }

  public async Task<Result<DepartmentViewModel>> GetDepartmentList()
  {
    Result<DepartmentViewModel> result = new ();
    IEnumerable<Department> departments = await _departmentRepository.GetAll();
    List<DepartmentViewModel> data = _mapper.Map<List<DepartmentViewModel>>(departments);

    if (departments == null || !departments.Any())
    {
      result.Success = false;
      result.Message = "No Department Found";
      result.StatusCode = 404;
    }
    result.MethodResults = data.ToList();
    result.TotalRecords = data.Count;
    result.Success = true;
    result.StatusCode = 200;
    result.Message = "List Of Departments";
    return result;
  }
}

