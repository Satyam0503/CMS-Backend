using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Employees.Interface;

/// <summary>Private storage for offboarding evidence. Implementations must never return public URLs.</summary>
public interface IEmployeeExitEvidenceStorage
{
    Task<(string StorageKey, string Sha256)> SaveAsync(string companyId, string exitId, IFormFile file, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType, string FileName)?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
