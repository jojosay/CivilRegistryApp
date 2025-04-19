using CivilRegistryApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data.Repositories
{
    public class DocumentRepository : Repository<Document>, IDocumentRepository
    {
        public DocumentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            return await _dbSet
                .Where(d =>
                    d.GivenName.Contains(searchTerm) ||
                    d.MiddleName.Contains(searchTerm) ||
                    d.FamilyName.Contains(searchTerm) ||
                    d.CertificateNumber.Contains(searchTerm) ||
                    d.RegistryOffice.Contains(searchTerm))
                .Include(d => d.UploadedByUser)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> AdvancedSearchAsync(
            string? searchTerm = null,
            string? documentType = null,
            string? registryOffice = null,
            string? province = null,
            string? cityMunicipality = null,
            string? barangay = null,
            int? uploadedBy = null,
            DateTime? eventDateFrom = null,
            DateTime? eventDateTo = null,
            DateTime? registrationDateFrom = null,
            DateTime? registrationDateTo = null,
            DateTime? uploadDateFrom = null,
            DateTime? uploadDateTo = null)
        {
            // Start with all documents
            var query = _dbSet.AsQueryable();

            // Apply filters if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(d =>
                    d.GivenName.ToLower().Contains(searchTerm) ||
                    (d.MiddleName != null && d.MiddleName.ToLower().Contains(searchTerm)) ||
                    d.FamilyName.ToLower().Contains(searchTerm) ||
                    d.CertificateNumber.ToLower().Contains(searchTerm) ||
                    d.RegistryOffice.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(documentType) && documentType != "All Documents")
            {
                query = query.Where(d => d.DocumentType == documentType);
            }

            if (!string.IsNullOrWhiteSpace(registryOffice) && registryOffice != "All Offices")
            {
                query = query.Where(d => d.RegistryOffice == registryOffice);
            }

            if (!string.IsNullOrWhiteSpace(province) && province != "All Provinces")
            {
                query = query.Where(d => d.Province == province);
            }

            if (!string.IsNullOrWhiteSpace(cityMunicipality) && cityMunicipality != "All Cities")
            {
                query = query.Where(d => d.CityMunicipality == cityMunicipality);
            }

            if (!string.IsNullOrWhiteSpace(barangay) && barangay != "All Barangays")
            {
                query = query.Where(d => d.Barangay == barangay);
            }

            if (uploadedBy.HasValue)
            {
                query = query.Where(d => d.UploadedBy == uploadedBy.Value);
            }

            if (eventDateFrom.HasValue)
            {
                query = query.Where(d => d.DateOfEvent >= eventDateFrom.Value);
            }

            if (eventDateTo.HasValue)
            {
                query = query.Where(d => d.DateOfEvent <= eventDateTo.Value);
            }

            if (registrationDateFrom.HasValue)
            {
                query = query.Where(d => d.RegistrationDate >= registrationDateFrom.Value);
            }

            if (registrationDateTo.HasValue)
            {
                query = query.Where(d => d.RegistrationDate <= registrationDateTo.Value);
            }

            if (uploadDateFrom.HasValue)
            {
                query = query.Where(d => d.UploadedAt >= uploadDateFrom.Value);
            }

            if (uploadDateTo.HasValue)
            {
                query = query.Where(d => d.UploadedAt <= uploadDateTo.Value);
            }

            // Include related entities
            query = query.Include(d => d.UploadedByUser);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetDocumentsByTypeAsync(string documentType)
        {
            return await _dbSet
                .Where(d => d.DocumentType == documentType)
                .Include(d => d.UploadedByUser)
                .ToListAsync();
        }

        public async Task<Document?> GetDocumentWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(d => d.UploadedByUser)
                .Include(d => d.Requests)
                .FirstOrDefaultAsync(d => d.DocumentId == id);
        }
    }

    public interface IDocumentRepository : IRepository<Document>
    {
        Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm);
        Task<IEnumerable<Document>> GetDocumentsByTypeAsync(string documentType);
        Task<Document?> GetDocumentWithDetailsAsync(int id);
        Task<IEnumerable<Document>> AdvancedSearchAsync(
            string? searchTerm = null,
            string? documentType = null,
            string? registryOffice = null,
            string? province = null,
            string? cityMunicipality = null,
            string? barangay = null,
            int? uploadedBy = null,
            DateTime? eventDateFrom = null,
            DateTime? eventDateTo = null,
            DateTime? registrationDateFrom = null,
            DateTime? registrationDateTo = null,
            DateTime? uploadDateFrom = null,
            DateTime? uploadDateTo = null);
    }
}
