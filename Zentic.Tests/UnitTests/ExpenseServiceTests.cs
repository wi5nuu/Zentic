using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Repositories;
using Zentic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class ExpenseServiceTests
    {
        private static (AppDbContext db, ExpenseService service) CreateService(decimal threshold = 50_000_000m)
        {
            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppSettings:MultipleQuotationThreshold"] = threshold.ToString()
                })
                .Build();

            var fraudOptions = Options.Create(new FraudDetectionOptions());
            
            var service = new ExpenseService(
                new Repository<ExpenseRequisition>(db),
                new Repository<AppUser>(db),
                new Repository<Department>(db),
                new FraudDetectionService(db, fraudOptions),
                new AuditService(db),
                db,
                db,
                config);

            return (db, service);
        }

        private static async Task SeedTestUsersAsync(AppDbContext db)
        {
            // Create department
            db.Departments.Add(new Department { Id = 1, Name = "IT", Code = "IT", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            
            // Create requester (Staff)
            var requester = new AppUser 
            { 
                Id = 1, 
                FullName = "Requester", 
                Email = "requester@test.com", 
                EmployeeId = "EMP-0001",
                Role = UserRole.Staff,
                DepartmentId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(requester);
            
            // Create Finance approver
            var financeApprover = new AppUser 
            { 
                Id = 99, 
                FullName = "Finance Approver", 
                Email = "finance@test.com", 
                EmployeeId = "EMP-0099",
                Role = UserRole.Finance,
                DepartmentId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(financeApprover);
            
            // Create Manager approver
            var managerApprover = new AppUser 
            { 
                Id = 100, 
                FullName = "Manager Approver", 
                Email = "manager@test.com", 
                EmployeeId = "EMP-0100",
                Role = UserRole.Manager,
                DepartmentId = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(managerApprover);
            
            await db.SaveChangesAsync();
        }

        private static async Task<ExpenseRequisition> CreateSubmittedRequisitionAsync(
            AppDbContext db, decimal amount, int requesterId = 1, int? vendorId = 1, bool flagged = false)
        {
            if (!db.Users.Any(u => u.Id == requesterId))
            {
                var requester = new AppUser { Id = requesterId, FullName = "Requester", Email = $"u{requesterId}@test.com", EmployeeId = $"EMP-{requesterId:D4}", Role = UserRole.Staff, DepartmentId = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                db.Users.Add(requester);
            }
            if (vendorId.HasValue)
                db.Vendors.Add(new Vendor { Id = vendorId.Value, Name = $"Vendor {vendorId.Value}", Code = $"VND-{vendorId.Value:D3}" });
            var req = new ExpenseRequisition
            {
                RequesterId = requesterId,
                VendorId = vendorId,
                DepartmentId = 1,
                Title = "Office Equipment",
                Description = "Purchase of office equipment for the department.",
                RequisitionNumber = $"EXP-TEST-{Guid.NewGuid():N}",
                Amount = amount,
                Status = RequisitionStatus.Submitted,
                IsFlaggedForReview = flagged,
                Category = ExpenseCategory.ITEquipment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.ExpenseRequisitions.Add(req);
            await db.SaveChangesAsync();
            return req;
        }

        [Fact]
        public async Task ApproveAsync_AboveThreshold_WithLessThanTwoQuotations_Throws()
        {
            var (db, service) = CreateService();
            await SeedTestUsersAsync(db);
            var req = await CreateSubmittedRequisitionAsync(db, 60_000_000m);
            db.RequisitionQuotations.Add(new RequisitionQuotation
            {
                RequisitionId = req.Id,
                VendorId = 1,
                QuotedPrice = 59_000_000m,
                QuotationDate = DateTime.Today
            });
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(req.Id, 99, null));
        }

        [Fact]
        public async Task ApproveAsync_AboveThreshold_WithTwoQuotations_Succeeds()
        {
            var (db, service) = CreateService();
            await SeedTestUsersAsync(db);
            var req = await CreateSubmittedRequisitionAsync(db, 60_000_000m);
            db.RequisitionQuotations.Add(new RequisitionQuotation
            {
                RequisitionId = req.Id,
                VendorId = 1,
                QuotedPrice = 59_000_000m,
                QuotationDate = DateTime.Today
            });
            db.Vendors.Add(new Vendor { Id = 2, Name = "Vendor 2", Code = "VND-002" });
            db.RequisitionQuotations.Add(new RequisitionQuotation
            {
                RequisitionId = req.Id,
                VendorId = 2,
                QuotedPrice = 60_500_000m,
                QuotationDate = DateTime.Today
            });
            await db.SaveChangesAsync();

            await service.ApproveAsync(req.Id, 99, "approved");

            var approved = await db.ExpenseRequisitions.FindAsync(req.Id);
            Assert.Equal(RequisitionStatus.Approved, approved!.Status);
            Assert.NotNull(approved.ApprovedAt);
        }

        [Fact]
        public async Task ApproveAsync_RecordsPriceHistory()
        {
            var (db, service) = CreateService();
            await SeedTestUsersAsync(db);
            var req = await CreateSubmittedRequisitionAsync(db, 5_000_000m);

            await service.ApproveAsync(req.Id, 99, null);

            var history = await db.PriceHistories.FirstOrDefaultAsync(p => p.RequisitionId == req.Id);
            Assert.NotNull(history);
            Assert.Equal(req.Amount, history!.Price);
            Assert.Equal(1, history.VendorId);
            Assert.Equal(req.Title, history.ItemDescription);
        }

        [Fact]
        public async Task SubmitAsync_FlaggedRequisition_GoesToUnderReview()
        {
            var (db, service) = CreateService();
            await SeedTestUsersAsync(db);
            var req = await CreateSubmittedRequisitionAsync(db, 5_000_000m, flagged: true);
            req.Status = RequisitionStatus.Draft;
            await db.SaveChangesAsync();

            await service.SubmitAsync(req.Id, 1);

            var updated = await db.ExpenseRequisitions.FindAsync(req.Id);
            Assert.Equal(RequisitionStatus.UnderReview, updated!.Status);
        }

        [Fact]
        public async Task MarkAsPaidAsync_ApprovedRequisition_SetsPaid()
        {
            var (db, service) = CreateService();
            await SeedTestUsersAsync(db);
            var req = await CreateSubmittedRequisitionAsync(db, 5_000_000m);
            req.Status = RequisitionStatus.Approved;
            await db.SaveChangesAsync();

            await service.MarkAsPaidAsync(req.Id, 99);

            var paid = await db.ExpenseRequisitions.FindAsync(req.Id);
            Assert.Equal(RequisitionStatus.Paid, paid!.Status);
            Assert.NotNull(paid.PaidAt);
        }

        [Fact]
        public async Task MarkAsPaidAsync_NotApproved_Throws()
        {
            var (db, service) = CreateService();
            var req = await CreateSubmittedRequisitionAsync(db, 5_000_000m);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkAsPaidAsync(req.Id, 99));
        }
    }
}
