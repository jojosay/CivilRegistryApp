using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Modules.Documents;
using CivilRegistryApp.Modules.Documents.Views;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CivilRegistryApp.Modules.Requests.Views
{
    public partial class RequestDetailsView : UserControl
    {
        private readonly IRequestService _requestService;
        private readonly IDocumentService _documentService;
        private readonly IFileStorageService _fileStorageService;
        private readonly int _requestId;
        private readonly bool _processingMode;
        private DocumentRequest _request = null!;
        private Document _relatedDocument = null!;

        public BitmapImage DocumentImageSource { get; private set; } = new BitmapImage();

        public RequestDetailsView(IRequestService requestService, int requestId, bool processingMode = false)
        {
            InitializeComponent();
            _requestService = requestService ?? throw new ArgumentNullException(nameof(requestService));
            _requestId = requestId;
            _processingMode = processingMode;

            // Get document service and file storage service from the application's service provider
            _documentService = ((App)Application.Current).ServiceProvider.GetService(typeof(IDocumentService)) as IDocumentService;
            _fileStorageService = ((App)Application.Current).ServiceProvider.GetService(typeof(IFileStorageService)) as IFileStorageService;

            if (_documentService == null)
                throw new InvalidOperationException("Could not resolve IDocumentService from the service provider.");
            if (_fileStorageService == null)
                throw new InvalidOperationException("Could not resolve IFileStorageService from the service provider.");

            // Load request details when the control is loaded
            Loaded += (s, e) => LoadRequestDetailsAsync();
        }

        private async void LoadRequestDetailsAsync()
        {
            try
            {
                // Get request details
                _request = await _requestService.GetRequestByIdAsync(_requestId);
                if (_request == null)
                {
                    MessageBox.Show($"Request with ID {_requestId} not found.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                    Window.GetWindow(this).Close();
                    return;
                }

                // Get related document details
                _relatedDocument = await _documentService.GetDocumentByIdAsync(_request.RelatedDocumentId);
                if (_relatedDocument == null)
                {
                    Log.Warning("Related document not found for request ID: {RequestId}, document ID: {DocumentId}",
                        _requestId, _request.RelatedDocumentId);
                }

                // Update UI with request and document details
                UpdateUI();

                // Configure UI based on processing mode
                ConfigureProcessingMode();

                Log.Information("Loaded details for request ID: {RequestId}", _requestId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading request details for request ID: {RequestId}", _requestId);
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            try
            {
                // Update title and subtitle
                RequestTitleTextBlock.Text = "Request Details";
                RequestSubtitleTextBlock.Text = $"Request ID: {_request.RequestId}";

                // Update status with color coding
                StatusTextBlock.Text = $"Status: {_request.Status}";
                switch (_request.Status)
                {
                    case "Pending":
                        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
                        break;
                    case "Approved":
                        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                        break;
                    case "Rejected":
                        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                        break;
                    case "Completed":
                        StatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
                        break;
                    default:
                        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                        break;
                }

                // Requestor Information
                RequestorNameTextBlock.Text = _request.RequestorName;
                RequestorAddressTextBlock.Text = _request.RequestorAddress;
                RequestorContactTextBlock.Text = _request.RequestorContact;
                PurposeTextBlock.Text = _request.Purpose;

                // Request Information
                RequestDateTextBlock.Text = _request.RequestDate.ToString("MM/dd/yyyy HH:mm:ss");
                HandledByTextBlock.Text = _request.HandledByUser?.FullName ?? "Not yet handled";
                RequestStatusTextBlock.Text = _request.Status;

                // Related Document Information
                if (_relatedDocument != null)
                {
                    DocumentIdTextBlock.Text = _relatedDocument.DocumentId.ToString();
                    DocumentTypeTextBlock.Text = _relatedDocument.DocumentType;
                    CertificateNumberTextBlock.Text = _relatedDocument.CertificateNumber;
                    DocumentNameTextBlock.Text = $"{_relatedDocument.GivenName} {_relatedDocument.MiddleName} {_relatedDocument.FamilyName}";
                    RegistryOfficeTextBlock.Text = _relatedDocument.RegistryOffice;

                    // Document Preview
                    if (!string.IsNullOrEmpty(_relatedDocument.FilePath) && File.Exists(_relatedDocument.FilePath))
                    {
                        try
                        {
                            DocumentImageSource = _fileStorageService.LoadImageFromFile(_relatedDocument.FilePath);
                            DocumentPreviewImage.Source = DocumentImageSource;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error loading document image preview for document ID: {DocumentId}", _relatedDocument.DocumentId);
                            DocumentPreviewImage.Source = null;
                        }
                    }
                    else
                    {
                        DocumentPreviewImage.Source = null;
                    }
                }
                else
                {
                    DocumentIdTextBlock.Text = _request.RelatedDocumentId.ToString();
                    DocumentTypeTextBlock.Text = "Document not found";
                    CertificateNumberTextBlock.Text = "N/A";
                    DocumentNameTextBlock.Text = "N/A";
                    RegistryOfficeTextBlock.Text = "N/A";
                    DocumentPreviewImage.Source = null;
                }

                // Set DataContext for binding
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating UI with request details for request ID: {RequestId}", _requestId);
                MessageBox.Show($"Error displaying request details: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureProcessingMode()
        {
            if (_processingMode)
            {
                // Show processing controls
                ProcessingGroupBox.Visibility = Visibility.Visible;
                UpdateStatusButton.Visibility = Visibility.Visible;

                // Set default radio button based on current status
                switch (_request.Status)
                {
                    case "Pending":
                        ApproveRadioButton.IsChecked = true;
                        break;
                    case "Approved":
                        CompleteRadioButton.IsChecked = true;
                        break;
                    case "Rejected":
                        RejectRadioButton.IsChecked = true;
                        break;
                    case "Completed":
                        // All options disabled if already completed
                        ApproveRadioButton.IsEnabled = false;
                        RejectRadioButton.IsEnabled = false;
                        CompleteRadioButton.IsEnabled = false;
                        UpdateStatusButton.IsEnabled = false;
                        break;
                }
            }
            else
            {
                // Hide processing controls
                ProcessingGroupBox.Visibility = Visibility.Collapsed;
                UpdateStatusButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ViewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_relatedDocument != null)
                {
                    // Check if front side file exists
                    if (!string.IsNullOrEmpty(_relatedDocument.FilePath) && File.Exists(_relatedDocument.FilePath))
                    {
                        Log.Information("Opening document preview for document ID: {DocumentId}", _relatedDocument.DocumentId);

                        string extension = Path.GetExtension(_relatedDocument.FilePath).ToLower();

                        // Handle different file types
                        if (extension == ".pdf")
                        {
                            // For PDF files, we can't directly display them in the preview window
                            // Instead, we'll offer to open the file externally
                            var result = MessageBox.Show(
                                "PDF files cannot be previewed directly. Would you like to open it in your default PDF viewer?",
                                "PDF Preview", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                // Open the PDF in the default application
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = _relatedDocument.FilePath,
                                    UseShellExecute = true
                                });
                            }
                        }
                        else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tiff")
                        {
                            // For image files, open the preview window
                            var previewWindow = new DocumentPreviewWindow(_relatedDocument.FilePath, "Front Side");
                            previewWindow.Owner = Window.GetWindow(this);
                            previewWindow.ShowDialog();

                            // If there's a back side, offer to view it too
                            if (!string.IsNullOrEmpty(_relatedDocument.BackFilePath) && File.Exists(_relatedDocument.BackFilePath))
                            {
                                var backResult = MessageBox.Show(
                                    "Would you like to view the back side of the document?",
                                    "View Back Side", MessageBoxButton.YesNo, MessageBoxImage.Question);

                                if (backResult == MessageBoxResult.Yes)
                                {
                                    var backPreviewWindow = new DocumentPreviewWindow(_relatedDocument.BackFilePath, "Back Side");
                                    backPreviewWindow.Owner = Window.GetWindow(this);
                                    backPreviewWindow.ShowDialog();
                                }
                            }
                        }
                        else
                        {
                            // Unsupported file type
                            MessageBox.Show("This file type cannot be previewed.", "Preview Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        Log.Warning("Document file not found for document ID: {DocumentId}", _request.RelatedDocumentId);
                        MessageBox.Show("Document file not found or cannot be opened.",
                            "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    Log.Warning("Related document not found for request ID: {RequestId}", _requestId);
                    MessageBox.Show("Related document not found.",
                        "Document Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening document preview for document ID: {DocumentId}", _request.RelatedDocumentId);
                MessageBox.Show($"Error opening document preview: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateStatusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get selected status
                string newStatus = null;
                if (ApproveRadioButton.IsChecked == true)
                    newStatus = "Approved";
                else if (RejectRadioButton.IsChecked == true)
                    newStatus = "Rejected";
                else if (CompleteRadioButton.IsChecked == true)
                    newStatus = "Completed";

                if (string.IsNullOrEmpty(newStatus))
                {
                    MessageBox.Show("Please select a status option.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirm status update
                var result = MessageBox.Show(
                    $"Are you sure you want to update the request status to '{newStatus}'?\nThis action cannot be undone.",
                    "Confirm Status Update",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Disable button to prevent multiple submissions
                    UpdateStatusButton.IsEnabled = false;
                    UpdateStatusButton.Content = "Updating...";

                    // Update request status
                    await _requestService.UpdateRequestStatusAsync(_requestId, newStatus);

                    Log.Information("Updated status for request ID: {RequestId} to {Status}", _requestId, newStatus);
                    MessageBox.Show($"Request status updated to '{newStatus}'.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Close the dialog with success result
                    Window.GetWindow(this).DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating request status for request ID: {RequestId}", _requestId);
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Re-enable button
                UpdateStatusButton.IsEnabled = true;
                UpdateStatusButton.Content = "Update Status";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the dialog with cancel result
            Window.GetWindow(this).DialogResult = false;
        }
    }
}
