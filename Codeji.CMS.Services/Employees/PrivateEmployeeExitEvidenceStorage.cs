using System.Security.Cryptography;
using Codeji.CMS.Services.Employees.Interface;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Employees;

/// <summary>Local private implementation; its root is intentionally outside wwwroot/static Uploads.</summary>
public sealed class PrivateEmployeeExitEvidenceStorage : IEmployeeExitEvidenceStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "application/pdf", "image/png", "image/jpeg", "message/rfc822" };
    private readonly string _root;

    public PrivateEmployeeExitEvidenceStorage(string? root = null) =>
        _root = Path.GetFullPath(root ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "EmployeeExitEvidence"));

    public async Task<(string StorageKey, string Sha256)> SaveAsync(string companyId, string exitId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length is <= 0 or > 10 * 1024 * 1024 || !AllowedContentTypes.Contains(file.ContentType)) throw new InvalidOperationException("Unsupported or invalid evidence file.");
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(exitId) || companyId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || exitId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidOperationException("Invalid evidence storage scope.");
        var extension = file.ContentType switch { "application/pdf" => ".pdf", "image/png" => ".png", "image/jpeg" => ".jpg", "message/rfc822" => ".eml", _ => throw new InvalidOperationException("Unsupported evidence file.") };
        var key = Path.Combine(companyId, exitId, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var input = file.OpenReadStream()) await using (var output = File.Create(path))
        {
            await input.CopyToAsync(output, cancellationToken); await output.FlushAsync(cancellationToken);
        }
        if (!await HasExpectedSignature(path, file.ContentType, cancellationToken)) { File.Delete(path); throw new InvalidOperationException("Evidence file content does not match its declared type."); }
        await using var hashed = File.OpenRead(path); using var hash = SHA256.Create(); var sha = Convert.ToHexString(await hash.ComputeHashAsync(hashed, cancellationToken));
        return (key, sha);
    }

    public async Task<(string StorageKey, string Sha256)> SaveAsync(string companyId, string exitId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        if (content is null || !AllowedContentTypes.Contains(contentType)) throw new InvalidOperationException("Unsupported generated evidence file.");
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(exitId) || companyId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || exitId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidOperationException("Invalid evidence storage scope.");
        var extension = contentType switch { "application/pdf" => ".pdf", "image/png" => ".png", "image/jpeg" => ".jpg", "message/rfc822" => ".eml", _ => throw new InvalidOperationException("Unsupported generated evidence file.") };
        var key = Path.Combine(companyId, exitId, $"{Guid.NewGuid():N}{extension}").Replace('\\', '/');
        var path = Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var output = File.Create(path)) { await content.CopyToAsync(output, cancellationToken); await output.FlushAsync(cancellationToken); }
        if (!await HasExpectedSignature(path, contentType, cancellationToken)) { File.Delete(path); throw new InvalidOperationException("Generated evidence content is invalid."); }
        await using var hashed = File.OpenRead(path); using var hash = SHA256.Create();
        return (key, Convert.ToHexString(await hash.ComputeHashAsync(hashed, cancellationToken)));
    }

    public Task<(Stream Content, string ContentType, string FileName)?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return Task.FromResult<(Stream, string, string)?>(null);
        var path = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return Task.FromResult<(Stream, string, string)?>(null);
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch { ".pdf" => "application/pdf", ".png" => "image/png", ".jpg" => "image/jpeg", ".eml" => "message/rfc822", _ => "application/octet-stream" };
        return Task.FromResult<(Stream, string, string)?>(new(File.OpenRead(path), contentType, Path.GetFileName(path)));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return Task.CompletedTask;
        var path = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) && File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private static async Task<bool> HasExpectedSignature(string path, string contentType, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path); var header = new byte[32]; var read = await stream.ReadAsync(header, cancellationToken);
        return contentType switch
        {
            "application/pdf" => read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            "image/png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            // RFC 5322 messages do not have a magic number. Require recognizable headers
            // and reject binary payloads at the beginning of the file.
            "message/rfc822" => read > 0 && !header.Take(read).Contains((byte)0) && System.Text.Encoding.UTF8.GetString(header, 0, read).Contains(':', StringComparison.Ordinal),
            _ => false
        };
    }
}
