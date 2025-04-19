using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CivilRegistryApp.Modules.Documents.Views
{
    /// <summary>
    /// Interaction logic for DocumentPreviewWindow.xaml
    /// </summary>
    public partial class DocumentPreviewWindow : Window
    {
        private double _currentZoom = 1.0;
        private double _rotationAngle = 0;
        private readonly string _filePath;
        private readonly string _documentSide;

        public DocumentPreviewWindow(string filePath, string documentSide)
        {
            InitializeComponent();

            // Logger is already initialized by the application

            _filePath = filePath;
            _documentSide = documentSide;

            // Set the window title
            Title = $"Document Preview - {documentSide}";
            PreviewTitleTextBlock.Text = $"Document Preview - {documentSide}";

            // Load the image
            LoadImage();
        }

        private void LoadImage()
        {
            try
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    MessageBox.Show("No file selected for preview.", "Preview Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                string extension = Path.GetExtension(_filePath).ToLower();

                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tiff")
                {
                    try
                    {
                        // Load the image
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load the image right away so the file isn't locked

                        // Check if the path is absolute or relative
                        if (Path.IsPathRooted(_filePath))
                        {
                            // Absolute path - use file URI
                            bitmap.UriSource = new Uri(_filePath);
                        }
                        else
                        {
                            // Relative path - convert to absolute path
                            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                            string absolutePath = Path.Combine(baseDir, _filePath);

                            if (File.Exists(absolutePath))
                            {
                                bitmap.UriSource = new Uri(absolutePath);
                            }
                            else
                            {
                                throw new FileNotFoundException($"File not found: {absolutePath}");
                            }
                        }

                        bitmap.EndInit();

                        // Set the image source
                        PreviewImage.Source = bitmap;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error loading image from path: {FilePath}", _filePath);
                        MessageBox.Show($"Error loading image: {ex.Message}", "Preview Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                    }
                }
                else
                {
                    // Unsupported file type
                    MessageBox.Show($"Cannot preview this file type: {extension}. Only image files can be previewed.",
                        "Preview Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading image for preview: {FilePath}", _filePath);
                MessageBox.Show($"An error occurred while loading the preview: {ex.Message}",
                    "Preview Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom += 0.25;
            ApplyTransform();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentZoom > 0.25) // Prevent zooming out too much
            {
                _currentZoom -= 0.25;
                ApplyTransform();
            }
        }

        private void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            _rotationAngle += 90;
            if (_rotationAngle >= 360)
                _rotationAngle = 0;

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            // Create a transformation group
            TransformGroup transformGroup = new TransformGroup();

            // Add scale transform
            transformGroup.Children.Add(new ScaleTransform(_currentZoom, _currentZoom));

            // Add rotation transform
            transformGroup.Children.Add(new RotateTransform(_rotationAngle));

            // Apply the transformation
            PreviewImage.RenderTransform = transformGroup;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
