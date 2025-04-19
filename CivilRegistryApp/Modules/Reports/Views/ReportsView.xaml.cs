using CivilRegistryApp.Data;
using CivilRegistryApp.Modules.Auth;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CivilRegistryApp.Modules.Reports.Views
{
    /// <summary>
    /// Interaction logic for ReportsView.xaml
    /// </summary>
    public partial class ReportsView : System.Windows.Controls.UserControl
    {
        private readonly IReportService _reportService = null!;
        private readonly IAuthenticationService _authService = null!;
        private readonly AppDbContext _dbContext = null!;

        public ReportsView(IReportService reportService, IAuthenticationService authService, AppDbContext dbContext)
        {
            try
            {
                Log.Debug("Initializing ReportsView");
                InitializeComponent();

                _reportService = reportService;
                _authService = authService;
                _dbContext = dbContext;

                // Initialize child views
                ReportGeneratorView.Initialize(_reportService, _authService, _dbContext);
                ScheduledReportsView.Initialize(_reportService, _authService, _dbContext);

                Log.Information("ReportsView initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ReportsView");
                MessageBox.Show($"Error initializing Reports view: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReportsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.Source is System.Windows.Controls.TabControl tabControl)
                {
                    if (tabControl.SelectedItem is TabItem selectedTab)
                    {
                        Log.Debug("Selected tab: {TabHeader}", selectedTab.Header);

                        // Refresh the selected tab's data
                        if (selectedTab.Header.ToString() == "Generate Reports")
                        {
                            ReportGeneratorView.RefreshData();
                        }
                        else if (selectedTab.Header.ToString() == "Scheduled Reports")
                        {
                            ScheduledReportsView.RefreshData();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ReportsTabControl_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
