using CivilRegistryApp.Data;
using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class RegistrationWindow : Window
    {
        private readonly IAuthenticationService _authService = null!;
        private readonly AppDbContext _dbContext = null!;
        private readonly bool _isFirstRun;

        public RegistrationWindow(IAuthenticationService authService, AppDbContext dbContext, bool isFirstRun = false)
        {
            try
            {
                Log.Debug("Initializing RegistrationWindow");
                InitializeComponent();

                _authService = authService;
                _dbContext = dbContext;
                _isFirstRun = isFirstRun;

                // If this is the first run, update the UI accordingly
                if (_isFirstRun)
                {
                    HeaderTextBlock.Text = "Create Initial Admin User";
                    RoleComboBox.IsEnabled = false; // Force Admin role for first user
                }

                Log.Information("RegistrationWindow initialized successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "RegistrationWindow.Constructor");
                MessageBox.Show($"Error initializing the registration window: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Register button clicked");

                // Clear any previous error message
                ErrorMessageTextBlock.Visibility = Visibility.Collapsed;

                // Validate input
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(FullNameTextBox.Text) ||
                    PasswordBox.SecurePassword.Length == 0 ||
                    ConfirmPasswordBox.SecurePassword.Length == 0)
                {
                    ErrorMessageTextBlock.Text = "Please fill in all fields.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    return;
                }

                // Check if passwords match
                string password = GetPasswordFromSecureString(PasswordBox.SecurePassword);
                string confirmPassword = GetPasswordFromSecureString(ConfirmPasswordBox.SecurePassword);

                if (password != confirmPassword)
                {
                    ErrorMessageTextBlock.Text = "Passwords do not match.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                    PasswordBox.Password = string.Empty;
                    ConfirmPasswordBox.Password = string.Empty;
                    return;
                }

                // Get selected role
                string role = "Staff"; // Default
                if (RoleComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    role = selectedItem.Content.ToString();
                }

                // Force Admin role for first user
                if (_isFirstRun)
                {
                    role = "Admin";
                }

                // Register the user
                string username = UsernameTextBox.Text.Trim();
                string fullName = FullNameTextBox.Text.Trim();

                bool registrationSuccess = await _authService.RegisterAsync(username, password, fullName, role);

                if (registrationSuccess)
                {
                    Log.Information("User {Username} registered successfully with role {Role}", username, role);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    Log.Warning("Registration failed for user {Username} - username may already exist", username);
                    ErrorMessageTextBlock.Text = "Username already exists. Please choose a different username.";
                    ErrorMessageTextBlock.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during user registration");
                ErrorMessageTextBlock.Text = $"An error occurred during registration: {ex.Message}";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
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
