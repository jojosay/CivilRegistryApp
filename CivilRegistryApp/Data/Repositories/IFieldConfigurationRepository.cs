using CivilRegistryApp.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data.Repositories
{
    public interface IFieldConfigurationRepository : IRepository<FieldConfiguration>
    {
        /// <summary>
        /// Gets all field configurations for a specific document type
        /// </summary>
        /// <param name="documentType">The document type to get configurations for</param>
        /// <returns>A collection of field configurations</returns>
        Task<IEnumerable<FieldConfiguration>> GetByDocumentTypeAsync(string documentType);

        /// <summary>
        /// Gets a specific field configuration by document type and field name
        /// </summary>
        /// <param name="documentType">The document type</param>
        /// <param name="fieldName">The field name</param>
        /// <returns>The field configuration if found, null otherwise</returns>
        Task<FieldConfiguration?> GetByDocumentTypeAndFieldNameAsync(string documentType, string fieldName);

        /// <summary>
        /// Gets all required fields for a specific document type
        /// </summary>
        /// <param name="documentType">The document type</param>
        /// <returns>A collection of required field names</returns>
        Task<IEnumerable<string>> GetRequiredFieldsAsync(string documentType);
    }
}
