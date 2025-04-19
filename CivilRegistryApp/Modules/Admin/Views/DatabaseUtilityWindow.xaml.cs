using CivilRegistryApp.Data;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CivilRegistryApp.Modules.Admin.Views
{
    /// <summary>
    /// Interaction logic for DatabaseUtilityWindow.xaml
    /// </summary>
    public partial class DatabaseUtilityWindow : Window
    {
        private readonly AppDbContext _dbContext;

        public DatabaseUtilityWindow(AppDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            Log.Debug("DatabaseUtilityWindow initialized");
        }

        private async void ClearDocumentDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Confirm with the user before proceeding
                var result = MessageBox.Show(
                    "This will permanently delete ALL document data and related requests from the database. This action cannot be undone.\n\nAre you sure you want to continue?",
                    "Confirm Data Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    StatusTextBlock.Text = "Operation cancelled.";
                    return;
                }

                // Disable the button during operation
                ClearDocumentDataButton.IsEnabled = false;
                StatusTextBlock.Text = "Clearing document data...";

                // Clear the document data
                await SeedData.ClearDocumentDataAsync(_dbContext);

                // Update status
                StatusTextBlock.Text = "All document data has been successfully cleared from the database.";
                Log.Information("Document data cleared successfully via DatabaseUtilityWindow");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error clearing document data");
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable the button
                ClearDocumentDataButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
