using Codeji.CMS.Services.Employees;
using Microsoft.AspNetCore.Http;

namespace Codeji.CMS.Services.Tests;

public sealed class PrivateEmployeeExitEvidenceStorageTests
{
    [Fact]
    public async Task SaveAsync_ValidPdf_SavesPrivatelyAndReturnsPdfOnRead()
    {
        var root = CreateRoot();
        try
        {
            var storage = new PrivateEmployeeExitEvidenceStorage(root);
            var saved = await storage.SaveAsync("company-1", "exit-1", File("letter.pdf", "application/pdf", "%PDF-1.7\ncontent"));

            Assert.DoesNotContain("letter.pdf", saved.StorageKey, StringComparison.OrdinalIgnoreCase);
            var opened = await storage.OpenReadAsync(saved.StorageKey);
            Assert.NotNull(opened); Assert.Equal("application/pdf", opened!.Value.ContentType);
            await opened.Value.Content.DisposeAsync();
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task SaveAsync_SpoofedPdf_IsRejectedAndNotStored()
    {
        var root = CreateRoot();
        try
        {
            var storage = new PrivateEmployeeExitEvidenceStorage(root);
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveAsync("company-1", "exit-1", File("malware.pdf", "application/pdf", "not a pdf")));
            Assert.Empty(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task OpenReadAsync_PathOutsideRoot_IsRejected()
    {
        var root = CreateRoot();
        try { Assert.Null(await new PrivateEmployeeExitEvidenceStorage(root).OpenReadAsync("../outside.pdf")); }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static IFormFile File(string name, string contentType, string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text); var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = contentType };
    }
    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "codeji-evidence-tests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); return root;
    }
}
