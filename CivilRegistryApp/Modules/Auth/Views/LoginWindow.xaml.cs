using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthenticationService _authService = null!;
        private readonly AppDbContext _dbContext = null!;
        private bool _isFirstRun = false;

        public LoginWindow(IAuthenticationService authService, AppDbContext dbContext)
        {
            try
            {
                Log.Debug("Initializing LoginWindow");
                InitializeComponent();

                _authService = authService;
                _dbContext = dbContext;

                // Check if this is the first run (no users in the database)
                CheckFirstRun();

                Log.Information("LoginWindow initialized successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "LoginWindow.Constructor");
                MessageBox.Show($"Error initializing the login window: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void CheckFirstRun()
        {
            try
            {
                // Check if there are any users in the database
                _isFirstRun = !_dbContext.Users.Any();

                if (_isFirstRun)
                {
                    Log.Information("First run detected - no users in database");
                    CreateAdminButton.Visibility = Visibility.Visible;
                    CreateAdminButton.Content = "Create Initial Admin User";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking if this is the first run");
                // Don't throw - we'll assume it's not the first run
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Login button clicked");

                // Clear any previous error message
                ErrorMessageTextBlock.Visibility = Visibility.Collapsed;

                // Validate input
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) || PasswordBox.SecurePassword.Length == 0)
                {
                    ErrorMessageTextBlock.Text = "Please enter both username and password.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    return;
                }

                // Attempt login
                string username = UsernameTextBox.Text.Trim();
                string password = GetPasswordFromSecureString(PasswordBox.SecurePassword);

                bool loginSuccess = await _authService.LoginAsync(username, password);

                if (loginSuccess)
                {
                    Log.Information("User {Username} logged in successfully", username);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    Log.Warning("Login failed for user {Username}", username);
                    ErrorMessageTextBlock.Text = "Invalid username or password. Please try again.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    PasswordBox.Password = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during login attempt");
                ErrorMessageTextBlock.Text = $"An error occurred during login: {ex.Message}";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void CreateAdminButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Create admin button clicked");

                // Open the registration window
                var registrationWindow = new RegistrationWindow(_authService, _dbContext, isFirstRun: _isFirstRun);
                bool? result = registrationWindow.ShowDialog();

                if (result == true)
                {
                    // If this was the first run and an admin was created, hide the button
                    if (_isFirstRun)
                    {
                        _isFirstRun = false;
                        CreateAdminButton.Visibility = Visibility.Collapsed;
                    }

                    Log.Information("Admin user created successfully");
                    MessageBox.Show("User created successfully. You can now log in.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening registration window");
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug("Cancel button clicked");
            DialogResult = false;
            Close();
        }

        private string GetPasswordFromSecureString(System.Security.SecureString securePassword)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
