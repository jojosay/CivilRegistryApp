using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivilRegistryApp.Data.Entities
{
    public class ScheduledReport
    {
        [Key]
        public int ScheduledReportId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string RegistryOffice { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CityMunicipality { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Barangay { get; set; } = string.Empty;

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        [Required]
        [MaxLength(50)]
        public string Schedule { get; set; } = string.Empty; // Cron expression

        [Required]
        [MaxLength(50)]
        public string ExportFormat { get; set; } = string.Empty; // PDF, Excel, etc.

        [MaxLength(500)]
        public string OutputPath { get; set; } = string.Empty;

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastRunAt { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string EmailRecipients { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public virtual User CreatedByUser { get; set; } = null!;
    }
}
