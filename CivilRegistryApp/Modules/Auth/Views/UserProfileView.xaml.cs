using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure.Logging;
using Microsoft.Win32;
using Serilog;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class UserProfileView : UserControl
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserActivityService _userActivityService;
        private User _currentUser = null!;
        private string _selectedProfilePicturePath = string.Empty;
        private bool _isProfilePictureChanged = false;

        public UserProfileView(IAuthenticationService authService, IUserActivityService userActivityService)
        {
            InitializeComponent();

            _authService = authService;
            _userActivityService = userActivityService;

            Loaded += UserProfileView_Loaded;
        }

        private async void UserProfileView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get current user
                _currentUser = _authService.CurrentUser;

                if (_currentUser == null)
                {
                    MessageBox.Show("You must be logged in to view your profile.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Load user data
                LoadUserData();

                // Log activity
                await _userActivityService.LogActivityAsync(
                    "UserProfile",
                    "Viewed user profile");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "UserProfileView_Loaded");
                MessageBox.Show($"An error occurred while loading profile: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserData()
        {
            try
            {
                // Set text fields
                UsernameTextBox.Text = _currentUser.Username;
                FullNameTextBox.Text = _currentUser.FullName;
                EmailTextBox.Text = _currentUser.Email;
                PhoneNumberTextBox.Text = _currentUser.PhoneNumber;
                DepartmentTextBox.Text = _currentUser.Department;
                PositionTextBox.Text = _currentUser.Position;
                RoleTextBox.Text = _currentUser.RoleDisplay;

                // Set account information
                CreatedAtTextBox.Text = _currentUser.CreatedAt.ToString("MM/dd/yyyy hh:mm tt");
                LastLoginTextBox.Text = _currentUser.LastLoginAt?.ToString("MM/dd/yyyy hh:mm tt") ?? "Never";
                LastPasswordChangeTextBox.Text = _currentUser.LastPasswordChangeAt?.ToString("MM/dd/yyyy hh:mm tt") ?? "Never";
                AccountStatusTextBox.Text = _currentUser.IsActive ? "Active" : "Inactive";

                // Load profile picture if available
                if (!string.IsNullOrEmpty(_currentUser.ProfilePicturePath) && File.Exists(_currentUser.ProfilePicturePath))
                {
                    LoadProfilePicture(_currentUser.ProfilePicturePath);
                    _selectedProfilePicturePath = _currentUser.ProfilePicturePath;
                }

                Log.Debug("Loaded profile data for user {Username}", _currentUser.Username);
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "LoadUserData");
                throw;
            }
        }

        private void LoadProfilePicture(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ProfileImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading profile picture from {Path}", path);
                // Don't throw - just log the error
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

                // Update user object
                _currentUser.FullName = FullNameTextBox.Text;
                _currentUser.Email = EmailTextBox.Text;
                _currentUser.PhoneNumber = PhoneNumberTextBox.Text;
                _currentUser.Department = DepartmentTextBox.Text;
                _currentUser.Position = PositionTextBox.Text;

                // Handle profile picture
                if (_isProfilePictureChanged && !string.IsNullOrEmpty(_selectedProfilePicturePath))
                {
                    // Copy the selected image to the application's profile pictures directory
                    string profilePicturesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfilePictures");

                    // Create directory if it doesn't exist
                    if (!Directory.Exists(profilePicturesDir))
                    {
                        Directory.CreateDirectory(profilePicturesDir);
                    }

                    // Generate a unique filename
                    string fileName = $"{_currentUser.UserId}_{DateTime.Now.Ticks}{Path.GetExtension(_selectedProfilePicturePath)}";
                    string destinationPath = Path.Combine(profilePicturesDir, fileName);

                    // Copy the file
                    File.Copy(_selectedProfilePicturePath, destinationPath, true);

                    // Update user object
                    _currentUser.ProfilePicturePath = destinationPath;
                }

                // Save changes
                bool success = await _authService.UpdateUserProfileAsync(_currentUser);

                if (success)
                {
                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserProfile",
                        "Updated user profile");

                    MessageBox.Show("Profile updated successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Close the window
                    Window.GetWindow(this).DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Failed to update profile.",
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

        private void ChangeProfilePictureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create OpenFileDialog
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Profile Picture",
                    Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                // Show dialog
                if (openFileDialog.ShowDialog() == true)
                {
                    // Get selected file path
                    _selectedProfilePicturePath = openFileDialog.FileName;

                    // Load and display the image
                    LoadProfilePicture(_selectedProfilePicturePath);

                    // Mark as changed
                    _isProfilePictureChanged = true;
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ChangeProfilePictureButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var changePasswordView = new ChangePasswordView(_authService, _userActivityService);
                var window = new Window
                {
                    Title = "Change Password",
                    Content = changePasswordView,
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Refresh last password change date
                    LastPasswordChangeTextBox.Text = _currentUser.LastPasswordChangeAt?.ToString("MM/dd/yyyy hh:mm tt") ?? "Never";

                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserProfile",
                        "Changed password");
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ChangePasswordButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
