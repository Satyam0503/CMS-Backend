using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;

namespace Codeji.CMS.Services.PayRoll.Interface;

public interface IPayRollServices
{
    Task<Result<GetEmpPayRollResponseDto>> GetEmployeePayRoll(GetEmpPayRollRequestDto payload, string companyId);
    Task<(byte[] pdfBytes, string pdfName)> GenerateEmpSalarySlip(PayslipRequestDto model, string userId);
    Task<Result> UploadPayrollData(EmplyeePayRollRequestDto model, string companyId);
}
