using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class ChangePasswordView : UserControl
    {
        private readonly IAuthenticationService _authService;
        private readonly IUserActivityService _userActivityService;
        
        public ChangePasswordView(IAuthenticationService authService, IUserActivityService userActivityService)
        {
            InitializeComponent();
            
            _authService = authService;
            _userActivityService = userActivityService;
            
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
        
        private async void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(CurrentPasswordBox.Password))
                {
                    MessageBox.Show("Please enter your current password.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CurrentPasswordBox.Focus();
                    return;
                }
                
                if (string.IsNullOrEmpty(NewPasswordBox.Password))
                {
                    MessageBox.Show("Please enter a new password.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    NewPasswordBox.Focus();
                    return;
                }
                
                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("New password and confirmation do not match.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }
                
                // Check password strength
                int strength = CalculatePasswordStrength(NewPasswordBox.Password);
                if (strength < 3)
                {
                    var result = MessageBox.Show(
                        "Your password is weak. It should be at least 8 characters long and include uppercase letters, lowercase letters, numbers, and special characters. Do you want to continue anyway?", 
                        "Weak Password", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        NewPasswordBox.Focus();
                        return;
                    }
                }
                
                // Change password
                bool success = await _authService.ChangePasswordAsync(
                    _authService.CurrentUser.UserId,
                    CurrentPasswordBox.Password,
                    NewPasswordBox.Password);
                
                if (success)
                {
                    // Log activity (without including the password!)
                    await _userActivityService.LogActivityAsync(
                        "Security", 
                        "Changed password");
                    
                    MessageBox.Show("Password changed successfully.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Close the window
                    Window.GetWindow(this).DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Failed to change password. Please check your current password and try again.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentPasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "ChangeButton_Click");
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
