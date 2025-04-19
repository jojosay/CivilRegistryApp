using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Admin;
using CivilRegistryApp.Modules.Auth;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Documents
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuthenticationService _authService;
        private readonly IFieldConfigurationService _fieldConfigService;
        private readonly AppDbContext _dbContext;

        public DocumentService(
            IDocumentRepository documentRepository,
            IFileStorageService fileStorageService,
            IAuthenticationService authService,
            IFieldConfigurationService fieldConfigService,
            AppDbContext dbContext)
        {
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
            _authService = authService;
            _fieldConfigService = fieldConfigService;
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
        {
            try
            {
                return await _documentRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving all documents");
                throw;
            }
        }

        public async Task<Document?> GetDocumentByIdAsync(int documentId)
        {
            try
            {
                Log.Debug("Getting document by ID: {DocumentId}", documentId);

                // Get document with details
                var document = await _documentRepository.GetDocumentWithDetailsAsync(documentId);

                if (document == null)
                {
                    Log.Warning("Document not found with ID: {DocumentId}", documentId);
                }
                else
                {
                    Log.Information("Retrieved document with ID: {DocumentId}", documentId);
                }

                return document;
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentService.GetDocumentByIdAsync");
                throw;
            }
        }

        public async Task<IEnumerable<Document>> SearchDocumentsAsync(string searchText, string? documentType = null)
        {
            try
            {
                Log.Debug("Searching documents with search text: {SearchText}, document type: {DocumentType}", searchText, documentType);

                searchText = searchText.ToLower();

                // Get all documents
                var allDocuments = await GetAllDocumentsAsync();

                // Filter by search text and document type
                var filteredDocuments = allDocuments.Where(d =>
                    (string.IsNullOrEmpty(documentType) || documentType == "All Documents" || d.DocumentType == documentType) &&
                    (string.IsNullOrEmpty(searchText) ||
                     d.GivenName.ToLower().Contains(searchText) ||
                     (d.MiddleName?.ToLower().Contains(searchText) == true) ||
                     d.FamilyName.ToLower().Contains(searchText) ||
                     d.CertificateNumber.ToLower().Contains(searchText) ||
                     d.RegistryOffice.ToLower().Contains(searchText)));

                Log.Information("Found {Count} documents matching search criteria", filteredDocuments.Count());
                return filteredDocuments;
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentService.SearchDocumentsAsync");
                throw;
            }
        }

        public async Task<IEnumerable<Document>> AdvancedSearchDocumentsAsync(
            string? searchText = null,
            string? documentType = null,
            string? registryOffice = null,
            string? province = null,
            string? cityMunicipality = null,
            string? barangay = null,
            string? uploadedByUsername = null,
            DateTime? eventDateFrom = null,
            DateTime? eventDateTo = null,
            DateTime? registrationDateFrom = null,
            DateTime? registrationDateTo = null,
            DateTime? uploadDateFrom = null,
            DateTime? uploadDateTo = null)
        {
            try
            {
                Log.Debug("Performing advanced search with multiple criteria");

                // Convert username to user ID if provided
                int? uploadedById = null;
                if (!string.IsNullOrEmpty(uploadedByUsername) && uploadedByUsername != "All Users")
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == uploadedByUsername);
                    if (user != null)
                    {
                        uploadedById = user.UserId;
                    }
                    else
                    {
                        Log.Warning("User not found with username {Username} for advanced search", uploadedByUsername);
                    }
                }

                // Use the repository method for advanced search
                var results = await _documentRepository.AdvancedSearchAsync(
                    searchText,
                    documentType,
                    registryOffice,
                    province,
                    cityMunicipality,
                    barangay,
                    uploadedById,
                    eventDateFrom,
                    eventDateTo,
                    registrationDateFrom,
                    registrationDateTo,
                    uploadDateFrom,
                    uploadDateTo);

                Log.Information("Advanced search found {Count} documents matching criteria", results.Count());
                return results;
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentService.AdvancedSearchDocumentsAsync");
                throw;
            }
        }

        public async Task<Dictionary<string, string>> ValidateDocumentAsync(Document document)
        {
            try
            {
                // Use the field configuration service to validate the document
                return await _fieldConfigService.ValidateDocumentAsync(document);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error validating document {DocumentType} - {CertificateNumber}",
                    document.DocumentType, document.CertificateNumber);
                throw;
            }
        }

        public async Task<Document> AddDocumentAsync(Document document, Stream fileStream, string fileName)
        {
            try
            {
                if (_authService.CurrentUser == null)
                    throw new UnauthorizedAccessException("User must be logged in to add documents");

                // Validate the document against field configuration
                var validationErrors = await ValidateDocumentAsync(document);
                if (validationErrors.Any())
                {
                    // Combine all validation errors into a single message
                    string errorMessage = string.Join("\n", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                    throw new ValidationException($"Document validation failed:\n{errorMessage}");
                }

                // Save the file
                string filePath = await _fileStorageService.SaveFileAsync(fileStream, fileName, document.DocumentType);

                // Set document properties
                document.FilePath = filePath;
                document.UploadedBy = _authService.CurrentUser.UserId;
                document.UploadedAt = DateTime.Now;

                // Save to database
                await _documentRepository.AddAsync(document);
                await _documentRepository.SaveChangesAsync();

                Log.Information("Document added: {DocumentType} - {CertificateNumber} by user {Username}",
                    document.DocumentType, document.CertificateNumber, _authService.CurrentUser.Username);

                return document;
            }
            catch (ValidationException ex)
            {
                Log.Warning(ex, "Validation error adding document {DocumentType} - {CertificateNumber}",
                    document.DocumentType, document.CertificateNumber);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding document {DocumentType} - {CertificateNumber}",
                    document.DocumentType, document.CertificateNumber);
                throw;
            }
        }

        public async Task<Document> AddDocumentWithBackAsync(Document document, Stream frontFileStream, string frontFileName, Stream backFileStream, string backFileName)
        {
            try
            {
                if (_authService.CurrentUser == null)
                    throw new UnauthorizedAccessException("User must be logged in to add documents");

                // Validate the document against field configuration
                var validationErrors = await ValidateDocumentAsync(document);
                if (validationErrors.Any())
                {
                    // Combine all validation errors into a single message
                    string errorMessage = string.Join("\n", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                    throw new ValidationException($"Document validation failed:\n{errorMessage}");
                }

                // Save the front file
                string frontFilePath = await _fileStorageService.SaveFileAsync(frontFileStream, frontFileName, document.DocumentType);

                // Save the back file
                string backFilePath = await _fileStorageService.SaveFileAsync(backFileStream, backFileName, document.DocumentType);

                // Set document properties
                document.FilePath = frontFilePath;
                document.BackFilePath = backFilePath;
                document.UploadedBy = _authService.CurrentUser.UserId;
                document.UploadedAt = DateTime.Now;

                // Save to database
                await _documentRepository.AddAsync(document);
                await _documentRepository.SaveChangesAsync();

                Log.Information("Document added with front and back files: {DocumentType} - {CertificateNumber} by user {Username}",
                    document.DocumentType, document.CertificateNumber, _authService.CurrentUser.Username);

                return document;
            }
            catch (ValidationException ex)
            {
                Log.Warning(ex, "Validation error adding document with front and back files {DocumentType} - {CertificateNumber}",
                    document.DocumentType, document.CertificateNumber);
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding document with front and back files {DocumentType} - {CertificateNumber}",
                    document.DocumentType, document.CertificateNumber);
                throw;
            }
        }

        public async Task<bool> UpdateDocumentAsync(Document document)
        {
            try
            {
                Log.Debug("Updating document with ID: {DocumentId}", document.DocumentId);

                // Validate the document against field configuration
                var validationErrors = await ValidateDocumentAsync(document);
                if (validationErrors.Any())
                {
                    // Combine all validation errors into a single message
                    string errorMessage = string.Join("\n", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                    throw new ValidationException($"Document validation failed:\n{errorMessage}");
                }

                // Update the document in the database
                await _documentRepository.UpdateAsync(document);
                await _documentRepository.SaveChangesAsync();

                Log.Information("Document updated successfully with ID: {DocumentId}", document.DocumentId);
                return true;
            }
            catch (ValidationException ex)
            {
                Log.Warning(ex, "Validation error updating document with ID: {DocumentId}", document.DocumentId);
                throw;
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentService.UpdateDocumentAsync");
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            try
            {
                Log.Debug("Deleting document with ID: {DocumentId}", documentId);

                // Get the document
                var document = await _documentRepository.GetByIdAsync(documentId);

                if (document == null)
                {
                    Log.Warning("Document not found with ID: {DocumentId}", documentId);
                    return false;
                }

                // Delete the front file
                if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
                {
                    await _fileStorageService.DeleteFileAsync(document.FilePath);
                }

                // Delete the back file if it exists
                if (!string.IsNullOrEmpty(document.BackFilePath) && File.Exists(document.BackFilePath))
                {
                    await _fileStorageService.DeleteFileAsync(document.BackFilePath);
                }

                // Delete the document from the database
                await _documentRepository.DeleteAsync(document);
                await _documentRepository.SaveChangesAsync();

                Log.Information("Document deleted successfully with ID: {DocumentId}", documentId);
                return true;
            }
            catch (Exception ex)
            {
                SerilogConfig.LogUnhandledException(ex, "DocumentService.DeleteDocumentAsync");
                throw;
            }
        }


    }


}
