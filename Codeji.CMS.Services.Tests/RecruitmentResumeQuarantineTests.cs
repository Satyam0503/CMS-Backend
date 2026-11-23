using System.Security.Cryptography;
using System.Text;
using Codeji.CMS.Services.Recruitments;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeQuarantineTests
{
    [Fact]
    public async Task Quarantine_moves_only_bytes_matching_the_persisted_scan_digest()
    {
        string root = Path.Combine(Path.GetTempPath(), $"ats-resume-quarantine-{Guid.NewGuid():N}");
        string active = Path.Combine(root, "active");
        string quarantine = Path.Combine(root, "quarantine");
        Directory.CreateDirectory(active);
        string source = Path.Combine(active, "legacy.pdf");
        byte[] content = Encoding.ASCII.GetBytes("%PDF-1.7\nlegacy resume");
        await File.WriteAllBytesAsync(source, content);
        string digest = Convert.ToHexString(SHA256.HashData(content));
        string quarantineName = $"{Guid.NewGuid():N}.quarantine";

        try
        {
            Assert.True(await RecruitmentResumeStorage.MoveToQuarantineInDirectory(source, quarantine, quarantineName, digest));
            string quarantined = Path.Combine(quarantine, quarantineName);
            Assert.False(File.Exists(source));
            Assert.True(await RecruitmentResumeStorage.FileMatchesSha256(quarantined, digest));
            Assert.True(await RecruitmentResumeStorage.MoveToQuarantineInDirectory(source, quarantine, quarantineName, digest));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Quarantine_does_not_move_source_when_digest_does_not_match()
    {
        string root = Path.Combine(Path.GetTempPath(), $"ats-resume-quarantine-{Guid.NewGuid():N}");
        string active = Path.Combine(root, "active");
        string quarantine = Path.Combine(root, "quarantine");
        Directory.CreateDirectory(active);
        string source = Path.Combine(active, "legacy.pdf");
        await File.WriteAllBytesAsync(source, Encoding.ASCII.GetBytes("%PDF-1.7\nlegacy resume"));

        try
        {
            Assert.False(await RecruitmentResumeStorage.MoveToQuarantineInDirectory(source, quarantine,
                $"{Guid.NewGuid():N}.quarantine", new string('0', 64)));
            Assert.True(File.Exists(source));
            Assert.Empty(Directory.EnumerateFiles(quarantine));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Quarantine_is_deleted_during_durable_candidate_erasure_cleanup()
    {
        string root = Path.Combine(Path.GetTempPath(), $"ats-resume-quarantine-{Guid.NewGuid():N}");
        string quarantine = Path.Combine(root, "quarantine");
        Directory.CreateDirectory(quarantine);
        string quarantineName = $"{Guid.NewGuid():N}.quarantine";
        await File.WriteAllTextAsync(Path.Combine(quarantine, quarantineName), "quarantined candidate data");

        try
        {
            Assert.True(RecruitmentResumeStorage.DeleteQuarantinedInDirectory(quarantine, quarantineName));
            Assert.False(File.Exists(Path.Combine(quarantine, quarantineName)));
            Assert.True(RecruitmentResumeStorage.DeleteQuarantinedInDirectory(quarantine, quarantineName));
            Assert.False(RecruitmentResumeStorage.DeleteQuarantinedInDirectory(quarantine, "..\\candidate.pdf"));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
