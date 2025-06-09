using System.Linq.Expressions;
using AngleSharp.Text;
using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.NoticeBoard;
using Codeji.CMS.GenericRepository.Interfaces;
using Codeji.CMS.Repository.Entities.Employees;
using Codeji.CMS.Repository.Entities.NoticeBoard;
using Codeji.CMS.Utility;

namespace Codeji.CMS.Services.NoticeBoard;

public class NoticeBoardServices : INoticeBoardService
{
    private readonly IMongoDbRepository<Notice> _notice;
    private readonly IMongoDbRepository<EmpUser> _empUser;
    public NoticeBoardServices(IMongoDbRepository<Notice> notice, IMongoDbRepository<EmpUser> empUser)
    {
        _notice = notice;
        _empUser = empUser;
    }

    public async Task<Result> PostNotice(AddNoticeRequestModel model)
    {
        Notice notice = new Notice()
        {
            Title = model.Title,
            Message = model.Message,
            Target = model.Target,
            NoticeType = model.NoticeType,
            Departments = model.Departments,
        };
        return await _notice.AddOne(notice);
    }

    public async Task<Result<NoticeViewModel>> GetAllNotices(string userId)
    {
        EmpUser? employee = await _empUser.FirstOrDefault(x => x.UserId == userId);
        Expression<Func<Notice, bool>> whereCondition = x => (x.Departments.Equals("all") || x.Departments.Equals(employee.Department))
        && (x.Target.Equals("all") || x.Target.Equals(employee.RoleId));
        IEnumerable<Notice> noticeList = await _notice.GetAll(whereCondition);
        List<string> empIdList = noticeList.Select(x => x.CreatedBy).Distinct().ToList();
        IEnumerable<EmpUser> empUsers = await _empUser.GetAll(x => empIdList.Contains(x.UserId));
        var data = (from notice in noticeList
                    join emp in empUsers on notice.CreatedBy equals emp.UserId
                    select new NoticeViewModel
                    {
                        UserName = $"{emp.FirstName} {emp.LastName}",
                        UserDesignation = emp.JobRole,
                        NoticeMessage = notice.Message,
                        NoticeTitle = notice.Title,
                        CreatedDateTime = notice.CreatedDate,
                        NoticeType = notice.NoticeType,
                        UserProfile = string.IsNullOrEmpty(emp.ProfileUrl) ? Common.GetEmployeeImageUrl(null) : Common.GetEmployeeImageUrl(emp.ProfileUrl),
                    }).OrderByDescending(x => x.CreatedDateTime).ToList();

        return new Result<NoticeViewModel>()
        {
            Success = true,
            MethodResults = data,
            TotalRecords = data.Count
        };
    }
}