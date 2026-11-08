using Microsoft.EntityFrameworkCore;
using KABILIN.Models;

namespace KABILIN.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Database Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Deceased> Deceased { get; set; }
        public DbSet<DocumentRequest> DocumentRequests { get; set; }
        public DbSet<FuneralHome> FuneralHomes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<RequestType> RequestTypes { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users -> Deceased
            modelBuilder.Entity<Deceased>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Users -> DocumentRequests
            modelBuilder.Entity<DocumentRequest>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // Deceased -> DocumentRequests
            modelBuilder.Entity<DocumentRequest>()
                .HasOne<Deceased>()
                .WithMany()
                .HasForeignKey(r => r.DeceasedID)
                .OnDelete(DeleteBehavior.Restrict);

            // RequestTypes -> DocumentRequests
            modelBuilder.Entity<DocumentRequest>()
                .HasOne<RequestType>()
                .WithMany()
                .HasForeignKey(r => r.TypeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Status -> DocumentRequests
            modelBuilder.Entity<DocumentRequest>()
                .HasOne<Status>()
                .WithMany()
                .HasForeignKey(r => r.StatusID)
                .OnDelete(DeleteBehavior.Restrict);

            // DocumentRequests -> StatusHistory
            modelBuilder.Entity<StatusHistory>()
                .HasOne<DocumentRequest>()
                .WithMany()
                .HasForeignKey(h => h.RequestID)
                .OnDelete(DeleteBehavior.Restrict);

            // Status -> StatusHistory
            modelBuilder.Entity<StatusHistory>()
                .HasOne<Status>()
                .WithMany()
                .HasForeignKey(h => h.StatusID)
                .OnDelete(DeleteBehavior.Restrict);

            // Initial Status Records
            modelBuilder.Entity<Status>().HasData(
                new Status
                {
                    StatusID = 1,
                    StatusName = "Pending"
                },
                new Status
                {
                    StatusID = 2,
                    StatusName = "Processing"
                },
                new Status
                {
                    StatusID = 3,
                    StatusName = "Released"
                }
            );

            // Initial Request Types
            modelBuilder.Entity<RequestType>().HasData(
                new RequestType
                {
                    TypeID = 1,
                    TypeName = "Death Certificate"
                },
                new RequestType
                {
                    TypeID = 2,
                    TypeName = "Burial Permit"
                },
                new RequestType
                {
                    TypeID = 3,
                    TypeName = "Insurance Claim"
                }

                
            );

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");

                entity.HasKey(n => n.NotificationID);

                // Notification belongs to an existing user
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(n => n.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Notification may belong to a document request
                entity.HasOne<DocumentRequest>()
                    .WithMany()
                    .HasForeignKey(n => n.RequestID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

        }
    }
}