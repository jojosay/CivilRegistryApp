using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivilRegistryApp.Data.Entities
{
    public class UserActivity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActivityType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string IpAddress { get; set; } = string.Empty;

        [MaxLength(200)]
        public string UserAgent { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Optional: Related entity information
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
