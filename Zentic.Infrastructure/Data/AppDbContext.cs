using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Threading;

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
        public DbSet<NumberSequence> NumberSequences { get; set; }
        public DbSet<ApprovalDelegation> ApprovalDelegations { get; set; }
        public DbSet<EscalationRule> EscalationRules { get; set; }
        public DbSet<ApprovalWorkflow> ApprovalWorkflows { get; set; }
        public DbSet<ApprovalStep> ApprovalSteps { get; set; }
        
        // New entities
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<BudgetAllocation> BudgetAllocations { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
        public DbSet<GoodsReceiptItem> GoodsReceiptItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft delete global filter
            modelBuilder.Entity<AppUser>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ExpenseRequisition>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Vendor>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Document>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Payment>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<BudgetAllocation>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<GoodsReceipt>().HasQueryFilter(e => !e.IsDeleted);

            // Unique indexes
            modelBuilder.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<AppUser>().HasIndex(u => u.EmployeeId).IsUnique();
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.RequisitionNumber).IsUnique();
            modelBuilder.Entity<Vendor>().HasIndex(v => v.Code).IsUnique();
            modelBuilder.Entity<Payment>().HasIndex(p => p.PaymentNumber).IsUnique();
            modelBuilder.Entity<PurchaseOrder>().HasIndex(po => po.PONumber).IsUnique();
            modelBuilder.Entity<GoodsReceipt>().HasIndex(gr => gr.GRNumber).IsUnique();
            modelBuilder.Entity<BudgetAllocation>().HasIndex(b => b.BudgetCode).IsUnique();

            // Performance indexes - single column
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.Status);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.RequesterId);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.CreatedAt);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.IsFlaggedForReview);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.DepartmentId);
            modelBuilder.Entity<ExpenseRequisition>().HasIndex(r => r.VendorId);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.UserId);
            modelBuilder.Entity<AuditLog>().HasIndex(a => a.EntityName);
            modelBuilder.Entity<Notification>().HasIndex(n => n.RecipientId);
            modelBuilder.Entity<Notification>().HasIndex(n => n.IsRead);
            modelBuilder.Entity<Notification>().HasIndex(n => n.CreatedAt);
            modelBuilder.Entity<Payment>().HasIndex(p => p.Status);
            modelBuilder.Entity<Payment>().HasIndex(p => p.RequisitionId);
            modelBuilder.Entity<Document>().HasIndex(d => d.RequisitionId);
            modelBuilder.Entity<Document>().HasIndex(d => d.VendorId);
            modelBuilder.Entity<BudgetAllocation>().HasIndex(b => b.DepartmentId);
            modelBuilder.Entity<BudgetAllocation>().HasIndex(b => b.FiscalYear);

            // Composite indexes for complex queries
            modelBuilder.Entity<ExpenseRequisition>()
                .HasIndex(r => new { r.RequesterId, r.Status, r.CreatedAt });
            modelBuilder.Entity<ExpenseRequisition>()
                .HasIndex(r => new { r.DepartmentId, r.Status, r.Amount });
            modelBuilder.Entity<ExpenseRequisition>()
                .HasIndex(r => new { r.VendorId, r.Category });
            modelBuilder.Entity<ExpenseRequisition>()
                .HasIndex(r => new { r.Status, r.IsFlaggedForReview });
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });
            modelBuilder.Entity<Payment>()
                .HasIndex(p => new { p.Status, p.PaymentDate });
            modelBuilder.Entity<BudgetAllocation>()
                .HasIndex(b => new { b.DepartmentId, b.FiscalYear, b.IsActive });
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityName, a.EntityId, a.Timestamp });

            // Decimal precision — prevent silent truncation
            modelBuilder.Entity<ExpenseRequisition>().Property(r => r.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<ExpenseRequisition>().Property(r => r.BenchmarkPrice).HasPrecision(18, 2);
            modelBuilder.Entity<ExpenseRequisition>().Property(r => r.PriceDeviation).HasPrecision(18, 4);
            modelBuilder.Entity<PriceHistory>().Property(p => p.Price).HasPrecision(18, 2);
            modelBuilder.Entity<RequisitionQuotation>().Property(q => q.QuotedPrice).HasPrecision(18, 2);
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<BudgetAllocation>().Property(b => b.AllocatedAmount).HasPrecision(18, 2);
            modelBuilder.Entity<BudgetAllocation>().Property(b => b.SpentAmount).HasPrecision(18, 2);
            modelBuilder.Entity<BudgetAllocation>().Property(b => b.CommittedAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(po => po.SubTotal).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(po => po.TaxAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(po => po.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrder>().Property(po => po.TotalAmount).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrderItem>().Property(poi => poi.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseOrderItem>().Property(poi => poi.LineTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Vendor>().Property(v => v.AverageRating).HasPrecision(5, 2);
            modelBuilder.Entity<Vendor>().Property(v => v.TotalTransactionValue).HasPrecision(18, 2);

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

            // Vendor blacklisted by
            modelBuilder.Entity<Vendor>()
                .HasOne(v => v.BlacklistedBy)
                .WithMany()
                .HasForeignKey(v => v.BlacklistedById)
                .OnDelete(DeleteBehavior.SetNull);

            // AppUser Manager hierarchy
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Manager)
                .WithMany(m => m.Subordinates)
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Department hierarchy
            modelBuilder.Entity<Department>()
                .HasOne(d => d.ParentDepartment)
                .WithMany(p => p.SubDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Department Manager
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            // ExpenseRequisition - Manager Approver
            modelBuilder.Entity<ExpenseRequisition>()
                .HasOne(r => r.ManagerApprover)
                .WithMany()
                .HasForeignKey(r => r.ManagerApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExpenseRequisition - Finance Approver
            modelBuilder.Entity<ExpenseRequisition>()
                .HasOne(r => r.FinanceApprover)
                .WithMany()
                .HasForeignKey(r => r.FinanceApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExpenseRequisition - Budget Allocation
            modelBuilder.Entity<ExpenseRequisition>()
                .HasOne(r => r.BudgetAllocation)
                .WithMany(b => b.Requisitions)
                .HasForeignKey(r => r.BudgetAllocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Document - uploaded by
            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Document - Requisition
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Requisition)
                .WithMany(r => r.Documents)
                .HasForeignKey(d => d.RequisitionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Document - Vendor
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Vendor)
                .WithMany(v => v.Documents)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Document - Payment
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Payment)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Document versioning
            modelBuilder.Entity<Document>()
                .HasOne(d => d.PreviousVersion)
                .WithMany(pv => pv.NextVersions)
                .HasForeignKey(d => d.PreviousVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification - Recipient
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.SetNull);

            // Payment - Requisition
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Requisition)
                .WithMany(r => r.Payments)
                .HasForeignKey(p => p.RequisitionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment - Vendor
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment - Processed by
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ProcessedBy)
                .WithMany()
                .HasForeignKey(p => p.ProcessedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment - Reconciled by
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ReconciledBy)
                .WithMany()
                .HasForeignKey(p => p.ReconciledById)
                .OnDelete(DeleteBehavior.Restrict);

            // BudgetAllocation - Department
            modelBuilder.Entity<BudgetAllocation>()
                .HasOne(b => b.Department)
                .WithMany(d => d.BudgetAllocations)
                .HasForeignKey(b => b.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // BudgetAllocation - Approved by
            modelBuilder.Entity<BudgetAllocation>()
                .HasOne(b => b.ApprovedBy)
                .WithMany()
                .HasForeignKey(b => b.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            // PurchaseOrder - Requisition
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Requisition)
                .WithMany()
                .HasForeignKey(po => po.RequisitionId)
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseOrder - Vendor
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Vendor)
                .WithMany()
                .HasForeignKey(po => po.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseOrder - Created by
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.CreatedBy)
                .WithMany()
                .HasForeignKey(po => po.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseOrder - Approved by
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.ApprovedBy)
                .WithMany()
                .HasForeignKey(po => po.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseOrderItem - PurchaseOrder
            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // GoodsReceipt - PurchaseOrder
            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(gr => gr.PurchaseOrder)
                .WithMany(po => po.GoodsReceipts)
                .HasForeignKey(gr => gr.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // GoodsReceipt - Received by
            modelBuilder.Entity<GoodsReceipt>()
                .HasOne(gr => gr.ReceivedBy)
                .WithMany()
                .HasForeignKey(gr => gr.ReceivedById)
                .OnDelete(DeleteBehavior.Restrict);

            // GoodsReceiptItem - GoodsReceipt
            modelBuilder.Entity<GoodsReceiptItem>()
                .HasOne(gri => gri.GoodsReceipt)
                .WithMany(gr => gr.Items)
                .HasForeignKey(gri => gri.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            // GoodsReceiptItem - PurchaseOrderItem
            modelBuilder.Entity<GoodsReceiptItem>()
                .HasOne(gri => gri.PurchaseOrderItem)
                .WithMany()
                .HasForeignKey(gri => gri.PurchaseOrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // NumberSequence configuration
            modelBuilder.Entity<NumberSequence>(entity =>
            {
                entity.HasIndex(e => new { e.SequenceName, e.YearMonth }).IsUnique();
                entity.Property(e => e.SequenceName).HasMaxLength(100);
                entity.Property(e => e.Prefix).HasMaxLength(20);
                entity.Property(e => e.YearMonth).HasMaxLength(6);
            });

            // Approval Workflow entities
            modelBuilder.Entity<ApprovalDelegation>(entity =>
            {
                entity.HasIndex(e => new { e.DelegatorId, e.DelegateId, e.StartDate }).IsUnique();
                entity.HasOne(e => e.Delegator)
                    .WithMany()
                    .HasForeignKey(e => e.DelegatorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Delegate)
                    .WithMany()
                    .HasForeignKey(e => e.DelegateId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Department)
                    .WithMany()
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<EscalationRule>(entity =>
            {
                entity.HasOne(e => e.EscalateToUser)
                    .WithMany()
                    .HasForeignKey(e => e.EscalateToUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Department)
                    .WithMany()
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ApprovalWorkflow>(entity =>
            {
                entity.HasOne(e => e.Department)
                    .WithMany()
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasMany(e => e.Steps)
                    .WithOne(s => s.Workflow)
                    .HasForeignKey(s => s.WorkflowId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ApprovalStep>(entity =>
            {
                entity.HasOne(e => e.Workflow)
                    .WithMany(w => w.Steps)
                    .HasForeignKey(e => e.WorkflowId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.SpecificUser)
                    .WithMany()
                    .HasForeignKey(e => e.SpecificUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Department)
                    .WithMany()
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.EscalationRule)
                    .WithMany()
                    .HasForeignKey(e => e.EscalationRuleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

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

            modelBuilder.Entity<NumberSequence>().HasData(
                new NumberSequence { Id = 1, SequenceName = "ExpenseRequisition", Prefix = "EXP", CurrentValue = 0, IncrementBy = 1, MinValue = 1, MaxValue = 9999, Cycle = false, CreatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc), UpdatedAt = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc) }
            );
        }

        // IUnitOfWork implementation
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
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
