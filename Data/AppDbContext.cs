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

        public DbSet<User> Users => Set<User>();
        public DbSet<Deceased> Deceased => Set<Deceased>();
        public DbSet<DocumentRequest> DocumentRequests => Set<DocumentRequest>();
        public DbSet<FuneralHome> FuneralHomes => Set<FuneralHome>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Status> Statuses => Set<Status>();
        public DbSet<RequestType> RequestTypes => Set<RequestType>();
        public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
        public DbSet<DocumentRequirement> DocumentRequirements => Set<DocumentRequirement>();
        public DbSet<UploadedDocument> UploadedDocuments => Set<UploadedDocument>();
        public DbSet<VerificationRecord> VerificationRecords => Set<VerificationRecord>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Users ----------
            modelBuilder.Entity<User>()
                .HasIndex(u => u.EmailAddress)
                .IsUnique();

            // ---------- Deceased ----------
            modelBuilder.Entity<Deceased>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // ---------- DocumentRequests ----------
            modelBuilder.Entity<DocumentRequest>(e =>
            {
                e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Deceased).WithMany(d => d.Requests).HasForeignKey(r => r.DeceasedID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Type).WithMany().HasForeignKey(r => r.TypeID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Status).WithMany().HasForeignKey(r => r.StatusID)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(r => r.ReferenceNumber).IsUnique();
                e.HasIndex(r => new { r.UserID, r.StatusID });
                e.HasIndex(r => r.DateFiled);
            });

            // ---------- StatusHistories ----------
            modelBuilder.Entity<StatusHistory>(e =>
            {
                e.HasOne(h => h.Request).WithMany(r => r.Histories).HasForeignKey(h => h.RequestID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(h => h.Status).WithMany().HasForeignKey(h => h.StatusID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(h => h.ChangedBy).WithMany().HasForeignKey(h => h.ChangedByUserID)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------- Notifications ----------
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(n => n.NotificationID);

                entity.HasOne<User>().WithMany().HasForeignKey(n => n.UserID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<DocumentRequest>().WithMany().HasForeignKey(n => n.RequestID)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(n => new { n.UserID, n.IsRead });
            });

            // ---------- Requirements / uploads / verification ----------
            modelBuilder.Entity<DocumentRequirement>(e =>
            {
                e.HasOne(r => r.Type).WithMany(t => t.Requirements).HasForeignKey(r => r.TypeID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UploadedDocument>(e =>
            {
                e.HasOne(d => d.Request).WithMany(r => r.Documents).HasForeignKey(d => d.RequestID)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(d => d.Requirement).WithMany().HasForeignKey(d => d.RequirementID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(d => d.UploadedBy).WithMany().HasForeignKey(d => d.UploadedByUserID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(d => new { d.RequestID, d.RequirementID });
            });

            modelBuilder.Entity<VerificationRecord>(e =>
            {
                e.HasOne(v => v.Document).WithMany().HasForeignKey(v => v.DocumentID)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(v => v.VerifiedBy).WithMany().HasForeignKey(v => v.VerifiedByUserID)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(v => v.RequestID);
            });

            // ---------- Activity log ----------
            modelBuilder.Entity<ActivityLog>(e =>
            {
                e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserID)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(a => a.Request).WithMany().HasForeignKey(a => a.RequestID)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(a => a.CreatedAt);
                e.HasIndex(a => a.RequestID);
            });

            // ---------- Reference data ----------
            // Status IDs 1-3 existed before this change ("Pending", "Processing", "Released").
            // They are renamed rather than replaced so existing requests stay valid.
            modelBuilder.Entity<Status>().HasData(
                new Status { StatusID = StatusIds.Submitted,   StatusName = "Submitted" },
                new Status { StatusID = StatusIds.Processing,  StatusName = "Processing" },
                new Status { StatusID = StatusIds.Completed,   StatusName = "Completed" },
                new Status { StatusID = StatusIds.UnderReview, StatusName = "Under Review" },
                new Status { StatusID = StatusIds.Incomplete,  StatusName = "Incomplete Requirements" },
                new Status { StatusID = StatusIds.Approved,    StatusName = "Approved" },
                new Status { StatusID = StatusIds.Rejected,    StatusName = "Rejected" }
            );

            modelBuilder.Entity<RequestType>().HasData(
                new RequestType { TypeID = 1, TypeName = "Death Certificate", SortOrder = 1,
                    Description = "Official record of the death, needed for most other processes." },
                new RequestType { TypeID = 2, TypeName = "Burial Permit", SortOrder = 2,
                    Description = "Permission to proceed with burial, entombment or cremation." },
                new RequestType { TypeID = 3, TypeName = "Insurance Claims", SortOrder = 4,
                    Description = "Claim benefits from the deceased person's insurance policy." },
                new RequestType { TypeID = 4, TypeName = "Benefits and Assistance", SortOrder = 3,
                    Description = "Apply for funeral, death and other assistance benefits." }
            );

            modelBuilder.Entity<DocumentRequirement>().HasData(
                // Death Certificate
                Req(1, 1, "Medical certificate of death", "Issued by the attending physician or health officer.", true, 1),
                Req(2, 1, "Valid ID of the deceased", "A clear copy of any government-issued ID.", true, 2),
                Req(3, 1, "Valid ID of the family representative", "Front and back, if applicable.", true, 3),
                Req(4, 1, "Proof of relationship to the deceased", "For example a birth or marriage certificate.", true, 4),
                Req(5, 1, "Barangay certification", "Only if the death occurred at home or without a physician.", false, 5),
                // Burial Permit
                Req(6, 2, "Death certificate (copy)", "A copy of the registered death certificate.", true, 1),
                Req(7, 2, "Valid ID of the family representative", "A clear copy of a government-issued ID.", true, 2),
                Req(8, 2, "Burial or cremation arrangement", "Cemetery or crematorium confirmation, or funeral home contract.", true, 3),
                Req(9, 2, "Permit to transfer remains", "Only if the remains will be transported to another place.", false, 4),
                // Insurance Claims
                Req(10, 3, "Death certificate (copy)", "A copy of the registered death certificate.", true, 1),
                Req(11, 3, "Insurance policy document", "The policy or certificate of coverage.", true, 2),
                Req(12, 3, "Claimant's valid ID", "A clear copy of the beneficiary's government-issued ID.", true, 3),
                Req(13, 3, "Claim form", "The insurer's claim form, completed and signed.", true, 4),
                Req(14, 3, "Attending physician's statement", "Medical records related to the cause of death.", false, 5),
                // Benefits and Assistance
                Req(15, 4, "Death certificate (copy)", "A copy of the registered death certificate.", true, 1),
                Req(16, 4, "Valid ID of the family representative", "A clear copy of a government-issued ID.", true, 2),
                Req(17, 4, "Proof of membership or eligibility", "Membership ID or number of the deceased (e.g. SSS, GSIS, PhilHealth).", true, 3),
                Req(18, 4, "Funeral expense receipt or statement", "Receipt or billing statement from the funeral home.", false, 4),
                Req(19, 4, "Barangay certificate of indigency", "Only if applying for local government assistance.", false, 5)
            );
        }

        private static DocumentRequirement Req(int id, int typeId, string name, string description, bool required, int order) =>
            new DocumentRequirement
            {
                RequirementID = id, TypeID = typeId, Name = name, Description = description,
                IsRequired = required, SortOrder = order, IsActive = true
            };
    }
}
