using Codeji.CMS.Repository.Entities.Recruitments;

namespace Codeji.CMS.Services.CareerPortal.Interface;

public interface ICareerNotificationService
{
    Task HandleJobSaved(JobVacancy? previous, JobVacancy current);
    Task ProcessNext(CancellationToken cancellationToken);
}
