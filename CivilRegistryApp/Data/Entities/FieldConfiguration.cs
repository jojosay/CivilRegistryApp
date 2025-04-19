using System;
using System.ComponentModel.DataAnnotations;

namespace CivilRegistryApp.Data.Entities
{
    /// <summary>
    /// Represents a configuration for document field requirements
    /// </summary>
    public class FieldConfiguration
    {
        [Key]
        public int FieldConfigurationId { get; set; }

        /// <summary>
        /// The document type this configuration applies to (e.g., "Birth Certificate", "Marriage Certificate")
        /// </summary>
        [Required]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// The name of the field (e.g., "GivenName", "MiddleName", "FamilyName")
        /// </summary>
        [Required]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the field is required for this document type
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Display name for the field (for UI purposes)
        /// </summary>
        [Required]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description or help text for the field
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The order in which the field should be displayed in forms
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// When this configuration was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// ID of the user who created this configuration
        /// </summary>
        public int CreatedBy { get; set; }

        /// <summary>
        /// When this configuration was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID of the user who last updated this configuration
        /// </summary>
        public int? UpdatedBy { get; set; }
    }
}
