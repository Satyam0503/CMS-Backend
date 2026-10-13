using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.CareerPortal;
using Codeji.CMS.Repository.Entities.CareerPortal;

namespace Codeji.CMS.Services.CareerPortal.Interface;

public interface ICareerSubscriptionService
{
    Task<Result> Subscribe(CareerSubscribeRequest request);
    Task<Result<CareerSessionResponse>> Verify(string token);
    Task<Result> Resend(string email);
    Task<Result> Unsubscribe(string token);
    Task<Result<CareerPreferencesDto>> GetPreferences(string sessionToken);
    Task<Result<CareerPreferencesDto>> UpdatePreferences(string sessionToken, UpdateCareerPreferencesRequest request);
    Task<CareerSubscriber?> Authorize(string sessionToken);
}
