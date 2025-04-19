using CivilRegistryApp.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Admin
{
    public interface IFieldConfigurationService
    {
        /// <summary>
        /// Gets all field configurations
        /// </summary>
        /// <returns>A collection of all field configurations</returns>
        Task<IEnumerable<FieldConfiguration>> GetAllFieldConfigurationsAsync();

        /// <summary>
        /// Gets all field configurations for a specific document type
        /// </summary>
        /// <param name="documentType">The document type to get configurations for</param>
        /// <returns>A collection of field configurations</returns>
        Task<IEnumerable<FieldConfiguration>> GetFieldConfigurationsByDocumentTypeAsync(string documentType);

        /// <summary>
        /// Gets a specific field configuration
        /// </summary>
        /// <param name="id">The field configuration ID</param>
        /// <returns>The field configuration if found, null otherwise</returns>
        Task<FieldConfiguration?> GetFieldConfigurationAsync(int id);

        /// <summary>
        /// Gets a specific field configuration by document type and field name
        /// </summary>
        /// <param name="documentType">The document type</param>
        /// <param name="fieldName">The field name</param>
        /// <returns>The field configuration if found, null otherwise</returns>
        Task<FieldConfiguration?> GetFieldConfigurationAsync(string documentType, string fieldName);

        /// <summary>
        /// Creates a new field configuration
        /// </summary>
        /// <param name="fieldConfiguration">The field configuration to create</param>
        /// <returns>The created field configuration</returns>
        Task<FieldConfiguration> CreateFieldConfigurationAsync(FieldConfiguration fieldConfiguration);

        /// <summary>
        /// Updates an existing field configuration
        /// </summary>
        /// <param name="fieldConfiguration">The field configuration to update</param>
        /// <returns>The updated field configuration</returns>
        Task<FieldConfiguration> UpdateFieldConfigurationAsync(FieldConfiguration fieldConfiguration);

        /// <summary>
        /// Deletes a field configuration
        /// </summary>
        /// <param name="id">The field configuration ID</param>
        /// <returns>True if the field configuration was deleted, false otherwise</returns>
        Task<bool> DeleteFieldConfigurationAsync(int id);

        /// <summary>
        /// Gets all required fields for a specific document type
        /// </summary>
        /// <param name="documentType">The document type</param>
        /// <returns>A collection of required field names</returns>
        Task<IEnumerable<string>> GetRequiredFieldsAsync(string documentType);

        /// <summary>
        /// Validates a document against the field configuration
        /// </summary>
        /// <param name="document">The document to validate</param>
        /// <returns>A dictionary of field names and error messages, empty if valid</returns>
        Task<Dictionary<string, string>> ValidateDocumentAsync(Document document);

        /// <summary>
        /// Initializes default field configurations for all document types if they don't exist
        /// </summary>
        /// <returns>True if configurations were created, false if they already existed</returns>
        Task<bool> InitializeDefaultConfigurationsAsync();
    }
}
