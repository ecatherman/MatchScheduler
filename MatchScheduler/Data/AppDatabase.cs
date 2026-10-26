using CathermanCore;
using CathermanCore.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace MatchScheduler.Data;

/// <summary>
/// Owns the location of the local SQLite file and creates contexts for it.
/// Contexts are short-lived: create one per operation and dispose it.
/// </summary>
public static class AppDatabase
{
    public static string FolderPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VolleyballScheduler");

    public static string FilePath { get; } = Path.Combine(FolderPath, "scheduler.db");

    /// <summary>Creates the folder, the database file and its tables if they do not exist yet.</summary>
    public static void EnsureCreated()
    {
        Directory.CreateDirectory(FolderPath);
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public static SchedulerContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BaseContext>()
            .UseSqlite($"Data Source={FilePath}")
            .Options;

        var logging = Microsoft.Extensions.Options.Options.Create(new Logging
        {
            EfLogLevel = nameof(Serilog.Events.LogEventLevel.Warning)
        });

        return new SchedulerContext(options, logging)
        {
            UserName = Environment.UserName
        };
    }
}
