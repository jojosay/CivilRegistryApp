using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Admin;
using CivilRegistryApp.Modules.Auth;
using Serilog;
using System;
using System.Collections.Generic;
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
    public partial class DocumentAddView : UserControl
    {
        private readonly IDocumentService _documentService = null!;
        private readonly IAuthenticationService _authService = null!;
        private readonly IFileStorageService _fileStorageService = null!;
        private readonly IFieldConfigurationService _fieldConfigService = null!;
        private string? _selectedFrontFilePath;
        private string? _selectedBackFilePath;
        private string _currentDocumentType = "Birth Certificate";

        public DocumentAddView(IDocumentService documentService, IAuthenticationService authService)
        {
            try
            {
                Log.Debug("Initializing DocumentAddView");
                InitializeComponent();

                _documentService = documentService;
                _authService = authService;

                // Get services from the application's service provider
                _fileStorageService = (Application.Current as App).ServiceProvider.GetService(typeof(IFileStorageService)) as IFileStorageService;
                _fieldConfigService = (Application.Current as App).ServiceProvider.GetService(typeof(IFieldConfigurationService)) as IFieldConfigurationService;

                // Set default dates
                DateOfEventDatePicker.SelectedDate = DateTime.Today;
                RegistrationDateDatePicker.SelectedDate = DateTime.Today;
                MarriageDatePicker.SelectedDate = DateTime.Today;

                // Show the appropriate fields for the default document type
                UpdateDocumentTypeFields("Birth Certificate");

                // Mark required fields with red asterisks based on field configuration
                MarkRequiredFieldsAsync("Birth Certificate");

                Log.Information("DocumentAddView initialized successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentAddView.Constructor");
                MessageBox.Show($"Error initializing the document add view: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseFrontButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Debug("Browse front button clicked");

                string filePath = _fileStorageService.OpenFileDialog("Document Files (*.pdf;*.jpg;*.jpeg;*.png;*.tiff)|*.pdf;*.jpg;*.jpeg;*.png;*.tiff|All files (*.*)|*.*");

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

                string filePath = _fileStorageService.OpenFileDialog("Document Files (*.pdf;*.jpg;*.jpeg;*.png;*.tiff)|*.pdf;*.jpg;*.jpeg;*.png;*.tiff|All files (*.*)|*.*");

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
                    RegistrationDateDatePicker.SelectedDate != null &&
                    !string.IsNullOrEmpty(_selectedFrontFilePath); // Front file is required

                // For Marriage Certificate, we don't need to validate Person Information fields
                if (_currentDocumentType == "Marriage Certificate")
                {
                    if (!commonFieldsValid)
                    {
                        MessageBox.Show("Please fill in all required fields and select a document file.",
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
                        MessageBox.Show("Please fill in all required fields and select a document file.",
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

                // Create new document with common fields
                var document = new Document
                {
                    DocumentType = _currentDocumentType,
                    CertificateNumber = CertificateNumberTextBox.Text.Trim(),
                    RegistryOffice = RegistryOfficeTextBox.Text.Trim(),
                    DateOfEvent = DateOfEventDatePicker.SelectedDate ?? DateTime.Today,
                    RegistrationDate = RegistrationDateDatePicker.SelectedDate ?? DateTime.Today,
                    Gender = (GenderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "M",

                    Barangay = BarangayTextBox.Text.Trim(),
                    CityMunicipality = CityMunicipalityTextBox.Text.Trim(),
                    Province = ProvinceTextBox.Text.Trim(),

                    RegistryBook = RegistryBookTextBox.Text.Trim(),
                    VolumeNumber = VolumeNumberTextBox.Text.Trim(),
                    PageNumber = PageNumberTextBox.Text.Trim(),
                    LineNumber = LineNumberTextBox.Text.Trim(),
                    AttendantName = AttendantNameTextBox.Text.Trim(),

                    Remarks = RemarksTextBox.Text.Trim()
                };

                // Handle document type-specific fields
                if (_currentDocumentType == "Marriage Certificate")
                {
                    // For Marriage Certificate, use the husband's name as the primary name
                    document.Prefix = ""; // Usually not used for marriage certificates
                    document.GivenName = HusbandGivenNameTextBox.Text.Trim();
                    document.MiddleName = HusbandMiddleNameTextBox.Text.Trim();
                    document.FamilyName = HusbandFamilyNameTextBox.Text.Trim();
                    document.Suffix = "";

                    // Parents information is not typically filled for marriage certificates
                    document.FatherGivenName = "";
                    document.FatherMiddleName = "";
                    document.FatherFamilyName = "";
                    document.MotherMaidenGiven = "";
                    document.MotherMaidenMiddleName = "";
                    document.MotherMaidenFamily = "";
                }
                else if (_currentDocumentType == "CENOMAR")
                {
                    // For CENOMAR, use default values for the required Person fields
                    document.Prefix = "";
                    document.GivenName = "CENOMAR";
                    document.MiddleName = "";
                    document.FamilyName = "Document";
                    document.Suffix = "";

                    // Parents information is not typically filled for CENOMAR
                    document.FatherGivenName = "";
                    document.FatherMiddleName = "";
                    document.FatherFamilyName = "";
                    document.MotherMaidenGiven = "";
                    document.MotherMaidenMiddleName = "";
                    document.MotherMaidenFamily = "";
                }
                else
                {
                    // For other document types, use the Person Information section
                    document.Prefix = PrefixTextBox.Text.Trim();
                    document.GivenName = GivenNameTextBox.Text.Trim();
                    document.MiddleName = MiddleNameTextBox.Text.Trim();
                    document.FamilyName = FamilyNameTextBox.Text.Trim();
                    document.Suffix = SuffixTextBox.Text.Trim();

                    document.FatherGivenName = FatherGivenNameTextBox.Text.Trim();
                    document.FatherMiddleName = FatherMiddleNameTextBox.Text.Trim();
                    document.FatherFamilyName = FatherFamilyNameTextBox.Text.Trim();
                    document.MotherMaidenGiven = MotherMaidenGivenTextBox.Text.Trim();
                    document.MotherMaidenMiddleName = MotherMaidenMiddleNameTextBox.Text.Trim();
                    document.MotherMaidenFamily = MotherMaidenFamilyTextBox.Text.Trim();
                }

                // Add document type-specific information to remarks
                string additionalInfo = "";

                switch (_currentDocumentType)
                {
                    case "Birth Certificate":
                        additionalInfo = $"Birth Weight: {BirthWeightTextBox.Text.Trim()} kg, " +
                                        $"Birth Order: {BirthOrderTextBox.Text.Trim()}, " +
                                        $"Type of Birth: {(TypeOfBirthComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Single"}";
                        break;

                    case "Marriage Certificate":
                        additionalInfo = $"Husband: {HusbandGivenNameTextBox.Text.Trim()} {HusbandMiddleNameTextBox.Text.Trim()} {HusbandFamilyNameTextBox.Text.Trim()}, " +
                                        $"Wife: {WifeGivenNameTextBox.Text.Trim()} {WifeMiddleNameTextBox.Text.Trim()} {WifeFamilyNameTextBox.Text.Trim()}, " +
                                        $"Marriage Date: {MarriageDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd")}, " +
                                        $"Marriage Type: {(MarriageTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Civil"}";
                        break;

                    case "Death Certificate":
                        additionalInfo = $"Cause of Death: {CauseOfDeathTextBox.Text.Trim()}, " +
                                        $"Age at Death: {AgeAtDeathTextBox.Text.Trim()}, " +
                                        $"Place of Death: {PlaceOfDeathTextBox.Text.Trim()}";
                        break;

                    case "CENOMAR":
                        additionalInfo = $"Document Purpose: {DocumentPurposeTextBox.Text.Trim()}, " +
                                        $"Document Category: {DocumentCategoryTextBox.Text.Trim()}, " +
                                        $"Certificate of No Marriage Record";
                        break;

                    case "Other":
                        additionalInfo = $"Document Purpose: {DocumentPurposeTextBox.Text.Trim()}, " +
                                        $"Document Category: {DocumentCategoryTextBox.Text.Trim()}";
                        break;
                }

                // Append the additional info to remarks
                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    if (!string.IsNullOrEmpty(document.Remarks))
                        document.Remarks += "\n\n";

                    document.Remarks += $"Additional Information:\n{additionalInfo}";
                }

                // Save the document with front file (required)
                using (var frontFileStream = File.OpenRead(_selectedFrontFilePath))
                {
                    string frontFileName = Path.GetFileName(_selectedFrontFilePath);

                    // Handle back file if provided
                    if (!string.IsNullOrEmpty(_selectedBackFilePath))
                    {
                        using (var backFileStream = File.OpenRead(_selectedBackFilePath))
                        {
                            string backFileName = Path.GetFileName(_selectedBackFilePath);

                            // Save document with both front and back files
                            var savedDocument = await _documentService.AddDocumentWithBackAsync(
                                document,
                                frontFileStream, frontFileName,
                                backFileStream, backFileName);

                            Log.Information("Document added successfully with ID: {DocumentId} (with front and back files)", savedDocument.DocumentId);
                            MessageBox.Show("Document added successfully with front and back files.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Close the window with success result
                            var window = Window.GetWindow(this);
                            if (window != null)
                                window.DialogResult = true;
                            Window.GetWindow(this)?.Close();
                        }
                    }
                    else
                    {
                        // Save document with front file only
                        var savedDocument = await _documentService.AddDocumentAsync(document, frontFileStream, frontFileName);

                        Log.Information("Document added successfully with ID: {DocumentId} (front file only)", savedDocument.DocumentId);
                        MessageBox.Show("Document added successfully.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Close the window with success result
                        var window = Window.GetWindow(this);
                        if (window != null)
                            window.DialogResult = true;
                        Window.GetWindow(this)?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving new document");
                MessageBox.Show($"Error saving document: {ex.Message}",
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

        private void DocumentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DocumentTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string documentType = selectedItem.Content?.ToString() ?? "Unknown";
                    Log.Debug("Document type changed to: {DocumentType}", documentType);

                    _currentDocumentType = documentType;
                    UpdateDocumentTypeFields(documentType);

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

        private void UpdateDocumentTypeFields(string documentType)
        {
            try
            {
                // Check if the UI elements are initialized
                if (BirthCertificateFieldsGroup == null ||
                    MarriageCertificateFieldsGroup == null ||
                    DeathCertificateFieldsGroup == null ||
                    OtherDocumentFieldsGroup == null)
                {
                    // UI elements not yet initialized, store the document type for later
                    _currentDocumentType = documentType;
                    return;
                }

                // Find the Person Information GroupBox
                var personInfoGroupBox = this.FindName("PersonInformationGroupBox") as System.Windows.Controls.GroupBox;
                if (personInfoGroupBox == null)
                {
                    // Try to find it by traversing the visual tree
                    var stackPanel = BirthCertificateFieldsGroup.Parent as StackPanel;
                    if (stackPanel != null)
                    {
                        foreach (var child in stackPanel.Children)
                        {
                            if (child is System.Windows.Controls.GroupBox box && box.Header?.ToString() == "Person Information")
                            {
                                personInfoGroupBox = box;
                                break;
                            }
                        }
                    }
                }

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
                        // Show Person Information for Birth Certificate
                        if (personInfoGroupBox != null)
                            personInfoGroupBox.Visibility = Visibility.Visible;
                        break;

                    case "Marriage Certificate":
                        MarriageCertificateFieldsGroup.Visibility = Visibility.Visible;
                        // Hide Person Information for Marriage Certificate
                        if (personInfoGroupBox != null)
                            personInfoGroupBox.Visibility = Visibility.Collapsed;
                        break;

                    case "Death Certificate":
                        DeathCertificateFieldsGroup.Visibility = Visibility.Visible;
                        // Show Person Information for Death Certificate
                        if (personInfoGroupBox != null)
                            personInfoGroupBox.Visibility = Visibility.Visible;
                        break;

                    case "CENOMAR":
                        OtherDocumentFieldsGroup.Visibility = Visibility.Visible;
                        // Update the header for CENOMAR
                        OtherDocumentFieldsGroup.Header = "CENOMAR Information";
                        // Hide Person Information for CENOMAR
                        if (personInfoGroupBox != null)
                            personInfoGroupBox.Visibility = Visibility.Collapsed;
                        break;

                    case "Other":
                        OtherDocumentFieldsGroup.Visibility = Visibility.Visible;
                        // Update the header for Other document types
                        OtherDocumentFieldsGroup.Header = "Other Document Information";
                        // Show Person Information for Other document types
                        if (personInfoGroupBox != null)
                            personInfoGroupBox.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in UpdateDocumentTypeFields");
                // Don't show a message box here as this might be called during initialization
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
                if (!string.IsNullOrEmpty(_selectedFrontFilePath))
                {
                    Log.Debug("Preview front button clicked for file: {FilePath}", _selectedFrontFilePath);

                    string extension = Path.GetExtension(_selectedFrontFilePath).ToLower();

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
                                FileName = _selectedFrontFilePath,
                                UseShellExecute = true
                            });
                        }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tiff")
                    {
                        // For image files, open the preview window
                        var previewWindow = new DocumentPreviewWindow(_selectedFrontFilePath, "Front Side");
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
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in PreviewFrontButton_Click");
                MessageBox.Show($"An error occurred while previewing the front document: {ex.Message}",
                    "Preview Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviewBackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_selectedBackFilePath))
                {
                    Log.Debug("Preview back button clicked for file: {FilePath}", _selectedBackFilePath);

                    string extension = Path.GetExtension(_selectedBackFilePath).ToLower();

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
                                FileName = _selectedBackFilePath,
                                UseShellExecute = true
                            });
                        }
                    }
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tiff")
                    {
                        // For image files, open the preview window
                        var previewWindow = new DocumentPreviewWindow(_selectedBackFilePath, "Back Side");
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
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in PreviewBackButton_Click");
                MessageBox.Show($"An error occurred while previewing the back document: {ex.Message}",
                    "Preview Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
