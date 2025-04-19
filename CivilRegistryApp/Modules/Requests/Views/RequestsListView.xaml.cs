using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Modules.Auth;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace CivilRegistryApp.Modules.Requests.Views
{
    public partial class RequestsListView : UserControl
    {
        private readonly IRequestService _requestService;
        private readonly IAuthenticationService _authService;
        private IEnumerable<DocumentRequest> _allRequests = new List<DocumentRequest>();
        private IEnumerable<DocumentRequest> _filteredRequests = new List<DocumentRequest>();
        private int _currentPage = 1;
        private const int _itemsPerPage = 20;

        public RequestsListView(IRequestService requestService, IAuthenticationService authService)
        {
            try
            {
                InitializeComponent();
                _requestService = requestService ?? throw new ArgumentNullException(nameof(requestService));
                _authService = authService ?? throw new ArgumentNullException(nameof(authService));

                // Initialize collections
                _allRequests = new List<DocumentRequest>();
                _filteredRequests = new List<DocumentRequest>();

                // Initialize UI
                if (RequestsDataGrid != null)
                {
                    RequestsDataGrid.ItemsSource = new List<DocumentRequest>();
                }

                if (StatusComboBox != null && StatusComboBox.Items.Count > 0)
                {
                    StatusComboBox.SelectedIndex = 0; // Select "All Requests"
                }

                // Load requests when the control is loaded
                Loaded += (s, e) =>
                {
                    try
                    {
                        LoadRequestsAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error in Loaded event handler");
                    }
                };

                Log.Information("RequestsListView initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing RequestsListView");
                MessageBox.Show($"An error occurred while initializing the requests view: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadRequestsAsync()
        {
            try
            {
                // Show loading indicator or disable UI if needed
                // You could add a loading indicator here

                // Get all requests
                var requests = await _requestService.GetAllRequestsAsync();
                _allRequests = requests ?? new List<DocumentRequest>();

                // Apply current filter
                ApplyFilter();

                // Update pagination
                UpdatePagination();

                Log.Information("Loaded {Count} document requests", _allRequests.Count());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading document requests");

                // Initialize with empty lists if there was an error
                _allRequests = new List<DocumentRequest>();
                _filteredRequests = new List<DocumentRequest>();

                // Update UI with empty data
                if (RequestsDataGrid != null)
                {
                    RequestsDataGrid.ItemsSource = new List<DocumentRequest>();
                }

                // Update pagination
                UpdatePagination();

                // Show error message
                MessageBox.Show($"An error occurred while loading requests: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Hide loading indicator or enable UI if needed
            }
        }

        private void ApplyFilter()
        {
            try
            {
                // Ensure _allRequests is not null
                if (_allRequests == null)
                {
                    _allRequests = new List<DocumentRequest>();
                }

                // Get the selected status filter
                string statusFilter = "All Requests";
                if (StatusComboBox?.SelectedItem != null)
                {
                    statusFilter = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Requests";
                }

                // Get the search text
                string searchText = SearchTextBox?.Text?.Trim().ToLower() ?? string.Empty;

                // Apply filters
                _filteredRequests = _allRequests.ToList(); // Create a new list to avoid reference issues

                // Apply status filter if not "All Requests"
                if (statusFilter != "All Requests")
                {
                    _filteredRequests = _filteredRequests.Where(r => r != null && r.Status == statusFilter).ToList();
                }

                // Apply search filter if not empty
                if (!string.IsNullOrEmpty(searchText))
                {
                    _filteredRequests = _filteredRequests.Where(r =>
                        r != null && (
                            (r.RequestorName != null && r.RequestorName.ToLower().Contains(searchText)) ||
                            (r.Purpose != null && r.Purpose.ToLower().Contains(searchText)) ||
                            r.RelatedDocumentId.ToString().Contains(searchText)
                        )
                    ).ToList();
                }

                // Update the DataGrid
                UpdateDataGrid();

                Log.Debug("Applied filters: Status={Status}, Search={Search}, Filtered count={Count}",
                    statusFilter, searchText, _filteredRequests.Count());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error applying filters");
                // Don't throw - we want the application to continue even if filtering fails
                // Initialize with empty lists if there was an error
                _filteredRequests = new List<DocumentRequest>();
            }
        }

        private void UpdateDataGrid()
        {
            try
            {
                // Ensure _filteredRequests is not null
                if (_filteredRequests == null)
                {
                    _filteredRequests = new List<DocumentRequest>();
                }

                // Ensure current page is valid
                if (_currentPage < 1)
                {
                    _currentPage = 1;
                }

                // Calculate the items for the current page
                var pagedRequests = _filteredRequests
                    .Skip((_currentPage - 1) * _itemsPerPage)
                    .Take(_itemsPerPage)
                    .ToList();

                // Update the DataGrid
                RequestsDataGrid.ItemsSource = pagedRequests;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating data grid");
                // Set an empty list if there was an error
                RequestsDataGrid.ItemsSource = new List<DocumentRequest>();
            }
        }

        private void UpdatePagination()
        {
            try
            {
                // Ensure _filteredRequests is not null
                if (_filteredRequests == null)
                {
                    _filteredRequests = new List<DocumentRequest>();
                }

                int totalItems = _filteredRequests.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / _itemsPerPage);

                // Ensure current page is valid
                if (_currentPage > totalPages && totalPages > 0)
                {
                    _currentPage = totalPages;
                }
                else if (_currentPage < 1)
                {
                    _currentPage = 1;
                }

                // Update page info text
                if (PageInfoTextBlock != null)
                {
                    PageInfoTextBlock.Text = $"Page {_currentPage} of {Math.Max(1, totalPages)}";
                }

                // Enable/disable navigation buttons
                if (PreviousPageButton != null)
                {
                    PreviousPageButton.IsEnabled = _currentPage > 1;
                }

                if (NextPageButton != null)
                {
                    NextPageButton.IsEnabled = _currentPage < totalPages;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating pagination");
                // Don't throw - we want the application to continue even if pagination fails
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Search button clicked with text: {SearchText}", SearchTextBox.Text);

                // Reset to first page when searching
                _currentPage = 1;

                // Apply filters
                ApplyFilter();

                // Update pagination
                UpdatePagination();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SearchButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = StatusComboBox.SelectedItem as ComboBoxItem;
                Log.Debug("Status filter changed to: {Status}", selectedItem?.Content);

                // Reset to first page when changing filter
                _currentPage = 1;

                // Apply filters
                ApplyFilter();

                // Update pagination
                UpdatePagination();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in StatusComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle selection changed if needed
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button == null)
                {
                    Log.Warning("ViewButton_Click called with null sender or sender is not a Button");
                    return;
                }
                var request = button.DataContext as DocumentRequest;

                if (request != null)
                {
                    Log.Information("Viewing request details for request ID: {RequestId}", request.RequestId);

                    // Open request details view
                    var requestDetailsView = new RequestDetailsView(_requestService, request.RequestId);
                    var window = new Window
                    {
                        Title = $"Request Details - ID: {request.RequestId}",
                        Content = requestDetailsView,
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    bool? result = window.ShowDialog();

                    if (result == true)
                    {
                        // Refresh the request list if changes were made
                        LoadRequestsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ViewButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button == null)
                {
                    Log.Warning("ProcessButton_Click called with null sender or sender is not a Button");
                    return;
                }
                var request = button.DataContext as DocumentRequest;

                if (request != null)
                {
                    Log.Information("Processing request ID: {RequestId}", request.RequestId);

                    // Open request details view in processing mode
                    var requestDetailsView = new RequestDetailsView(_requestService, request.RequestId, true);
                    var window = new Window
                    {
                        Title = $"Process Request - ID: {request.RequestId}",
                        Content = requestDetailsView,
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    bool? result = window.ShowDialog();

                    if (result == true)
                    {
                        // Refresh the request list
                        LoadRequestsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ProcessButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddRequestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Adding new document request");

                // Open request form view
                var requestFormView = new RequestFormView(_requestService, _authService);
                var window = new Window
                {
                    Title = "New Document Request",
                    Content = requestFormView,
                    Width = 700,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Refresh the request list
                    LoadRequestsAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AddRequestButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdateDataGrid();
                UpdatePagination();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            int totalItems = _filteredRequests?.Count() ?? 0;
            int totalPages = (int)Math.Ceiling((double)totalItems / _itemsPerPage);

            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdateDataGrid();
                UpdatePagination();
            }
        }
    }


}
