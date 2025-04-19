using CivilRegistryApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data.Repositories
{
    public class DocumentRequestRepository : Repository<DocumentRequest>, IDocumentRequestRepository
    {
        public DocumentRequestRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<DocumentRequest>> GetPendingRequestsAsync()
        {
            return await _dbSet
                .Where(r => r.Status == "Pending")
                .Include(r => r.RelatedDocument)
                .ToListAsync();
        }

        public async Task<IEnumerable<DocumentRequest>> GetRequestsByStatusAsync(string status)
        {
            return await _dbSet
                .Where(r => r.Status == status)
                .Include(r => r.RelatedDocument)
                .Include(r => r.HandledByUser)
                .ToListAsync();
        }

        public async Task<DocumentRequest?> GetRequestWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(r => r.RelatedDocument)
                .Include(r => r.HandledByUser)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

        public async Task<IEnumerable<DocumentRequest>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(r => r.RelatedDocument)
                .Include(r => r.HandledByUser)
                .ToListAsync();
        }
    }

    public interface IDocumentRequestRepository : IRepository<DocumentRequest>
    {
        Task<IEnumerable<DocumentRequest>> GetPendingRequestsAsync();
        Task<IEnumerable<DocumentRequest>> GetRequestsByStatusAsync(string status);
        Task<DocumentRequest?> GetRequestWithDetailsAsync(int id);
        Task<IEnumerable<DocumentRequest>> GetAllWithDetailsAsync();
    }
}
