using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Modules.Auth;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CivilRegistryApp.Modules.Reports.Views
{
    /// <summary>
    /// Interaction logic for ScheduledReportsView.xaml
    /// </summary>
    public partial class ScheduledReportsView : System.Windows.Controls.UserControl
    {
        private IReportService _reportService = null!;
        private IAuthenticationService _authService = null!;
        private AppDbContext _dbContext = null!;
        private bool _isInitialized = false;

        public ScheduledReportsView()
        {
            InitializeComponent();
        }

        public void Initialize(IReportService reportService, IAuthenticationService authService, AppDbContext dbContext)
        {
            try
            {
                Log.Debug("Initializing ScheduledReportsView");
                _reportService = reportService;
                _authService = authService;
                _dbContext = dbContext;

                // Load scheduled reports
                LoadScheduledReports();

                _isInitialized = true;
                Log.Information("ScheduledReportsView initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ScheduledReportsView");
                MessageBox.Show($"Error initializing Scheduled Reports view: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshData()
        {
            if (_isInitialized)
            {
                LoadScheduledReports();
            }
        }

        private async void LoadScheduledReports()
        {
            try
            {
                // Show loading cursor
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                try
                {
                    // Get scheduled reports
                    IEnumerable<ScheduledReport> reports;

                    // If user is admin, show all reports, otherwise show only user's reports
                    if (_authService.IsUserInRole("Admin"))
                    {
                        reports = await _reportService.GetAllScheduledReportsAsync();
                    }
                    else
                    {
                        reports = await _reportService.GetScheduledReportsByUserAsync(_authService.CurrentUser.UserId);
                    }

                    // Bind to data grid
                    ScheduledReportsDataGrid.ItemsSource = reports;
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table: ScheduledReports"))
                {
                    // The ScheduledReports table doesn't exist yet
                    Log.Warning("ScheduledReports table does not exist yet. Showing empty list.");
                    ScheduledReportsDataGrid.ItemsSource = new List<ScheduledReport>();

                    // Show a message to the user
                    MessageBox.Show("The Scheduled Reports feature is being set up for the first time. " +
                                  "You can start creating scheduled reports now.",
                                  "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Reset cursor
                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading scheduled reports");
                MessageBox.Show($"Error loading scheduled reports: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void ScheduledReportsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable/disable buttons based on selection
            bool hasSelection = ScheduledReportsDataGrid.SelectedItem != null;
            RunNowButton.IsEnabled = hasSelection;
            EditButton.IsEnabled = hasSelection;
            ToggleActiveButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;

            // Update toggle button text
            if (hasSelection && ScheduledReportsDataGrid.SelectedItem is ScheduledReport report)
            {
                ToggleActiveButton.Content = report.IsActive ? "Deactivate" : "Activate";
            }
        }

        private async void RunNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ScheduledReportsDataGrid.SelectedItem is ScheduledReport report)
                {
                    // Show loading cursor
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                    // Run the report
                    string fileName = await _reportService.RunScheduledReportAsync(report.ScheduledReportId);

                    // Reset cursor
                    Mouse.OverrideCursor = null;

                    // Show success message
                    var result = MessageBox.Show($"Report generated successfully: {fileName}\n\nDo you want to open the output folder?",
                        "Report Generated", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(report.OutputPath))
                    {
                        // Open the output folder
                        if (Directory.Exists(report.OutputPath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = report.OutputPath,
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            MessageBox.Show($"Output folder does not exist: {report.OutputPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    // Refresh the list
                    RefreshData();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running scheduled report");
                MessageBox.Show($"Error running scheduled report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ScheduledReportsDataGrid.SelectedItem is ScheduledReport report)
                {
                    // Show edit dialog
                    var scheduleDialog = new ScheduleReportDialog(report);
                    if (scheduleDialog.ShowDialog() == true)
                    {
                        // Update the report
                        await _reportService.UpdateScheduledReportAsync(report);

                        // Refresh the list
                        RefreshData();

                        MessageBox.Show("Scheduled report updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error editing scheduled report");
                MessageBox.Show($"Error editing scheduled report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ScheduledReportsDataGrid.SelectedItem is ScheduledReport report)
                {
                    // Toggle active status
                    await _reportService.ToggleScheduledReportStatusAsync(report.ScheduledReportId);

                    // Refresh the list
                    RefreshData();

                    MessageBox.Show($"Report {(report.IsActive ? "deactivated" : "activated")} successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error toggling scheduled report status");
                MessageBox.Show($"Error toggling scheduled report status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ScheduledReportsDataGrid.SelectedItem is ScheduledReport report)
                {
                    // Confirm deletion
                    var result = MessageBox.Show($"Are you sure you want to delete the scheduled report '{report.Name}'?",
                        "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Delete the report
                        await _reportService.DeleteScheduledReportAsync(report.ScheduledReportId);

                        // Refresh the list
                        RefreshData();

                        MessageBox.Show("Scheduled report deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting scheduled report");
                MessageBox.Show($"Error deleting scheduled report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
