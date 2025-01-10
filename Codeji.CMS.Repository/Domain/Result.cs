using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Domain.Models
{
    public class Result<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }
        public int TotalRecords { get; set; }
        public Result()
        {
            Success = false;
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
        public Result()
        {
            Success = false;
            Message = "";
            StatusCode = StatusCodes.Status200OK;
        }
    }

}