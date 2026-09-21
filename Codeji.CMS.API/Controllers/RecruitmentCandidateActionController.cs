using Codeji.CMS.DTO.Recruitments;
using Codeji.CMS.Services.Recruitments.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Codeji.CMS.API.Controllers;

[ApiController]
[Route("api/recruitment/candidate-actions")]
[AllowAnonymous]
public sealed class RecruitmentCandidateActionController(IRecruitmentCandidateActionService service) : ControllerBase
{
    [HttpPost("offer/respond")]
    public async Task<IActionResult> RespondToOffer(RecruitmentCandidateOfferActionCommand command) => ToHttp(await service.RespondToOffer(command));

    [HttpPost("interview/confirm")]
    public async Task<IActionResult> ConfirmInterview(RecruitmentCandidateInterviewActionCommand command) => ToHttp(await service.ConfirmInterview(command));

    private IActionResult ToHttp<T>(Codeji.CMS.Domain.Models.Result<T> result) => StatusCode(result.StatusCode == 0 ? 200 : result.StatusCode, result);
}
