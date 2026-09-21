namespace Codeji.CMS.Services.Recruitments.Interface;

public interface IRecruitmentEmailOutboxProcessor
{
    Task ProcessNext(CancellationToken cancellationToken);
}
