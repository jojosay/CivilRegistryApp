using CivilRegistryApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data.Repositories
{
    public class FieldConfigurationRepository : Repository<FieldConfiguration>, IFieldConfigurationRepository
    {
        public FieldConfigurationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<FieldConfiguration>> GetByDocumentTypeAsync(string documentType)
        {
            return await _dbSet
                .Where(f => f.DocumentType == documentType)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();
        }

        public async Task<FieldConfiguration?> GetByDocumentTypeAndFieldNameAsync(string documentType, string fieldName)
        {
            return await _dbSet
                .FirstOrDefaultAsync(f => f.DocumentType == documentType && f.FieldName == fieldName);
        }

        public async Task<IEnumerable<string>> GetRequiredFieldsAsync(string documentType)
        {
            return await _dbSet
                .Where(f => f.DocumentType == documentType && f.IsRequired)
                .Select(f => f.FieldName)
                .ToListAsync();
        }
    }
}
