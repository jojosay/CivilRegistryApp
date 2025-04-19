using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Modules.Auth;
using CivilRegistryApp.Modules.Documents;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CivilRegistryApp.Modules.Requests.Views
{
    public partial class RequestFormView : UserControl
    {
        private readonly IRequestService _requestService;
        private readonly IAuthenticationService _authService;
        private readonly IDocumentService _documentService;
        private Document? _selectedDocument;

        public RequestFormView(IRequestService requestService, IAuthenticationService authService)
        {
            InitializeComponent();
            _requestService = requestService ?? throw new ArgumentNullException(nameof(requestService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Get document service from the application's service provider
            _documentService = ((App)Application.Current).ServiceProvider.GetService(typeof(IDocumentService)) as IDocumentService;
            if (_documentService == null)
                throw new InvalidOperationException("Could not resolve IDocumentService from the service provider.");
        }

        private async void SearchDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(DocumentIdTextBox.Text, out int documentId))
                {
                    MessageBox.Show("Please enter a valid Document ID.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Show loading indicator or disable UI if needed
                SearchDocumentButton.IsEnabled = false;
                SearchDocumentButton.Content = "Searching...";

                // Get document by ID
                _selectedDocument = await _documentService.GetDocumentByIdAsync(documentId);

                if (_selectedDocument != null)
                {
                    // Update UI with document details
                    DocumentTypeTextBox.Text = _selectedDocument.DocumentType;
                    CertificateNumberTextBox.Text = _selectedDocument.CertificateNumber;

                    // Make Certificate Number copyable
                    ClipboardHelper.MakeTextBoxCopyable(CertificateNumberTextBox);

                    Log.Information("Document found for ID: {DocumentId}", documentId);
                }
                else
                {
                    // Clear document details
                    DocumentTypeTextBox.Text = string.Empty;
                    CertificateNumberTextBox.Text = string.Empty;

                    MessageBox.Show($"No document found with ID: {documentId}", "Document Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Warning("No document found for ID: {DocumentId}", documentId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching for document with ID: {DocumentId}", DocumentIdTextBox.Text);
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Hide loading indicator or enable UI
                SearchDocumentButton.IsEnabled = true;
                SearchDocumentButton.Content = "Search";
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (!ValidateInputs())
                    return;

                // Disable submit button to prevent multiple submissions
                SubmitButton.IsEnabled = false;
                SubmitButton.Content = "Submitting...";

                // Create new request
                var request = new DocumentRequest
                {
                    RelatedDocumentId = _selectedDocument.DocumentId,
                    RequestorName = RequestorNameTextBox.Text.Trim(),
                    RequestorAddress = RequestorAddressTextBox.Text.Trim(),
                    RequestorContact = RequestorContactTextBox.Text.Trim(),
                    Purpose = PurposeTextBox.Text.Trim(),
                    RequestDate = DateTime.Now,
                    Status = "Pending"
                };

                // Submit request
                var addedRequest = await _requestService.AddRequestAsync(request);

                Log.Information("Document request submitted for document ID: {DocumentId}", _selectedDocument.DocumentId);
                MessageBox.Show("Your request has been submitted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close the dialog with success result
                Window.GetWindow(this).DialogResult = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error submitting document request");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable submit button
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "Submit Request";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the dialog with cancel result
            Window.GetWindow(this).DialogResult = false;
        }

        private bool ValidateInputs()
        {
            // Validate document selection
            if (_selectedDocument == null)
            {
                MessageBox.Show("Please search and select a valid document.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validate requestor name
            if (string.IsNullOrWhiteSpace(RequestorNameTextBox.Text))
            {
                MessageBox.Show("Please enter the requestor's full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                RequestorNameTextBox.Focus();
                return false;
            }

            // Validate requestor address
            if (string.IsNullOrWhiteSpace(RequestorAddressTextBox.Text))
            {
                MessageBox.Show("Please enter the requestor's address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                RequestorAddressTextBox.Focus();
                return false;
            }

            // Validate requestor contact
            if (string.IsNullOrWhiteSpace(RequestorContactTextBox.Text))
            {
                MessageBox.Show("Please enter the requestor's contact number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                RequestorContactTextBox.Focus();
                return false;
            }

            // Validate purpose
            if (string.IsNullOrWhiteSpace(PurposeTextBox.Text))
            {
                MessageBox.Show("Please enter the purpose of the request.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                PurposeTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}
