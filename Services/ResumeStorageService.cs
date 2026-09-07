namespace WebApp.Services;

/// <summary>
/// Stores resume PDFs on disk (a mounted volume in production) rather than in
/// Postgres, so uploads don't grow the database or its backups -- only the
/// small filename/size/timestamp metadata lives in the JobApplication row.
/// </summary>
public class ResumeStorageService(IWebHostEnvironment environment, IConfiguration configuration)
{
    /// <summary>Overridable via config (e.g. in tests) so callers aren't forced to write under the real content root.</summary>
    private string Directory => configuration["Resumes:StorageDirectory"]
        ?? Path.Combine(environment.ContentRootPath, "resumes");

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        System.IO.Directory.CreateDirectory(Directory);

        var storedFileName = $"{Guid.NewGuid():N}.pdf";
        var path = Path.Combine(Directory, storedFileName);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return storedFileName;
    }

    public string GetPath(string storedFileName) => Path.Combine(Directory, storedFileName);

    public void Delete(string storedFileName)
    {
        var path = GetPath(storedFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
