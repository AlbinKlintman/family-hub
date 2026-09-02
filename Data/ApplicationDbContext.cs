using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Colleague> Colleagues => Set<Colleague>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<JobSearchLog> JobSearchLogs => Set<JobSearchLog>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();

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

            entity.HasMany(a => a.Descriptions)
                  .WithOne()
                  .HasForeignKey(d => d.JobApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.Links)
                  .WithOne()
                  .HasForeignKey(l => l.JobApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);

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

            entity.HasOne(a => a.Schedule)
                  .WithMany(s => s.JobApplications)
                  .HasForeignKey(a => a.ScheduleId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(a => new { a.UserId, a.Status });
        });

        builder.Entity<ApplicationDescription>(entity =>
        {
            // No HasMaxLength: descriptions are copy-pasted job postings and
            // can run long -- the old single Description column was unbounded
            // text for the same reason; a cap here would risk truncating them.
            entity.Property(d => d.Text).IsRequired();
        });

        builder.Entity<ApplicationLink>(entity =>
        {
            entity.Property(l => l.Url).IsRequired().HasMaxLength(2048);
        });

        builder.Entity<Folder>(entity =>
        {
            entity.Property(f => f.Name).IsRequired().HasMaxLength(100);
            entity.Property(f => f.Color).HasConversion<string>().HasMaxLength(10).IsRequired();

            entity.HasOne(f => f.ParentFolder)
                  .WithMany(f => f.Subfolders)
                  .HasForeignKey(f => f.ParentFolderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Schedule)
                  .WithMany(s => s.Folders)
                  .HasForeignKey(f => f.ScheduleId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(f => f.UserId);
        });

        builder.Entity<Schedule>(entity =>
        {
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Color).HasConversion<string>().HasMaxLength(10).IsRequired();

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.UserId);
        });

        builder.Entity<Colleague>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.UserId);
        });

        builder.Entity<Note>(entity =>
        {
            entity.HasDiscriminator<string>("NoteType")
                  .HasValue<ToDoNote>(nameof(Models.NoteType.ToDo))
                  .HasValue<LaundryNote>(nameof(Models.NoteType.Laundry))
                  .HasValue<WorkShiftNote>(nameof(Models.NoteType.WorkShift))
                  .HasValue<FastingNote>(nameof(Models.NoteType.Fasting));

            entity.Property(n => n.Title).HasMaxLength(2000);
            entity.Property(n => n.Priority).HasConversion<string>().HasMaxLength(10);

            entity.HasOne(n => n.Folder)
                  .WithMany(f => f.Notes)
                  .HasForeignKey(n => n.FolderId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Schedule)
                  .WithMany(s => s.Notes)
                  .HasForeignKey(n => n.ScheduleId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(n => new { n.UserId, n.IsDone });
        });

        builder.Entity<ToDoNote>(entity =>
        {
            entity.Property(n => n.RecurrenceIntervalUnit).HasConversion<string>().HasMaxLength(10);

            entity.HasMany(n => n.Reminders)
                  .WithOne()
                  .HasForeignKey(r => r.NoteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NoteReminder>(entity =>
        {
            entity.Property(r => r.OffsetUnit).HasConversion<string>().HasMaxLength(10).IsRequired();
        });

        builder.Entity<LaundryNote>(entity =>
        {
            entity.Property(n => n.LaundryType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(n => n.Room).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(n => n.TimeWindow).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        builder.Entity<WorkShiftNote>(entity =>
        {
            entity.Property(n => n.Location).IsRequired().HasMaxLength(200);

            entity.HasMany(n => n.Colleagues)
                  .WithMany(c => c.Shifts)
                  .UsingEntity(j => j.ToTable("WorkShiftColleagues"));
        });

        builder.Entity<FastingNote>(entity =>
        {
            entity.Property(n => n.Level).HasConversion<string>().HasMaxLength(30).IsRequired();

            // One fasting entry per user per day -- lets the bulk calendar page upsert by day.
            entity.HasIndex(n => new { n.UserId, n.Day })
                  .IsUnique()
                  .HasFilter("\"NoteType\" = 'Fasting'");
        });

        builder.Entity<WeightEntry>(entity =>
        {
            entity.Property(w => w.WeightKg).HasColumnType("numeric(5,2)");

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(w => new { w.UserId, w.Date });
        });

        builder.Entity<JobSearchLog>(entity =>
        {
            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(j => j.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(j => new { j.UserId, j.Date }).IsUnique();
        });

        builder.Entity<Exercise>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
        });

        builder.Entity<WorkoutLog>(entity =>
        {
            entity.Property(w => w.WeightKg).HasColumnType("numeric(6,2)");

            entity.Property(w => w.SessionType)
                  .HasConversion<string>()
                  .HasMaxLength(10)
                  .IsRequired();

            entity.HasOne(w => w.Exercise)
                  .WithMany(e => e.WorkoutLogs)
                  .HasForeignKey(w => w.ExerciseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<IdentityUser>()
                  .WithMany()
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(w => new { w.UserId, w.ExerciseId });
        });
    }
}
