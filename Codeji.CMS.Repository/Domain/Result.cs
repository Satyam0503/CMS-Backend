using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Domain.Models
{
    public sealed class ValidationError
    {
        public string? JobTitleId { get; set; }
        public string? JobTitleName { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
    public class Result<T>
    {
        public string UserId;

        public bool Success { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public int TotalRecords { get; set; }
        public T Data { get; set; }
        public Result()
        {
            Success = true;
            Message = "";
            StatusCode = StatusCodes.Status200OK;
            MethodResults = new List<T>();
            TotalRecords = 0;
        }
        public T MethodResult { get; set; }
        public List<T> MethodResults { get; set; }
    }
    public class Result
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        // Optional, structured validation information for clients that can show
        // a problem next to the field which caused it.
        public List<ValidationError> Errors { get; set; } = [];
        public Result()
        {
            Success = false;
            Message = "";
            StatusCode = StatusCodes.Status200OK;
        }
    }

}
