using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Company>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.UserId);
        });

        builder.Entity<JobApplication>(entity =>
        {
            entity.Property(a => a.RoleName).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Link).HasMaxLength(2048);

            entity.Property(a => a.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(a => a.Chance)
                  .HasConversion<string>()
                  .HasMaxLength(10);

            entity.HasOne(a => a.Company)
                  .WithMany(c => c.JobApplications)
                  .HasForeignKey(a => a.CompanyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => new { a.UserId, a.Status });
        });
    }
}
