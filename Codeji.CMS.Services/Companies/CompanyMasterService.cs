

using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Interface;

namespace Codeji.CMS.Services.Companies;

public class CompanyMasterService : ICompanyMasterService
{
    private readonly IMongoDbRepository<Department> _departmentRepository;
    readonly IMapper _mapper;
    public CompanyMasterService(IMongoDbRepository<Department> departmentRepository, IMapper mapper)
    {
        _departmentRepository = departmentRepository;
        _mapper = mapper;
    }
    public async Task<Result> AddEditDepartment(DepartmentRequestModel model)
    {
        Result result = new();
        Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == model.DepartmentId;
        Department dep = _departmentRepository.FirstOrDefault(whereCondition)?.Result ?? new();
        if (string.IsNullOrEmpty(dep.DepartmentId))
        {
            dep.DepartmentName = model.DepartmentName;
            dep.DepartmentDescription = model.DepartmentDescription;

            result = await _departmentRepository.AddOne(dep);
            if (result.Success) result.Message = "Department Added Successfully";
        }
        else
        {
            dep.DepartmentName = model.DepartmentName;
            dep.DepartmentDescription = model.DepartmentDescription;

            result = await _departmentRepository.Update(whereCondition, dep);
            if (result.Success) result.Message = "Department Edited Successfully";
        }
        return result;
    }

    public async Task<Result<DepartmentViewModel>> GetDepartmentList()
    {
        Result<DepartmentViewModel> result = new();
        IEnumerable<Department> departments = await _departmentRepository.GetAll(x => x.IsDeleted == false);
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

