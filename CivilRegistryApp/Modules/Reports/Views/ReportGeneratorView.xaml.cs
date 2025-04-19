using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Modules.Auth;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CivilRegistryApp.Modules.Reports.Views
{
    /// <summary>
    /// Interaction logic for ReportGeneratorView.xaml
    /// </summary>
    public partial class ReportGeneratorView : System.Windows.Controls.UserControl
    {
        private IReportService _reportService = null!;
        private IAuthenticationService _authService = null!;
        private AppDbContext _dbContext = null!;
        private bool _isInitialized = false;

        public ReportGeneratorView()
        {
            InitializeComponent();
        }

        public void Initialize(IReportService reportService, IAuthenticationService authService, AppDbContext dbContext)
        {
            try
            {
                Log.Debug("Initializing ReportGeneratorView");
                _reportService = reportService;
                _authService = authService;
                _dbContext = dbContext;

                // Load users for activity report
                LoadUsers();

                _isInitialized = true;
                Log.Information("ReportGeneratorView initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ReportGeneratorView");
                MessageBox.Show($"Error initializing Report Generator view: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshData()
        {
            if (_isInitialized)
            {
                LoadUsers();
            }
        }

        private void LoadUsers()
        {
            try
            {
                // Clear existing items except "All Users"
                UserComboBox.Items.Clear();
                UserComboBox.Items.Add(new ComboBoxItem { Content = "All Users", IsSelected = true });

                // Load users from database
                var users = _dbContext.Users.OrderBy(u => u.Username).ToList();
                foreach (var user in users)
                {
                    UserComboBox.Items.Add(new ComboBoxItem { Content = user.Username, Tag = user.UserId });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading users for report");
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Check if UI elements are initialized
                if (DocumentReportGrid == null || RequestReportGrid == null || ActivityReportGrid == null)
                {
                    // UI elements are not yet initialized, skip processing
                    return;
                }

                if (ReportTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string reportType = selectedItem.Content.ToString();

                    // Hide all report parameter grids
                    DocumentReportGrid.Visibility = Visibility.Collapsed;
                    RequestReportGrid.Visibility = Visibility.Collapsed;
                    ActivityReportGrid.Visibility = Visibility.Collapsed;

                    // Show the selected report parameter grid
                    switch (reportType)
                    {
                        case "Document Report":
                            DocumentReportGrid.Visibility = Visibility.Visible;
                            break;
                        case "Request Report":
                            RequestReportGrid.Visibility = Visibility.Visible;
                            break;
                        case "Activity Report":
                            ActivityReportGrid.Visibility = Visibility.Visible;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ReportTypeComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    MessageBox.Show("Report service is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Show loading cursor
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                // Get report type
                string reportType = ((ComboBoxItem)ReportTypeComboBox.SelectedItem).Content.ToString();
                string exportFormat = "PDF";
                MemoryStream reportStream = null;

                // Generate report based on type
                switch (reportType)
                {
                    case "Document Report":
                        exportFormat = ((ComboBoxItem)ExportFormatComboBox.SelectedItem).Content.ToString();
                        reportStream = await GenerateDocumentReport(exportFormat);
                        break;
                    case "Request Report":
                        exportFormat = ((ComboBoxItem)RequestExportFormatComboBox.SelectedItem).Content.ToString();
                        reportStream = await GenerateRequestReport(exportFormat);
                        break;
                    case "Activity Report":
                        exportFormat = ((ComboBoxItem)ActivityExportFormatComboBox.SelectedItem).Content.ToString();
                        reportStream = await GenerateActivityReport(exportFormat);
                        break;
                }

                // Reset cursor
                Mouse.OverrideCursor = null;

                if (reportStream != null)
                {
                    // Save the report to a file
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                    saveFileDialog.Title = "Save Report";
                    saveFileDialog.Filter = exportFormat.Equals("PDF", StringComparison.OrdinalIgnoreCase)
                        ? "PDF Files (*.pdf)|*.pdf"
                        : "Excel Files (*.xlsx)|*.xlsx";
                    saveFileDialog.FileName = $"{reportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write))
                        {
                            reportStream.CopyTo(fileStream);
                        }

                        // Ask if user wants to open the file
                        var result = MessageBox.Show("Report generated successfully. Do you want to open it?",
                            "Report Generated", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = saveFileDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                    }

                    // Dispose the stream
                    reportStream.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating report");
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Reset cursor
                Mouse.OverrideCursor = null;
            }
        }

        private async void SaveAsScheduledButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    MessageBox.Show("Report service is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Get report type
                string reportType = ((ComboBoxItem)ReportTypeComboBox.SelectedItem).Content.ToString();

                // Create scheduled report based on type
                ScheduledReport scheduledReport = new ScheduledReport
                {
                    Name = $"{reportType} - {DateTime.Now:yyyy-MM-dd}",
                    Schedule = "0 0 12 ? * MON", // Default: Every Monday at noon
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = _authService.CurrentUser.UserId
                };

                switch (reportType)
                {
                    case "Document Report":
                        scheduledReport.ReportType = "Document";
                        scheduledReport.ExportFormat = ((ComboBoxItem)ExportFormatComboBox.SelectedItem).Content.ToString();

                        // Get document type
                        string documentType = ((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Content.ToString();
                        scheduledReport.DocumentType = documentType == "All Types" ? null : documentType;

                        // Get other parameters
                        scheduledReport.RegistryOffice = string.IsNullOrWhiteSpace(RegistryOfficeTextBox.Text) ? null : RegistryOfficeTextBox.Text;
                        scheduledReport.Province = string.IsNullOrWhiteSpace(ProvinceTextBox.Text) ? null : ProvinceTextBox.Text;
                        scheduledReport.CityMunicipality = string.IsNullOrWhiteSpace(CityMunicipalityTextBox.Text) ? null : CityMunicipalityTextBox.Text;
                        scheduledReport.Barangay = string.IsNullOrWhiteSpace(BarangayTextBox.Text) ? null : BarangayTextBox.Text;
                        scheduledReport.DateFrom = DateFromPicker.SelectedDate;
                        scheduledReport.DateTo = DateToPicker.SelectedDate;
                        break;

                    case "Request Report":
                        scheduledReport.ReportType = "Request";
                        scheduledReport.ExportFormat = ((ComboBoxItem)RequestExportFormatComboBox.SelectedItem).Content.ToString();

                        // Get status
                        string status = ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString();
                        scheduledReport.DocumentType = status == "All Statuses" ? null : status; // Reusing DocumentType field for status

                        // Get date parameters
                        scheduledReport.DateFrom = RequestDateFromPicker.SelectedDate;
                        scheduledReport.DateTo = RequestDateToPicker.SelectedDate;
                        break;

                    case "Activity Report":
                        scheduledReport.ReportType = "Activity";
                        scheduledReport.ExportFormat = ((ComboBoxItem)ActivityExportFormatComboBox.SelectedItem).Content.ToString();

                        // Get activity type
                        string activityType = ((ComboBoxItem)ActivityTypeComboBox.SelectedItem).Content.ToString();
                        scheduledReport.DocumentType = activityType == "All Types" ? null : activityType; // Reusing DocumentType field for activity type

                        // Get date parameters
                        scheduledReport.DateFrom = ActivityDateFromPicker.SelectedDate;
                        scheduledReport.DateTo = ActivityDateToPicker.SelectedDate;
                        break;
                }

                // Show dialog to configure schedule
                var scheduleDialog = new ScheduleReportDialog(scheduledReport);
                if (scheduleDialog.ShowDialog() == true)
                {
                    // Save the scheduled report
                    await _reportService.AddScheduledReportAsync(scheduledReport);

                    MessageBox.Show("Report scheduled successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving scheduled report");
                MessageBox.Show($"Error saving scheduled report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<MemoryStream> GenerateDocumentReport(string exportFormat)
        {
            // Get parameters
            string documentType = ((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Content.ToString();
            documentType = documentType == "All Types" ? null : documentType;

            string registryOffice = string.IsNullOrWhiteSpace(RegistryOfficeTextBox.Text) ? null : RegistryOfficeTextBox.Text;
            string province = string.IsNullOrWhiteSpace(ProvinceTextBox.Text) ? null : ProvinceTextBox.Text;
            string cityMunicipality = string.IsNullOrWhiteSpace(CityMunicipalityTextBox.Text) ? null : CityMunicipalityTextBox.Text;
            string barangay = string.IsNullOrWhiteSpace(BarangayTextBox.Text) ? null : BarangayTextBox.Text;

            DateTime? dateFrom = DateFromPicker.SelectedDate;
            DateTime? dateTo = DateToPicker.SelectedDate;

            // Generate report
            return await _reportService.GenerateDocumentReportAsync(
                documentType: documentType,
                registryOffice: registryOffice,
                province: province,
                cityMunicipality: cityMunicipality,
                barangay: barangay,
                dateFrom: dateFrom,
                dateTo: dateTo,
                exportFormat: exportFormat);
        }

        private async System.Threading.Tasks.Task<MemoryStream> GenerateRequestReport(string exportFormat)
        {
            // Get parameters
            string status = ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString();
            status = status == "All Statuses" ? null : status;

            DateTime? dateFrom = RequestDateFromPicker.SelectedDate;
            DateTime? dateTo = RequestDateToPicker.SelectedDate;

            // Generate report
            return await _reportService.GenerateRequestReportAsync(
                status: status,
                dateFrom: dateFrom,
                dateTo: dateTo,
                exportFormat: exportFormat);
        }

        private async System.Threading.Tasks.Task<MemoryStream> GenerateActivityReport(string exportFormat)
        {
            // Get parameters
            string activityType = ((ComboBoxItem)ActivityTypeComboBox.SelectedItem).Content.ToString();
            activityType = activityType == "All Types" ? null : activityType;

            int? userId = null;
            if (UserComboBox.SelectedItem is ComboBoxItem selectedUser && selectedUser.Content.ToString() != "All Users")
            {
                userId = (int)selectedUser.Tag;
            }

            DateTime? dateFrom = ActivityDateFromPicker.SelectedDate;
            DateTime? dateTo = ActivityDateToPicker.SelectedDate;

            // Generate report
            return await _reportService.GenerateActivityReportAsync(
                activityType: activityType,
                userId: userId,
                dateFrom: dateFrom,
                dateTo: dateTo,
                exportFormat: exportFormat);
        }
    }
}
