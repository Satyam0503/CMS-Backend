namespace Codeji.CMS.Services.Exceptions;

public sealed class SalaryEffectiveDateConflictException : InvalidOperationException
{
    public SalaryEffectiveDateConflictException()
        : base("A salary structure already exists for this employee on the selected effective date.")
    {
    }
}
