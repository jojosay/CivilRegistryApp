using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Auth;
using CivilRegistryApp.Modules.Documents;
using CivilRegistryApp.Modules.Requests;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Serilog;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CivilRegistryApp.Modules.Dashboard.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private readonly IDocumentService _documentService = null!;
        private readonly IRequestService _requestService = null!;
        private readonly IAuthenticationService _authService = null!;
        private readonly AppDbContext _dbContext = null!;

        // Chart data
        public ISeries[] DocumentTypeSeries { get; set; } = Array.Empty<ISeries>();
        public ISeries[] MonthlyDocumentsSeries { get; set; } = Array.Empty<ISeries>();
        public Axis[] MonthlyDocumentsXAxes { get; set; } = Array.Empty<Axis>();

        // Activity feed data
        public ObservableCollection<ActivityItem> ActivityItems { get; set; } = new ObservableCollection<ActivityItem>();

        public DashboardView(IDocumentService documentService, IRequestService requestService, IAuthenticationService authService, AppDbContext dbContext)
        {
            try
            {
                Log.Debug("Initializing DashboardView");
                InitializeComponent();

                _documentService = documentService;
                _requestService = requestService;
                _authService = authService;
                _dbContext = dbContext;

                // Initialize activity feed
                ActivityItems = new ObservableCollection<ActivityItem>();
                ActivityFeedItemsControl.ItemsSource = ActivityItems;

                // Set DataContext for binding
                this.DataContext = this;

                Log.Information("DashboardView initialized successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DashboardView.Constructor");
                MessageBox.Show($"Error initializing dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("DashboardView loaded, refreshing data");
                await RefreshDashboardAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in DashboardView_Loaded");
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Refresh button clicked");
                await RefreshDashboardAsync();
                MessageBox.Show("Dashboard refreshed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in RefreshButton_Click");
                MessageBox.Show($"Error refreshing dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshDashboardAsync()
        {
            try
            {
                Log.Debug("Refreshing dashboard data");

                // Update statistics widgets
                await UpdateStatisticsAsync();

                // Update charts
                await UpdateChartsAsync();

                // Update activity feed
                await UpdateActivityFeedAsync();

                Log.Information("Dashboard data refreshed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error refreshing dashboard data");
                throw;
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                Log.Debug("Updating dashboard statistics");

                // Get all documents
                var documents = await _documentService.GetAllDocumentsAsync();
                int totalDocuments = documents.Count();
                TotalDocumentsTextBlock.Text = totalDocuments.ToString();

                // Get documents added this month
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                int monthlyDocuments = documents.Count(d => d.UploadedAt.Month == currentMonth && d.UploadedAt.Year == currentYear);
                MonthlyDocumentsTextBlock.Text = monthlyDocuments.ToString();

                // Get all requests
                var requests = await _requestService.GetAllRequestsAsync();

                // Get pending requests
                int pendingRequests = requests.Count(r => r.Status == "Pending");
                PendingRequestsTextBlock.Text = pendingRequests.ToString();

                // Get completed requests
                int completedRequests = requests.Count(r => r.Status == "Completed");
                CompletedRequestsTextBlock.Text = completedRequests.ToString();

                Log.Information("Dashboard statistics updated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating dashboard statistics");
                throw;
            }
        }

        private async Task UpdateChartsAsync()
        {
            try
            {
                Log.Debug("Updating dashboard charts");

                // Get all documents
                var documents = await _documentService.GetAllDocumentsAsync();

                // Update Documents by Type chart
                UpdateDocumentsByTypeChart(documents);

                // Update Monthly Documents chart
                UpdateMonthlyDocumentsChart(documents);

                Log.Information("Dashboard charts updated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating dashboard charts");
                throw;
            }
        }

        private void UpdateDocumentsByTypeChart(IEnumerable<Document> documents)
        {
            try
            {
                Log.Debug("Updating Documents by Type chart");

                // Group documents by type and count
                var documentsByType = documents
                    .GroupBy(d => d.DocumentType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // Create pie chart series
                var series = new List<PieSeries<int>>();
                var colors = new List<SKColor>
                {
                    SKColors.DodgerBlue,
                    SKColors.OrangeRed,
                    SKColors.MediumSeaGreen,
                    SKColors.Gold,
                    SKColors.MediumPurple
                };

                for (int i = 0; i < documentsByType.Count; i++)
                {
                    var item = documentsByType[i];
                    var colorIndex = i % colors.Count;

                    series.Add(new PieSeries<int>
                    {
                        Values = new[] { item.Count },
                        Name = $"{item.Type} ({item.Count})",
                        Fill = new SolidColorPaint(colors[colorIndex]),
                        Stroke = null,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}"
                    });
                }

                DocumentTypeSeries = series.ToArray();
                DocumentsByTypeChart.Series = DocumentTypeSeries;

                Log.Information("Documents by Type chart updated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating Documents by Type chart");
                throw;
            }
        }

        private void UpdateMonthlyDocumentsChart(IEnumerable<Document> documents)
        {
            try
            {
                Log.Debug("Updating Monthly Documents chart");

                // Get the last 6 months
                var today = DateTime.Today;
                var months = Enumerable.Range(0, 6)
                    .Select(i => today.AddMonths(-i))
                    .Select(date => new { Month = date.Month, Year = date.Year, Label = date.ToString("MMM yyyy") })
                    .Reverse()
                    .ToList();

                // Count documents per month
                var documentCounts = new List<int>();
                foreach (var month in months)
                {
                    int count = documents.Count(d => d.UploadedAt.Month == month.Month && d.UploadedAt.Year == month.Year);
                    documentCounts.Add(count);
                }

                // Create column chart series
                var series = new List<ISeries>
                {
                    new ColumnSeries<int>
                    {
                        Values = documentCounts.ToArray(),
                        Fill = new SolidColorPaint(SKColors.DodgerBlue),
                        Stroke = null,
                        DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                        DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}"
                    }
                };

                // Create X axis with month labels
                var xAxes = new List<Axis>
                {
                    new Axis
                    {
                        Labels = months.Select(m => m.Label).ToArray(),
                        LabelsRotation = 45
                    }
                };

                MonthlyDocumentsSeries = series.ToArray();
                MonthlyDocumentsXAxes = xAxes.ToArray();

                MonthlyDocumentsChart.Series = MonthlyDocumentsSeries;
                MonthlyDocumentsChart.XAxes = MonthlyDocumentsXAxes;

                Log.Information("Monthly Documents chart updated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating Monthly Documents chart");
                throw;
            }
        }

        private async Task UpdateActivityFeedAsync()
        {
            try
            {
                Log.Debug("Updating activity feed");

                // Clear existing items
                ActivityItems.Clear();

                // Get recent documents (last 10)
                var recentDocuments = await _documentService.GetAllDocumentsAsync();
                var documents = recentDocuments
                    .OrderByDescending(d => d.UploadedAt)
                    .Take(5)
                    .ToList();

                // Get recent requests (last 10)
                var recentRequests = await _requestService.GetAllRequestsAsync();
                var requests = recentRequests
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .ToList();

                // Combine and sort by date
                var combinedActivities = new List<ActivityItem>();

                // Add documents to activity feed
                foreach (var doc in documents)
                {
                    combinedActivities.Add(new ActivityItem
                    {
                        IconText = "D",
                        IconBackground = new SolidColorBrush(Colors.Green),
                        Description = $"Document added: {doc.DocumentType} - {doc.CertificateNumber} by {doc.UploadedByUser?.FullName ?? "Unknown"}",
                        Timestamp = doc.UploadedAt.ToString("MMM dd, yyyy HH:mm"),
                        ActivityDate = doc.UploadedAt
                    });
                }

                // Add requests to activity feed
                foreach (var req in requests)
                {
                    combinedActivities.Add(new ActivityItem
                    {
                        IconText = "R",
                        IconBackground = new SolidColorBrush(Colors.Blue),
                        Description = $"Request {req.Status.ToLower()}: {req.RequestorName} for {req.RelatedDocument?.DocumentType ?? "Unknown Document"}",
                        Timestamp = req.RequestDate.ToString("MMM dd, yyyy HH:mm"),
                        ActivityDate = req.RequestDate
                    });
                }

                // Sort by date (newest first) and take top 10
                var sortedActivities = combinedActivities
                    .OrderByDescending(a => a.ActivityDate)
                    .Take(10)
                    .ToList();

                // Add to observable collection
                foreach (var activity in sortedActivities)
                {
                    ActivityItems.Add(activity);
                }

                Log.Information("Activity feed updated successfully with {Count} items", ActivityItems.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating activity feed");
                throw;
            }
        }
    }

    public class ActivityItem
    {
        public string IconText { get; set; } = string.Empty;
        public Brush IconBackground { get; set; } = Brushes.Gray;
        public string Description { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; } = DateTime.Now;
    }
}
