public sealed class AttendanceInitializationRequestDto
{
    public DateTime Month { get; set; }
}

public sealed class AttendanceInitializationResultDto
{
    public int EmployeesProcessed { get; set; }
    public int EligibleWorkingDays { get; set; }
    public int Created { get; set; }
    public int AlreadyExisting { get; set; }
    public int EmploymentSkipped { get; set; }
    public int InvalidSkipped { get; set; }
    public List<string> Failures { get; set; } = [];
}
