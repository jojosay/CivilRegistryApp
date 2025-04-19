using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace CivilRegistryApp.Infrastructure
{
    /// <summary>
    /// Helper class for clipboard operations
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// Copies text to clipboard and shows a tooltip notification
        /// </summary>
        /// <param name="text">Text to copy</param>
        /// <param name="element">UI element that triggered the copy</param>
        public static void CopyToClipboard(string text, FrameworkElement element)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                // Copy to clipboard
                Clipboard.SetText(text);

                // Show notification tooltip
                if (element != null)
                {
                    var tooltip = new ToolTip
                    {
                        Content = "Copied to clipboard!",
                        IsOpen = true,
                        PlacementTarget = element
                    };

                    // Auto-close tooltip after 1 second
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };

                    timer.Tick += (s, e) =>
                    {
                        tooltip.IsOpen = false;
                        timer.Stop();
                    };

                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Attaches a click handler to a TextBlock to copy its text to clipboard when clicked
        /// </summary>
        /// <param name="textBlock">TextBlock to attach the handler to</param>
        public static void MakeTextBlockCopyable(TextBlock textBlock)
        {
            if (textBlock == null)
                return;

            // Set cursor to indicate it's clickable
            textBlock.Cursor = Cursors.Hand;

            // Add a tooltip
            textBlock.ToolTip = "Click to copy to clipboard";

            // Add click handler
            textBlock.MouseLeftButtonDown += (sender, e) =>
            {
                if (sender is TextBlock tb && !string.IsNullOrEmpty(tb.Text))
                {
                    // Copy to clipboard
                    Clipboard.SetText(tb.Text);

                    // Show tooltip notification
                    var tooltip = new ToolTip
                    {
                        Content = "Copied to clipboard!",
                        IsOpen = true,
                        PlacementTarget = tb
                    };

                    // Auto-close tooltip after 1 second
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };

                    timer.Tick += (s, args) =>
                    {
                        tooltip.IsOpen = false;
                        timer.Stop();
                    };

                    timer.Start();
                }
            };
        }

        /// <summary>
        /// Attaches a click handler to a TextBox to copy its text to clipboard when clicked
        /// </summary>
        /// <param name="textBox">TextBox to attach the handler to</param>
        public static void MakeTextBoxCopyable(TextBox textBox)
        {
            if (textBox == null)
                return;

            // Set cursor to indicate it's clickable
            textBox.Cursor = Cursors.Hand;

            // Add a tooltip
            textBox.ToolTip = "Click to copy to clipboard";

            // Add click handler
            textBox.PreviewMouseLeftButtonDown += (sender, e) =>
            {
                if (sender is TextBox tb && tb.IsReadOnly && !string.IsNullOrEmpty(tb.Text))
                {
                    // Copy to clipboard
                    Clipboard.SetText(tb.Text);

                    // Show tooltip notification
                    var tooltip = new ToolTip
                    {
                        Content = "Copied to clipboard!",
                        IsOpen = true,
                        PlacementTarget = tb
                    };

                    // Auto-close tooltip after 1 second
                    var timer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };

                    timer.Tick += (s, args) =>
                    {
                        tooltip.IsOpen = false;
                        timer.Stop();
                    };

                    timer.Start();

                    e.Handled = true; // Prevent default behavior
                }
            };
        }

        /// <summary>
        /// Attaches a click handler to a DataGrid cell to copy its content to clipboard when clicked
        /// </summary>
        /// <param name="dataGrid">DataGrid to attach the handler to</param>
        /// <param name="columnName">Name of the column to make copyable</param>
        public static void MakeDataGridColumnCopyable(DataGrid dataGrid, string columnName)
        {
            if (dataGrid == null || string.IsNullOrEmpty(columnName))
                return;

            // Find the column index by header name
            int targetColumnIndex = -1;
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                if (dataGrid.Columns[i].Header?.ToString() == columnName)
                {
                    targetColumnIndex = i;

                    // Add tooltip to column header - create a completely new style
                    var column = dataGrid.Columns[i];
                    try
                    {
                        // Create a new style with tooltip
                        Style headerStyle = new Style(typeof(DataGridColumnHeader));
                        headerStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, "Click on a cell in this column to copy its value"));

                        // Apply the style
                        column.HeaderStyle = headerStyle;
                    }
                    catch (Exception)
                    {
                        // If we can't modify the style, just continue without the tooltip
                    }
                    break;
                }
            }

            if (targetColumnIndex == -1)
                return; // Column not found

            // Create a handler for cell click
            MouseButtonEventHandler cellClickHandler = null;
            cellClickHandler = (sender, e) =>
            {
                try
                {
                    // Get hit test result
                    var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
                    if (hitTestResult == null || hitTestResult.VisualHit == null)
                        return;

                    // Find the DataGridCell
                    DependencyObject element = hitTestResult.VisualHit;
                    while (element != null && !(element is DataGridCell))
                    {
                        element = VisualTreeHelper.GetParent(element);
                    }

                    // If we found a cell
                    if (element is DataGridCell cell)
                    {
                        // Get the cell's column index
                        int columnIndex = -1;
                        if (cell.Column != null && cell.Column is DataGridColumn)
                        {
                            columnIndex = cell.Column.DisplayIndex;
                        }

                        // Check if it's our target column
                        if (columnIndex == targetColumnIndex)
                        {
                            // Get the cell content
                            string cellContent = null;

                            // Try to get content from the cell
                            if (cell.Content is TextBlock textBlock)
                            {
                                cellContent = textBlock.Text;
                            }
                            else if (cell.Content != null)
                            {
                                cellContent = cell.Content.ToString();
                            }
                            else
                            {
                                // Try to get from the data item
                                var row = DataGridRow.GetRowContainingElement(cell);
                                if (row?.Item != null && targetColumnIndex < dataGrid.Columns.Count)
                                {
                                    var column = dataGrid.Columns[targetColumnIndex];
                                    if (column is DataGridBoundColumn boundColumn)
                                    {
                                        var binding = boundColumn.Binding as System.Windows.Data.Binding;
                                        if (binding != null && !string.IsNullOrEmpty(binding.Path.Path))
                                        {
                                            var property = row.Item.GetType().GetProperty(binding.Path.Path);
                                            if (property != null)
                                            {
                                                var value = property.GetValue(row.Item);
                                                if (value != null)
                                                {
                                                    cellContent = value.ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Copy to clipboard if we got content
                            if (!string.IsNullOrEmpty(cellContent))
                            {
                                try
                                {
                                    // Copy to clipboard without changing cell appearance
                                    Clipboard.SetText(cellContent);

                                    // Show tooltip notification
                                    var tooltip = new ToolTip
                                    {
                                        Content = "Copied to clipboard!",
                                        IsOpen = true,
                                        PlacementTarget = cell
                                    };

                                    // Auto-close tooltip after 1 second
                                    var timer = new System.Windows.Threading.DispatcherTimer
                                    {
                                        Interval = TimeSpan.FromSeconds(1)
                                    };

                                    timer.Tick += (s, args) =>
                                    {
                                        tooltip.IsOpen = false;
                                        timer.Stop();
                                    };

                                    timer.Start();
                                }
                                catch
                                {
                                    // If tooltip fails, at least we copied to clipboard
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Silently fail - don't disrupt the user experience
                }
            };

            // Add the handler
            dataGrid.PreviewMouseLeftButtonDown += cellClickHandler;
        }


    }
}
