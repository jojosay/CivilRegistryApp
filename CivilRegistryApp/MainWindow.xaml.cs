using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Admin;
using CivilRegistryApp.Modules.Admin.Views;
using CivilRegistryApp.Modules.Auth;
using CivilRegistryApp.Modules.Auth.Views;
using CivilRegistryApp.Modules.Dashboard.Views;
using CivilRegistryApp.Modules.Documents;
using CivilRegistryApp.Modules.Documents.Views;
using CivilRegistryApp.Modules.Reports;
using CivilRegistryApp.Modules.Reports.Views;
using CivilRegistryApp.Modules.Requests;
using CivilRegistryApp.Modules.Requests.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
// Explicitly use WPF controls to avoid ambiguity
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;

namespace CivilRegistryApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IAuthenticationService _authService;
    private readonly IDocumentService _documentService;
    private readonly IRequestService _requestService;
    private readonly IReportService? _reportService;
    private readonly AppDbContext _dbContext;
    private readonly IUserActivityService _userActivityService;
    private readonly IUserRepository _userRepository;

    // Sidebar state
    private bool _isSidebarExpanded = true;
    private Storyboard? _expandSidebarStoryboard;
    private Storyboard? _collapseSidebarStoryboard;

    public MainWindow(IAuthenticationService authService, IDocumentService documentService, IRequestService requestService, AppDbContext dbContext, IUserActivityService userActivityService, IUserRepository userRepository)
    {
        try
        {
            Log.Debug("Initializing MainWindow");
            InitializeComponent();
            _authService = authService;
            _documentService = documentService;
            _requestService = requestService;
            _dbContext = dbContext;
            _userActivityService = userActivityService;
            _userRepository = userRepository;
            _reportService = ((App)Application.Current).ServiceProvider.GetService<IReportService>();

            // Set initial UI state
            UpdateUIBasedOnAuthState();

            // Initialize storyboards
            _expandSidebarStoryboard = FindResource("ExpandSidebar") as Storyboard;
            _collapseSidebarStoryboard = FindResource("CollapseSidebar") as Storyboard;

            // Set initial sidebar state
            SidebarIcons.Visibility = Visibility.Collapsed;
            SidebarContent.Visibility = Visibility.Visible;

            // Log successful initialization
            Log.Information("MainWindow initialized successfully");

            // Add window events for logging
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            // Load Dashboard view by default if user is logged in
            if (_authService.CurrentUser != null)
            {
                LoadDashboardView();
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            SerilogConfig.LogUnhandledException(ex, "MainWindow.Constructor");

            // Show error message and rethrow to let the global handler deal with it
            MessageBox.Show($"Error initializing the application: {ex.Message}\n\nThe application will now close.",
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Log.Information("MainWindow loaded");
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Log.Information("MainWindow closing");
    }

    private void UpdateUIBasedOnAuthState()
    {
        try
        {
            Log.Debug("Updating UI based on authentication state");

            if (_authService.CurrentUser != null)
            {
                // User is logged in
                UserNameTextBlock.Text = $"Logged in as: {_authService.CurrentUser.FullName}";
                LoginButton.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Visible;

                // Show profile button
                ProfileButton.Visibility = Visibility.Visible;

                // Show/hide admin-only buttons
                bool isAdmin = _authService.IsUserInRole("Admin");
                UsersButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                ActivityLogButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                FieldConfigButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                DatabaseUtilityButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

                // Also update the icon buttons
                UsersIconButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                ActivityLogIconButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                FieldConfigIconButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                DatabaseUtilityIconButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;

                Log.Debug("UI updated for logged-in user: {Username}", _authService.CurrentUser.Username);
            }
            else
            {
                // User is not logged in
                UserNameTextBlock.Text = "Not logged in";
                LoginButton.Visibility = Visibility.Visible;
                LogoutButton.Visibility = Visibility.Collapsed;
                ProfileButton.Visibility = Visibility.Collapsed;

                // Hide all admin buttons
                UsersButton.Visibility = Visibility.Collapsed;
                ActivityLogButton.Visibility = Visibility.Collapsed;
                FieldConfigButton.Visibility = Visibility.Collapsed;
                DatabaseUtilityButton.Visibility = Visibility.Collapsed;

                // Hide all icon buttons
                UsersIconButton.Visibility = Visibility.Collapsed;
                ActivityLogIconButton.Visibility = Visibility.Collapsed;
                FieldConfigIconButton.Visibility = Visibility.Collapsed;
                DatabaseUtilityIconButton.Visibility = Visibility.Collapsed;

                Log.Debug("UI updated for non-authenticated state");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating UI based on authentication state");
            // Don't rethrow - we want the application to continue even if this fails
        }
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Login button clicked");

            // Show login dialog
            var loginWindow = new LoginWindow(_authService, _dbContext);
            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                // User logged in successfully
                UpdateUIBasedOnAuthState();

                // Load the Dashboard view
                LoadDashboardView();

                Log.Information("User logged in successfully through login dialog");
            }
            else
            {
                Log.Information("Login dialog canceled or closed without login");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in LoginButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Logout button clicked");

            // Log activity before logout
            if (_authService.CurrentUser != null)
            {
                await _userActivityService.LogActivityAsync(
                    "Authentication",
                    "User logged out");
            }

            await _authService.LogoutAsync();
            UpdateUIBasedOnAuthState();

            // Reset to welcome screen
            MainFrame.Content = null;
            WelcomeTextBlock.Visibility = Visibility.Visible;

            Log.Information("User logged out successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in LogoutButton_Click");
            MessageBox.Show($"An error occurred during logout: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DashboardButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Dashboard button clicked");
            LoadDashboardView();
            Log.Information("Dashboard view shown from button click");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in DashboardButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Loads the Dashboard view into the main content area
    /// </summary>
    private void LoadDashboardView()
    {
        try
        {
            // Navigate to Dashboard page
            WelcomeTextBlock.Visibility = Visibility.Collapsed;

            // Set initial opacity for animation
            MainFrame.Opacity = 0;

            // Create and show the dashboard view
            var dashboardView = new DashboardView(_documentService, _requestService, _authService, _dbContext);
            MainFrame.Content = dashboardView;

            // Play the fade-in animation
            var contentFadeIn = FindResource("ContentFadeIn") as Storyboard;
            contentFadeIn?.Begin(MainFrame);

            Log.Information("Dashboard view loaded");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading Dashboard view");
            MessageBox.Show($"An error occurred while loading the Dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DocumentsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Documents button clicked");

            // Navigate to Documents page
            WelcomeTextBlock.Visibility = Visibility.Collapsed;

            // Set initial opacity for animation
            MainFrame.Opacity = 0;

            // Create and show the documents list view
            var documentsListView = new DocumentsListView(_documentService, _authService);
            MainFrame.Content = documentsListView;

            // Play the fade-in animation
            var contentFadeIn = FindResource("ContentFadeIn") as Storyboard;
            contentFadeIn?.Begin(MainFrame);

            Log.Information("Documents list view shown");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in DocumentsButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RequestsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Requests button clicked");

            // Navigate to Requests page
            WelcomeTextBlock.Visibility = Visibility.Collapsed;

            // Set initial opacity for animation
            MainFrame.Opacity = 0;

            // Create and show the requests list view
            var requestsListView = new Modules.Requests.Views.RequestsListView(_requestService, _authService);
            MainFrame.Content = requestsListView;

            // Play the fade-in animation
            var contentFadeIn = FindResource("ContentFadeIn") as Storyboard;
            contentFadeIn?.Begin(MainFrame);

            Log.Information("Requests list view shown");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in RequestsButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ReportsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Reports button clicked");

            // Check if report service is available
            if (_reportService == null)
            {
                MessageBox.Show("Report service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Navigate to Reports page
            WelcomeTextBlock.Visibility = Visibility.Collapsed;

            // Set initial opacity for animation
            MainFrame.Opacity = 0;

            // Create and show the reports view
            var reportsView = new Modules.Reports.Views.ReportsView(_reportService, _authService, _dbContext);
            MainFrame.Content = reportsView;

            // Play the fade-in animation
            var contentFadeIn = FindResource("ContentFadeIn") as Storyboard;
            contentFadeIn?.Begin(MainFrame);

            Log.Information("Reports view shown");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ReportsButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void UsersButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Users button clicked");

            // Check if user is admin
            if (!_authService.IsUserInRole("Admin"))
            {
                MessageBox.Show("You do not have permission to access user management.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Navigate to User Management view
            WelcomeTextBlock.Visibility = Visibility.Collapsed;

            // Set initial opacity for animation
            MainFrame.Opacity = 0;

            // Create and show the user management view
            var userManagementView = new UserManagementView(_userRepository, _authService, _userActivityService);
            MainFrame.Content = userManagementView;

            // Play the fade-in animation
            var contentFadeIn = FindResource("ContentFadeIn") as Storyboard;
            contentFadeIn?.Begin(MainFrame);

            // Log activity
            await _userActivityService.LogActivityAsync(
                "UserManagement",
                "Accessed user management");

            Log.Information("User management view shown");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in UsersButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Profile button clicked");

            // Check if user is logged in
            if (_authService.CurrentUser == null)
            {
                MessageBox.Show("You must be logged in to view your profile.",
                    "Not Logged In", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show user profile view
            var userProfileView = new UserProfileView(_authService, _userActivityService);
            var window = new Window
            {
                Title = "My Profile",
                Content = userProfileView,
                Width = 500,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            // Log activity
            await _userActivityService.LogActivityAsync(
                "UserProfile",
                "Opened user profile");

            window.ShowDialog();

            Log.Information("User profile window closed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ProfileButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ActivityLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Activity Log button clicked");

            // Check if user is admin
            if (!_authService.IsUserInRole("Admin"))
            {
                MessageBox.Show("You do not have permission to access activity logs.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Navigate to Activity Log view
            WelcomeTextBlock.Visibility = Visibility.Collapsed;

            // Set initial opacity for animation
            MainFrame.Opacity = 0;

            // Create and show the activity log view
            var activityLogView = new UserActivityLogView(_userActivityService, _userRepository, _authService);
            MainFrame.Content = activityLogView;

            // Play the fade-in animation
            var contentFadeIn = FindResource("ContentFadeIn") as Storyboard;
            contentFadeIn?.Begin(MainFrame);

            // Log activity
            await _userActivityService.LogActivityAsync(
                "Admin",
                "Accessed activity logs");

            Log.Information("Activity log view shown");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ActivityLogButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void FieldConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Field Configuration button clicked");

            // Check if user is admin
            if (!_authService.IsUserInRole("Admin"))
            {
                MessageBox.Show("You do not have permission to access field configuration.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Navigate to Field Configuration view
            WelcomeTextBlock.Visibility = Visibility.Collapsed;

            // Set initial opacity for animation
            MainFrame.Opacity = 0;

            // Get the field configuration service from the service provider
            var fieldConfigService = ((App)Application.Current).ServiceProvider.GetService<IFieldConfigurationService>();

            // Create and show the field configuration view
            var fieldConfigView = new FieldConfigurationView(fieldConfigService, _authService);
            MainFrame.Content = fieldConfigView;

            // Play the fade-in animation
            var contentFadeIn = FindResource("ContentFadeIn") as Storyboard;
            contentFadeIn?.Begin(MainFrame);

            // Log activity
            await _userActivityService.LogActivityAsync(
                "Admin",
                "Accessed field configuration");

            Log.Information("Field Configuration view shown");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in FieldConfigButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DatabaseUtilityButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Database Utility button clicked");

            // Check if user is admin
            if (!_authService.IsUserInRole("Admin"))
            {
                MessageBox.Show("You do not have permission to access database utilities.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show database utility window
            var databaseUtilityWindow = new DatabaseUtilityWindow(_dbContext);
            databaseUtilityWindow.Owner = this;
            databaseUtilityWindow.ShowDialog();

            Log.Information("Database Utility window closed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in DatabaseUtilityButton_Click");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Toggles the sidebar between expanded and collapsed states
    /// </summary>
    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Toggle sidebar button clicked");

            if (_isSidebarExpanded)
            {
                // Collapse sidebar - set visibility after animation completes
                _collapseSidebarStoryboard?.Begin(this);

                // Use a timer instead of event handler to avoid potential memory leaks
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(300) // Match animation duration
                };

                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    SidebarContent.Visibility = Visibility.Collapsed;
                    SidebarIcons.Visibility = Visibility.Visible;
                };

                timer.Start();

                // Rotate the menu icon to indicate change
                RotateTransform rotateTransform = new RotateTransform(90);
                MenuIcon.RenderTransform = rotateTransform;
            }
            else
            {
                // For expanding, we need to make content visible before animation
                SidebarContent.Visibility = Visibility.Visible;
                SidebarIcons.Visibility = Visibility.Collapsed;

                // Expand sidebar
                _expandSidebarStoryboard?.Begin(this);

                // Reset the menu icon rotation
                MenuIcon.RenderTransform = null;
            }

            _isSidebarExpanded = !_isSidebarExpanded;
            Log.Information("Sidebar toggled to {State}", _isSidebarExpanded ? "expanded" : "collapsed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error toggling sidebar");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the accessibility options dialog
    /// </summary>
    private void AccessibilityButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Accessibility button clicked");

            // Create a simple accessibility options dialog
            var dialog = new Window
            {
                Title = "Accessibility Options",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            // Create content
            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock { Text = "Accessibility Options", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) });

            // Font size slider
            panel.Children.Add(new TextBlock { Text = "Text Size", Margin = new Thickness(0, 10, 0, 5) });
            var fontSizeSlider = new Slider { Minimum = 12, Maximum = 24, Value = 14, TickFrequency = 2, IsSnapToTickEnabled = true };
            panel.Children.Add(fontSizeSlider);

            // High contrast toggle
            panel.Children.Add(new TextBlock { Text = "High Contrast", Margin = new Thickness(0, 20, 0, 5) });
            var highContrastCheckbox = new CheckBox { Content = "Enable high contrast mode" };
            panel.Children.Add(highContrastCheckbox);

            // Screen reader toggle
            panel.Children.Add(new TextBlock { Text = "Screen Reader", Margin = new Thickness(0, 20, 0, 5) });
            var screenReaderCheckbox = new CheckBox { Content = "Optimize for screen readers" };
            panel.Children.Add(screenReaderCheckbox);

            // Apply button
            var applyButton = new Button { Content = "Apply Changes", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
            applyButton.Click += (s, args) => {
                // In a real implementation, this would apply the accessibility settings
                MessageBox.Show("Accessibility settings applied.", "Settings Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                dialog.Close();
            };
            panel.Children.Add(applyButton);

            dialog.Content = panel;
            dialog.ShowDialog();

            Log.Information("Accessibility dialog closed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing accessibility options");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the help dialog
    /// </summary>
    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Debug("Help button clicked");

            // Create a simple help dialog
            var dialog = new Window
            {
                Title = "Help",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            // Create content
            var scrollViewer = new ScrollViewer();
            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock { Text = "Civil Registry Archiving System Help", FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 20) });

            // Add help sections
            AddHelpSection(panel, "Documents", "The Documents section allows you to manage all civil registry documents. You can add, edit, view, and search for documents.");
            AddHelpSection(panel, "Requests", "The Requests section allows you to manage document requests from citizens. You can process requests and track their status.");
            AddHelpSection(panel, "Reports", "The Reports section provides various reports and statistics about documents and requests.");
            AddHelpSection(panel, "User Management", "The User Management section allows administrators to manage user accounts, roles, and permissions.");
            AddHelpSection(panel, "Field Configuration", "The Field Configuration section allows administrators to configure which fields are required for different document types.");
            AddHelpSection(panel, "Database Utility", "The Database Utility provides tools for database maintenance and backup.");

            // Close button
            var closeButton = new Button { Content = "Close", Margin = new Thickness(0, 20, 0, 0), Padding = new Thickness(10, 5, 10, 5), HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
            closeButton.Click += (s, args) => dialog.Close();
            panel.Children.Add(closeButton);

            scrollViewer.Content = panel;
            dialog.Content = scrollViewer;
            dialog.ShowDialog();

            Log.Information("Help dialog closed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing help dialog");
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Helper method to add a help section to the help dialog
    /// </summary>
    private void AddHelpSection(StackPanel panel, string title, string content)
    {
        panel.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
        panel.Children.Add(new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 15) });
    }
}