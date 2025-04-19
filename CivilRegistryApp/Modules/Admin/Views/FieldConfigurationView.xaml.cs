using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Modules.Auth;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CivilRegistryApp.Modules.Admin.Views
{
    /// <summary>
    /// Interaction logic for FieldConfigurationView.xaml
    /// </summary>
    public partial class FieldConfigurationView : UserControl
    {
        private readonly IFieldConfigurationService _fieldConfigService = null!;
        private readonly IAuthenticationService _authService = null!;
        private readonly IUserActivityService _userActivityService = null!;
        private List<FieldConfiguration> _currentConfigurations = new List<FieldConfiguration>();
        private bool _isInitialized = false;

        public FieldConfigurationView()
        {
            InitializeComponent();
        }

        public FieldConfigurationView(IFieldConfigurationService fieldConfigService, IAuthenticationService authService)
        {
            InitializeComponent();
            _fieldConfigService = fieldConfigService;
            _authService = authService;
            _userActivityService = ((App)Application.Current).ServiceProvider.GetService(typeof(IUserActivityService)) as IUserActivityService;

            // Set default selection
            DocumentTypeComboBox.SelectedIndex = 0;

            _isInitialized = true;
        }

        private async void DocumentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || DocumentTypeComboBox.SelectedItem == null)
                return;

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                if (DocumentTypeComboBox.SelectedItem == null)
                {
                    Log.Warning("DocumentTypeComboBox.SelectedItem is null");
                    return;
                }

                string documentType = ((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Content?.ToString() ?? "All";
                await LoadFieldConfigurationsAsync(documentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading field configurations");
                MessageBox.Show($"Error loading field configurations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DocumentTypeComboBox.SelectedItem == null)
                return;

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                string documentType = ((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Content?.ToString() ?? "All";
                await LoadFieldConfigurationsAsync(documentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error refreshing field configurations");
                MessageBox.Show($"Error refreshing field configurations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                // Check if user has permission
                if (!_authService.IsUserInRole("Admin"))
                {
                    MessageBox.Show("You do not have permission to modify field configurations.",
                        "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get the configurations from the DataGrid
                var configurations = FieldConfigDataGrid.ItemsSource as List<FieldConfiguration>;
                if (configurations == null || !configurations.Any())
                    return;

                // Get the document type
                string documentType = "Unknown";
                if (DocumentTypeComboBox.SelectedItem != null)
                {
                    documentType = ((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Content?.ToString() ?? "Unknown";
                }

                // Save each configuration
                foreach (var config in configurations)
                {
                    await _fieldConfigService.UpdateFieldConfigurationAsync(config);
                }

                // Log the activity with EntityType
                await _userActivityService.LogActivityAsync(
                    "Admin",
                    $"Updated field configurations for {documentType}",
                    "FieldConfiguration");

                MessageBox.Show("Field configurations saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving field configurations");
                MessageBox.Show($"Error saving field configurations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if user has permission
                if (!_authService.IsUserInRole("Admin"))
                {
                    MessageBox.Show("You do not have permission to reset field configurations.",
                        "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirm reset
                var result = MessageBox.Show(
                    "Are you sure you want to reset all field configurations to their default values? This cannot be undone.",
                    "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                // Delete all existing configurations
                var allConfigs = await _fieldConfigService.GetAllFieldConfigurationsAsync();
                foreach (var config in allConfigs)
                {
                    await _fieldConfigService.DeleteFieldConfigurationAsync(config.FieldConfigurationId);
                }

                // Initialize default configurations
                await _fieldConfigService.InitializeDefaultConfigurationsAsync();

                // Reload the current document type
                string documentType = "All";
                if (DocumentTypeComboBox.SelectedItem != null)
                {
                    documentType = ((ComboBoxItem)DocumentTypeComboBox.SelectedItem).Content?.ToString() ?? "All";
                    await LoadFieldConfigurationsAsync(documentType);
                }

                // Log the activity with EntityType
                await _userActivityService.LogActivityAsync(
                    "Admin",
                    $"Reset field configurations to default values",
                    "FieldConfiguration");

                MessageBox.Show("Field configurations have been reset to default values.",
                    "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error resetting field configurations");
                MessageBox.Show($"Error resetting field configurations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void FieldConfigDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method can be used to show details of the selected field configuration
            // or to enable/disable buttons based on selection
        }

        private async System.Threading.Tasks.Task LoadFieldConfigurationsAsync(string documentType)
        {
            try
            {
                // Get configurations for the selected document type
                var configurations = await _fieldConfigService.GetFieldConfigurationsByDocumentTypeAsync(documentType);
                _currentConfigurations = configurations.ToList();

                // Bind to DataGrid
                FieldConfigDataGrid.ItemsSource = _currentConfigurations;

                Log.Information("Loaded {Count} field configurations for document type {DocumentType}",
                    _currentConfigurations.Count, documentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading field configurations for document type {DocumentType}", documentType);
                throw;
            }
        }
    }
}
