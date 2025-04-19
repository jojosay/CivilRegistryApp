using CivilRegistryApp.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivilRegistryApp.Data.Repositories
{
    public interface IReportRepository : IRepository<ScheduledReport>
    {
        Task<IEnumerable<ScheduledReport>> GetActiveScheduledReportsAsync();
        Task<IEnumerable<ScheduledReport>> GetScheduledReportsByUserAsync(int userId);
        Task<ScheduledReport?> GetScheduledReportWithDetailsAsync(int id);
        Task<IEnumerable<ScheduledReport>> GetDueReportsAsync();
        Task UpdateLastRunTimeAsync(int reportId, DateTime runTime);
    }
}
