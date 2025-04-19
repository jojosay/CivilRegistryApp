using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivilRegistryApp.Data.Entities
{
    public class Document
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string RegistryOffice { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CertificateNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfEvent { get; set; }

        [Required]
        [MaxLength(100)]
        public string Barangay { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CityMunicipality { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;

        [Required]
        public DateTime RegistrationDate { get; set; }

        [MaxLength(20)]
        public string Prefix { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string GivenName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MiddleName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FamilyName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Suffix { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FatherGivenName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FatherMiddleName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FatherFamilyName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MotherMaidenGiven { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MotherMaidenMiddleName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MotherMaidenFamily { get; set; } = string.Empty;

        [MaxLength(50)]
        public string RegistryBook { get; set; } = string.Empty;

        [MaxLength(20)]
        public string VolumeNumber { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PageNumber { get; set; } = string.Empty;

        [MaxLength(20)]
        public string LineNumber { get; set; } = string.Empty;

        [MaxLength(1)]
        public string Gender { get; set; } = string.Empty;

        [MaxLength(200)]
        public string AttendantName { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string BackFilePath { get; set; } = string.Empty;

        [Required]
        public int UploadedBy { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("UploadedBy")]
        public virtual User UploadedByUser { get; set; } = null!;

        public virtual ICollection<DocumentRequest> Requests { get; set; } = new List<DocumentRequest>();
    }
}
