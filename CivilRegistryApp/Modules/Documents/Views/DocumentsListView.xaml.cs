using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Auth;
using CivilRegistryApp.Modules.Documents.Views;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CivilRegistryApp.Modules.Documents.Views
{
    public partial class DocumentsListView : UserControl
    {
        private readonly IDocumentService _documentService = null!;
        private readonly IAuthenticationService _authService = null!;
        private List<Document> _allDocuments = new List<Document>();
        private List<Document> _filteredDocuments = new List<Document>();
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 1;

        // Advanced search filters
        private string _selectedRegistryOffice = "All Offices";
        private string _selectedProvince = "All Provinces";
        private string _selectedCity = "All Cities";
        private string _selectedBarangay = "All Barangays";
        private string _selectedUploadedBy = "All Users";
        private DateTime? _eventDateFrom = null;
        private DateTime? _eventDateTo = null;
        private DateTime? _regDateFrom = null;
        private DateTime? _regDateTo = null;
        private DateTime? _uploadDateFrom = null;
        private DateTime? _uploadDateTo = null;

        // Lists for dropdown population
        private List<string> _registryOffices = new List<string>();
        private List<string> _provinces = new List<string>();
        private List<string> _cities = new List<string>();
        private List<string> _barangays = new List<string>();
        private List<User> _users = new List<User>();

        private bool _isInitialized = false;
        private bool _isAdvancedSearchVisible = false;

        public DocumentsListView(IDocumentService documentService, IAuthenticationService authService)
        {
            try
            {
                Log.Debug("Initializing DocumentsListView");
                InitializeComponent();

                _documentService = documentService;
                _authService = authService;

                // Disable UI controls until data is loaded
                DocumentsDataGrid.IsEnabled = false;

                // Load documents after initialization is complete
                this.Loaded += (s, e) =>
                {
                    LoadDocumentsAsync();
                };

                Log.Information("DocumentsListView initialized successfully");
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentsListView.Constructor");
                MessageBox.Show($"Error initializing the documents view: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadDocumentsAsync()
        {
            try
            {
                Log.Debug("Loading documents");

                // Show loading indicator or disable UI
                DocumentsDataGrid.IsEnabled = false;

                // Get all documents
                _allDocuments = (await _documentService.GetAllDocumentsAsync()).ToList();

                // Populate filter dropdowns
                PopulateFilterDropdowns();

                // Apply initial filter (All Documents)
                ApplyFilter();

                // Update UI
                UpdatePaginationInfo();
                DocumentsDataGrid.IsEnabled = true;

                // Mark as initialized after everything is loaded
                _isInitialized = true;

                // Hide advanced search panel initially
                AdvancedSearchExpander.IsExpanded = false;

                // Make Certificate Number column copyable - do this after initialization
                try
                {
                    ClipboardHelper.MakeDataGridColumnCopyable(DocumentsDataGrid, "Certificate No.");

                    // Add tooltip to show that the certificate number is copyable
                    var certificateColumn = DocumentsDataGrid.Columns.FirstOrDefault(c => c.Header.ToString() == "Certificate No.");
                    if (certificateColumn != null)
                    {
                        certificateColumn.Header = "Certificate No. 📋";
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't show error to user - this is a non-critical feature
                    Log.Warning(ex, "Could not make Certificate Number column copyable");
                }

                Log.Information("Documents loaded successfully. Total count: {DocumentCount}", _allDocuments.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading documents");
                MessageBox.Show($"Error loading documents: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DocumentsDataGrid.IsEnabled = true;
            }
        }

        private void PopulateFilterDropdowns()
        {
            try
            {
                Log.Debug("Populating filter dropdowns");

                // Clear existing items (except the "All" options which are added in XAML)
                RegistryOfficeComboBox.Items.Clear();
                ProvinceComboBox.Items.Clear();
                CityComboBox.Items.Clear();
                BarangayComboBox.Items.Clear();
                UploadedByComboBox.Items.Clear();

                // Add default "All" options
                RegistryOfficeComboBox.Items.Add(new ComboBoxItem { Content = "All Offices", IsSelected = true });
                ProvinceComboBox.Items.Add(new ComboBoxItem { Content = "All Provinces", IsSelected = true });
                CityComboBox.Items.Add(new ComboBoxItem { Content = "All Cities", IsSelected = true });
                BarangayComboBox.Items.Add(new ComboBoxItem { Content = "All Barangays", IsSelected = true });
                UploadedByComboBox.Items.Add(new ComboBoxItem { Content = "All Users", IsSelected = true });

                if (_allDocuments != null && _allDocuments.Any())
                {
                    // Get unique registry offices
                    _registryOffices = _allDocuments
                        .Select(d => d.RegistryOffice)
                        .Distinct()
                        .OrderBy(o => o)
                        .ToList();

                    // Get unique provinces
                    _provinces = _allDocuments
                        .Select(d => d.Province)
                        .Distinct()
                        .OrderBy(p => p)
                        .ToList();

                    // Get unique cities
                    _cities = _allDocuments
                        .Select(d => d.CityMunicipality)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    // Get unique barangays
                    _barangays = _allDocuments
                        .Select(d => d.Barangay)
                        .Distinct()
                        .OrderBy(b => b)
                        .ToList();

                    // Get unique users
                    _users = _allDocuments
                        .Select(d => d.UploadedByUser)
                        .Distinct()
                        .OrderBy(u => u.Username)
                        .ToList();

                    // Populate registry office dropdown
                    foreach (var office in _registryOffices)
                    {
                        RegistryOfficeComboBox.Items.Add(new ComboBoxItem { Content = office });
                    }

                    // Populate province dropdown
                    foreach (var province in _provinces)
                    {
                        ProvinceComboBox.Items.Add(new ComboBoxItem { Content = province });
                    }

                    // Populate city dropdown
                    foreach (var city in _cities)
                    {
                        CityComboBox.Items.Add(new ComboBoxItem { Content = city });
                    }

                    // Populate barangay dropdown
                    foreach (var barangay in _barangays)
                    {
                        BarangayComboBox.Items.Add(new ComboBoxItem { Content = barangay });
                    }

                    // Populate users dropdown
                    foreach (var user in _users)
                    {
                        UploadedByComboBox.Items.Add(new ComboBoxItem { Content = user.Username });
                    }
                }

                Log.Information("Filter dropdowns populated successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error populating filter dropdowns");
                MessageBox.Show($"Error populating filter dropdowns: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string searchText = SearchTextBox.Text?.Trim().ToLower() ?? string.Empty;
                string selectedDocumentType = (DocumentTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Documents";

                // Apply filters
                if (_allDocuments != null)
                {
                    _filteredDocuments = _allDocuments.Where(d =>
                        // Document type filter
                        (selectedDocumentType == "All Documents" || d.DocumentType == selectedDocumentType) &&
                        // Search text filter
                        (string.IsNullOrEmpty(searchText) ||
                         d.GivenName?.ToLower().Contains(searchText) == true ||
                         d.MiddleName?.ToLower().Contains(searchText) == true ||
                         d.FamilyName?.ToLower().Contains(searchText) == true ||
                         d.CertificateNumber?.ToLower().Contains(searchText) == true ||
                         d.RegistryOffice?.ToLower().Contains(searchText) == true) &&
                        // Advanced filters - only apply if advanced search is visible
                        (!_isAdvancedSearchVisible || (
                            // Registry Office filter
                            (_selectedRegistryOffice == "All Offices" || d.RegistryOffice == _selectedRegistryOffice) &&
                            // Province filter
                            (_selectedProvince == "All Provinces" || d.Province == _selectedProvince) &&
                            // City filter
                            (_selectedCity == "All Cities" || d.CityMunicipality == _selectedCity) &&
                            // Barangay filter
                            (_selectedBarangay == "All Barangays" || d.Barangay == _selectedBarangay) &&
                            // Uploaded By filter
                            (_selectedUploadedBy == "All Users" || d.UploadedByUser.Username == _selectedUploadedBy) &&
                            // Date of Event range
                            (!_eventDateFrom.HasValue || d.DateOfEvent >= _eventDateFrom.Value) &&
                            (!_eventDateTo.HasValue || d.DateOfEvent <= _eventDateTo.Value) &&
                            // Registration Date range
                            (!_regDateFrom.HasValue || d.RegistrationDate >= _regDateFrom.Value) &&
                            (!_regDateTo.HasValue || d.RegistrationDate <= _regDateTo.Value) &&
                            // Upload Date range
                            (!_uploadDateFrom.HasValue || d.UploadedAt >= _uploadDateFrom.Value) &&
                            (!_uploadDateTo.HasValue || d.UploadedAt <= _uploadDateTo.Value)
                        ))
                    ).ToList();
                }
                else
                {
                    _filteredDocuments = new List<Document>();
                }

                // Calculate total pages
                _totalPages = (_filteredDocuments.Count + _pageSize - 1) / _pageSize;
                if (_totalPages < 1) _totalPages = 1;

                // Ensure current page is valid
                if (_currentPage > _totalPages)
                    _currentPage = _totalPages;

                // Get current page data
                var currentPageData = _filteredDocuments
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                // Update DataGrid
                DocumentsDataGrid.ItemsSource = currentPageData;

                Log.Debug("Filter applied. Filtered count: {FilteredCount}, Current page: {CurrentPage}, Total pages: {TotalPages}",
                    _filteredDocuments.Count, _currentPage, _totalPages);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error applying filter");
                MessageBox.Show($"Error applying filter: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePaginationInfo()
        {
            try
            {
                if (PageInfoTextBlock == null || PreviousPageButton == null || NextPageButton == null)
                    return;

                PageInfoTextBlock.Text = $"Page {_currentPage} of {_totalPages}";
                PreviousPageButton.IsEnabled = _currentPage > 1;
                NextPageButton.IsEnabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in UpdatePaginationInfo");
                // Don't show message box as this might be called during initialization
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Skip if not initialized
                if (!_isInitialized)
                    return;

                Log.Debug("Search button clicked");
                _currentPage = 1;
                ApplyFilter();
                UpdatePaginationInfo();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SearchButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Skip if not initialized
                if (!_isInitialized)
                    return;

                if (e.Key == Key.Enter)
                {
                    Log.Debug("Search triggered by Enter key");
                    _currentPage = 1;
                    ApplyFilter();
                    UpdatePaginationInfo();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SearchTextBox_KeyDown");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DocumentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Skip during initialization
                if (!_isInitialized)
                    return;

                Log.Debug("Document type filter changed");
                _currentPage = 1;
                ApplyFilter();
                UpdatePaginationInfo();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in DocumentTypeComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Skip if not initialized
                if (!_isInitialized)
                    return;

                if (_currentPage > 1)
                {
                    _currentPage--;
                    ApplyFilter();
                    UpdatePaginationInfo();
                    Log.Debug("Navigated to previous page: {CurrentPage}", _currentPage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in PreviousPageButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Skip if not initialized
                if (!_isInitialized)
                    return;

                if (_currentPage < _totalPages)
                {
                    _currentPage++;
                    ApplyFilter();
                    UpdatePaginationInfo();
                    Log.Debug("Navigated to next page: {CurrentPage}", _currentPage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in NextPageButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DocumentsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This event can be used if we need to do something when a row is selected
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                var document = button.DataContext as Document;

                if (document != null)
                {
                    Log.Information("Viewing document details for document ID: {DocumentId}", document.DocumentId);

                    // Open document details view
                    var documentDetailsView = new DocumentDetailsView(_documentService, document.DocumentId);
                    var window = new Window
                    {
                        Title = $"Document Details - {document.DocumentType} #{document.CertificateNumber}",
                        Content = documentDetailsView,
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ViewButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                var document = button.DataContext as Document;

                if (document != null)
                {
                    Log.Information("Editing document with ID: {DocumentId}", document.DocumentId);

                    // Check if user has permission to edit
                    if (!_authService.IsUserInRole("Admin") && document.UploadedBy != _authService.CurrentUser.UserId)
                    {
                        MessageBox.Show("You do not have permission to edit this document.",
                            "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Open document edit view
                    var documentEditView = new DocumentEditView(_documentService, document.DocumentId);
                    var window = new Window
                    {
                        Title = $"Edit Document - {document.DocumentType} #{document.CertificateNumber}",
                        Content = documentEditView,
                        Width = 800,
                        Height = 700,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    bool? result = window.ShowDialog();

                    if (result == true)
                    {
                        // Refresh the document list
                        LoadDocumentsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in EditButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Adding new document");

                // Open document add view
                var documentAddView = new DocumentAddView(_documentService, _authService);
                var window = new Window
                {
                    Title = "Add New Document",
                    Content = documentAddView,
                    Width = 800,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                bool? result = window.ShowDialog();

                if (result == true)
                {
                    // Refresh the document list
                    LoadDocumentsAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AddDocumentButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Advanced Search Event Handlers

        private void AdvancedSearchToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Toggle advanced search panel visibility
                _isAdvancedSearchVisible = !_isAdvancedSearchVisible;
                AdvancedSearchExpander.IsExpanded = _isAdvancedSearchVisible;

                Log.Debug("Advanced search panel toggled. Visible: {IsVisible}", _isAdvancedSearchVisible);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in AdvancedSearchToggle_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegistryOfficeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return;

                _selectedRegistryOffice = (RegistryOfficeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Offices";
                Log.Debug("Registry office filter changed to: {RegistryOffice}", _selectedRegistryOffice);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in RegistryOfficeComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProvinceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return;

                _selectedProvince = (ProvinceComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Provinces";
                Log.Debug("Province filter changed to: {Province}", _selectedProvince);

                // Update city and barangay dropdowns based on selected province
                UpdateCityDropdown();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ProvinceComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return;

                _selectedCity = (CityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Cities";
                Log.Debug("City filter changed to: {City}", _selectedCity);

                // Update barangay dropdown based on selected city
                UpdateBarangayDropdown();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in CityComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BarangayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return;

                _selectedBarangay = (BarangayComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Barangays";
                Log.Debug("Barangay filter changed to: {Barangay}", _selectedBarangay);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in BarangayComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UploadedByComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return;

                _selectedUploadedBy = (UploadedByComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Users";
                Log.Debug("Uploaded by filter changed to: {User}", _selectedUploadedBy);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in UploadedByComboBox_SelectionChanged");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ApplyFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return;

                // Get date values from date pickers
                _eventDateFrom = EventDateFromPicker.SelectedDate;
                _eventDateTo = EventDateToPicker.SelectedDate;
                _regDateFrom = RegDateFromPicker.SelectedDate;
                _regDateTo = RegDateToPicker.SelectedDate;
                _uploadDateFrom = UploadDateFromPicker.SelectedDate;
                _uploadDateTo = UploadDateToPicker.SelectedDate;

                // Reset to first page
                _currentPage = 1;

                // Show loading indicator
                DocumentsDataGrid.IsEnabled = false;

                // Get search text and document type
                string searchText = SearchTextBox.Text?.Trim() ?? string.Empty;
                string selectedDocumentType = (DocumentTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Documents";

                // Use advanced search service
                _filteredDocuments = (await _documentService.AdvancedSearchDocumentsAsync(
                    searchText,
                    selectedDocumentType,
                    _selectedRegistryOffice,
                    _selectedProvince,
                    _selectedCity,
                    _selectedBarangay,
                    _selectedUploadedBy,
                    _eventDateFrom,
                    _eventDateTo,
                    _regDateFrom,
                    _regDateTo,
                    _uploadDateFrom,
                    _uploadDateTo)).ToList();

                // Calculate total pages
                _totalPages = (_filteredDocuments.Count + _pageSize - 1) / _pageSize;
                if (_totalPages < 1) _totalPages = 1;

                // Ensure current page is valid
                if (_currentPage > _totalPages)
                    _currentPage = _totalPages;

                // Get current page data
                var currentPageData = _filteredDocuments
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                // Update DataGrid
                DocumentsDataGrid.ItemsSource = currentPageData;
                DocumentsDataGrid.IsEnabled = true;

                // Update pagination
                UpdatePaginationInfo();

                Log.Information("Advanced filters applied. Found {Count} documents", _filteredDocuments.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ApplyFiltersButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DocumentsDataGrid.IsEnabled = true;
            }
        }

        private async void ResetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized) return;

                // Reset date pickers
                EventDateFromPicker.SelectedDate = null;
                EventDateToPicker.SelectedDate = null;
                RegDateFromPicker.SelectedDate = null;
                RegDateToPicker.SelectedDate = null;
                UploadDateFromPicker.SelectedDate = null;
                UploadDateToPicker.SelectedDate = null;

                // Reset dropdowns
                RegistryOfficeComboBox.SelectedIndex = 0; // "All Offices"
                ProvinceComboBox.SelectedIndex = 0; // "All Provinces"
                CityComboBox.SelectedIndex = 0; // "All Cities"
                BarangayComboBox.SelectedIndex = 0; // "All Barangays"
                UploadedByComboBox.SelectedIndex = 0; // "All Users"

                // Reset filter values
                _selectedRegistryOffice = "All Offices";
                _selectedProvince = "All Provinces";
                _selectedCity = "All Cities";
                _selectedBarangay = "All Barangays";
                _selectedUploadedBy = "All Users";
                _eventDateFrom = null;
                _eventDateTo = null;
                _regDateFrom = null;
                _regDateTo = null;
                _uploadDateFrom = null;
                _uploadDateTo = null;

                // Reset to first page
                _currentPage = 1;

                // Show loading indicator
                DocumentsDataGrid.IsEnabled = false;

                // Get all documents (no filters)
                _filteredDocuments = (await _documentService.GetAllDocumentsAsync()).ToList();

                // Calculate total pages
                _totalPages = (_filteredDocuments.Count + _pageSize - 1) / _pageSize;
                if (_totalPages < 1) _totalPages = 1;

                // Get current page data
                var currentPageData = _filteredDocuments
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                // Update DataGrid
                DocumentsDataGrid.ItemsSource = currentPageData;
                DocumentsDataGrid.IsEnabled = true;

                // Update pagination
                UpdatePaginationInfo();

                Log.Information("Advanced filters reset. Showing all {Count} documents", _filteredDocuments.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in ResetFiltersButton_Click");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DocumentsDataGrid.IsEnabled = true;
            }
        }

        private void UpdateCityDropdown()
        {
            try
            {
                // Clear existing items
                CityComboBox.Items.Clear();
                CityComboBox.Items.Add(new ComboBoxItem { Content = "All Cities", IsSelected = true });

                // If "All Provinces" is selected, show all cities
                if (_selectedProvince == "All Provinces")
                {
                    foreach (var city in _cities)
                    {
                        CityComboBox.Items.Add(new ComboBoxItem { Content = city });
                    }
                }
                else
                {
                    // Filter cities by selected province
                    var filteredCities = _allDocuments
                        .Where(d => d.Province == _selectedProvince)
                        .Select(d => d.CityMunicipality)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList();

                    foreach (var city in filteredCities)
                    {
                        CityComboBox.Items.Add(new ComboBoxItem { Content = city });
                    }
                }

                // Reset selected city
                _selectedCity = "All Cities";
                CityComboBox.SelectedIndex = 0;

                // Update barangay dropdown
                UpdateBarangayDropdown();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in UpdateCityDropdown");
                // Don't show message box as this might be called during initialization
            }
        }

        private void UpdateBarangayDropdown()
        {
            try
            {
                // Clear existing items
                BarangayComboBox.Items.Clear();
                BarangayComboBox.Items.Add(new ComboBoxItem { Content = "All Barangays", IsSelected = true });

                // If "All Cities" is selected, show all barangays for the selected province
                if (_selectedCity == "All Cities")
                {
                    if (_selectedProvince == "All Provinces")
                    {
                        // Show all barangays
                        foreach (var barangay in _barangays)
                        {
                            BarangayComboBox.Items.Add(new ComboBoxItem { Content = barangay });
                        }
                    }
                    else
                    {
                        // Filter barangays by selected province
                        var filteredBarangays = _allDocuments
                            .Where(d => d.Province == _selectedProvince)
                            .Select(d => d.Barangay)
                            .Distinct()
                            .OrderBy(b => b)
                            .ToList();

                        foreach (var barangay in filteredBarangays)
                        {
                            BarangayComboBox.Items.Add(new ComboBoxItem { Content = barangay });
                        }
                    }
                }
                else
                {
                    // Filter barangays by selected city and province
                    var filteredBarangays = _allDocuments
                        .Where(d => d.Province == _selectedProvince && d.CityMunicipality == _selectedCity)
                        .Select(d => d.Barangay)
                        .Distinct()
                        .OrderBy(b => b)
                        .ToList();

                    foreach (var barangay in filteredBarangays)
                    {
                        BarangayComboBox.Items.Add(new ComboBoxItem { Content = barangay });
                    }
                }

                // Reset selected barangay
                _selectedBarangay = "All Barangays";
                BarangayComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in UpdateBarangayDropdown");
                // Don't show message box as this might be called during initialization
            }
        }

        #endregion
    }
}
