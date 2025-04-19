using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Admin;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Diagnostics;

namespace CivilRegistryApp.Modules.Documents.Views
{
    public partial class DocumentEditView : UserControl
    {
        private readonly IDocumentService _documentService = null!;
        private readonly IFileStorageService _fileStorageService = null!;
        private readonly IFieldConfigurationService _fieldConfigService = null!;
        private readonly int _documentId;
        private Document _document = null!;
        private string? _selectedFrontFilePath;
        private string? _selectedBackFilePath;
        private string _currentDocumentType = "Birth Certificate";

        public DocumentEditView(IDocumentService documentService, int documentId)
        {
            try
            {
                Log.Debug("Initializing DocumentEditView for document ID: {DocumentId}", documentId);
                InitializeComponent();

                _documentService = documentService;
                _documentId = documentId;

                // Get services from the application's service provider
                _fileStorageService = (Application.Current as App).ServiceProvider.GetService(typeof(IFileStorageService)) as IFileStorageService;
                _fieldConfigService = (Application.Current as App).ServiceProvider.GetService(typeof(IFieldConfigurationService)) as IFieldConfigurationService;

                // Load document details
                LoadDocumentDetailsAsync();

                Log.Information("DocumentEditView initialized successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentEditView.Constructor");
                MessageBox.Show($"Error initializing the document edit view: {ex.Message}",
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
                    Window.GetWindow(this)?.Close();
                    return;
                }

                // Update UI with document details
                UpdateUI();

                // Mark required fields with red asterisks based on field configuration
                MarkRequiredFieldsAsync(_document.DocumentType);

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
                // Update title and subtitle
                DocumentTitleTextBlock.Text = $"Edit {_document.DocumentType}";
                DocumentSubtitleTextBlock.Text = $"Certificate #: {_document.CertificateNumber}";

                // Make certificate number copyable
                ClipboardHelper.MakeTextBlockCopyable(DocumentSubtitleTextBlock);

                // Basic Information
                DocumentTypeComboBox.SelectedIndex = GetDocumentTypeIndex(_document.DocumentType);
                _currentDocumentType = _document.DocumentType;
                CertificateNumberTextBox.Text = _document.CertificateNumber;
                RegistryOfficeTextBox.Text = _document.RegistryOffice;
                DateOfEventDatePicker.SelectedDate = _document.DateOfEvent;
                RegistrationDateDatePicker.SelectedDate = _document.RegistrationDate;
                GenderComboBox.SelectedIndex = _document.Gender == "M" ? 0 : 1;

                // Person Information
                PrefixTextBox.Text = _document.Prefix ?? string.Empty;
                GivenNameTextBox.Text = _document.GivenName;
                MiddleNameTextBox.Text = _document.MiddleName ?? string.Empty;
                FamilyNameTextBox.Text = _document.FamilyName;
                SuffixTextBox.Text = _document.Suffix ?? string.Empty;

                // Parents Information
                FatherGivenNameTextBox.Text = _document.FatherGivenName ?? string.Empty;
                FatherMiddleNameTextBox.Text = _document.FatherMiddleName ?? string.Empty;
                FatherFamilyNameTextBox.Text = _document.FatherFamilyName ?? string.Empty;
                MotherMaidenGivenTextBox.Text = _document.MotherMaidenGiven ?? string.Empty;
                MotherMaidenMiddleNameTextBox.Text = _document.MotherMaidenMiddleName ?? string.Empty;
                MotherMaidenFamilyTextBox.Text = _document.MotherMaidenFamily ?? string.Empty;

                // Location Information
                BarangayTextBox.Text = _document.Barangay;
                CityMunicipalityTextBox.Text = _document.CityMunicipality;
                ProvinceTextBox.Text = _document.Province;

                // Registry Information
                RegistryBookTextBox.Text = _document.RegistryBook ?? string.Empty;
                VolumeNumberTextBox.Text = _document.VolumeNumber ?? string.Empty;
                PageNumberTextBox.Text = _document.PageNumber ?? string.Empty;
                LineNumberTextBox.Text = _document.LineNumber ?? string.Empty;
                AttendantNameTextBox.Text = _document.AttendantName ?? string.Empty;

                // Additional Information
                RemarksTextBox.Text = _document.Remarks ?? string.Empty;

                // Document type-specific fields
                // Parse document type-specific information from Remarks
                if (!string.IsNullOrEmpty(_document.Remarks))
                {
                    string remarks = _document.Remarks;

                    switch (_document.DocumentType)
                    {
                        case "Birth Certificate":
                            // Try to extract birth certificate information from remarks
                            if (remarks.Contains("Birth Weight:"))
                            {
                                int startIndex = remarks.IndexOf("Birth Weight:") + "Birth Weight:".Length;
                                int endIndex = remarks.IndexOf("kg,", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string weightStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    BirthWeightTextBox.Text = weightStr;
                                }
                            }

                            if (remarks.Contains("Birth Order:"))
                            {
                                int startIndex = remarks.IndexOf("Birth Order:") + "Birth Order:".Length;
                                int endIndex = remarks.IndexOf(",", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string orderStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    BirthOrderTextBox.Text = orderStr;
                                }
                            }

                            if (remarks.Contains("Type of Birth:"))
                            {
                                int startIndex = remarks.IndexOf("Type of Birth:") + "Type of Birth:".Length;
                                string typeStr = remarks.Substring(startIndex).Trim();

                                foreach (ComboBoxItem item in TypeOfBirthComboBox.Items)
                                {
                                    if (typeStr.StartsWith(item.Content.ToString()))
                                    {
                                        TypeOfBirthComboBox.SelectedItem = item;
                                        break;
                                    }
                                }
                            }
                            break;

                        case "Marriage Certificate":
                            // Try to extract marriage certificate information from remarks
                            if (remarks.Contains("Husband:"))
                            {
                                int startIndex = remarks.IndexOf("Husband:") + "Husband:".Length;
                                int endIndex = remarks.IndexOf(",", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string husbandStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    string[] husbandParts = husbandStr.Split(' ');

                                    if (husbandParts.Length >= 3)
                                    {
                                        HusbandGivenNameTextBox.Text = husbandParts[0];
                                        HusbandMiddleNameTextBox.Text = husbandParts[1];
                                        HusbandFamilyNameTextBox.Text = husbandParts[2];
                                    }
                                    else if (husbandParts.Length == 2)
                                    {
                                        HusbandGivenNameTextBox.Text = husbandParts[0];
                                        HusbandFamilyNameTextBox.Text = husbandParts[1];
                                    }
                                }
                            }

                            if (remarks.Contains("Wife:"))
                            {
                                int startIndex = remarks.IndexOf("Wife:") + "Wife:".Length;
                                int endIndex = remarks.IndexOf(",", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string wifeStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    string[] wifeParts = wifeStr.Split(' ');

                                    if (wifeParts.Length >= 3)
                                    {
                                        WifeGivenNameTextBox.Text = wifeParts[0];
                                        WifeMiddleNameTextBox.Text = wifeParts[1];
                                        WifeFamilyNameTextBox.Text = wifeParts[2];
                                    }
                                    else if (wifeParts.Length == 2)
                                    {
                                        WifeGivenNameTextBox.Text = wifeParts[0];
                                        WifeFamilyNameTextBox.Text = wifeParts[1];
                                    }
                                }
                            }

                            if (remarks.Contains("Marriage Date:"))
                            {
                                int startIndex = remarks.IndexOf("Marriage Date:") + "Marriage Date:".Length;
                                int endIndex = remarks.IndexOf(",", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string dateStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    if (DateTime.TryParse(dateStr, out DateTime marriageDate))
                                    {
                                        MarriageDatePicker.SelectedDate = marriageDate;
                                    }
                                }
                            }

                            if (remarks.Contains("Marriage Type:"))
                            {
                                int startIndex = remarks.IndexOf("Marriage Type:") + "Marriage Type:".Length;
                                string typeStr = remarks.Substring(startIndex).Trim();

                                foreach (ComboBoxItem item in MarriageTypeComboBox.Items)
                                {
                                    if (typeStr.StartsWith(item.Content.ToString()))
                                    {
                                        MarriageTypeComboBox.SelectedItem = item;
                                        break;
                                    }
                                }
                            }
                            break;

                        case "Death Certificate":
                            // Try to extract death certificate information from remarks
                            if (remarks.Contains("Cause of Death:"))
                            {
                                int startIndex = remarks.IndexOf("Cause of Death:") + "Cause of Death:".Length;
                                int endIndex = remarks.IndexOf(",", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string causeStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    CauseOfDeathTextBox.Text = causeStr;
                                }
                            }

                            if (remarks.Contains("Age at Death:"))
                            {
                                int startIndex = remarks.IndexOf("Age at Death:") + "Age at Death:".Length;
                                int endIndex = remarks.IndexOf(",", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string ageStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    AgeAtDeathTextBox.Text = ageStr;
                                }
                            }

                            if (remarks.Contains("Place of Death:"))
                            {
                                int startIndex = remarks.IndexOf("Place of Death:") + "Place of Death:".Length;
                                string placeStr = remarks.Substring(startIndex).Trim();
                                PlaceOfDeathTextBox.Text = placeStr;
                            }
                            break;

                        case "Other":
                            // Try to extract other document information from remarks
                            if (remarks.Contains("Document Purpose:"))
                            {
                                int startIndex = remarks.IndexOf("Document Purpose:") + "Document Purpose:".Length;
                                int endIndex = remarks.IndexOf(",", startIndex);
                                if (endIndex > startIndex)
                                {
                                    string purposeStr = remarks.Substring(startIndex, endIndex - startIndex).Trim();
                                    DocumentPurposeTextBox.Text = purposeStr;
                                }
                            }

                            if (remarks.Contains("Document Category:"))
                            {
                                int startIndex = remarks.IndexOf("Document Category:") + "Document Category:".Length;
                                string categoryStr = remarks.Substring(startIndex).Trim();
                                DocumentCategoryTextBox.Text = categoryStr;
                            }
                            break;
                    }
                }

                // Update UI based on document type
                UpdateUIBasedOnDocumentType(_document.DocumentType);

                // Document Files
                if (!string.IsNullOrEmpty(_document.FilePath))
                {
                    FrontFilePathTextBox.Text = Path.GetFileName(_document.FilePath);
                    PreviewFrontButton.IsEnabled = true;
                }

                if (!string.IsNullOrEmpty(_document.BackFilePath))
                {
                    BackFilePathTextBox.Text = Path.GetFileName(_document.BackFilePath);
                    PreviewBackButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating UI with document details for document ID: {DocumentId}", _documentId);
                MessageBox.Show($"Error displaying document details: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUIBasedOnDocumentType(string documentType)
        {
            try
            {
                // Hide all document type-specific fields first
                BirthCertificateFieldsGroup.Visibility = Visibility.Collapsed;
                MarriageCertificateFieldsGroup.Visibility = Visibility.Collapsed;
                DeathCertificateFieldsGroup.Visibility = Visibility.Collapsed;
                OtherDocumentFieldsGroup.Visibility = Visibility.Collapsed;

                // Show the appropriate fields based on document type
                switch (documentType)
                {
                    case "Birth Certificate":
                        BirthCertificateFieldsGroup.Visibility = Visibility.Visible;
                        PersonInformationGroupBox.Visibility = Visibility.Visible;
                        break;

                    case "Marriage Certificate":
                        MarriageCertificateFieldsGroup.Visibility = Visibility.Visible;
                        PersonInformationGroupBox.Visibility = Visibility.Collapsed; // Hide person section for Marriage Certificate
                        break;

                    case "Death Certificate":
                        DeathCertificateFieldsGroup.Visibility = Visibility.Visible;
                        PersonInformationGroupBox.Visibility = Visibility.Visible;
                        break;

                    case "CENOMAR":
                        OtherDocumentFieldsGroup.Visibility = Visibility.Visible;
                        // Update the header for CENOMAR
                        OtherDocumentFieldsGroup.Header = "CENOMAR Information";
                        PersonInformationGroupBox.Visibility = Visibility.Collapsed; // Hide person section for CENOMAR
                        break;

                    case "Other":
                        OtherDocumentFieldsGroup.Visibility = Visibility.Visible;
                        // Update the header for Other document types
                        OtherDocumentFieldsGroup.Header = "Other Document Information";
                        PersonInformationGroupBox.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating UI based on document type");
                MessageBox.Show($"Error updating UI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DocumentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DocumentTypeComboBox.SelectedItem != null)
                {
                    string documentType = (DocumentTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Birth Certificate";
                    _currentDocumentType = documentType;
                    UpdateUIBasedOnDocumentType(documentType);

                    // Update required field markers based on the new document type
                    MarkRequiredFieldsAsync(documentType);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in DocumentTypeComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetDocumentTypeIndex(string documentType)
        {
            switch (documentType)
            {
                case "Birth Certificate":
                    return 0;
                case "Marriage Certificate":
                    return 1;
                case "Death Certificate":
                    return 2;
                case "CENOMAR":
                    return 3;
                case "Other":
                    return 4;
                default:
                    return 0;
            }
        }

        private void BrowseFrontButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Browse front button clicked");

                string? filePath = _fileStorageService.OpenFileDialog("Document Files (*.pdf;*.jpg;*.jpeg;*.png;*.tiff)|*.pdf;*.jpg;*.jpeg;*.png;*.tiff|All files (*.*)|*.*");

                if (!string.IsNullOrEmpty(filePath))
                {
                    _selectedFrontFilePath = filePath;
                    FrontFilePathTextBox.Text = filePath;
                    Log.Debug("Front file selected: {FilePath}", filePath);

                    // Enable the preview button
                    PreviewFrontButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in BrowseFrontButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseBackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Browse back button clicked");

                string? filePath = _fileStorageService.OpenFileDialog("Document Files (*.pdf;*.jpg;*.jpeg;*.png;*.tiff)|*.pdf;*.jpg;*.jpeg;*.png;*.tiff|All files (*.*)|*.*");

                if (!string.IsNullOrEmpty(filePath))
                {
                    _selectedBackFilePath = filePath;
                    BackFilePathTextBox.Text = filePath;
                    Log.Debug("Back file selected: {FilePath}", filePath);

                    // Enable the preview button
                    PreviewBackButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in BrowseBackButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Marks required fields with red asterisks based on field configuration
        /// </summary>
        /// <param name="documentType">The document type to get required fields for</param>
        private async void MarkRequiredFieldsAsync(string documentType)
        {
            try
            {
                // Clear all asterisks first by resetting all TextBlocks
                ClearAllRequiredFieldMarkers();

                if (_fieldConfigService == null)
                {
                    Log.Warning("FieldConfigurationService is not available. Using default required fields.");
                    MarkDefaultRequiredFields();
                    return;
                }

                // Get required fields from the field configuration service
                var requiredFields = await _fieldConfigService.GetRequiredFieldsAsync(documentType);

                if (requiredFields == null || !requiredFields.Any())
                {
                    Log.Warning("No required fields found for document type {DocumentType}. Using default required fields.", documentType);
                    MarkDefaultRequiredFields();
                    return;
                }

                // Mark each required field with a red asterisk
                foreach (var fieldName in requiredFields)
                {
                    // Convert field name to label name (e.g., "GivenName" -> "GivenNameLabel")
                    string labelName = fieldName + "Label";
                    var textBlock = this.FindName(labelName) as TextBlock;

                    // Handle special cases where field names don't match exactly with label names
                    if (textBlock == null)
                    {
                        // Try some common mappings
                        switch (fieldName)
                        {
                            case "MotherMaidenGiven":
                                textBlock = this.FindName("MotherMaidenGivenLabel") as TextBlock;
                                break;
                            case "MotherMaidenFamily":
                                textBlock = this.FindName("MotherMaidenFamilyLabel") as TextBlock;
                                break;
                            case "PlaceOfMarriage":
                                // This field might not exist in the UI
                                break;
                            case "SolemnizingOfficer":
                                // This field might not exist in the UI
                                break;
                            case "DateOfBirth":
                                textBlock = this.FindName("DateOfEventLabel") as TextBlock; // Use DateOfEvent for Birth Certificate
                                break;
                            case "PlaceOfBirth":
                                // This might map to Barangay, City, Province
                                var barangayLabel = this.FindName("BarangayLabel") as TextBlock;
                                var cityLabel = this.FindName("CityMunicipalityLabel") as TextBlock;
                                var provinceLabel = this.FindName("ProvinceLabel") as TextBlock;

                                if (barangayLabel != null) RequiredFieldHelper.MarkAsRequired(barangayLabel);
                                if (cityLabel != null) RequiredFieldHelper.MarkAsRequired(cityLabel);
                                if (provinceLabel != null) RequiredFieldHelper.MarkAsRequired(provinceLabel);
                                break;
                            case "Purpose":
                                textBlock = this.FindName("DocumentPurposeLabel") as TextBlock;
                                break;
                            case "CauseOfDeath":
                                textBlock = this.FindName("CauseOfDeathLabel") as TextBlock;
                                break;
                            case "PlaceOfDeath":
                                textBlock = this.FindName("PlaceOfDeathLabel") as TextBlock;
                                break;
                            case "AgeAtDeath":
                                textBlock = this.FindName("AgeAtDeathLabel") as TextBlock;
                                break;
                        }
                    }

                    if (textBlock != null)
                    {
                        RequiredFieldHelper.MarkAsRequired(textBlock);
                    }
                    else
                    {
                        Log.Debug("Could not find TextBlock for required field {FieldName}", fieldName);
                    }
                }

                // Always mark the front side document as required
                var frontSideLabel = this.FindName("FrontSideLabel") as TextBlock;
                if (frontSideLabel != null)
                {
                    RequiredFieldHelper.MarkAsRequired(frontSideLabel);
                }

                Log.Debug("Marked {Count} required fields for document type {DocumentType}", requiredFields.Count(), documentType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error marking required fields for document type {DocumentType}", documentType);
                // Don't show a message box here as this is called during initialization
            }
        }

        /// <summary>
        /// Clears all required field markers (red asterisks)
        /// </summary>
        private void ClearAllRequiredFieldMarkers()
        {
            try
            {
                // Find all TextBlocks that might have been marked as required
                var textBlocks = new[]
                {
                    // Basic Information
                    this.FindName("CertificateNumberLabel") as TextBlock,
                    this.FindName("RegistryOfficeLabel") as TextBlock,
                    this.FindName("DateOfEventLabel") as TextBlock,
                    this.FindName("RegistrationDateLabel") as TextBlock,
                    this.FindName("BarangayLabel") as TextBlock,
                    this.FindName("CityMunicipalityLabel") as TextBlock,
                    this.FindName("ProvinceLabel") as TextBlock,

                    // Person Information
                    this.FindName("GivenNameLabel") as TextBlock,
                    this.FindName("MiddleNameLabel") as TextBlock,
                    this.FindName("FamilyNameLabel") as TextBlock,
                    this.FindName("PrefixLabel") as TextBlock,
                    this.FindName("SuffixLabel") as TextBlock,

                    // Birth Certificate
                    this.FindName("BirthWeightLabel") as TextBlock,
                    this.FindName("BirthOrderLabel") as TextBlock,

                    // Marriage Certificate
                    this.FindName("HusbandGivenNameLabel") as TextBlock,
                    this.FindName("HusbandMiddleNameLabel") as TextBlock,
                    this.FindName("HusbandFamilyNameLabel") as TextBlock,
                    this.FindName("WifeGivenNameLabel") as TextBlock,
                    this.FindName("WifeMiddleNameLabel") as TextBlock,
                    this.FindName("WifeFamilyNameLabel") as TextBlock,
                    this.FindName("MarriageDateLabel") as TextBlock,

                    // Death Certificate
                    this.FindName("CauseOfDeathLabel") as TextBlock,
                    this.FindName("AgeAtDeathLabel") as TextBlock,
                    this.FindName("PlaceOfDeathLabel") as TextBlock,

                    // Other/CENOMAR
                    this.FindName("DocumentPurposeLabel") as TextBlock,
                    this.FindName("DocumentCategoryLabel") as TextBlock,

                    // Parents Information
                    this.FindName("FatherGivenNameLabel") as TextBlock,
                    this.FindName("FatherMiddleNameLabel") as TextBlock,
                    this.FindName("FatherFamilyNameLabel") as TextBlock,
                    this.FindName("MotherMaidenGivenLabel") as TextBlock,
                    this.FindName("MotherMaidenMiddleNameLabel") as TextBlock,
                    this.FindName("MotherMaidenFamilyLabel") as TextBlock,

                    // Registry Information
                    this.FindName("RegistryBookLabel") as TextBlock,
                    this.FindName("VolumeNumberLabel") as TextBlock,
                    this.FindName("PageNumberLabel") as TextBlock,
                    this.FindName("LineNumberLabel") as TextBlock,
                    this.FindName("AttendantNameLabel") as TextBlock,

                    // Document Files
                    this.FindName("FrontSideLabel") as TextBlock,
                    this.FindName("BackSideLabel") as TextBlock
                };

                // Use the RequiredFieldHelper to unmark all TextBlocks
                RequiredFieldHelper.UnmarkAsRequired(textBlocks.Where(tb => tb != null).ToArray());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error clearing required field markers");
            }
        }

        /// <summary>
        /// Marks default required fields with red asterisks (used as fallback)
        /// </summary>
        private void MarkDefaultRequiredFields()
        {
            try
            {
                // Common required fields
                RequiredFieldHelper.MarkAsRequired(
                    // Basic Information
                    this.FindName("CertificateNumberLabel") as TextBlock,
                    this.FindName("RegistryOfficeLabel") as TextBlock,
                    this.FindName("DateOfEventLabel") as TextBlock,
                    this.FindName("RegistrationDateLabel") as TextBlock,
                    this.FindName("CityMunicipalityLabel") as TextBlock,
                    this.FindName("ProvinceLabel") as TextBlock,

                    // Person Information (for most document types)
                    this.FindName("GivenNameLabel") as TextBlock,
                    this.FindName("FamilyNameLabel") as TextBlock,

                    // Document Files
                    this.FindName("FrontSideLabel") as TextBlock
                );

                // Birth Certificate specific required fields
                if (_currentDocumentType == "Birth Certificate")
                {
                    RequiredFieldHelper.MarkAsRequired(
                        this.FindName("BirthWeightLabel") as TextBlock,
                        this.FindName("BirthOrderLabel") as TextBlock
                    );
                }

                // Marriage Certificate specific required fields
                else if (_currentDocumentType == "Marriage Certificate")
                {
                    RequiredFieldHelper.MarkAsRequired(
                        this.FindName("HusbandGivenNameLabel") as TextBlock,
                        this.FindName("HusbandFamilyNameLabel") as TextBlock,
                        this.FindName("WifeGivenNameLabel") as TextBlock,
                        this.FindName("WifeFamilyNameLabel") as TextBlock,
                        this.FindName("MarriageDateLabel") as TextBlock
                    );
                }

                // Death Certificate specific required fields
                else if (_currentDocumentType == "Death Certificate")
                {
                    RequiredFieldHelper.MarkAsRequired(
                        this.FindName("CauseOfDeathLabel") as TextBlock,
                        this.FindName("AgeAtDeathLabel") as TextBlock
                    );
                }

                // CENOMAR and Other document types specific required fields
                else if (_currentDocumentType == "CENOMAR" || _currentDocumentType == "Other")
                {
                    RequiredFieldHelper.MarkAsRequired(
                        this.FindName("DocumentPurposeLabel") as TextBlock
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error marking default required fields");
            }
        }

        private void PreviewFrontButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = _selectedFrontFilePath ?? _document.FilePath;
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
                string filePath = _selectedBackFilePath ?? _document.BackFilePath;
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Save button clicked");

                // Validate common required fields
                bool commonFieldsValid = !string.IsNullOrWhiteSpace(CertificateNumberTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(RegistryOfficeTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(BarangayTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(CityMunicipalityTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(ProvinceTextBox.Text) &&
                    DateOfEventDatePicker.SelectedDate != null &&
                    RegistrationDateDatePicker.SelectedDate != null;

                // For Marriage Certificate and CENOMAR, we don't need to validate Person Information fields
                if (_currentDocumentType == "Marriage Certificate" || _currentDocumentType == "CENOMAR")
                {
                    if (!commonFieldsValid)
                    {
                        MessageBox.Show("Please fill in all required fields.",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    // For other document types, also validate Person Information fields
                    if (!commonFieldsValid ||
                        string.IsNullOrWhiteSpace(GivenNameTextBox.Text) ||
                        string.IsNullOrWhiteSpace(FamilyNameTextBox.Text))
                    {
                        MessageBox.Show("Please fill in all required fields.",
                            "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Validate document type-specific required fields
                bool isValid = true;
                string errorMessage = "Please fill in all required fields for this document type.";

                switch (_currentDocumentType)
                {
                    case "Birth Certificate":
                        if (string.IsNullOrWhiteSpace(BirthWeightTextBox.Text) ||
                            string.IsNullOrWhiteSpace(BirthOrderTextBox.Text))
                        {
                            isValid = false;
                        }
                        break;

                    case "Marriage Certificate":
                        if (string.IsNullOrWhiteSpace(HusbandGivenNameTextBox.Text) ||
                            string.IsNullOrWhiteSpace(HusbandFamilyNameTextBox.Text) ||
                            string.IsNullOrWhiteSpace(WifeGivenNameTextBox.Text) ||
                            string.IsNullOrWhiteSpace(WifeFamilyNameTextBox.Text) ||
                            MarriageDatePicker.SelectedDate == null)
                        {
                            isValid = false;
                        }
                        break;

                    case "Death Certificate":
                        if (string.IsNullOrWhiteSpace(CauseOfDeathTextBox.Text) ||
                            string.IsNullOrWhiteSpace(AgeAtDeathTextBox.Text))
                        {
                            isValid = false;
                        }
                        break;

                    case "CENOMAR":
                        if (string.IsNullOrWhiteSpace(DocumentPurposeTextBox.Text))
                        {
                            isValid = false;
                        }
                        break;

                    case "Other":
                        if (string.IsNullOrWhiteSpace(DocumentPurposeTextBox.Text))
                        {
                            isValid = false;
                        }
                        break;
                }

                if (!isValid)
                {
                    MessageBox.Show(errorMessage, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update document with form values
                _document.DocumentType = (DocumentTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Other";
                _document.CertificateNumber = CertificateNumberTextBox.Text.Trim();
                _document.RegistryOffice = RegistryOfficeTextBox.Text.Trim();
                _document.DateOfEvent = DateOfEventDatePicker.SelectedDate ?? DateTime.Today;
                _document.RegistrationDate = RegistrationDateDatePicker.SelectedDate ?? DateTime.Today;
                _document.Gender = (GenderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "M";

                // Handle document type-specific fields
                if (_currentDocumentType == "Marriage Certificate")
                {
                    // For Marriage Certificate, use the husband's name as the primary name
                    _document.Prefix = "";
                    _document.GivenName = HusbandGivenNameTextBox.Text.Trim();
                    _document.MiddleName = HusbandMiddleNameTextBox.Text.Trim();
                    _document.FamilyName = HusbandFamilyNameTextBox.Text.Trim();
                    _document.Suffix = "";

                    // Store marriage-specific fields in remarks
                    string marriageInfo = $"Husband: {HusbandGivenNameTextBox.Text.Trim()} {HusbandMiddleNameTextBox.Text.Trim()} {HusbandFamilyNameTextBox.Text.Trim()}, " +
                                         $"Wife: {WifeGivenNameTextBox.Text.Trim()} {WifeMiddleNameTextBox.Text.Trim()} {WifeFamilyNameTextBox.Text.Trim()}, " +
                                         $"Marriage Date: {MarriageDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd")}, " +
                                         $"Marriage Type: {(MarriageTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Civil"}";

                    // Append to existing remarks or set as new remarks
                    if (!string.IsNullOrEmpty(_document.Remarks) && !_document.Remarks.Contains("Husband:"))
                    {
                        _document.Remarks += "\n\nAdditional Information:\n" + marriageInfo;
                    }
                    else
                    {
                        // Extract any user-entered remarks (before the Additional Information section)
                        string userRemarks = RemarksTextBox.Text.Trim();
                        if (userRemarks.Contains("Additional Information:"))
                        {
                            userRemarks = userRemarks.Substring(0, userRemarks.IndexOf("Additional Information:")).Trim();
                        }

                        if (!string.IsNullOrEmpty(userRemarks))
                        {
                            _document.Remarks = userRemarks + "\n\nAdditional Information:\n" + marriageInfo;
                        }
                        else
                        {
                            _document.Remarks = "Additional Information:\n" + marriageInfo;
                        }
                    }
                }
                else if (_currentDocumentType == "CENOMAR")
                {
                    // For CENOMAR, we don't use the Person Information section
                    // Use default values for required fields
                    _document.Prefix = "";
                    _document.GivenName = "CENOMAR";
                    _document.MiddleName = "";
                    _document.FamilyName = "Document";
                    _document.Suffix = "";

                    // Store CENOMAR-specific fields in remarks
                    string cenomarInfo = $"Document Purpose: {DocumentPurposeTextBox.Text.Trim()}, " +
                                         $"Document Category: {DocumentCategoryTextBox.Text.Trim()}";

                    // Append to existing remarks or set as new remarks
                    if (!string.IsNullOrEmpty(_document.Remarks) && !_document.Remarks.Contains("Document Purpose:"))
                    {
                        _document.Remarks += "\n\nAdditional Information:\n" + cenomarInfo;
                    }
                    else
                    {
                        // Extract any user-entered remarks (before the Additional Information section)
                        string userRemarks = RemarksTextBox.Text.Trim();
                        if (userRemarks.Contains("Additional Information:"))
                        {
                            userRemarks = userRemarks.Substring(0, userRemarks.IndexOf("Additional Information:")).Trim();
                        }

                        if (!string.IsNullOrEmpty(userRemarks))
                        {
                            _document.Remarks = userRemarks + "\n\nAdditional Information:\n" + cenomarInfo;
                        }
                        else
                        {
                            _document.Remarks = "Additional Information:\n" + cenomarInfo;
                        }
                    }
                }
                else
                {
                    // For other document types, use the Person Information section
                    _document.Prefix = PrefixTextBox.Text.Trim();
                    _document.GivenName = GivenNameTextBox.Text.Trim();
                    _document.MiddleName = MiddleNameTextBox.Text.Trim();
                    _document.FamilyName = FamilyNameTextBox.Text.Trim();
                    _document.Suffix = SuffixTextBox.Text.Trim();

                    // Store document type-specific fields in remarks
                    string additionalInfo = "";

                    switch (_currentDocumentType)
                    {
                        case "Birth Certificate":
                            additionalInfo = $"Birth Weight: {BirthWeightTextBox.Text.Trim()} kg, " +
                                            $"Birth Order: {BirthOrderTextBox.Text.Trim()}, " +
                                            $"Type of Birth: {(TypeOfBirthComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Single"}";
                            break;

                        case "Death Certificate":
                            additionalInfo = $"Cause of Death: {CauseOfDeathTextBox.Text.Trim()}, " +
                                            $"Age at Death: {AgeAtDeathTextBox.Text.Trim()}, " +
                                            $"Place of Death: {PlaceOfDeathTextBox.Text.Trim()}";
                            break;

                        case "Other":
                            additionalInfo = $"Document Purpose: {DocumentPurposeTextBox.Text.Trim()}, " +
                                            $"Document Category: {DocumentCategoryTextBox.Text.Trim()}";
                            break;
                    }

                    // Append to existing remarks or set as new remarks
                    if (!string.IsNullOrEmpty(additionalInfo))
                    {
                        // Extract any user-entered remarks (before the Additional Information section)
                        string userRemarks = RemarksTextBox.Text.Trim();
                        if (userRemarks.Contains("Additional Information:"))
                        {
                            userRemarks = userRemarks.Substring(0, userRemarks.IndexOf("Additional Information:")).Trim();
                        }

                        if (!string.IsNullOrEmpty(userRemarks))
                        {
                            _document.Remarks = userRemarks + "\n\nAdditional Information:\n" + additionalInfo;
                        }
                        else
                        {
                            _document.Remarks = "Additional Information:\n" + additionalInfo;
                        }
                    }
                    else
                    {
                        _document.Remarks = RemarksTextBox.Text.Trim();
                    }
                }

                // Common fields for all document types
                _document.FatherGivenName = FatherGivenNameTextBox.Text.Trim();
                _document.FatherMiddleName = FatherMiddleNameTextBox.Text.Trim();
                _document.FatherFamilyName = FatherFamilyNameTextBox.Text.Trim();
                _document.MotherMaidenGiven = MotherMaidenGivenTextBox.Text.Trim();
                _document.MotherMaidenMiddleName = MotherMaidenMiddleNameTextBox.Text.Trim();
                _document.MotherMaidenFamily = MotherMaidenFamilyTextBox.Text.Trim();

                _document.Barangay = BarangayTextBox.Text.Trim();
                _document.CityMunicipality = CityMunicipalityTextBox.Text.Trim();
                _document.Province = ProvinceTextBox.Text.Trim();

                _document.RegistryBook = RegistryBookTextBox.Text.Trim();
                _document.VolumeNumber = VolumeNumberTextBox.Text.Trim();
                _document.PageNumber = PageNumberTextBox.Text.Trim();
                _document.LineNumber = LineNumberTextBox.Text.Trim();
                _document.AttendantName = AttendantNameTextBox.Text.Trim();

                _document.Remarks = RemarksTextBox.Text.Trim();

                // Handle front file update if a new file was selected
                if (!string.IsNullOrEmpty(_selectedFrontFilePath) && _selectedFrontFilePath != _document.FilePath)
                {
                    // Save the new front file
                    using (var fileStream = File.OpenRead(_selectedFrontFilePath))
                    {
                        string fileName = Path.GetFileName(_selectedFrontFilePath);
                        string filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, _document.DocumentType ?? "Other");

                        // Delete the old front file if it exists
                        if (!string.IsNullOrEmpty(_document.FilePath) && File.Exists(_document.FilePath))
                        {
                            await _fileStorageService.DeleteFileAsync(_document.FilePath);
                        }

                        // Update the file path
                        _document.FilePath = filePath;
                    }
                }

                // Handle back file update if a new file was selected
                if (!string.IsNullOrEmpty(_selectedBackFilePath) && _selectedBackFilePath != _document.BackFilePath)
                {
                    // Save the new back file
                    using (var fileStream = File.OpenRead(_selectedBackFilePath))
                    {
                        string fileName = Path.GetFileName(_selectedBackFilePath);
                        string filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, _document.DocumentType ?? "Other");

                        // Delete the old back file if it exists
                        if (!string.IsNullOrEmpty(_document.BackFilePath) && File.Exists(_document.BackFilePath))
                        {
                            await _fileStorageService.DeleteFileAsync(_document.BackFilePath);
                        }

                        // Update the back file path
                        _document.BackFilePath = filePath;
                    }
                }

                // Save the updated document
                await _documentService.UpdateDocumentAsync(_document);

                Log.Information("Document updated successfully for document ID: {DocumentId}", _documentId);
                MessageBox.Show("Document updated successfully.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close the window with success result
                var window = Window.GetWindow(this);
                if (window != null)
                    window.DialogResult = true;
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving document changes for document ID: {DocumentId}", _documentId);
                MessageBox.Show($"Error saving document changes: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Cancel button clicked");
                var window = Window.GetWindow(this);
                if (window != null)
                    window.DialogResult = false;
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in CancelButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
