using CathermanCore;
using CathermanCore.EntityFramework;
using MatchScheduler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchScheduler.Data;

/// <summary>
/// The app's database. BaseContext supplies soft delete (IsLive), Added/Updated/LastUpdateBy stamping,
/// and the warnings that catch sloppy queries, so every entity here just inherits Entity.
/// </summary>
public class SchedulerContext : BaseContext
{
    public SchedulerContext(DbContextOptions<BaseContext> options, IOptions<Logging> loggingOptions)
        : base(options, loggingOptions)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleAssignment> ScheduleAssignments => Set<ScheduleAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Player>(b =>
        {
            b.Property(p => p.Name).IsRequired().HasMaxLength(100);
            b.Property(p => p.PhoneNumber).HasMaxLength(40);
            b.Property(p => p.Email).HasMaxLength(200);
            b.Property(p => p.DesiredPosition).HasConversion<string>().HasMaxLength(30);
        });

        builder.Entity<Schedule>(b =>
        {
            b.Property(s => s.Name).IsRequired().HasMaxLength(150);
            b.HasMany(s => s.Assignments).WithOne(a => a.Schedule).HasForeignKey(a => a.ScheduleId);
        });

        builder.Entity<ScheduleAssignment>(b =>
        {
            b.Property(a => a.PlayerName).IsRequired().HasMaxLength(100);
            b.Property(a => a.PlayerPosition).HasConversion<string>().HasMaxLength(30);
            b.HasIndex(a => a.ScheduleId);
        });

        // Last, so BaseContext's query filters and defaults reach every entity registered above.
        base.OnModelCreating(builder);
    }
}
