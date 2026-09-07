using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using WebApp.Services;

namespace WebApp.Tests.Services;

public class ResumeStorageServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"resume-storage-tests-{Guid.NewGuid():N}");

    private ResumeStorageService BuildService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Resumes:StorageDirectory"] = _tempDir })
            .Build();

        return new ResumeStorageService(new FakeWebHostEnvironment(), config);
    }

    private static IFormFile MakeFile(byte[] content) =>
        new FormFile(new MemoryStream(content), 0, content.Length, "file", "resume.pdf");

    [Fact]
    public async Task SaveAsync_WritesFileToDisk_WithGuidBasedName()
    {
        var service = BuildService();
        var content = "%PDF-1.4 test content"u8.ToArray();

        var storedFileName = await service.SaveAsync(MakeFile(content));

        Assert.EndsWith(".pdf", storedFileName);
        var path = service.GetPath(storedFileName);
        Assert.True(File.Exists(path));
        Assert.Equal(content, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task SaveAsync_TwoUploads_GetDifferentStoredNames()
    {
        var service = BuildService();
        var first = await service.SaveAsync(MakeFile("%PDF-1"u8.ToArray()));
        var second = await service.SaveAsync(MakeFile("%PDF-2"u8.ToArray()));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Delete_RemovesFileFromDisk()
    {
        var service = BuildService();
        var storedFileName = await service.SaveAsync(MakeFile("%PDF-"u8.ToArray()));
        var path = service.GetPath(storedFileName);
        Assert.True(File.Exists(path));

        service.Delete(storedFileName);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Delete_NonExistentFile_DoesNotThrow()
    {
        var service = BuildService();

        service.Delete("does-not-exist.pdf");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "WebApp.Tests";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
