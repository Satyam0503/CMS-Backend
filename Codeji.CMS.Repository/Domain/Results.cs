namespace Codeji.CMS.Domain.Models
{
    // public class Result
    // {
    //     public bool Success { get; set; }
    //     public string Message { get; set; }
    //     public int ErrorCode { get; set; }
    //     public object Data { get; set; }
    //     public Result()
    //     {
    //         Success = false;
    //         Message = "";
    //         ErrorCode = 500;
    //     }
    // }
    public class GetOneResult<TEntity> : Result where TEntity : class, new()
    {
        public TEntity Entity { get; set; }
    }
    public class GetManyResult<TEntity> : Result where TEntity : class, new()
    {
        public IEnumerable<TEntity> Entities { get; set; }
    }
    public class GetListResult<T> : Result
    {
        public List<T> Entities { get; set; }
    }
}
