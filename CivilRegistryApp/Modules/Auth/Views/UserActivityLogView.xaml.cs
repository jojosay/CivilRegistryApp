using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class UserActivityLogView : UserControl
    {
        private readonly IUserActivityService _userActivityService;
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authService;

        private List<UserActivity> _allActivities = new List<UserActivity>();
        private List<UserActivity> _filteredActivities = new List<UserActivity>();
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalPages = 1;

        private bool _isInitialized = false;

        public UserActivityLogView(IUserActivityService userActivityService, IUserRepository userRepository, IAuthenticationService authService)
        {
            InitializeComponent();

            _userActivityService = userActivityService;
            _userRepository = userRepository;
            _authService = authService;

            // Check if current user has permission to view activity logs
            if (!_authService.IsUserInRole("Admin"))
            {
                MessageBox.Show("You do not have permission to view activity logs.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Loaded += UserActivityLogView_Loaded;
        }

        private async void UserActivityLogView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set default date range (last 30 days)
                EndDatePicker.SelectedDate = DateTime.Today;
                StartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);

                // Load activity types
                await PopulateActivityTypesAsync();

                // Load users for filter
                await PopulateUserFilterAsync();

                // Load activities
                await LoadActivitiesAsync();

                // Mark as initialized
                _isInitialized = true;

                // Log this activity
                await _userActivityService.LogActivityAsync(
                    "Admin",
                    "Viewed activity logs",
                    "UserInterface");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "UserActivityLogView_Loaded");
                MessageBox.Show($"An error occurred while loading activity logs: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadActivitiesAsync()
        {
            try
            {
                // Show loading indicator
                LoadingIndicator.Visibility = Visibility.Visible;
                ActivityLogDataGrid.Visibility = Visibility.Collapsed;
                NoDataTextBlock.Visibility = Visibility.Collapsed;

                // Get filter values
                string activityType = (ActivityTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (activityType == "All Activities") activityType = null;

                string username = (UserFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (username == "All Users") username = null;

                try
                {
                    // Get activities
                    var activities = await _userActivityService.GetAllActivitiesAsync(
                        StartDatePicker.SelectedDate,
                        EndDatePicker.SelectedDate?.AddDays(1), // Include the end date
                        activityType,
                        username);

                    _allActivities = activities?.ToList() ?? new List<UserActivity>();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error loading activities");
                    _allActivities = new List<UserActivity>();
                    MessageBox.Show("There was an error loading the activity logs. Please try again later.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Apply filter
                ApplyFilter();

                // Update pagination
                UpdatePaginationInfo();

                // Hide loading indicator and show data
                LoadingIndicator.Visibility = Visibility.Collapsed;
                ActivityLogDataGrid.Visibility = Visibility.Visible;

                // Show "No Data" message if there are no activities
                if (_filteredActivities.Count == 0)
                {
                    NoDataTextBlock.Visibility = Visibility.Visible;
                    ActivityLogDataGrid.Visibility = Visibility.Collapsed;
                }

                Log.Information("Loaded {Count} activities", _allActivities.Count);
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "LoadActivitiesAsync");
                throw;
            }
        }

        private async Task PopulateActivityTypesAsync()
        {
            try
            {
                // Clear existing items (except "All Activities")
                ActivityTypeComboBox.Items.Clear();
                ActivityTypeComboBox.Items.Add(new ComboBoxItem { Content = "All Activities", IsSelected = true });

                // Get all activity types
                var activityTypes = await _userActivityService.GetActivityTypesAsync();

                // Add activity types to dropdown
                foreach (var type in activityTypes)
                {
                    ActivityTypeComboBox.Items.Add(new ComboBoxItem { Content = type });
                }

                Log.Debug("Populated activity type filter with {Count} types", activityTypes.Count());
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "PopulateActivityTypesAsync");
                throw;
            }
        }

        private async Task PopulateUserFilterAsync()
        {
            try
            {
                // Clear existing items (except "All Users")
                UserFilterComboBox.Items.Clear();
                UserFilterComboBox.Items.Add(new ComboBoxItem { Content = "All Users", IsSelected = true });

                // Get all users
                var users = await _userRepository.GetAllAsync();

                // Add users to dropdown
                foreach (var user in users.OrderBy(u => u.Username))
                {
                    UserFilterComboBox.Items.Add(new ComboBoxItem { Content = user.Username });
                }

                Log.Debug("Populated user filter with {Count} users", users.Count());
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "PopulateUserFilterAsync");
                throw;
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;

                // Apply search filter
                if (_allActivities != null)
                {
                    _filteredActivities = _allActivities.Where(a =>
                        string.IsNullOrEmpty(searchText) ||
                        (a.User?.Username?.ToLower()?.Contains(searchText) ?? false) ||
                        (a.Description?.ToLower()?.Contains(searchText) ?? false) ||
                        (a.ActivityType?.ToLower()?.Contains(searchText) ?? false)
                    ).ToList();
                }
                else
                {
                    _filteredActivities = new List<UserActivity>();
                }

                // Calculate total pages
                _totalPages = (_filteredActivities.Count + _pageSize - 1) / _pageSize;
                if (_totalPages < 1) _totalPages = 1;

                // Ensure current page is valid
                if (_currentPage > _totalPages)
                    _currentPage = _totalPages;

                // Get current page data
                var currentPageData = _filteredActivities
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                // Update DataGrid
                ActivityLogDataGrid.ItemsSource = currentPageData;

                Log.Debug("Applied filter. Showing {Count} of {TotalCount} activities",
                    currentPageData.Count, _filteredActivities.Count);
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ApplyFilter");
                MessageBox.Show($"An error occurred while filtering activities: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePaginationInfo()
        {
            PaginationInfoTextBlock.Text = $"Page {_currentPage} of {_totalPages}";

            // Enable/disable pagination buttons
            FirstPageButton.IsEnabled = _currentPage > 1;
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            LastPageButton.IsEnabled = _currentPage < _totalPages;
        }

        #region Event Handlers

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            // Reset to first page
            _currentPage = 1;

            // Apply filter
            ApplyFilter();

            // Update pagination
            UpdatePaginationInfo();
        }

        private async void ActivityTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            // Reset to first page
            _currentPage = 1;

            // Reload activities with new filter
            await LoadActivitiesAsync();
        }

        private async void UserFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            // Reset to first page
            _currentPage = 1;

            // Reload activities with new filter
            await LoadActivitiesAsync();
        }

        private async void DatePicker_SelectedDateChanged(object sender, DateTime? selectedDate)
        {
            if (!_isInitialized) return;

            // Validate date range
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
                {
                    MessageBox.Show("Start date cannot be after end date.",
                        "Invalid Date Range", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Reset to valid range
                    if (sender == StartDatePicker)
                    {
                        StartDatePicker.SelectedDate = EndDatePicker.SelectedDate.Value.AddDays(-30);
                    }
                    else
                    {
                        EndDatePicker.SelectedDate = StartDatePicker.SelectedDate.Value.AddDays(30);
                    }
                }
            }

            // Reset to first page
            _currentPage = 1;

            // Reload activities with new date range
            await LoadActivitiesAsync();
        }

        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage = 1;
                ApplyFilter();
                UpdatePaginationInfo();
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyFilter();
                UpdatePaginationInfo();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                ApplyFilter();
                UpdatePaginationInfo();
            }
        }

        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage = _totalPages;
                ApplyFilter();
                UpdatePaginationInfo();
            }
        }

        #endregion
    }
}
