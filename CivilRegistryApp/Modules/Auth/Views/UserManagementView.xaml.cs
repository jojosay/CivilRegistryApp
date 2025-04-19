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
    public partial class UserManagementView : UserControl
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authService;
        private readonly IUserActivityService _userActivityService;

        private List<User> _allUsers = new List<User>();
        private List<User> _filteredUsers = new List<User>();
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;

        private bool _isInitialized = false;

        public UserManagementView(IUserRepository userRepository, IAuthenticationService authService, IUserActivityService userActivityService)
        {
            InitializeComponent();

            _userRepository = userRepository;
            _authService = authService;
            _userActivityService = userActivityService;

            // Check if current user has permission to manage users
            if (!_authService.HasPermission("ManageUsers"))
            {
                MessageBox.Show("You do not have permission to manage users.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Loaded += UserManagementView_Loaded;
        }

        private async void UserManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load users
                await LoadUsersAsync();

                // Populate role filter dropdown
                await PopulateRoleFilterAsync();

                // Mark as initialized
                _isInitialized = true;

                // Log activity
                await _userActivityService.LogActivityAsync(
                    "UserManagement",
                    "Accessed user management view");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "UserManagementView_Loaded");
                MessageBox.Show($"An error occurred while loading users: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                // Show loading indicator
                UsersDataGrid.IsEnabled = false;

                // Get all users
                _allUsers = (await _userRepository.GetAllAsync()).ToList();

                // Apply filter
                ApplyFilter();

                // Update pagination
                UpdatePaginationInfo();

                // Enable DataGrid
                UsersDataGrid.IsEnabled = true;

                Log.Information("Loaded {Count} users", _allUsers.Count);
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "LoadUsersAsync");
                throw;
            }
        }

        private async Task PopulateRoleFilterAsync()
        {
            try
            {
                // Clear existing items (except "All Roles")
                RoleFilterComboBox.Items.Clear();
                RoleFilterComboBox.Items.Add(new ComboBoxItem { Content = "All Roles", IsSelected = true });

                // Get all roles
                var roles = await _userRepository.GetAllRolesAsync();

                // Add roles to dropdown
                foreach (var role in roles)
                {
                    RoleFilterComboBox.Items.Add(new ComboBoxItem { Content = role });
                }

                Log.Debug("Populated role filter with {Count} roles", roles.Count());
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "PopulateRoleFilterAsync");
                throw;
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;
                string selectedRole = (RoleFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Roles";

                // Apply filters
                if (_allUsers != null)
                {
                    _filteredUsers = _allUsers.Where(u =>
                        // Role filter
                        (selectedRole == "All Roles" || u.Role == selectedRole) &&
                        // Search text filter
                        (string.IsNullOrEmpty(searchText) ||
                         u.Username.ToLower().Contains(searchText) ||
                         u.FullName.ToLower().Contains(searchText) ||
                         (u.Email?.ToLower().Contains(searchText) == true) ||
                         (u.Department?.ToLower().Contains(searchText) == true) ||
                         (u.Position?.ToLower().Contains(searchText) == true))
                    ).ToList();
                }
                else
                {
                    _filteredUsers = new List<User>();
                }

                // Calculate total pages
                _totalPages = (_filteredUsers.Count + _pageSize - 1) / _pageSize;
                if (_totalPages < 1) _totalPages = 1;

                // Ensure current page is valid
                if (_currentPage > _totalPages)
                    _currentPage = _totalPages;

                // Get current page data
                var currentPageData = _filteredUsers
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                // Update DataGrid
                UsersDataGrid.ItemsSource = currentPageData;

                Log.Debug("Applied filter. Showing {Count} of {TotalCount} users",
                    currentPageData.Count, _filteredUsers.Count);
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ApplyFilter");
                MessageBox.Show($"An error occurred while filtering users: {ex.Message}",
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

        private void RoleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            // Reset to first page
            _currentPage = 1;

            // Apply filter
            ApplyFilter();

            // Update pagination
            UpdatePaginationInfo();
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Not used currently, but can be used to enable/disable buttons based on selection
        }

        private async void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addUserView = new UserAddView(_userRepository, _authService, _userActivityService);
                var window = new Window
                {
                    Title = "Add New User",
                    Content = addUserView,
                    Width = 500,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Refresh user list
                    await LoadUsersAsync();

                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserManagement",
                        "Added a new user");
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "AddUserButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button == null)
                {
                    Log.Warning("EditUserButton_Click called with null sender or sender is not a Button");
                    return;
                }
                var user = button.DataContext as User;

                if (user == null)
                {
                    MessageBox.Show("No user selected.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var editUserView = new UserEditView(_userRepository, _authService, _userActivityService, user);
                var window = new Window
                {
                    Title = $"Edit User: {user.Username}",
                    Content = editUserView,
                    Width = 500,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Refresh user list
                    await LoadUsersAsync();

                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserManagement",
                        $"Edited user {user.Username}",
                        "User",
                        user.UserId);
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "EditUserButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button == null)
                {
                    Log.Warning("ResetPasswordButton_Click called with null sender or sender is not a Button");
                    return;
                }
                var user = button.DataContext as User;

                if (user == null)
                {
                    MessageBox.Show("No user selected.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var resetPasswordView = new ResetPasswordView(_userRepository, _authService, _userActivityService, user);
                var window = new Window
                {
                    Title = $"Reset Password: {user.Username}",
                    Content = resetPasswordView,
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserManagement",
                        $"Reset password for user {user.Username}",
                        "User",
                        user.UserId);
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ResetPasswordButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ToggleActivationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button == null)
                {
                    Log.Warning("ToggleActivationButton_Click called with null sender or sender is not a Button");
                    return;
                }
                var user = button.DataContext as User;

                if (user == null)
                {
                    MessageBox.Show("No user selected.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirm action
                string action = user.IsActive ? "deactivate" : "activate";
                var result = MessageBox.Show($"Are you sure you want to {action} user {user.Username}?",
                    "Confirm Action", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Toggle activation
                bool success;
                if (user.IsActive)
                {
                    success = await _userRepository.DeactivateUserAsync(user.UserId);
                }
                else
                {
                    success = await _userRepository.ActivateUserAsync(user.UserId);
                }

                if (success)
                {
                    // Refresh user list
                    await LoadUsersAsync();

                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserManagement",
                        $"{(user.IsActive ? "Deactivated" : "Activated")} user {user.Username}",
                        "User",
                        user.UserId);

                    MessageBox.Show($"User {user.Username} has been {(user.IsActive ? "deactivated" : "activated")}.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to {action} user {user.Username}.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ToggleActivationButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
