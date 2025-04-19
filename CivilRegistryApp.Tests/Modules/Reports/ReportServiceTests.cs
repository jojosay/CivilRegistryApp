using CivilRegistryApp.Data;
using CivilRegistryApp.Data.Entities;
using CivilRegistryApp.Data.Repositories;
using CivilRegistryApp.Modules.Auth;
using CivilRegistryApp.Modules.Documents;
using CivilRegistryApp.Modules.Reports;
using CivilRegistryApp.Modules.Requests;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CivilRegistryApp.Tests.Modules.Reports
{
    public class ReportServiceTests
    {
        private readonly Mock<IReportRepository> _mockReportRepository;
        private readonly Mock<IDocumentService> _mockDocumentService;
        private readonly Mock<IRequestService> _mockRequestService;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<AppDbContext> _mockDbContext;
        private readonly ReportService _reportService;

        public ReportServiceTests()
        {
            _mockReportRepository = new Mock<IReportRepository>();
            _mockDocumentService = new Mock<IDocumentService>();
            _mockRequestService = new Mock<IRequestService>();
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockDbContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());

            _reportService = new ReportService(
                _mockDbContext.Object,
                _mockReportRepository.Object,
                _mockDocumentService.Object,
                _mockRequestService.Object,
                _mockAuthService.Object);
        }

        [Fact]
        public async Task GetAllScheduledReportsAsync_ShouldReturnAllReports()
        {
            // Arrange
            var expectedReports = new List<ScheduledReport>
            {
                new ScheduledReport { ScheduledReportId = 1, Name = "Report 1" },
                new ScheduledReport { ScheduledReportId = 2, Name = "Report 2" }
            };

            _mockReportRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(expectedReports);

            // Act
            var result = await _reportService.GetAllScheduledReportsAsync();

            // Assert
            Assert.Equal(expectedReports.Count, result.Count());
            Assert.Equal(expectedReports[0].Name, result.First().Name);
            Assert.Equal(expectedReports[1].Name, result.Last().Name);
            _mockReportRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetActiveScheduledReportsAsync_ShouldReturnActiveReports()
        {
            // Arrange
            var expectedReports = new List<ScheduledReport>
            {
                new ScheduledReport { ScheduledReportId = 1, Name = "Report 1", IsActive = true }
            };

            _mockReportRepository.Setup(r => r.GetActiveScheduledReportsAsync())
                .ReturnsAsync(expectedReports);

            // Act
            var result = await _reportService.GetActiveScheduledReportsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(expectedReports[0].Name, result.First().Name);
            _mockReportRepository.Verify(r => r.GetActiveScheduledReportsAsync(), Times.Once);
        }

        [Fact]
        public async Task AddScheduledReportAsync_ShouldAddReport()
        {
            // Arrange
            var report = new ScheduledReport
            {
                Name = "Test Report",
                ReportType = "Document",
                Schedule = "0 0 12 * * ?",
                ExportFormat = "PDF"
            };

            var currentUser = new User { UserId = 1, Username = "testuser" };
            _mockAuthService.Setup(a => a.CurrentUser).Returns(currentUser);

            _mockReportRepository.Setup(r => r.AddAsync(It.IsAny<ScheduledReport>()))
                .Returns(Task.CompletedTask);
            _mockReportRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _reportService.AddScheduledReportAsync(report);

            // Assert
            Assert.Equal("Test Report", result.Name);
            Assert.Equal(1, result.CreatedBy);
            _mockReportRepository.Verify(r => r.AddAsync(It.IsAny<ScheduledReport>()), Times.Once);
            _mockReportRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateDocumentReportAsync_ShouldReturnMemoryStream()
        {
            // Arrange
            var documents = new List<Document>
            {
                new Document
                {
                    DocumentId = 1,
                    DocumentType = "Birth Certificate",
                    CertificateNumber = "BC123",
                    GivenName = "John",
                    FamilyName = "Doe",
                    DateOfEvent = new DateTime(2000, 1, 1),
                    RegistrationDate = new DateTime(2000, 1, 15)
                }
            };

            _mockDocumentService.Setup(d => d.AdvancedSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>()))
                .ReturnsAsync(documents);

            // Act
            var result = await _reportService.GenerateDocumentReportAsync(
                documentType: "Birth Certificate",
                exportFormat: "PDF");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<MemoryStream>(result);
            Assert.True(result.Length > 0);
        }
    }
}
