using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class DashboardServiceTests
    {
        private static (AppDbContext db, DashboardService service) CreateService()
        {
            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            var service = new DashboardService(db);
            return (db, service);
        }

        private static async Task SeedDataAsync(AppDbContext db)
        {
            var requester = new AppUser
            {
                Id = 1,
                FullName = "Test User",
                Email = "test@test.com",
                EmployeeId = "EMP001",
                Role = UserRole.Staff,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Users.Add(requester);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            db.ExpenseRequisitions.AddRange(
                new ExpenseRequisition
                {
                    Id = 1, RequisitionNumber = "EXP-001", Title = "Draft Req",
                    Amount = 1000000, Category = ExpenseCategory.OfficeSupplies,
                    Status = RequisitionStatus.Draft, RequesterId = 1,
                    CreatedAt = monthStart.AddDays(1), UpdatedAt = now
                },
                new ExpenseRequisition
                {
                    Id = 2, RequisitionNumber = "EXP-002", Title = "Submitted Req",
                    Amount = 2000000, Category = ExpenseCategory.Travel,
                    Status = RequisitionStatus.Submitted, RequesterId = 1,
                    CreatedAt = monthStart.AddDays(2), UpdatedAt = now
                },
                new ExpenseRequisition
                {
                    Id = 3, RequisitionNumber = "EXP-003", Title = "Approved Req",
                    Amount = 3000000, Category = ExpenseCategory.OfficeSupplies,
                    Status = RequisitionStatus.Approved, RequesterId = 1,
                    CreatedAt = monthStart.AddDays(3), UpdatedAt = now
                },
                new ExpenseRequisition
                {
                    Id = 4, RequisitionNumber = "EXP-004", Title = "Rejected Req",
                    Amount = 4000000, Category = ExpenseCategory.Utilities,
                    Status = RequisitionStatus.Rejected, RequesterId = 1,
                    CreatedAt = monthStart.AddDays(4), UpdatedAt = now
                },
                new ExpenseRequisition
                {
                    Id = 5, RequisitionNumber = "EXP-005", Title = "Flagged Req",
                    Amount = 5000000, Category = ExpenseCategory.Travel,
                    Status = RequisitionStatus.Submitted, RequesterId = 1,
                    IsFlaggedForReview = true,
                    CreatedAt = monthStart.AddDays(5), UpdatedAt = now
                }
            );
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task GetDashboardAsync_StaffRole_ReturnsOwnRequisitions()
        {
            var (db, service) = CreateService();
            await SeedDataAsync(db);

            var result = await service.GetDashboardAsync(1, "Staff");

            Assert.Equal(5, result.TotalRequisitions);
            Assert.Equal(2, result.PendingApproval); // Submitted + UnderReview
            Assert.Equal(1, result.ApprovedThisMonth);
            Assert.Equal(1, result.RejectedThisMonth);
            Assert.Equal(1, result.FlaggedForReview);
        }

        [Fact]
        public async Task GetDashboardAsync_AdminRole_ReturnsAllRequisitions()
        {
            var (db, service) = CreateService();
            await SeedDataAsync(db);

            var result = await service.GetDashboardAsync(1, "Admin");

            Assert.Equal(5, result.TotalRequisitions);
        }

        [Fact]
        public async Task GetDashboardAsync_EmptyDatabase_ReturnsZeros()
        {
            var (db, service) = CreateService();

            var result = await service.GetDashboardAsync(1, "Staff");

            Assert.Equal(0, result.TotalRequisitions);
            Assert.Equal(0, result.PendingApproval);
            Assert.Equal(0, result.ApprovedThisMonth);
            Assert.Equal(0, result.RejectedThisMonth);
            Assert.Equal(0, result.FlaggedForReview);
            Assert.Equal(0m, result.TotalAmountThisMonth);
            Assert.Equal(0m, result.TotalAmountApproved);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsCategoryBreakdown()
        {
            var (db, service) = CreateService();
            await SeedDataAsync(db);

            var result = await service.GetDashboardAsync(1, "Staff");

            Assert.NotEmpty(result.CategoryBreakdown);
        }

        [Fact]
        public async Task GetDashboardAsync_ReturnsRecentRequisitions()
        {
            var (db, service) = CreateService();
            await SeedDataAsync(db);

            var result = await service.GetDashboardAsync(1, "Staff");

            Assert.NotEmpty(result.RecentRequisitions);
            Assert.True(result.RecentRequisitions.Count <= 8);
        }

        [Fact]
        public async Task GetDashboardAsync_CalculatesTotalAmounts()
        {
            var (db, service) = CreateService();
            await SeedDataAsync(db);

            var result = await service.GetDashboardAsync(1, "Staff");

            Assert.True(result.TotalAmountThisMonth > 0);
            Assert.True(result.TotalAmountApproved > 0);
        }
    }
}
