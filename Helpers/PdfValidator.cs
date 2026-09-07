namespace WebApp.Helpers;

public static class PdfValidator
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly byte[] Signature = "%PDF-"u8.ToArray();

    /// <summary>
    /// Checks size and the file's actual leading bytes rather than trusting the
    /// browser-supplied content-type, which can be spoofed.
    /// </summary>
    public static async Task<bool> IsValidAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length is 0 or > MaxSizeBytes)
        {
            return false;
        }

        var buffer = new byte[Signature.Length];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer, cancellationToken);
        return read == Signature.Length && buffer.AsSpan().SequenceEqual(Signature);
    }
}
