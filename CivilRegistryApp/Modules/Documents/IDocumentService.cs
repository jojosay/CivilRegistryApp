using CivilRegistryApp.Data.Entities;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Documents
{
    public interface IDocumentService
    {
        /// <summary>
        /// Gets all documents from the database
        /// </summary>
        Task<IEnumerable<Document>> GetAllDocumentsAsync();

        /// <summary>
        /// Gets a document by its ID
        /// </summary>
        Task<Document?> GetDocumentByIdAsync(int documentId);

        /// <summary>
        /// Adds a new document with a front file
        /// </summary>
        Task<Document> AddDocumentAsync(Document document, Stream fileStream, string fileName);

        /// <summary>
        /// Validates a document against the field configuration
        /// </summary>
        /// <param name="document">The document to validate</param>
        /// <returns>A dictionary of field names and error messages, empty if valid</returns>
        Task<Dictionary<string, string>> ValidateDocumentAsync(Document document);

        /// <summary>
        /// Adds a new document with both front and back files
        /// </summary>
        Task<Document> AddDocumentWithBackAsync(Document document, Stream frontFileStream, string frontFileName, Stream backFileStream, string backFileName);

        /// <summary>
        /// Updates an existing document
        /// </summary>
        Task<bool> UpdateDocumentAsync(Document document);

        /// <summary>
        /// Deletes a document by its ID
        /// </summary>
        Task<bool> DeleteDocumentAsync(int documentId);

        /// <summary>
        /// Searches for documents based on search criteria
        /// </summary>
        Task<IEnumerable<Document>> SearchDocumentsAsync(string searchText, string? documentType = null);

        /// <summary>
        /// Performs an advanced search for documents with multiple filter criteria
        /// </summary>
        Task<IEnumerable<Document>> AdvancedSearchDocumentsAsync(
            string? searchText = null,
            string? documentType = null,
            string? registryOffice = null,
            string? province = null,
            string? cityMunicipality = null,
            string? barangay = null,
            string? uploadedByUsername = null,
            System.DateTime? eventDateFrom = null,
            System.DateTime? eventDateTo = null,
            System.DateTime? registrationDateFrom = null,
            System.DateTime? registrationDateTo = null,
            System.DateTime? uploadDateFrom = null,
            System.DateTime? uploadDateTo = null);
    }
}
