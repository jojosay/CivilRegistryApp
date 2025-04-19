using CivilRegistryApp.Infrastructure.Logging;
using Microsoft.Data.Sqlite;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CivilRegistryApp.Infrastructure
{
    public static class DiagnosticTools
    {
        public static async Task RunDiagnostics()
        {
            try
            {
                Log.Information("Starting application diagnostics");
                StringBuilder diagnosticResults = new StringBuilder();

                // Add header
                diagnosticResults.AppendLine("=== Civil Registry App Diagnostics ===");
                diagnosticResults.AppendLine($"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                diagnosticResults.AppendLine();

                // System information
                diagnosticResults.AppendLine("=== System Information ===");
                diagnosticResults.AppendLine($"OS Version: {Environment.OSVersion}");
                diagnosticResults.AppendLine($".NET Version: {Environment.Version}");
                diagnosticResults.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                diagnosticResults.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
                diagnosticResults.AppendLine($"Processor Count: {Environment.ProcessorCount}");
                diagnosticResults.AppendLine($"Machine Name: {Environment.MachineName}");
                diagnosticResults.AppendLine($"Current Directory: {Environment.CurrentDirectory}");
                diagnosticResults.AppendLine();

                // Application information
                diagnosticResults.AppendLine("=== Application Information ===");
                var assembly = Assembly.GetExecutingAssembly();
                diagnosticResults.AppendLine($"Application Name: {assembly.GetName().Name}");
                diagnosticResults.AppendLine($"Application Version: {assembly.GetName().Version}");
                diagnosticResults.AppendLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                diagnosticResults.AppendLine();

                // Check file system access
                diagnosticResults.AppendLine("=== File System Access ===");
                string logDirectory = SerilogConfig.LogDirectory;
                diagnosticResults.AppendLine($"Log Directory: {logDirectory}");
                diagnosticResults.AppendLine($"Log Directory Exists: {Directory.Exists(logDirectory)}");
                diagnosticResults.AppendLine($"Can Write to Log Directory: {CanWriteToDirectory(logDirectory)}");

                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                diagnosticResults.AppendLine($"App Directory: {appDirectory}");
                diagnosticResults.AppendLine($"Can Write to App Directory: {CanWriteToDirectory(appDirectory)}");
                diagnosticResults.AppendLine();

                // Check database connection
                diagnosticResults.AppendLine("=== Database Connection ===");
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CivilRegistry.db");
                string connectionString = $"Data Source={dbPath}";

                diagnosticResults.AppendLine($"Database Path: {dbPath}");
                diagnosticResults.AppendLine($"Database File Exists: {File.Exists(dbPath)}");

                try
                {
                    // Create the database file if it doesn't exist
                    if (!File.Exists(dbPath))
                    {
                        diagnosticResults.AppendLine("Creating new SQLite database file...");
                        // Just create an empty connection to create the file
                        using (var connection = new SqliteConnection($"Data Source={dbPath}"))
                        {
                            connection.Open();
                            connection.Close();
                        }
                    }

                    using (var connection = new SqliteConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        diagnosticResults.AppendLine("Database Connection: Success");

                        // Get SQLite version
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT sqlite_version()";
                            var version = await command.ExecuteScalarAsync();
                            diagnosticResults.AppendLine($"SQLite Version: {version}");
                        }

                        connection.Close();
                    }
                }
                catch (Exception ex)
                {
                    diagnosticResults.AppendLine($"Database Connection: Failed - {ex.Message}");
                    Log.Error(ex, "Database connection test failed");
                }
                diagnosticResults.AppendLine();

                // Check loaded assemblies
                diagnosticResults.AppendLine("=== Loaded Assemblies ===");
                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    diagnosticResults.AppendLine($"{loadedAssembly.GetName().Name} - {loadedAssembly.GetName().Version}");
                }

                // Write diagnostic results to file
                string diagnosticFilePath = Path.Combine(logDirectory, $"diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                await File.WriteAllTextAsync(diagnosticFilePath, diagnosticResults.ToString());

                Log.Information("Diagnostics completed and saved to {DiagnosticFilePath}", diagnosticFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running diagnostics");
            }
        }

        private static bool CanWriteToDirectory(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    return false;
                }

                string testFile = Path.Combine(directoryPath, $"write-test-{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "Test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
