using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class ResetPasswordView : UserControl
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authService;
        private readonly IUserActivityService _userActivityService;
        private readonly User _user;
        private string? _generatedPassword;

        public ResetPasswordView(IUserRepository userRepository, IAuthenticationService authService, IUserActivityService userActivityService, User user)
        {
            InitializeComponent();

            _userRepository = userRepository;
            _authService = authService;
            _userActivityService = userActivityService;
            _user = user;

            // Set user info
            UserInfoTextBlock.Text = $"Resetting password for: {_user.Username} ({_user.FullName})";

            // Add event handlers for password validation
            NewPasswordBox.PasswordChanged += NewPasswordBox_PasswordChanged;
            ConfirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Check password strength
            string password = NewPasswordBox.Password;

            if (string.IsNullOrEmpty(password))
            {
                PasswordStrengthText.Text = "Password strength: Not entered";
                PasswordStrengthText.Foreground = Brushes.Black;
                return;
            }

            int strength = CalculatePasswordStrength(password);

            if (strength < 3)
            {
                PasswordStrengthText.Text = "Password strength: Weak";
                PasswordStrengthText.Foreground = Brushes.Red;
            }
            else if (strength < 5)
            {
                PasswordStrengthText.Text = "Password strength: Medium";
                PasswordStrengthText.Foreground = Brushes.Orange;
            }
            else
            {
                PasswordStrengthText.Text = "Password strength: Strong";
                PasswordStrengthText.Foreground = Brushes.Green;
            }

            // Check if passwords match
            CheckPasswordsMatch();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Check if passwords match
            CheckPasswordsMatch();
        }

        private void CheckPasswordsMatch()
        {
            if (string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                PasswordMatchText.Visibility = Visibility.Collapsed;
                return;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                PasswordMatchText.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordMatchText.Visibility = Visibility.Collapsed;
            }
        }

        private int CalculatePasswordStrength(string password)
        {
            int strength = 0;

            // Length check
            if (password.Length >= 8)
                strength++;
            if (password.Length >= 12)
                strength++;

            // Contains uppercase
            if (Regex.IsMatch(password, @"[A-Z]"))
                strength++;

            // Contains lowercase
            if (Regex.IsMatch(password, @"[a-z]"))
                strength++;

            // Contains numbers
            if (Regex.IsMatch(password, @"[0-9]"))
                strength++;

            // Contains special characters
            if (Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
                strength++;

            return strength;
        }

        private string GenerateRandomPassword(int length = 12)
        {
            const string upperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijkmnopqrstuvwxyz";
            const string numberChars = "23456789";
            const string specialChars = "!@#$%^&*()_-+=<>?";

            var random = new Random();
            var password = new StringBuilder();

            // Ensure at least one of each type
            password.Append(upperChars[random.Next(upperChars.Length)]);
            password.Append(lowerChars[random.Next(lowerChars.Length)]);
            password.Append(numberChars[random.Next(numberChars.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            // Fill the rest with random characters
            var allChars = upperChars + lowerChars + numberChars + specialChars;
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            return new string(password.ToString().OrderBy(c => random.Next()).ToArray());
        }

        private void GenerateRandomPasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Generate a random password
            _generatedPassword = GenerateRandomPassword();

            // Display the generated password
            GeneratedPasswordTextBlock.Text = $"Generated password: {_generatedPassword}";
            GeneratedPasswordTextBlock.Visibility = Visibility.Visible;

            // Disable password fields
            NewPasswordBox.IsEnabled = false;
            ConfirmPasswordBox.IsEnabled = false;

            // Set the generated password in the password boxes
            NewPasswordBox.Password = _generatedPassword;
            ConfirmPasswordBox.Password = _generatedPassword;
        }

        private void GenerateRandomPasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Hide the generated password
            GeneratedPasswordTextBlock.Visibility = Visibility.Collapsed;

            // Enable password fields
            NewPasswordBox.IsEnabled = true;
            ConfirmPasswordBox.IsEnabled = true;

            // Clear the password boxes
            NewPasswordBox.Password = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;

            // Clear the generated password
            _generatedPassword = null;
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password;

                if (GenerateRandomPasswordCheckBox.IsChecked == true)
                {
                    // Use the generated password
                    password = _generatedPassword;
                }
                else
                {
                    // Validate input
                    if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
                    {
                        MessageBox.Show("New password is required.",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        NewPasswordBox.Focus();
                        return;
                    }

                    if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                    {
                        MessageBox.Show("Passwords do not match.",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ConfirmPasswordBox.Focus();
                        return;
                    }

                    // Check password strength
                    int strength = CalculatePasswordStrength(NewPasswordBox.Password);
                    if (strength < 3)
                    {
                        var result = MessageBox.Show(
                            "The password is weak. It should be at least 8 characters long and include uppercase letters, lowercase letters, numbers, and special characters. Do you want to continue anyway?",
                            "Weak Password",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result != MessageBoxResult.Yes)
                        {
                            NewPasswordBox.Focus();
                            return;
                        }
                    }

                    password = NewPasswordBox.Password;
                }

                // Reset password
                bool success = await _authService.ResetPasswordAsync(_user.UserId, password);

                if (success)
                {
                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserManagement",
                        $"Reset password for user {_user.Username}",
                        "User",
                        _user.UserId);

                    MessageBox.Show("Password reset successfully.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Close the window
                    Window.GetWindow(this).DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Failed to reset password.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ResetButton_Click");
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
