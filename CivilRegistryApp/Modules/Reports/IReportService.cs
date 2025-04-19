using CivilRegistryApp.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Reports
{
    public interface IReportService
    {
        // Report generation
        Task<MemoryStream> GenerateDocumentReportAsync(
            string? documentType = null,
            string? registryOffice = null,
            string? province = null,
            string? cityMunicipality = null,
            string? barangay = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string exportFormat = "PDF");

        Task<MemoryStream> GenerateRequestReportAsync(
            string? status = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string exportFormat = "PDF");

        Task<MemoryStream> GenerateActivityReportAsync(
            string? activityType = null,
            int? userId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string exportFormat = "PDF");

        // Scheduled reports
        Task<IEnumerable<ScheduledReport>> GetAllScheduledReportsAsync();
        Task<IEnumerable<ScheduledReport>> GetActiveScheduledReportsAsync();
        Task<IEnumerable<ScheduledReport>> GetScheduledReportsByUserAsync(int userId);
        Task<ScheduledReport?> GetScheduledReportAsync(int id);
        Task<ScheduledReport> AddScheduledReportAsync(ScheduledReport report);
        Task<ScheduledReport> UpdateScheduledReportAsync(ScheduledReport report);
        Task<bool> DeleteScheduledReportAsync(int id);
        Task<bool> ToggleScheduledReportStatusAsync(int id);

        // Run a scheduled report immediately
        Task<string> RunScheduledReportAsync(int id);
    }
}
