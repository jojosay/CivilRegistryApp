using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CivilRegistryApp.Modules.Auth.Views
{
    public partial class UserAddView : UserControl
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authService;
        private readonly IUserActivityService _userActivityService;
        
        public UserAddView(IUserRepository userRepository, IAuthenticationService authService, IUserActivityService userActivityService)
        {
            InitializeComponent();
            
            _userRepository = userRepository;
            _authService = authService;
            _userActivityService = userActivityService;
            
            // Add event handlers for password validation
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
            ConfirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;
        }
        
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Check password strength
            string password = PasswordBox.Password;
            
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
            
            if (PasswordBox.Password != ConfirmPasswordBox.Password)
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
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    MessageBox.Show("Username is required.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    UsernameTextBox.Focus();
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    MessageBox.Show("Password is required.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }
                
                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Passwords do not match.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                {
                    MessageBox.Show("Full Name is required.", 
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FullNameTextBox.Focus();
                    return;
                }
                
                // Check password strength
                int strength = CalculatePasswordStrength(PasswordBox.Password);
                if (strength < 3)
                {
                    var result = MessageBox.Show(
                        "The password is weak. It should be at least 8 characters long and include uppercase letters, lowercase letters, numbers, and special characters. Do you want to continue anyway?", 
                        "Weak Password", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        PasswordBox.Focus();
                        return;
                    }
                }
                
                // Get selected role
                string role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Staff";
                
                // Register user
                bool success = await _authService.RegisterAsync(
                    UsernameTextBox.Text,
                    PasswordBox.Password,
                    FullNameTextBox.Text,
                    role,
                    EmailTextBox.Text,
                    PhoneNumberTextBox.Text,
                    PositionTextBox.Text,
                    DepartmentTextBox.Text);
                
                if (success)
                {
                    // Log activity
                    await _userActivityService.LogActivityAsync(
                        "UserManagement", 
                        $"Added new user: {UsernameTextBox.Text} with role {role}");
                    
                    MessageBox.Show("User created successfully.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Close the window
                    Window.GetWindow(this).DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Failed to create user. The username may already be taken.", 
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
