using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;

namespace Codeji.CMS.Services.PayRoll.Interface;

public interface IPayRollServices
{
    Task<Result<GetEmpPayRollResponseDto>> GetEmployeePayRoll(GetEmpPayRollRequestDto payload, string companyId);
    Task<(byte[] pdfBytes, string pdfName)> GenerateEmpSalarySlip(PayslipRequestDto model, string userId);
    Task<(byte[] pdfBytes, string pdfName)> GenerateEmployeeSalarySlip(EmployeePayslipRequestDto model, string companyId);
    Task<Result<string>> UploadPayrollData(EmplyeePayRollRequestDto model, string companyId);
    Task<Result> AddUpdatePayRoll(AddUpdatePayRollRequestDto model, string companyId);
}
