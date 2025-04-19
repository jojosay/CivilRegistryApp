using CivilRegistryApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data.Repositories
{
    public class ReportRepository : Repository<ScheduledReport>, IReportRepository
    {
        public ReportRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ScheduledReport>> GetActiveScheduledReportsAsync()
        {
            return await _dbSet
                .Where(r => r.IsActive)
                .Include(r => r.CreatedByUser)
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduledReport>> GetScheduledReportsByUserAsync(int userId)
        {
            return await _dbSet
                .Where(r => r.CreatedBy == userId)
                .Include(r => r.CreatedByUser)
                .ToListAsync();
        }

        public async Task<ScheduledReport?> GetScheduledReportWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(r => r.CreatedByUser)
                .FirstOrDefaultAsync(r => r.ScheduledReportId == id);
        }

        public async Task<IEnumerable<ScheduledReport>> GetDueReportsAsync()
        {
            // This method will be used by the scheduler to find reports that need to be run
            // The actual scheduling logic will be in the scheduler service
            return await _dbSet
                .Where(r => r.IsActive)
                .Include(r => r.CreatedByUser)
                .ToListAsync();
        }

        public async Task UpdateLastRunTimeAsync(int reportId, DateTime runTime)
        {
            var report = await _dbSet.FindAsync(reportId);
            if (report != null)
            {
                report.LastRunAt = runTime;
                await SaveChangesAsync();
            }
        }
    }
}
