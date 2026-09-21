using Codeji.CMS.Domain.Models;
using Codeji.CMS.DTO.Recruitments;
namespace Codeji.CMS.Services.Recruitments.Interface;
public interface IRecruitmentTalentPoolService { Task<Result<string>> Upsert(RecruitmentTalentPoolCommand command, string actorUserId); }
