using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Admin;
using CivilRegistryApp.Modules.Auth;
using CivilRegistryApp.Modules.Documents;
using CivilRegistryApp.Modules.Reports;
using CivilRegistryApp.Modules.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CivilRegistryApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;

    public ServiceProvider ServiceProvider => serviceProvider ?? throw new InvalidOperationException("ServiceProvider is not initialized");

    public App()
    {
        // Configure Serilog
        SerilogConfig.Configure();

        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            Log.Information("Application starting up");
            base.OnStartup(e);

            // Run diagnostics to help identify potential issues
            await DiagnosticTools.RunDiagnostics();

            var services = new ServiceCollection();
            ConfigureServices(services);

            serviceProvider = services.BuildServiceProvider();

            // Ensure database is created and migrated
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await EnsureDatabaseCreatedAsync(dbContext);
            }

            // Start the scheduler service
            var schedulerService = serviceProvider.GetRequiredService<ISchedulerService>();
            await schedulerService.StartAsync();
            Log.Information("Scheduler service started");

            // Create and show the main window
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            Current.MainWindow = mainWindow;
            mainWindow.Show();

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            HandleStartupException(ex);
        }
    }

    private async Task EnsureDatabaseCreatedAsync(AppDbContext dbContext)
    {
        try
        {
            Log.Information("Ensuring database is created");
            await dbContext.Database.EnsureCreatedAsync();
            Log.Information("Database created or already exists");

            // Update database schema if needed
            Log.Information("Checking for database schema updates");
            await DatabaseInitializer.InitializeAsync(dbContext);
            Log.Information("Database schema is up to date");

            // Seed the database with sample data
            await SeedDatabaseAsync(dbContext);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating database");
            throw;
        }
    }

    private async Task SeedDatabaseAsync(AppDbContext dbContext)
    {
        try
        {
            Log.Information("Starting database seeding");

            // Generate sample document images
            SampleImageGenerator.GenerateSampleDocumentImages();

            // Seed documents
            await SeedData.SeedDocumentsAsync(dbContext);

            // Seed requests
            await SeedData.SeedRequestsAsync(dbContext);

            // Initialize field configurations
            var fieldConfigService = serviceProvider.GetService<IFieldConfigurationService>();
            if (fieldConfigService != null)
            {
                try
                {
                    await fieldConfigService.InitializeDefaultConfigurationsAsync();
                    Log.Information("Field configurations initialized");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error initializing field configurations");
                }
            }

            Log.Information("Database seeding completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error seeding database");
            // Don't throw the exception - we want the app to continue even if seeding fails
        }
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // Register database context
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CivilRegistry.db");

        // Create SQLite connection string and ensure the directory exists
        var connectionString = $"Data Source={dbPath}";
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!Directory.Exists(dbDirectory) && dbDirectory != null)
            Directory.CreateDirectory(dbDirectory);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentRequestRepository, DocumentRequestRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IFieldConfigurationRepository, FieldConfigurationRepository>();

        // Register services
        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserActivityService, UserActivityService>();

        // Register FieldConfigurationService with all required dependencies
        services.AddScoped<IFieldConfigurationService>(provider => new FieldConfigurationService(
            provider.GetRequiredService<IFieldConfigurationRepository>(),
            provider.GetRequiredService<IAuthenticationService>(),
            provider.GetRequiredService<IUserActivityService>()
        ));
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IRequestService, RequestService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddSingleton<ISchedulerService, SchedulerService>();

        // Register views
        services.AddTransient<MainWindow>(provider => new MainWindow(
            provider.GetRequiredService<IAuthenticationService>(),
            provider.GetRequiredService<IDocumentService>(),
            provider.GetRequiredService<IRequestService>(),
            provider.GetRequiredService<AppDbContext>(),
            provider.GetRequiredService<IUserActivityService>(),
            provider.GetRequiredService<IUserRepository>()
        ));

        // Configure scheduler service
        var schedulerService = services.BuildServiceProvider().GetRequiredService<ISchedulerService>();
        services.AddSingleton(schedulerService);
        // Add other views here as they are created
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Application shutting down");

            // Stop the scheduler service
            if (serviceProvider != null)
            {
                var schedulerService = serviceProvider.GetService<ISchedulerService>();
                if (schedulerService != null)
                {
                    await schedulerService.StopAsync();
                    Log.Information("Scheduler service stopped");
                }

                // Dispose the service provider
                serviceProvider.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during application shutdown");
        }
        finally
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        SerilogConfig.LogUnhandledException(exception ?? new Exception("Unknown exception"), "AppDomain.CurrentDomain.UnhandledException");

        if (e.IsTerminating)
        {
            ShowFatalErrorMessageBox(exception);
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SerilogConfig.LogUnhandledException(e.Exception, "Application.DispatcherUnhandledException");

        // Prevent default unhandled exception processing
        e.Handled = true;

        ShowErrorMessageBox(e.Exception);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        SerilogConfig.LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved(); // Prevent the process from being terminated
    }

    private void HandleStartupException(Exception ex)
    {
        SerilogConfig.LogUnhandledException(ex, "Application Startup");
        ShowFatalErrorMessageBox(ex);
        Shutdown(-1);
    }

    private void ShowErrorMessageBox(Exception ex)
    {
        string errorMessage = $"An error occurred: {ex.Message}\n\nPlease check the log files for more details.";
        MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowFatalErrorMessageBox(Exception ex)
    {
        string errorMessage = $"A fatal error occurred: {ex?.Message}\n\nThe application needs to close. Please check the log files in:\n{SerilogConfig.LogDirectory}";
        MessageBox.Show(errorMessage, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
