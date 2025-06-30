using System.Linq.Expressions;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Leave;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.Leave;
using Codeji.CMS.Utility.middlewares;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.LeaveManagement.LeaveTypes;

public class LeaveManagementService : ILeaveManagementService
{
    private readonly IMongoDbRepository<LeaveType> _leaveTypeRepo;
    private readonly IMongoDbRepository<EmpUser> _empUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public LeaveManagementService(IMongoDbRepository<LeaveType> leaveTypeRepo, IMongoDbRepository<EmpUser> empUser, IHttpContextAccessor httpContextAccessor)
    {
        _leaveTypeRepo = leaveTypeRepo;
        _httpContextAccessor = httpContextAccessor;
        _empUser = empUser;
    }

    public async Task<Result> CreateUpdateLeaveType(LeaveTypeResponseDto leaveTypeResponseDto)
    {
        Result result = new();
        // var userId = CurrentContext.UserId(_httpContextAccessor);
         Expression<Func<LeaveType, bool>> leaveTypeNameCondition = l => l.LeaveTypeName.Equals(leaveTypeResponseDto.LeaveTypeName,StringComparison.CurrentCultureIgnoreCase);
        if (string.IsNullOrEmpty(leaveTypeResponseDto.LeaveTypeId))
        {
            // Expression<Func<LeaveType,bool>> 
            var existingLeaveType = await _leaveTypeRepo.FirstOrDefault(leaveTypeNameCondition,true);
            if (existingLeaveType == null)
            {
                var leaveTypeDomain = new LeaveType
                {
                    LeaveTypeName = leaveTypeResponseDto.LeaveTypeName,
                    MaxLeaveLength = leaveTypeResponseDto.MaxLeaveLength,
                    LeaveTypes = leaveTypeResponseDto.LeaveTypes,
                    // CreatedBy = userId,
                    CreatedDate = DateTime.UtcNow
                };
                result = await _leaveTypeRepo.AddOne(leaveTypeDomain);

            }
            else if (existingLeaveType != null && existingLeaveType.IsDeleted == true)
            {
                existingLeaveType.IsDeleted = false;
                existingLeaveType.MaxLeaveLength = leaveTypeResponseDto.MaxLeaveLength;
                existingLeaveType.LeaveTypes = leaveTypeResponseDto.LeaveTypes;
                await _leaveTypeRepo.Update(leaveTypeNameCondition, existingLeaveType);
                result.Success = true;
            }
            return result;
        }
        else
        {
           
            var existingLeaveTypeName = await _leaveTypeRepo.FirstOrDefault(leaveTypeNameCondition,true);
            Expression<Func<LeaveType, bool>> whereCondition = l => l.LeaveTypeId == leaveTypeResponseDto.LeaveTypeId;
            var existingLeaveType = await _leaveTypeRepo.FirstOrDefault(whereCondition);
            if (existingLeaveType == null)
            {
                result.Success = false;
                return result;
            }
            if (existingLeaveTypeName != null && existingLeaveTypeName.LeaveTypeName == leaveTypeResponseDto.LeaveTypeName && existingLeaveTypeName.LeaveTypeId != leaveTypeResponseDto.LeaveTypeId)
            {
                if (existingLeaveTypeName.IsDeleted == true)
                {
                    // state change of already exist leave type
                    existingLeaveTypeName.IsDeleted = false;
                    existingLeaveTypeName.LeaveTypes = leaveTypeResponseDto.LeaveTypes;
                    existingLeaveTypeName.MaxLeaveLength = leaveTypeResponseDto.MaxLeaveLength;
                    await _leaveTypeRepo.Update(leaveTypeNameCondition, existingLeaveTypeName);
                    //state change of selected leave type
                    existingLeaveType.IsDeleted = true;
                    result.Success = true;
                    await _leaveTypeRepo.Update(whereCondition, existingLeaveType);
                    return result;
                }
                result.Success = false;
                result.Message = "Leave Type Already Exists";
                return result;
            }
            existingLeaveType.LeaveTypeName = leaveTypeResponseDto.LeaveTypeName;
            existingLeaveType.LeaveTypes = leaveTypeResponseDto.LeaveTypes;
            existingLeaveType.MaxLeaveLength = leaveTypeResponseDto.MaxLeaveLength;
            result = await _leaveTypeRepo.Update(whereCondition, existingLeaveType);

        }
        return result;
    }

    public async Task<Result<LeaveTypeRequestDto>> GetLeaveType()
    {
        Expression<Func<LeaveType, bool>> whereCondition = l => !l.IsDeleted;
        IEnumerable<LeaveType> leaveTypeList = [];
        leaveTypeList = await _leaveTypeRepo.GetAll(whereCondition);

        List<string> usersId = leaveTypeList.Select(l => l.CreatedBy).ToList();
        List<EmpUser> users = (await _empUser.GetAll(u => usersId.Contains(u.UserId))).ToList();

        List<LeaveTypeRequestDto> leaveTypeData = (from leaveType in leaveTypeList
                                                   join user in users on leaveType.CreatedBy equals user.UserId
                                                   select new LeaveTypeRequestDto
                                                   {
                                                       LeaveTypeId = leaveType.LeaveTypeId,
                                                       LeaveTypeName = leaveType.LeaveTypeName,
                                                       MaxLeaveLength = leaveType.MaxLeaveLength,
                                                       LeaveTypes = leaveType.LeaveTypes,
                                                       CreatedBy = user.FirstName + " " + user.LastName,
                                                       CreatedOn = leaveType.CreatedDate
                                                   }
                                                    ).OrderByDescending(d => d.CreatedOn).ToList();
        return new Result<LeaveTypeRequestDto>
        {
            Success = true,
            MethodResults = leaveTypeData
        };
    }

    public async Task<Result> DeleteLeaveType(string leaveTypeId)
    {
        Result result = new();
        Expression<Func<LeaveType, bool>> whereCondition = l => l.LeaveTypeId == leaveTypeId;
        var deletedLeaveType = await _leaveTypeRepo.FirstOrDefault(whereCondition);
        deletedLeaveType.IsDeleted = true;
        result = await _leaveTypeRepo.Update(whereCondition,deletedLeaveType);
        return result;
    }
}
