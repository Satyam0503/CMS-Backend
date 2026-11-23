using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Recruitments;

/// <summary>
/// Resolves resume paths outside the public static-file tree. Production hosts
/// should point ATS_RESUME_STORAGE_PATH at persistent private storage.
/// </summary>
public static class RecruitmentResumeStorage
{
    public const string PrivatePrefix = "private:";

    public static async Task<bool> HasPdfSignature(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length < 4) return false;
        try
        {
            await using var stream = file.OpenReadStream();
            byte[] signature = new byte[4];
            var bytesRead = 0;
            while (bytesRead < signature.Length)
            {
                var read = await stream.ReadAsync(signature.AsMemory(bytesRead), cancellationToken);
                if (read == 0) return false;
                bytesRead += read;
            }

            return signature[0] == (byte)'%' && signature[1] == (byte)'P' &&
                   signature[2] == (byte)'D' && signature[3] == (byte)'F';
        }
        catch
        {
            return false;
        }
    }

    public static string PrivateDirectory
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("ATS_RESUME_STORAGE_PATH");
            var path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Private", "Recruitment", "Resume")
                : configured.Trim();
            return ValidatePrivatePath(path, Directory.GetCurrentDirectory());
        }
    }

    public static string QuarantineDirectory
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("ATS_RESUME_QUARANTINE_PATH");
            var path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Private", "Recruitment", "Quarantine")
                : configured.Trim();
            string quarantine = ValidatePrivatePath(path, Directory.GetCurrentDirectory());
            string resumes = PrivateDirectory;
            if (IsSameOrChildPath(quarantine, resumes) || IsSameOrChildPath(resumes, quarantine))
                throw new InvalidOperationException("ATS resume quarantine storage must be separate from active resume storage.");
            return quarantine;
        }
    }

    public static string ValidatePrivatePath(string path, string contentRoot)
    {
        string root = Path.GetFullPath(contentRoot);
        string fullPath = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
        string uploadsPath = Path.Combine(root, "Uploads");
        string webRootPath = Path.Combine(root, "wwwroot");
        if (IsSameOrChildPath(fullPath, uploadsPath) || IsSameOrChildPath(fullPath, webRootPath))
            throw new InvalidOperationException("ATS resume storage must be outside public static-file directories.");
        return fullPath;
    }

    private static bool IsSameOrChildPath(string path, string directory)
    {
        string relativePath = Path.GetRelativePath(directory, path);
        return relativePath == "." ||
            (!Path.IsPathRooted(relativePath) && relativePath != ".." &&
             !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    public static string Resolve(string resumeReference)
    {
        var isPrivate = resumeReference.StartsWith(PrivatePrefix, StringComparison.OrdinalIgnoreCase);
        var fileName = Path.GetFileName(isPrivate ? resumeReference[PrivatePrefix.Length..] : resumeReference);
        var directory = isPrivate
            ? PrivateDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Resume");
        return Path.Combine(directory, fileName);
    }

    public static string ResolveQuarantined(string quarantineFileName)
    {
        if (string.IsNullOrWhiteSpace(quarantineFileName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(quarantineFileName, "^[A-Fa-f0-9]{32}\\.quarantine$"))
            throw new InvalidOperationException("Invalid resume quarantine reference.");
        return Path.Combine(QuarantineDirectory, quarantineFileName);
    }

    public static async Task<bool> MoveToQuarantine(string sourcePath, string quarantineFileName, string expectedSha256, CancellationToken cancellationToken = default)
    {
        return await MoveToQuarantineInDirectory(sourcePath, QuarantineDirectory, quarantineFileName, expectedSha256, cancellationToken);
    }

    internal static async Task<bool> MoveToQuarantineInDirectory(string sourcePath, string quarantineDirectory, string quarantineFileName, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(quarantineFileName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(quarantineFileName, "^[A-Fa-f0-9]{32}\\.quarantine$")) return false;
        string quarantinePath = Path.Combine(quarantineDirectory, quarantineFileName);
        Directory.CreateDirectory(quarantineDirectory);
        if ((File.GetAttributes(quarantineDirectory) & FileAttributes.ReparsePoint) != 0) return false;
        if (File.Exists(quarantinePath))
        {
            if (!await FileMatchesSha256(quarantinePath, expectedSha256, cancellationToken)) return false;
            if (File.Exists(sourcePath) && !string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(quarantinePath), StringComparison.OrdinalIgnoreCase))
            {
                if (!await FileMatchesSha256(sourcePath, expectedSha256, cancellationToken)) return false;
                File.Delete(sourcePath);
            }
            return true;
        }

        if (!File.Exists(sourcePath) || (File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) != 0) return false;
        var sourceInfo = new FileInfo(sourcePath);
        if (sourceInfo.Length is < 4 or > 5 * 1024 * 1024) return false;
        string partialPath = Path.Combine(quarantineDirectory, $"{Guid.NewGuid():N}.partial");
        try
        {
            await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var destination = new FileStream(partialPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await source.CopyToAsync(destination, cancellationToken);

            if (!await FileMatchesSha256(partialPath, expectedSha256, cancellationToken)) return false;
            try { File.Move(partialPath, quarantinePath); }
            catch (IOException)
            {
                if (!File.Exists(quarantinePath) || !await FileMatchesSha256(quarantinePath, expectedSha256, cancellationToken)) throw;
            }
            File.Delete(sourcePath);
            return true;
        }
        finally
        {
            if (File.Exists(partialPath)) File.Delete(partialPath);
        }
    }

    public static async Task<bool> FileMatchesSha256(string path, string expectedSha256, CancellationToken cancellationToken = default)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(expectedSha256 ?? string.Empty, "^[A-Fa-f0-9]{64}$")) return false;
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return false;
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            string actual = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken));
            return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return false; }
    }

    public static bool DeleteQuarantined(string? quarantineFileName)
    {
        return DeleteQuarantinedInDirectory(QuarantineDirectory, quarantineFileName);
    }

    internal static bool DeleteQuarantinedInDirectory(string quarantineDirectory, string? quarantineFileName)
    {
        if (string.IsNullOrWhiteSpace(quarantineFileName)) return true;
        if (!System.Text.RegularExpressions.Regex.IsMatch(quarantineFileName, "^[A-Fa-f0-9]{32}\\.quarantine$")) return false;
        try
        {
            string path = Path.Combine(quarantineDirectory, quarantineFileName);
            if (File.Exists(path)) File.Delete(path);
            return !File.Exists(path);
        }
        catch { return false; }
    }
}
