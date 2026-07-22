using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using MongoDB.Driver;

public interface IPayrollDivisorPolicyService
{
    Task<PayrollDivisorPolicyDto> GetEffective(string companyId, DateTime month);
    Task<Result> Save(string companyId, string userId, PayrollDivisorPolicyDto dto);
}

public sealed class PayrollDivisorPolicyService(IMongoDbRepository<PayrollDivisorPolicy> repository) : IPayrollDivisorPolicyService
{
    public async Task<PayrollDivisorPolicyDto> GetEffective(string companyId, DateTime month)
    {
        var date = new DateTime(month.Year, month.Month, 1);
        var item = (await repository.GetAll(x => x.CompanyId == companyId && x.IsActive && x.EffectiveFrom <= date && (!x.EffectiveTo.HasValue || x.EffectiveTo >= date))).OrderByDescending(x => x.Version).FirstOrDefault();
        return item == null
            ? new PayrollDivisorPolicyDto { DivisorPolicy="CALENDAR_DAYS", EffectiveFrom=date, IsActive=true, Version=0 }
            : new PayrollDivisorPolicyDto { Id=item.Id, DivisorPolicy=item.DivisorPolicy, EffectiveFrom=item.EffectiveFrom, EffectiveTo=item.EffectiveTo, IsActive=item.IsActive, Version=item.Version };
    }

    public async Task<Result> Save(string companyId, string userId, PayrollDivisorPolicyDto dto)
    {
        var allowed = new[] { "CALENDAR_DAYS", "WORKING_DAYS", "FIXED_30_DAYS" };
        if (!allowed.Contains(dto.DivisorPolicy) || (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date))
            return new Result { Success=false, Message="Invalid payroll divisor policy or effective dates." };
        var versions=(await repository.GetAll(x=>x.CompanyId==companyId)).OrderByDescending(x=>x.Version).ToList();
        var newStart=dto.EffectiveFrom.Date;var newEnd=dto.EffectiveTo?.Date??DateTime.MaxValue.Date;
        if(versions.Any(x=>x.EffectiveFrom.Date==newStart||
            (x.EffectiveTo.HasValue&&x.EffectiveFrom.Date<=newEnd&&x.EffectiveTo.Value.Date>=newStart)||
            (!x.EffectiveTo.HasValue&&x.EffectiveFrom.Date>newStart)))
            return new Result{Success=false,Message="The payroll divisor policy overlaps an existing version."};
        var open=versions.FirstOrDefault(x=>!x.EffectiveTo.HasValue&&x.EffectiveFrom<dto.EffectiveFrom.Date);
        if(open!=null){open.EffectiveTo=dto.EffectiveFrom.Date.AddDays(-1);open.UpdatedBy=userId;open.UpdatedDate=DateTime.UtcNow;await repository.Update(Builders<PayrollDivisorPolicy>.Filter.Eq(x=>x.Id,open.Id),open);}
        return await repository.AddOne(new PayrollDivisorPolicy { CompanyId=companyId,DivisorPolicy=dto.DivisorPolicy,EffectiveFrom=dto.EffectiveFrom.Date,EffectiveTo=dto.EffectiveTo?.Date,IsActive=dto.IsActive,Version=(versions.FirstOrDefault()?.Version??0)+1,CreatedBy=userId });
    }
}
