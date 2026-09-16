using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Employees.Interface;

/// <summary>Private storage for offboarding evidence. Implementations must never return public URLs.</summary>
public interface IEmployeeExitEvidenceStorage
{
    Task<(string StorageKey, string Sha256)> SaveAsync(string companyId, string exitId, IFormFile file, CancellationToken cancellationToken = default);
    /// <summary>Stores a server-generated document using the same private evidence boundary as uploaded files.</summary>
    Task<(string StorageKey, string Sha256)> SaveAsync(string companyId, string exitId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default);
    Task<(Stream Content, string ContentType, string FileName)?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    /// <summary>Removes an unreferenced private blob after its metadata write fails.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
