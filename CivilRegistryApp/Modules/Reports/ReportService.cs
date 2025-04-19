using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Infrastructure.Logging;
using CivilRegistryApp.Modules.Auth;
using CivilRegistryApp.Modules.Documents;
using CivilRegistryApp.Modules.Requests;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using DataDocument = CivilRegistryApp.Data.Entities.Document;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CivilRegistryApp.Modules.Reports
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _dbContext;
        private readonly IReportRepository _reportRepository;
        private readonly IDocumentService _documentService;
        private readonly IRequestService _requestService;
        private readonly IAuthenticationService _authService;

        public ReportService(
            AppDbContext dbContext,
            IReportRepository reportRepository,
            IDocumentService documentService,
            IRequestService requestService,
            IAuthenticationService authService)
        {
            _dbContext = dbContext;
            _reportRepository = reportRepository;
            _documentService = documentService;
            _requestService = requestService;
            _authService = authService;
        }

        #region Report Generation

        public async Task<MemoryStream> GenerateDocumentReportAsync(
            string? documentType = null,
            string? registryOffice = null,
            string? province = null,
            string? cityMunicipality = null,
            string? barangay = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string exportFormat = "PDF")
        {
            try
            {
                Log.Information("Generating document report with format {ExportFormat}", exportFormat);

                // Get documents based on criteria
                var documents = await _documentService.AdvancedSearchDocumentsAsync(
                    searchText: null,
                    documentType: documentType,
                    registryOffice: registryOffice,
                    province: province,
                    cityMunicipality: cityMunicipality,
                    barangay: barangay,
                    uploadedByUsername: null,
                    eventDateFrom: dateFrom,
                    eventDateTo: dateTo,
                    registrationDateFrom: null,
                    registrationDateTo: null,
                    uploadDateFrom: null,
                    uploadDateTo: null);

                // Generate report based on format
                if (exportFormat.Equals("PDF", StringComparison.OrdinalIgnoreCase))
                {
                    return GenerateDocumentPdfReport(documents.ToList());
                }
                else if (exportFormat.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                {
                    return GenerateDocumentExcelReport(documents.ToList());
                }
                else
                {
                    throw new ArgumentException($"Unsupported export format: {exportFormat}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating document report");
                throw;
            }
        }

        public async Task<MemoryStream> GenerateRequestReportAsync(
            string? status = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string exportFormat = "PDF")
        {
            try
            {
                Log.Information("Generating request report with format {ExportFormat}", exportFormat);

                // Get requests based on criteria
                var requests = await _requestService.GetAllRequestsAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    requests = requests.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                if (dateFrom.HasValue)
                {
                    requests = requests.Where(r => r.RequestDate >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    requests = requests.Where(r => r.RequestDate <= dateTo.Value);
                }

                // Generate report based on format
                if (exportFormat.Equals("PDF", StringComparison.OrdinalIgnoreCase))
                {
                    return GenerateRequestPdfReport(requests.ToList());
                }
                else if (exportFormat.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                {
                    return GenerateRequestExcelReport(requests.ToList());
                }
                else
                {
                    throw new ArgumentException($"Unsupported export format: {exportFormat}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating request report");
                throw;
            }
        }

        public async Task<MemoryStream> GenerateActivityReportAsync(
            string? activityType = null,
            int? userId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string exportFormat = "PDF")
        {
            try
            {
                Log.Information("Generating activity report with format {ExportFormat}", exportFormat);

                // Get activities based on criteria
                var query = _dbContext.UserActivities.AsQueryable();

                if (!string.IsNullOrEmpty(activityType))
                {
                    query = query.Where(a => a.ActivityType.Equals(activityType, StringComparison.OrdinalIgnoreCase));
                }

                if (userId.HasValue)
                {
                    query = query.Where(a => a.UserId == userId.Value);
                }

                if (dateFrom.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= dateFrom.Value);
                }

                if (dateTo.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= dateTo.Value);
                }

                // Include user information
                query = query.Include(a => a.User);

                // Execute query
                var activities = await query.OrderByDescending(a => a.Timestamp).ToListAsync();

                // Generate report based on format
                if (exportFormat.Equals("PDF", StringComparison.OrdinalIgnoreCase))
                {
                    return GenerateActivityPdfReport(activities);
                }
                else if (exportFormat.Equals("Excel", StringComparison.OrdinalIgnoreCase))
                {
                    return GenerateActivityExcelReport(activities);
                }
                else
                {
                    throw new ArgumentException($"Unsupported export format: {exportFormat}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating activity report");
                throw;
            }
        }

        #endregion

        #region PDF Report Generation

        private MemoryStream GenerateDocumentPdfReport(List<DataDocument> documents)
        {
            var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);

            try
            {
                // Add title
                var title = new Paragraph("Document Report");
                title.SetFontSize(18);
                title.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
                document.Add(title);

                var genDate = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                genDate.SetFontSize(10);
                document.Add(genDate);

                var totalDocs = new Paragraph($"Total Documents: {documents.Count}");
                totalDocs.SetFontSize(10);
                document.Add(totalDocs);

                document.Add(new Paragraph("\n"));

                // Create table
                var table = new Table(6);
                table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                // Add headers
                table.AddHeaderCell("Document Type");
                table.AddHeaderCell("Certificate Number");
                table.AddHeaderCell("Registry Office");
                table.AddHeaderCell("Name");
                table.AddHeaderCell("Date of Event");
                table.AddHeaderCell("Registration Date");

                // Add data rows
                foreach (var doc in documents)
                {
                    table.AddCell(doc.DocumentType);
                    table.AddCell(doc.CertificateNumber);
                    table.AddCell(doc.RegistryOffice);
                    table.AddCell($"{doc.GivenName} {doc.MiddleName} {doc.FamilyName}");
                    table.AddCell(doc.DateOfEvent.ToString("yyyy-MM-dd"));
                    table.AddCell(doc.RegistrationDate.ToString("yyyy-MM-dd"));
                }

                document.Add(table);
                document.Close();

                // Reset stream position
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating document PDF report");
                document.Close();
                stream.Dispose();
                throw;
            }
        }

        private MemoryStream GenerateRequestPdfReport(List<DocumentRequest> requests)
        {
            var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);

            try
            {
                // Add title
                var title = new Paragraph("Document Request Report");
                title.SetFontSize(18);
                title.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
                document.Add(title);

                var genDate = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                genDate.SetFontSize(10);
                document.Add(genDate);

                var totalReqs = new Paragraph($"Total Requests: {requests.Count}");
                totalReqs.SetFontSize(10);
                document.Add(totalReqs);

                document.Add(new Paragraph("\n"));

                // Create table
                var table = new Table(5);
                table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                // Add headers
                table.AddHeaderCell("Requestor Name");
                table.AddHeaderCell("Document Type");
                table.AddHeaderCell("Purpose");
                table.AddHeaderCell("Status");
                table.AddHeaderCell("Request Date");

                // Add data rows
                foreach (var req in requests)
                {
                    table.AddCell(req.RequestorName);
                    table.AddCell(req.RelatedDocument?.DocumentType ?? "Unknown");
                    table.AddCell(req.Purpose);
                    table.AddCell(req.Status);
                    table.AddCell(req.RequestDate.ToString("yyyy-MM-dd"));
                }

                document.Add(table);
                document.Close();

                // Reset stream position
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating request PDF report");
                document.Close();
                stream.Dispose();
                throw;
            }
        }

        private MemoryStream GenerateActivityPdfReport(List<UserActivity> activities)
        {
            var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdf);

            try
            {
                // Add title
                var title = new Paragraph("User Activity Report");
                title.SetFontSize(18);
                title.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
                document.Add(title);

                var genDate = new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                genDate.SetFontSize(10);
                document.Add(genDate);

                var totalActs = new Paragraph($"Total Activities: {activities.Count}");
                totalActs.SetFontSize(10);
                document.Add(totalActs);

                document.Add(new Paragraph("\n"));

                // Create table
                var table = new Table(4);
                table.SetWidth(iText.Layout.Properties.UnitValue.CreatePercentValue(100));

                // Add headers
                table.AddHeaderCell("User");
                table.AddHeaderCell("Activity Type");
                table.AddHeaderCell("Description");
                table.AddHeaderCell("Timestamp");

                // Add data rows
                foreach (var activity in activities)
                {
                    table.AddCell(activity.User?.Username ?? "Unknown");
                    table.AddCell(activity.ActivityType);
                    table.AddCell(activity.Description);
                    table.AddCell(activity.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                document.Add(table);
                document.Close();

                // Reset stream position
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating activity PDF report");
                document.Close();
                stream.Dispose();
                throw;
            }
        }

        #endregion

        #region Excel Report Generation

        private MemoryStream GenerateDocumentExcelReport(List<DataDocument> documents)
        {
            var stream = new MemoryStream();

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Documents");

                    // Add title
                    worksheet.Cell("A1").Value = "Document Report";
                    worksheet.Cell("A2").Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    worksheet.Cell("A3").Value = $"Total Documents: {documents.Count}";

                    // Add headers
                    worksheet.Cell("A5").Value = "Document Type";
                    worksheet.Cell("B5").Value = "Certificate Number";
                    worksheet.Cell("C5").Value = "Registry Office";
                    worksheet.Cell("D5").Value = "Name";
                    worksheet.Cell("E5").Value = "Date of Event";
                    worksheet.Cell("F5").Value = "Registration Date";
                    worksheet.Cell("G5").Value = "Province";
                    worksheet.Cell("H5").Value = "City/Municipality";
                    worksheet.Cell("I5").Value = "Barangay";

                    // Format header row
                    var headerRow = worksheet.Row(5);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data rows
                    int row = 6;
                    foreach (var doc in documents)
                    {
                        worksheet.Cell(row, 1).Value = doc.DocumentType;
                        worksheet.Cell(row, 2).Value = doc.CertificateNumber;
                        worksheet.Cell(row, 3).Value = doc.RegistryOffice;
                        worksheet.Cell(row, 4).Value = $"{doc.GivenName} {doc.MiddleName} {doc.FamilyName}";
                        worksheet.Cell(row, 5).Value = doc.DateOfEvent;
                        worksheet.Cell(row, 6).Value = doc.RegistrationDate;
                        worksheet.Cell(row, 7).Value = doc.Province;
                        worksheet.Cell(row, 8).Value = doc.CityMunicipality;
                        worksheet.Cell(row, 9).Value = doc.Barangay;
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Save to stream
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return stream;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating document Excel report");
                stream.Dispose();
                throw;
            }
        }

        private MemoryStream GenerateRequestExcelReport(List<DocumentRequest> requests)
        {
            var stream = new MemoryStream();

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Requests");

                    // Add title
                    worksheet.Cell("A1").Value = "Document Request Report";
                    worksheet.Cell("A2").Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    worksheet.Cell("A3").Value = $"Total Requests: {requests.Count}";

                    // Add headers
                    worksheet.Cell("A5").Value = "Requestor Name";
                    worksheet.Cell("B5").Value = "Requestor Contact";
                    worksheet.Cell("C5").Value = "Document Type";
                    worksheet.Cell("D5").Value = "Certificate Number";
                    worksheet.Cell("E5").Value = "Purpose";
                    worksheet.Cell("F5").Value = "Status";
                    worksheet.Cell("G5").Value = "Request Date";
                    worksheet.Cell("H5").Value = "Handled By";

                    // Format header row
                    var headerRow = worksheet.Row(5);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data rows
                    int row = 6;
                    foreach (var req in requests)
                    {
                        worksheet.Cell(row, 1).Value = req.RequestorName;
                        worksheet.Cell(row, 2).Value = req.RequestorContact;
                        worksheet.Cell(row, 3).Value = req.RelatedDocument?.DocumentType ?? "Unknown";
                        worksheet.Cell(row, 4).Value = req.RelatedDocument?.CertificateNumber ?? "N/A";
                        worksheet.Cell(row, 5).Value = req.Purpose;
                        worksheet.Cell(row, 6).Value = req.Status;
                        worksheet.Cell(row, 7).Value = req.RequestDate;
                        worksheet.Cell(row, 8).Value = req.HandledByUser?.FullName ?? "Not Assigned";
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Save to stream
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return stream;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating request Excel report");
                stream.Dispose();
                throw;
            }
        }

        private MemoryStream GenerateActivityExcelReport(List<UserActivity> activities)
        {
            var stream = new MemoryStream();

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Activities");

                    // Add title
                    worksheet.Cell("A1").Value = "User Activity Report";
                    worksheet.Cell("A2").Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    worksheet.Cell("A3").Value = $"Total Activities: {activities.Count}";

                    // Add headers
                    worksheet.Cell("A5").Value = "User ID";
                    worksheet.Cell("B5").Value = "Username";
                    worksheet.Cell("C5").Value = "Activity Type";
                    worksheet.Cell("D5").Value = "Description";
                    worksheet.Cell("E5").Value = "Timestamp";
                    worksheet.Cell("F5").Value = "IP Address";

                    // Format header row
                    var headerRow = worksheet.Row(5);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data rows
                    int row = 6;
                    foreach (var activity in activities)
                    {
                        worksheet.Cell(row, 1).Value = activity.UserId;
                        worksheet.Cell(row, 2).Value = activity.User?.Username ?? "Unknown";
                        worksheet.Cell(row, 3).Value = activity.ActivityType;
                        worksheet.Cell(row, 4).Value = activity.Description;
                        worksheet.Cell(row, 5).Value = activity.Timestamp;
                        worksheet.Cell(row, 6).Value = activity.IpAddress;
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Columns().AdjustToContents();

                    // Save to stream
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return stream;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating activity Excel report");
                stream.Dispose();
                throw;
            }
        }

        #endregion

        #region Scheduled Reports

        public async Task<IEnumerable<ScheduledReport>> GetAllScheduledReportsAsync()
        {
            try
            {
                try
                {
                    return await _reportRepository.GetAllAsync();
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table: ScheduledReports"))
                {
                    // The ScheduledReports table doesn't exist yet
                    Log.Warning("ScheduledReports table does not exist yet. Returning empty list.");
                    return new List<ScheduledReport>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all scheduled reports");
                throw;
            }
        }

        public async Task<IEnumerable<ScheduledReport>> GetActiveScheduledReportsAsync()
        {
            try
            {
                try
                {
                    return await _reportRepository.GetActiveScheduledReportsAsync();
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table: ScheduledReports"))
                {
                    // The ScheduledReports table doesn't exist yet
                    Log.Warning("ScheduledReports table does not exist yet. Returning empty list.");
                    return new List<ScheduledReport>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting active scheduled reports");
                throw;
            }
        }

        public async Task<IEnumerable<ScheduledReport>> GetScheduledReportsByUserAsync(int userId)
        {
            try
            {
                try
                {
                    return await _reportRepository.GetScheduledReportsByUserAsync(userId);
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table: ScheduledReports"))
                {
                    // The ScheduledReports table doesn't exist yet
                    Log.Warning("ScheduledReports table does not exist yet. Returning empty list.");
                    return new List<ScheduledReport>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting scheduled reports for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ScheduledReport> GetScheduledReportAsync(int id)
        {
            try
            {
                try
                {
                    return await _reportRepository.GetScheduledReportWithDetailsAsync(id);
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("no such table: ScheduledReports"))
                {
                    // The ScheduledReports table doesn't exist yet
                    Log.Warning("ScheduledReports table does not exist yet. Returning null.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting scheduled report {ReportId}", id);
                throw;
            }
        }

        public async Task<ScheduledReport> AddScheduledReportAsync(ScheduledReport report)
        {
            try
            {
                // Set created by and created at
                report.CreatedBy = _authService.CurrentUser.UserId;
                report.CreatedAt = DateTime.Now;

                await _reportRepository.AddAsync(report);
                await _reportRepository.SaveChangesAsync();

                Log.Information("Added scheduled report {ReportName} by user {Username}",
                    report.Name, _authService.CurrentUser.Username);

                return report;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding scheduled report {ReportName}", report.Name);
                throw;
            }
        }

        public async Task<ScheduledReport> UpdateScheduledReportAsync(ScheduledReport report)
        {
            try
            {
                var existingReport = await _reportRepository.GetByIdAsync(report.ScheduledReportId);
                if (existingReport == null)
                {
                    throw new ArgumentException($"Scheduled report with ID {report.ScheduledReportId} not found");
                }

                // Update properties
                existingReport.Name = report.Name;
                existingReport.ReportType = report.ReportType;
                existingReport.DocumentType = report.DocumentType;
                existingReport.RegistryOffice = report.RegistryOffice;
                existingReport.Province = report.Province;
                existingReport.CityMunicipality = report.CityMunicipality;
                existingReport.Barangay = report.Barangay;
                existingReport.DateFrom = report.DateFrom;
                existingReport.DateTo = report.DateTo;
                existingReport.Schedule = report.Schedule;
                existingReport.ExportFormat = report.ExportFormat;
                existingReport.OutputPath = report.OutputPath;
                existingReport.IsActive = report.IsActive;
                existingReport.EmailRecipients = report.EmailRecipients;

                await _reportRepository.UpdateAsync(existingReport);
                await _reportRepository.SaveChangesAsync();

                Log.Information("Updated scheduled report {ReportName} by user {Username}",
                    report.Name, _authService.CurrentUser.Username);

                return existingReport;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating scheduled report {ReportId}", report.ScheduledReportId);
                throw;
            }
        }

        public async Task<bool> DeleteScheduledReportAsync(int id)
        {
            try
            {
                var report = await _reportRepository.GetByIdAsync(id);
                if (report == null)
                {
                    return false;
                }

                await _reportRepository.DeleteAsync(report);
                await _reportRepository.SaveChangesAsync();

                Log.Information("Deleted scheduled report {ReportName} by user {Username}",
                    report.Name, _authService.CurrentUser.Username);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting scheduled report {ReportId}", id);
                throw;
            }
        }

        public async Task<bool> ToggleScheduledReportStatusAsync(int id)
        {
            try
            {
                var report = await _reportRepository.GetByIdAsync(id);
                if (report == null)
                {
                    return false;
                }

                report.IsActive = !report.IsActive;
                await _reportRepository.UpdateAsync(report);
                await _reportRepository.SaveChangesAsync();

                Log.Information("Toggled scheduled report {ReportName} status to {Status} by user {Username}",
                    report.Name, report.IsActive ? "Active" : "Inactive", _authService.CurrentUser.Username);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error toggling scheduled report status {ReportId}", id);
                throw;
            }
        }

        public async Task<string> RunScheduledReportAsync(int id)
        {
            try
            {
                var report = await _reportRepository.GetScheduledReportWithDetailsAsync(id);
                if (report == null)
                {
                    throw new ArgumentException($"Scheduled report with ID {id} not found");
                }

                // Generate the report based on type
                MemoryStream reportStream = null;
                string fileName = $"{report.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";

                switch (report.ReportType.ToLower())
                {
                    case "document":
                        reportStream = await GenerateDocumentReportAsync(
                            documentType: report.DocumentType,
                            registryOffice: report.RegistryOffice,
                            province: report.Province,
                            cityMunicipality: report.CityMunicipality,
                            barangay: report.Barangay,
                            dateFrom: report.DateFrom,
                            dateTo: report.DateTo,
                            exportFormat: report.ExportFormat);
                        break;

                    case "request":
                        reportStream = await GenerateRequestReportAsync(
                            status: null,
                            dateFrom: report.DateFrom,
                            dateTo: report.DateTo,
                            exportFormat: report.ExportFormat);
                        break;

                    case "activity":
                        reportStream = await GenerateActivityReportAsync(
                            activityType: null,
                            userId: null,
                            dateFrom: report.DateFrom,
                            dateTo: report.DateTo,
                            exportFormat: report.ExportFormat);
                        break;

                    default:
                        throw new ArgumentException($"Unsupported report type: {report.ReportType}");
                }

                // Determine file extension
                string extension = report.ExportFormat.Equals("PDF", StringComparison.OrdinalIgnoreCase) ? ".pdf" : ".xlsx";
                fileName += extension;

                // Save the report to the output path if specified
                if (!string.IsNullOrEmpty(report.OutputPath))
                {
                    string outputPath = report.OutputPath;
                    if (!Directory.Exists(outputPath))
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    string filePath = Path.Combine(outputPath, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        reportStream.CopyTo(fileStream);
                    }
                }

                // Update last run time
                await _reportRepository.UpdateLastRunTimeAsync(id, DateTime.Now);

                // Return the file name
                return fileName;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running scheduled report {ReportId}", id);
                throw;
            }
        }

        #endregion
    }
}
