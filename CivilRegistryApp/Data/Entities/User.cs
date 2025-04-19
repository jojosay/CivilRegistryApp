using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivilRegistryApp.Data.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Staff";

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Position { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Department { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ProfilePicturePath { get; set; } = string.Empty;

        // Permissions (can be customized based on role)
        public bool CanAddDocuments { get; set; } = true;
        public bool CanEditDocuments { get; set; } = true;
        public bool CanDeleteDocuments { get; set; } = false;
        public bool CanViewRequests { get; set; } = true;
        public bool CanProcessRequests { get; set; } = true;
        public bool CanManageUsers { get; set; } = false;

        // Account status
        public bool IsActive { get; set; } = true;

        // Timestamps
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastPasswordChangeAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
        public virtual ICollection<DocumentRequest> HandledRequests { get; set; } = new List<DocumentRequest>();
        public virtual ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();

        [NotMapped]
        public string RoleDisplay
        {
            get
            {
                return Role switch
                {
                    "Admin" => "Administrator",
                    "Staff" => "Staff Member",
                    "Clerk" => "Clerk",
                    _ => Role
                };
            }
        }

        // Helper method to set permissions based on role
        public void SetPermissionsByRole()
        {
            switch (Role)
            {
                case "Admin":
                    CanAddDocuments = true;
                    CanEditDocuments = true;
                    CanDeleteDocuments = true;
                    CanViewRequests = true;
                    CanProcessRequests = true;
                    CanManageUsers = true;
                    break;

                case "Staff":
                    CanAddDocuments = true;
                    CanEditDocuments = true;
                    CanDeleteDocuments = false;
                    CanViewRequests = true;
                    CanProcessRequests = true;
                    CanManageUsers = false;
                    break;

                case "Clerk":
                    CanAddDocuments = true;
                    CanEditDocuments = false;
                    CanDeleteDocuments = false;
                    CanViewRequests = true;
                    CanProcessRequests = false;
                    CanManageUsers = false;
                    break;

                default:
                    // Default permissions for unknown roles
                    CanAddDocuments = false;
                    CanEditDocuments = false;
                    CanDeleteDocuments = false;
                    CanViewRequests = true;
                    CanProcessRequests = false;
                    CanManageUsers = false;
                    break;
            }
        }
    }
}
