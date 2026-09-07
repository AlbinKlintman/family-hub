using Microsoft.AspNetCore.Http;
using WebApp.Helpers;

namespace WebApp.Tests.Helpers;

public class PdfValidatorTests
{
    private static IFormFile MakeFile(byte[] content, string fileName = "resume.pdf") =>
        new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName);

    [Fact]
    public async Task IsValidAsync_RealPdfSignature_ReturnsTrue()
    {
        var content = "%PDF-1.4\n%fake pdf body\n%%EOF"u8.ToArray();

        Assert.True(await PdfValidator.IsValidAsync(MakeFile(content)));
    }

    [Fact]
    public async Task IsValidAsync_WrongSignature_ReturnsFalse()
    {
        var content = "Not a pdf at all, just plain text."u8.ToArray();

        Assert.False(await PdfValidator.IsValidAsync(MakeFile(content)));
    }

    [Fact]
    public async Task IsValidAsync_EmptyFile_ReturnsFalse()
    {
        Assert.False(await PdfValidator.IsValidAsync(MakeFile([])));
    }

    [Fact]
    public async Task IsValidAsync_TooLarge_ReturnsFalse()
    {
        var content = new byte[PdfValidator.MaxSizeBytes + 1];
        "%PDF-"u8.CopyTo(content);

        Assert.False(await PdfValidator.IsValidAsync(MakeFile(content)));
    }

    [Fact]
    public async Task IsValidAsync_ExactlyMaxSize_ReturnsTrue()
    {
        var content = new byte[PdfValidator.MaxSizeBytes];
        "%PDF-"u8.CopyTo(content);

        Assert.True(await PdfValidator.IsValidAsync(MakeFile(content)));
    }
}
