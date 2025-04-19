using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivilRegistryApp.Data.Entities
{
    public class DocumentRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(200)]
        public string RequestorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string RequestorAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string RequestorContact { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Purpose { get; set; } = string.Empty;

        public int RelatedDocumentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        public int? HandledBy { get; set; }

        // Navigation properties
        [ForeignKey("RelatedDocumentId")]
        public virtual Document RelatedDocument { get; set; } = null!;

        [ForeignKey("HandledBy")]
        public virtual User? HandledByUser { get; set; }
    }
}
