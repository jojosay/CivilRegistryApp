using Microsoft.EntityFrameworkCore;
using CivilRegistryApp.Data.Entities;

namespace CivilRegistryApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentRequest> DocumentRequests { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<ScheduledReport> ScheduledReports { get; set; }
        public DbSet<FieldConfiguration> FieldConfigurations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Configure Document entity
            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedByUser)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DocumentRequest entity
            modelBuilder.Entity<DocumentRequest>()
                .HasOne(r => r.RelatedDocument)
                .WithMany(d => d.Requests)
                .HasForeignKey(r => r.RelatedDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentRequest>()
                .HasOne(r => r.HandledByUser)
                .WithMany(u => u.HandledRequests)
                .HasForeignKey(r => r.HandledBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure UserActivity entity
            modelBuilder.Entity<UserActivity>()
                .HasOne(a => a.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ScheduledReport entity
            modelBuilder.Entity<ScheduledReport>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure FieldConfiguration entity
            modelBuilder.Entity<FieldConfiguration>()
                .HasIndex(f => new { f.DocumentType, f.FieldName })
                .IsUnique();
        }
    }
}
