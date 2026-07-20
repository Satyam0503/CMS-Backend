namespace Codeji.CMS.DTO.PayRoll;

public class SalarySlipTemplateModel
{
    public string CompanyName { get; set; }
    public string CompanyAddress { get; set; }
    public string CompanyLogo { get; set; }
    public string EmployeeName { get; set; }
    public string Designation { get; set; }
    public string PanNumber { get; set; }
    public string BankAccountNo { get; set; }
    public string EmployeeType { get; set; }
    public string PaySlipMonth { get; set; }
    public string EmployeeId { get; set; }
    public string PaidDate { get; set; }
    public float PaidDays { get; set; } = 0;
    public float LossofPayDays { get; set; } = 0;
    public decimal BasicSalary { get; set; } = 0;
    public decimal HRA { get; set; } = 0;
    public decimal LtaAllowance { get; set; } = 0;
    public decimal OtherAllowance { get; set; } = 0;
    public decimal Bonus { get; set; } = 0;
    public decimal LossOfPays { get; set; } = 0;
    public decimal IncomeTax { get; set; } = 0;
    public decimal HealthInsurance { get; set; } = 0;
    public decimal EPF { get; set; } = 0;
    public decimal ESIC { get; set; } = 0;
    public decimal GrossPay { get; set; } = 0;
    public decimal TotalDeduction { get; set; } = 0;
    public decimal NetSalary { get; set; } = 0;
    public string NetSalaryInWords { get; set; }
}