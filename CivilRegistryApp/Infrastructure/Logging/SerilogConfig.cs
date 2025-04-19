using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Text;

namespace CivilRegistryApp.Infrastructure.Logging
{
    public static class SerilogConfig
    {
        public static string LogDirectory { get; private set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        public static void Configure()
        {
            LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", "CivilRegistryApp")
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(LogDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    encoding: Encoding.UTF8,
                    fileSizeLimitBytes: 10 * 1024 * 1024) // 10MB file size limit
                .CreateLogger();

            Log.Information("Logging initialized");
        }

        public static void LogUnhandledException(Exception ex, string source)
        {
            Log.Fatal(ex, "Unhandled exception in {Source}", source);

            // Also write to a special crash log file
            string crashLogPath = Path.Combine(LogDirectory, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

            try
            {
                using (var writer = new StreamWriter(crashLogPath, false, Encoding.UTF8))
                {
                    writer.WriteLine($"Crash occurred at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    writer.WriteLine($"Source: {source}");
                    writer.WriteLine("\nException details:");
                    writer.WriteLine($"Message: {ex.Message}");
                    writer.WriteLine($"Type: {ex.GetType().FullName}");
                    writer.WriteLine($"Stack trace: {ex.StackTrace}");

                    if (ex.InnerException != null)
                    {
                        writer.WriteLine("\nInner exception details:");
                        writer.WriteLine($"Message: {ex.InnerException.Message}");
                        writer.WriteLine($"Type: {ex.InnerException.GetType().FullName}");
                        writer.WriteLine($"Stack trace: {ex.InnerException.StackTrace}");
                    }
                }
            }
            catch (Exception logEx)
            {
                // If we can't write to the crash log, at least try to log this failure
                Log.Error(logEx, "Failed to write crash log to {CrashLogPath}", crashLogPath);
            }
        }
    }
}
