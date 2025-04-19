using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class UserEditView : UserControl
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authService;
        private readonly IUserActivityService _userActivityService;
        private readonly User _user;

        public UserEditView(IUserRepository userRepository, IAuthenticationService authService, IUserActivityService userActivityService, User user)
        {
            InitializeComponent();

            _userRepository = userRepository;
            _authService = authService;
            _userActivityService = userActivityService;
            _user = user;

            Loaded += UserEditView_Loaded;
        }

        private void UserEditView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Load user data
                LoadUserData();

                Log.Debug("Loaded user data for editing: {Username}", _user.Username);
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "UserEditView_Loaded");
                MessageBox.Show($"An error occurred while loading user data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserData()
        {
            try
            {
                // Set text fields
                UsernameTextBox.Text = _user.Username;
                FullNameTextBox.Text = _user.FullName;
                EmailTextBox.Text = _user.Email;
                PhoneNumberTextBox.Text = _user.PhoneNumber;
                DepartmentTextBox.Text = _user.Department;
                PositionTextBox.Text = _user.Position;

                // Set role
                var roleItem = RoleComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Content.ToString() == _user.Role);
                if (roleItem != null)
                {
                    roleItem.IsSelected = true;
                }

                // Set active status
                IsActiveCheckBox.IsChecked = _user.IsActive;

                // Set account information
                CreatedAtTextBox.Text = _user.CreatedAt.ToString("MM/dd/yyyy hh:mm tt");
                LastLoginTextBox.Text = _user.LastLoginAt?.ToString("MM/dd/yyyy hh:mm tt") ?? "Never";
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "LoadUserData");
                throw;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                {
                    MessageBox.Show("Full Name is required.",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FullNameTextBox.Focus();
                    return;
                }

                // Get selected role
                string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? _user.Role;

                // Update user object
                _user.FullName = FullNameTextBox.Text;
                _user.Email = EmailTextBox.Text;
                _user.PhoneNumber = PhoneNumberTextBox.Text;
                _user.Department = DepartmentTextBox.Text;
                _user.Position = PositionTextBox.Text;
                _user.Role = role;
                _user.IsActive = IsActiveCheckBox.IsChecked ?? false;

                // Save changes
                await _userRepository.UpdateAsync(_user);
                await _userRepository.SaveChangesAsync();
                bool success = true;

                if (success)
                {
                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserManagement",
                        $"Updated user {_user.Username}",
                        "User",
                        _user.UserId);

                    MessageBox.Show("User updated successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Close the window
                    Window.GetWindow(this).DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Failed to update user.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "SaveButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the window without saving
            Window.GetWindow(this).DialogResult = false;
        }
    }
}
