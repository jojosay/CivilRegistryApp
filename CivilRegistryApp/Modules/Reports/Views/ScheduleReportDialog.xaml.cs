using CivilRegistryApp.Data.Entities;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;

namespace CivilRegistryApp.Modules.Reports.Views
{
    /// <summary>
    /// Interaction logic for ScheduleReportDialog.xaml
    /// </summary>
    public partial class ScheduleReportDialog : Window
    {
        private ScheduledReport _report;

        public ScheduleReportDialog(ScheduledReport report)
        {
            InitializeComponent();
            _report = report;
            LoadReportData();
        }

        private void LoadReportData()
        {
            try
            {
                // Set form values from report
                ReportNameTextBox.Text = _report.Name;
                OutputPathTextBox.Text = _report.OutputPath;
                EmailRecipientsTextBox.Text = _report.EmailRecipients;
                ActiveCheckBox.IsChecked = _report.IsActive;

                // Set export format
                foreach (ComboBoxItem item in ExportFormatComboBox.Items)
                {
                    if (item.Content.ToString() == _report.ExportFormat)
                    {
                        ExportFormatComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Set schedule
                bool foundPredefinedSchedule = false;
                foreach (ComboBoxItem item in ScheduleComboBox.Items)
                {
                    if (item.Tag != null && item.Tag.ToString() == _report.Schedule)
                    {
                        ScheduleComboBox.SelectedItem = item;
                        foundPredefinedSchedule = true;
                        break;
                    }
                }

                if (!foundPredefinedSchedule && !string.IsNullOrEmpty(_report.Schedule))
                {
                    // Select custom and set cron expression
                    foreach (ComboBoxItem item in ScheduleComboBox.Items)
                    {
                        if (item.Content.ToString() == "Custom")
                        {
                            ScheduleComboBox.SelectedItem = item;
                            CronExpressionTextBox.Text = _report.Schedule;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading report data in ScheduleReportDialog");
                System.Windows.MessageBox.Show($"Error loading report data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScheduleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ScheduleComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    bool isCustom = selectedItem.Content.ToString() == "Custom";
                    CronExpressionLabel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
                    CronExpressionTextBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;

                    if (!isCustom && selectedItem.Tag != null)
                    {
                        CronExpressionTextBox.Text = selectedItem.Tag.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ScheduleComboBox_SelectionChanged");
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ScheduleHelpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string helpText = "Cron Expression Format:\n\n" +
                    "Seconds Minutes Hours DayOfMonth Month DayOfWeek Year\n\n" +
                    "Examples:\n" +
                    "0 0 12 * * ? - Every day at noon\n" +
                    "0 0 12 ? * MON - Every Monday at noon\n" +
                    "0 0 12 1 * ? - First day of every month at noon\n" +
                    "0 0 12 ? * MON-FRI - Every weekday at noon\n" +
                    "0 0 12 15 * ? - 15th day of every month at noon\n\n" +
                    "For more information, search for 'Quartz Cron Expressions'";

                System.Windows.MessageBox.Show(helpText, "Cron Expression Help", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error showing schedule help");
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Select output folder for reports";
                    dialog.UseDescriptionForTitle = true;

                    if (!string.IsNullOrEmpty(OutputPathTextBox.Text))
                    {
                        dialog.SelectedPath = OutputPathTextBox.Text;
                    }

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        OutputPathTextBox.Text = dialog.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error browsing for output folder");
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate form
                if (string.IsNullOrWhiteSpace(ReportNameTextBox.Text))
                {
                    System.Windows.MessageBox.Show("Please enter a report name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ReportNameTextBox.Focus();
                    return;
                }

                if (ScheduleComboBox.SelectedItem == null)
                {
                    System.Windows.MessageBox.Show("Please select a schedule.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ScheduleComboBox.Focus();
                    return;
                }

                if (ExportFormatComboBox.SelectedItem == null)
                {
                    System.Windows.MessageBox.Show("Please select an export format.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ExportFormatComboBox.Focus();
                    return;
                }

                // Get cron expression
                string cronExpression;
                if (ScheduleComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    if (selectedItem.Content.ToString() == "Custom")
                    {
                        if (string.IsNullOrWhiteSpace(CronExpressionTextBox.Text))
                        {
                            System.Windows.MessageBox.Show("Please enter a cron expression.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            CronExpressionTextBox.Focus();
                            return;
                        }
                        cronExpression = CronExpressionTextBox.Text;
                    }
                    else
                    {
                        cronExpression = selectedItem.Tag.ToString();
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please select a schedule.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ScheduleComboBox.Focus();
                    return;
                }

                // Update report
                _report.Name = ReportNameTextBox.Text;
                _report.Schedule = cronExpression;
                _report.ExportFormat = ((ComboBoxItem)ExportFormatComboBox.SelectedItem).Content.ToString();
                _report.OutputPath = OutputPathTextBox.Text;
                _report.EmailRecipients = EmailRecipientsTextBox.Text;
                _report.IsActive = ActiveCheckBox.IsChecked ?? true;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving scheduled report");
                System.Windows.MessageBox.Show($"Error saving scheduled report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
