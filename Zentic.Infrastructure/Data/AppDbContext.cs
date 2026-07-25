using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Data
{
    public class AppDbContext : DbContext, IUnitOfWork
    {
        private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _currentTransaction;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<ExpenseRequisition> ExpenseRequisitions { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<RequisitionQuotation> RequisitionQuotations { get; set; }
        public DbSet<PriceHistory> PriceHistories { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RequisitionApprovalHistory> RequisitionApprovalHistories { get; set; }
        public DbSet<ConflictOfInterestDeclaration> ConflictOfInterestDeclarations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft delete global filter
            modelBuilder.Entity<AppUser>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ExpenseRequisition>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Vendor>().HasQueryFilter(e => !e.IsDeleted);

            // Unique indexes
            modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<AppUser>().HasIndex(u => u.EmployeeId).IsUnique();
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.RequisitionNumber).IsUnique();
            modelBuilder.Entity<Vendor>().HasIndex(v => v.Code).IsUnique();

            // Performance indexes
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.Status);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.RequesterId);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.CreatedAt);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.IsFlaggedForReview);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.UserId);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.EntityName);

            // Decimal precision — prevent silent truncation
            modelBuilder.Entity<ExpenseRequisition>().Property(r => r.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<ExpenseRequisition>().Property(r => r.BenchmarkPrice).HasPrecision(18, 2);
            modelBuilder.Entity<ExpenseRequisition>().Property(r => r.PriceDeviation).HasPrecision(18, 4);
            modelBuilder.Entity<PriceHistory>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<RequisitionQuotation>().Property(q => q.QuotedPrice).HasPrecision(18, 2);

            // AuditLog: no delete behavior — append only
            // Navigation made optional so global query filter on AppUser doesn't cause unexpected results
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // ConflictOfInterestDeclaration — optional navigation to avoid filter issues
            modelBuilder.Entity<ConflictOfInterestDeclaration>()
                .HasOne(c => c.Declarant)
                .WithMany()
                .HasForeignKey(c => c.DeclarantId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascade cycle on requisition
            modelBuilder.Entity<ExpenseRequisition>()
                .HasOne(r => r.Approver)
                .WithMany(u => u.ApprovedRequisitions)
                .HasForeignKey(r => r.ApproverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ExpenseRequisition>()
                .HasOne(r => r.Requester)
                .WithMany(u => u.Requisitions)
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Approval history: no cascade — optional navigation to avoid filter issues
            modelBuilder.Entity<RequisitionApprovalHistory>()
                .HasOne(h => h.Actor)
                .WithMany()
                .HasForeignKey(h => h.ActorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // PriceHistory — optional navigation to avoid filter issues
            modelBuilder.Entity<PriceHistory>()
                .HasOne(p => p.Requisition)
                .WithMany()
                .HasForeignKey(p => p.RequisitionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // RequisitionQuotation — optional navigation to avoid filter issues
            modelBuilder.Entity<RequisitionQuotation>()
                .HasOne(q => q.Requisition)
                .WithMany(r => r.Quotations)
                .HasForeignKey(q => q.RequisitionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Vendor verified by
            modelBuilder.Entity<Vendor>()
                .HasOne(v => v.VerifiedBy)
                .WithMany()
                .HasForeignKey(v => v.VerifiedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Finance", Code = "FIN", Description = "Finance Division", IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new Department { Id = 2, Name = "Operations", Code = "OPS", Description = "Operations Division", IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new Department { Id = 3, Name = "Information Technology", Code = "IT", Description = "IT Division", IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new Department { Id = 4, Name = "Human Resources", Code = "HR", Description = "HR Division", IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new Department { Id = 5, Name = "Executive", Code = "EXEC", Description = "Executive Division", IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) }
            );

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { Id = 1, FullName = "System Administrator", Email = "admin@company.internal", PasswordHash = "$2a$11$1RsGLs1ItvVblm1Kr2uq3eNwPy5fKkq8KsYvkMe5j/eTpN8tKh0Ai", EmployeeId = "EMP-0001", Position = "System Administrator", Role = UserRole.Admin, DepartmentId = 5, IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new AppUser { Id = 2, FullName = "Finance Manager", Email = "finance.manager@company.internal", PasswordHash = "$2a$11$lz5VPY.0m.UeNETgcj9ko.ndPD5w7u86W0c4t4lA8amAIBfeuY/0K", EmployeeId = "EMP-0002", Position = "Finance Manager", Role = UserRole.Finance, DepartmentId = 1, IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new AppUser { Id = 3, FullName = "Budi Santoso", Email = "budi.santoso@company.internal", PasswordHash = "$2a$11$VoYS0tgdotbTbplhWQ8u/u8IsNh.XbNo2A3CSjiB8bBw2ti2P.5Na", EmployeeId = "EMP-0003", Position = "Staff Operations", Role = UserRole.Staff, DepartmentId = 2, IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new AppUser { Id = 4, FullName = "Siti Rahayu", Email = "siti.rahayu@company.internal", PasswordHash = "$2a$11$gx1LDbpdRLS1v1yfvRwMV.Rl2SivcUh5srFo6kvzuMEXkVANch4my", EmployeeId = "EMP-0004", Position = "Operations Manager", Role = UserRole.Manager, DepartmentId = 2, IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new AppUser { Id = 5, FullName = "Internal Auditor", Email = "auditor@company.internal", PasswordHash = "$2a$11$bnmLbzVyXmyRXRMBVL08LOGnjIf3inWVdeHKSsNQFzaofXsogzTNy", EmployeeId = "EMP-0005", Position = "Internal Auditor", Role = UserRole.Auditor, DepartmentId = 1, IsActive = true, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) }
            );

            modelBuilder.Entity<Vendor>().HasData(
                new Vendor { Id = 1, Name = "PT Sumber Makmur", Code = "VND-001", Address = "Jakarta Selatan", PhoneNumber = "021-5551234", Email = "info@sumbermakmur.co.id", TaxId = "01.234.567.8-012.000", Status = VendorStatus.Active, BankName = "BCA", BankAccountHolderName = "PT Sumber Makmur", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) },
                new Vendor { Id = 2, Name = "CV Teknologi Maju", Code = "VND-002", Address = "Bandung", PhoneNumber = "022-5559876", Email = "cv.teknologimaju@gmail.com", TaxId = "02.345.678.9-023.000", Status = VendorStatus.Active, BankName = "Mandiri", BankAccountHolderName = "CV Teknologi Maju", CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) }
            );
        }

        // IUnitOfWork implementation
        public async Task<int> SaveChangesAsync()
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _currentTransaction = await Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Modified);
            foreach (var entry in entries)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
