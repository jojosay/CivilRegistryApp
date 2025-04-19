using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Infrastructure.Logging;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CivilRegistryApp.Modules.Documents.Views
{
    public partial class DocumentDetailsView : UserControl
    {
        private readonly IDocumentService _documentService = null!;
        private readonly IFileStorageService _fileStorageService = null!;
        private readonly int _documentId;
        private Document? _document;

        public BitmapImage? DocumentImageSource { get; private set; }

        public DocumentDetailsView(IDocumentService documentService, int documentId)
        {
            try
            {
                Log.Debug("Initializing DocumentDetailsView for document ID: {DocumentId}", documentId);
                InitializeComponent();

                _documentService = documentService;
                _documentId = documentId;

                // Get file storage service from document service
                _fileStorageService = (Application.Current as App).ServiceProvider.GetService(typeof(IFileStorageService)) as IFileStorageService;

                // Load document details
                LoadDocumentDetailsAsync();

                Log.Information("DocumentDetailsView initialized successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentDetailsView.Constructor");
                MessageBox.Show($"Error initializing the document details view: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadDocumentDetailsAsync()
        {
            try
            {
                Log.Debug("Loading document details for document ID: {DocumentId}", _documentId);

                // Get document details
                _document = await _documentService.GetDocumentByIdAsync(_documentId);

                if (_document == null)
                {
                    Log.Warning("Document not found with ID: {DocumentId}", _documentId);
                    MessageBox.Show($"Document with ID {_documentId} not found.",
                        "Document Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update UI with document details
                UpdateUI();

                Log.Information("Document details loaded successfully for document ID: {DocumentId}", _documentId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading document details for document ID: {DocumentId}", _documentId);
                MessageBox.Show($"Error loading document details: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            try
            {
                if (_document == null)
                {
                    Log.Warning("Cannot update UI: document is null");
                    return;
                }

                // Update title and subtitle
                DocumentTitleTextBlock.Text = $"{_document.DocumentType} Details";
                DocumentSubtitleTextBlock.Text = $"Certificate #: {_document.CertificateNumber}";

                // Basic Information
                DocumentTypeTextBlock.Text = _document.DocumentType;
                CertificateNumberTextBlock.Text = _document.CertificateNumber;
                // Make Certificate Number copyable
                ClipboardHelper.MakeTextBlockCopyable(CertificateNumberTextBlock);
                RegistryOfficeTextBlock.Text = _document.RegistryOffice;
                DateOfEventTextBlock.Text = _document.DateOfEvent.ToString("MM/dd/yyyy");
                RegistrationDateTextBlock.Text = _document.RegistrationDate.ToString("MM/dd/yyyy");
                GenderTextBlock.Text = _document.Gender;
                UploadedByTextBlock.Text = _document.UploadedByUser?.FullName ?? "Unknown";
                UploadedAtTextBlock.Text = _document.UploadedAt.ToString("MM/dd/yyyy HH:mm:ss");

                // Person Information
                PrefixTextBlock.Text = _document.Prefix ?? "-";
                GivenNameTextBlock.Text = _document.GivenName;
                MiddleNameTextBlock.Text = _document.MiddleName ?? "-";
                FamilyNameTextBlock.Text = _document.FamilyName;
                SuffixTextBlock.Text = _document.Suffix ?? "-";

                // Parents Information
                FatherGivenNameTextBlock.Text = _document.FatherGivenName ?? "-";
                FatherMiddleNameTextBlock.Text = _document.FatherMiddleName ?? "-";
                FatherFamilyNameTextBlock.Text = _document.FatherFamilyName ?? "-";
                MotherMaidenGivenTextBlock.Text = _document.MotherMaidenGiven ?? "-";
                MotherMaidenMiddleNameTextBlock.Text = _document.MotherMaidenMiddleName ?? "-";
                MotherMaidenFamilyTextBlock.Text = _document.MotherMaidenFamily ?? "-";

                // Location Information
                BarangayTextBlock.Text = _document.Barangay;
                CityMunicipalityTextBlock.Text = _document.CityMunicipality;
                ProvinceTextBlock.Text = _document.Province;

                // Registry Information
                RegistryBookTextBlock.Text = _document.RegistryBook ?? "-";
                VolumeNumberTextBlock.Text = _document.VolumeNumber ?? "-";
                PageNumberTextBlock.Text = _document.PageNumber ?? "-";
                LineNumberTextBlock.Text = _document.LineNumber ?? "-";
                AttendantNameTextBlock.Text = _document.AttendantName ?? "-";

                // Additional Information
                RemarksTextBlock.Text = _document.Remarks ?? "-";

                // Update document type-specific fields
                UpdateDocumentTypeSpecificFields();

                // Document Files
                if (!string.IsNullOrEmpty(_document.FilePath))
                {
                    FrontFilePathTextBlock.Text = Path.GetFileName(_document.FilePath);
                    PreviewFrontButton.IsEnabled = true;
                }
                else
                {
                    FrontFilePathTextBlock.Text = "No front document file";
                    PreviewFrontButton.IsEnabled = false;
                }

                if (!string.IsNullOrEmpty(_document.BackFilePath))
                {
                    BackFilePathTextBlock.Text = Path.GetFileName(_document.BackFilePath);
                    PreviewBackButton.IsEnabled = true;
                }
                else
                {
                    BackFilePathTextBlock.Text = "No back document file";
                    PreviewBackButton.IsEnabled = false;
                }

                // Set DataContext for binding
                this.DataContext = this;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating UI with document details for document ID: {DocumentId}", _documentId);
                MessageBox.Show($"Error displaying document details: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewFrontButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = _document?.FilePath;
                if (!string.IsNullOrEmpty(filePath))
                {
                    Log.Debug("Preview front button clicked for file: {FilePath}", filePath);

                    // Ensure the file exists
                    if (!Path.IsPathRooted(filePath))
                    {
                        // Convert relative path to absolute path
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        filePath = Path.Combine(baseDir, filePath);
                    }

                    if (!File.Exists(filePath))
                    {
                        MessageBox.Show($"File not found: {filePath}", "Preview Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string extension = Path.GetExtension(filePath).ToLower();

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
                                FileName = filePath,
                                UseShellExecute = true
                            });
                        }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tiff")
                    {
                        // For image files, open the preview window
                        var previewWindow = new DocumentPreviewWindow(filePath, "Front Side");
                        previewWindow.Owner = Window.GetWindow(this);
                        previewWindow.ShowDialog();
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
                    MessageBox.Show("No file selected for preview.", "Preview Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in PreviewFrontButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewBackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = _document?.BackFilePath;
                if (!string.IsNullOrEmpty(filePath))
                {
                    Log.Debug("Preview back button clicked for file: {FilePath}", filePath);

                    // Ensure the file exists
                    if (!Path.IsPathRooted(filePath))
                    {
                        // Convert relative path to absolute path
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        filePath = Path.Combine(baseDir, filePath);
                    }

                    if (!File.Exists(filePath))
                    {
                        MessageBox.Show($"File not found: {filePath}", "Preview Error",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string extension = Path.GetExtension(filePath).ToLower();

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
                                FileName = filePath,
                                UseShellExecute = true
                            });
                        }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tiff")
                    {
                        // For image files, open the preview window
                        var previewWindow = new DocumentPreviewWindow(filePath, "Back Side");
                        previewWindow.Owner = Window.GetWindow(this);
                        previewWindow.ShowDialog();
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
                    MessageBox.Show("No file selected for preview.", "Preview Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in PreviewBackButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Close button clicked");
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in CloseButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void UpdateDocumentTypeSpecificFields()
        {
            try
            {
                // Hide all document type-specific fields by default
                BirthCertificateFieldsGroup.Visibility = Visibility.Collapsed;
                MarriageCertificateFieldsGroup.Visibility = Visibility.Collapsed;
                DeathCertificateFieldsGroup.Visibility = Visibility.Collapsed;
                OtherDocumentFieldsGroup.Visibility = Visibility.Collapsed;

                if (_document == null)
                {
                    Log.Warning("Cannot update document type fields: document is null");
                    return;
                }

                // Show the appropriate fields based on document type
                switch (_document.DocumentType)
                {
                    case "Birth Certificate":
                        BirthCertificateFieldsGroup.Visibility = Visibility.Visible;
                        ExtractBirthCertificateInfo();
                        // Show Person Information for Birth Certificate
                        PersonInformationGroupBox.Visibility = Visibility.Visible;
                        ParentsInformationGroupBox.Visibility = Visibility.Visible;
                        break;

                    case "Marriage Certificate":
                        MarriageCertificateFieldsGroup.Visibility = Visibility.Visible;
                        ExtractMarriageCertificateInfo();
                        // Hide Person Information for Marriage Certificate
                        PersonInformationGroupBox.Visibility = Visibility.Collapsed;
                        ParentsInformationGroupBox.Visibility = Visibility.Collapsed;
                        break;

                    case "Death Certificate":
                        DeathCertificateFieldsGroup.Visibility = Visibility.Visible;
                        ExtractDeathCertificateInfo();
                        // Show Person Information for Death Certificate
                        PersonInformationGroupBox.Visibility = Visibility.Visible;
                        ParentsInformationGroupBox.Visibility = Visibility.Visible;
                        break;

                    case "CENOMAR":
                    case "Other":
                        OtherDocumentFieldsGroup.Visibility = Visibility.Visible;
                        ExtractOtherDocumentInfo();
                        // Show Person Information for Other Document types
                        PersonInformationGroupBox.Visibility = Visibility.Visible;
                        ParentsInformationGroupBox.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating document type-specific fields for document ID: {DocumentId}", _documentId);
            }
        }

        private void ExtractBirthCertificateInfo()
        {
            if (string.IsNullOrEmpty(_document.Remarks))
                return;

            try
            {
                if (_document.Remarks.Contains("Birth Weight:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Birth Weight:") + "Birth Weight:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string weightStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        BirthWeightTextBlock.Text = weightStr;
                    }
                }

                if (_document.Remarks.Contains("Birth Order:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Birth Order:") + "Birth Order:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string orderStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        BirthOrderTextBlock.Text = orderStr;
                    }
                }

                if (_document.Remarks.Contains("Type of Birth:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Type of Birth:") + "Type of Birth:".Length;
                    string typeStr = _document.Remarks.Substring(startIndex).Trim();
                    TypeOfBirthTextBlock.Text = typeStr;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting birth certificate information for document ID: {DocumentId}", _documentId);
            }
        }

        private void ExtractMarriageCertificateInfo()
        {
            if (string.IsNullOrEmpty(_document.Remarks))
                return;

            try
            {
                if (_document.Remarks.Contains("Husband:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Husband:") + "Husband:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string husbandStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        string[] husbandParts = husbandStr.Split(' ');

                        if (husbandParts.Length >= 3)
                        {
                            HusbandGivenNameTextBlock.Text = husbandParts[0];
                            HusbandMiddleNameTextBlock.Text = husbandParts[1];
                            HusbandFamilyNameTextBlock.Text = husbandParts[2];
                        }
                        else if (husbandParts.Length == 2)
                        {
                            HusbandGivenNameTextBlock.Text = husbandParts[0];
                            HusbandFamilyNameTextBlock.Text = husbandParts[1];
                        }
                    }
                }

                if (_document.Remarks.Contains("Wife:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Wife:") + "Wife:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string wifeStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        string[] wifeParts = wifeStr.Split(' ');

                        if (wifeParts.Length >= 3)
                        {
                            WifeGivenNameTextBlock.Text = wifeParts[0];
                            WifeMiddleNameTextBlock.Text = wifeParts[1];
                            WifeFamilyNameTextBlock.Text = wifeParts[2];
                        }
                        else if (wifeParts.Length == 2)
                        {
                            WifeGivenNameTextBlock.Text = wifeParts[0];
                            WifeFamilyNameTextBlock.Text = wifeParts[1];
                        }
                    }
                }

                if (_document.Remarks.Contains("Marriage Date:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Marriage Date:") + "Marriage Date:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string dateStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        MarriageDateTextBlock.Text = dateStr;
                    }
                }

                if (_document.Remarks.Contains("Marriage Type:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Marriage Type:") + "Marriage Type:".Length;
                    string typeStr = _document.Remarks.Substring(startIndex).Trim();
                    MarriageTypeTextBlock.Text = typeStr;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting marriage certificate information for document ID: {DocumentId}", _documentId);
            }
        }

        private void ExtractDeathCertificateInfo()
        {
            if (string.IsNullOrEmpty(_document.Remarks))
                return;

            try
            {
                if (_document.Remarks.Contains("Cause of Death:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Cause of Death:") + "Cause of Death:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string causeStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        CauseOfDeathTextBlock.Text = causeStr;
                    }
                }

                if (_document.Remarks.Contains("Age at Death:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Age at Death:") + "Age at Death:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string ageStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        AgeAtDeathTextBlock.Text = ageStr;
                    }
                }

                if (_document.Remarks.Contains("Place of Death:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Place of Death:") + "Place of Death:".Length;
                    string placeStr = _document.Remarks.Substring(startIndex).Trim();
                    PlaceOfDeathTextBlock.Text = placeStr;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting death certificate information for document ID: {DocumentId}", _documentId);
            }
        }

        private void ExtractOtherDocumentInfo()
        {
            if (string.IsNullOrEmpty(_document.Remarks))
                return;

            try
            {
                if (_document.Remarks.Contains("Document Purpose:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Document Purpose:") + "Document Purpose:".Length;
                    int endIndex = _document.Remarks.IndexOf(",", startIndex);
                    if (endIndex > startIndex)
                    {
                        string purposeStr = _document.Remarks.Substring(startIndex, endIndex - startIndex).Trim();
                        DocumentPurposeTextBlock.Text = purposeStr;
                    }
                }

                if (_document.Remarks.Contains("Document Category:"))
                {
                    int startIndex = _document.Remarks.IndexOf("Document Category:") + "Document Category:".Length;
                    string categoryStr = _document.Remarks.Substring(startIndex).Trim();
                    DocumentCategoryTextBlock.Text = categoryStr;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting other document information for document ID: {DocumentId}", _documentId);
            }
        }
    }
}
