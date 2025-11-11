using static Codeji.CMS.Utility.Enums.EnumsHelper;

namespace Codeji.CMS.DTO.Leave.LeaveRequest
{
    public class LeaveRequestDto
    {
        public string? UserId { get; set; }
        public required string LeavePolicyId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsHalfDay { get; set; }
        public required string Reason { get; set; }
    }

    public class UpdateLeaveRequestDto
    {
        public required string LeaveRequestId { get; set; }
        public string? UserId { get; set; }
        public required string LeavePolicyId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsHalfDay { get; set; }
        public required string Reason { get; set; }
    }

    public class LeaveRequestUpdateDto
    {
        public string Comment { get; set; }
        public LeaveRequestStatus Status { get; set; }
    }
}