using System.IO;
using System.Windows;
using MatchScheduler.Data;
using MatchScheduler.Services;
using Serilog;

namespace MatchScheduler;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Directory.CreateDirectory(AppDatabase.FolderPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File(
                    Path.Combine(AppDatabase.FolderPath, "Logs", "app-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .CreateLogger();

            // Creates the SQLite file and its tables the first time the app runs.
            AppDatabase.EnsureCreated();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The app couldn't open its database:\n\n{ex.Message}",
                "Volleyball Scheduler", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            DialogService.ShowError(args.Exception);
            args.Handled = true;
        };

        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
