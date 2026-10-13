using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.PayRoll;

namespace Codeji.CMS.Services.PayRoll.Interface;

public interface IPayRollServices
{
    Task<Result<GetEmpPayRollResponseDto>> GetEmployeePayRoll(GetEmpPayRollRequestDto payload, string companyId, string? viewerUserId = null, bool processedOnly = false);
    Task<(byte[] pdfBytes, string pdfName)> GenerateEmpSalarySlip(PayslipRequestDto model, string userId, string companyId);
    Task<(byte[] pdfBytes, string pdfName)> GenerateEmployeeSalarySlip(EmployeePayslipRequestDto model, string companyId);
    Task<Result<string>> UploadPayrollData(EmplyeePayRollRequestDto model, string companyId);
    Task<Result<string>> ValidatePayrollData(PayrollValidationRequestDto model, string companyId);
    Task<Result> AddUpdatePayRoll(AddUpdatePayRollRequestDto model, string companyId);
    Task<Result> ProcessPayrollMonth(DateTime payMonth, string companyId, string processedBy, IReadOnlyCollection<string>? employeeIds = null);
    Task<Result> ApprovePayrollMonth(DateTime payMonth, string companyId, string approvedBy);
    Task<Result> RecordPayrollPayment(RecordPayrollPaymentRequestDto model, string companyId, string recordedBy);
    Task<Result> ClosePayrollMonth(DateTime payMonth, string companyId, string closedBy);
    Task<Result<List<PayrollAuditEventDto>>> GetPayrollAuditHistory(string payrollId, string companyId);
}

