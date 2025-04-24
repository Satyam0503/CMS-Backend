

using System.Linq.Expressions;
using AutoMapper;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Company;
using Codeji.CMS.DTO.RequestModels.Company;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Company;
using Codeji.CMS.Repository.Entities.Recruitments;
using Codeji.CMS.Services.Interface;
using Microsoft.AspNetCore.Http;

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
    public async Task<Result> AddEditDepartment(DepartmentDTO model)
    {
        Result result = new();
        Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == model.DepartmentId;
        Department dep = _departmentRepository.FirstOrDefault(whereCondition)?.Result ?? new();
        if (string.IsNullOrEmpty(dep.DepartmentId))
        {
            dep.IsActive = model.IsActive;
            dep.Titles = model.Titles;

            result = await _departmentRepository.AddOne(dep);
            if (result.Success) result.Message = "Department Added Successfully";
        }
        else
        {
            dep.IsActive = model.IsActive;
            dep.Titles = model.Titles;
            result = await _departmentRepository.Update(whereCondition, dep);
            if (result.Success) result.Message = "Department Edited Successfully";
        }
        return result;
    }

    public async Task<Result<DepartmentDTO>> GetDepartmentList()
    {
        IEnumerable<Department> departments = await _departmentRepository.GetAll(x => x.IsDeleted == false);
        if (departments == null || !departments.Any())
        {
            return new Result<DepartmentDTO>()
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = "No Department Found"
            };
        }

        List<DepartmentDTO> data = _mapper.Map<List<DepartmentDTO>>(departments);
        return new Result<DepartmentDTO>()
        {
            MethodResults = data.ToList(),
            TotalRecords = data.Count,
            Success = true,
            StatusCode = 200,
            Message = "List Of Departments",
        };
    }

    public async Task<bool> DeleteDepartment(string departmentId)
    {
        Expression<Func<Department, bool>> whereCondition = x => x.DepartmentId == departmentId;
        Department? department = await _departmentRepository.FirstOrDefault(whereCondition);
        if(department is null){
            return false;
        }
        department.IsDeleted = true;
        await _departmentRepository.Update(whereCondition, department);
        return true;
    }
}

