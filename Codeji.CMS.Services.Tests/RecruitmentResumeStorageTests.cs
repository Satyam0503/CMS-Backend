using Codeji.CMS.Services.Recruitments;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Codeji.CMS.Services.Tests;

public sealed class RecruitmentResumeStorageTests
{
    [Theory]
    [InlineData("%PDF-1.7", true)]
    [InlineData("PK\u0003\u0004", false)]
    [InlineData("%PD", false)]
    public async Task Pdf_signature_validation_checks_the_file_content(string content, bool expected)
    {
        var bytes = Encoding.ASCII.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "resume", "resume.pdf");

        Assert.Equal(expected, await RecruitmentResumeStorage.HasPdfSignature(file));
    }

    [Theory]
    [InlineData("Uploads/Resume", true)]
    [InlineData("UploadsBackup/Resume", false)]
    [InlineData("wwwroot/ats-resumes", true)]
    [InlineData("App_Data/Private/Recruitment/Resume", false)]
    public void Resume_storage_path_must_not_be_inside_public_static_directories(string relativePath, bool shouldReject)
    {
        string root = Path.Combine(Path.GetTempPath(), "codeji-ats-storage-test");
        string candidatePath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (shouldReject)
        {
            Assert.Throws<InvalidOperationException>(() => RecruitmentResumeStorage.ValidatePrivatePath(candidatePath, root));
            return;
        }

        Assert.Equal(Path.GetFullPath(candidatePath), RecruitmentResumeStorage.ValidatePrivatePath(candidatePath, root));
    }
}
