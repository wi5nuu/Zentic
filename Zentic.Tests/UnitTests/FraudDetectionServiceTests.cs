using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class FraudDetectionServiceTests
    {
        private static AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static FraudDetectionService CreateService(AppDbContext db)
        {
            var fraudOptions = Options.Create(new FraudDetectionOptions());
            return new FraudDetectionService(db, fraudOptions);
        }

        [Fact]
        public async Task CheckPriceAsync_InsufficientHistory_ReturnsNotFlagged()
        {
            using var db = CreateDbContext();
            db.Vendors.Add(new Vendor { Id = 1, Name = "Test Vendor", Code = "TST-001" });
            db.PriceHistories.Add(new PriceHistory
            {
                VendorId = 1,
                ItemDescription = "Office Chair",
                Price = 500_000,
                PurchaseDate = DateTime.UtcNow.AddDays(-10),
                RequisitionId = 1
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.CheckPriceAsync(1, 1_000_000, "Office Chair");

            Assert.False(result.IsFlagged);
            Assert.Contains("Insufficient price history", result.Message);
        }

        [Fact]
        public async Task CheckPriceAsync_ExceedsThreshold_ReturnsFlagged()
        {
            using var db = CreateDbContext();
            db.Vendors.Add(new Vendor { Id = 1, Name = "Test Vendor", Code = "TST-001" });
            for (int i = 0; i < 5; i++)
            {
                db.PriceHistories.Add(new PriceHistory
                {
                    VendorId = 1,
                    ItemDescription = "Office Chair",
                    Price = 500_000,
                    PurchaseDate = DateTime.UtcNow.AddDays(-(i + 1)),
                    RequisitionId = i + 1
                });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.CheckPriceAsync(1, 700_000, "Office Chair");

            Assert.True(result.IsFlagged);
            Assert.NotNull(result.BenchmarkPrice);
            Assert.NotNull(result.DeviationPercent);
            Assert.True(result.DeviationPercent > 0);
        }

        [Fact]
        public async Task CheckPriceAsync_WithinThreshold_ReturnsNotFlagged()
        {
            using var db = CreateDbContext();
            db.Vendors.Add(new Vendor { Id = 1, Name = "Test Vendor", Code = "TST-001" });
            for (int i = 0; i < 5; i++)
            {
                db.PriceHistories.Add(new PriceHistory
                {
                    VendorId = 1,
                    ItemDescription = "Office Chair",
                    Price = 500_000,
                    PurchaseDate = DateTime.UtcNow.AddDays(-(i + 1)),
                    RequisitionId = i + 1
                });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.CheckPriceAsync(1, 550_000, "Office Chair");

            Assert.False(result.IsFlagged);
            Assert.NotNull(result.BenchmarkPrice);
        }

        [Fact]
        public async Task IsStructuringPatternAsync_AmountBelowThreshold_ReturnsFalse()
        {
            using var db = CreateDbContext();
            db.Vendors.Add(new Vendor { Id = 1, Name = "Test Vendor", Code = "TST-001" });

            var requester = new AppUser { Id = 1, FullName = "Test", Email = "test@test.com", EmployeeId = "EMP-TST" };
            db.Users.Add(requester);

            for (int i = 0; i < 6; i++)
            {
                db.ExpenseRequisitions.Add(new ExpenseRequisition
                {
                    RequesterId = 1,
                    VendorId = 1,
                    Amount = 1_000_000,
                    Title = $"Requisition {i}",
                    Description = "Test requisition",
                    RequisitionNumber = $"REQ-TST-{i:D4}",
                    Status = RequisitionStatus.Draft,
                    DepartmentId = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-(i * 2))
                });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.IsStructuringPatternAsync(1, 1, 50_000_000);

            Assert.False(result);
        }

        [Fact]
        public async Task IsStructuringPatternAsync_NearThresholdWithManyRequisitions_ReturnsTrue()
        {
            using var db = CreateDbContext();
            db.Vendors.Add(new Vendor { Id = 1, Name = "Test Vendor", Code = "TST-001" });

            var requester = new AppUser { Id = 1, FullName = "Test", Email = "test@test.com", EmployeeId = "EMP-TST" };
            db.Users.Add(requester);

            for (int i = 0; i < 6; i++)
            {
                db.ExpenseRequisitions.Add(new ExpenseRequisition
                {
                    RequesterId = 1,
                    VendorId = 1,
                    Amount = 80_000_000,
                    Title = $"Requisition {i}",
                    Description = "High-value requisition",
                    RequisitionNumber = $"REQ-HV-{i:D4}",
                    Status = RequisitionStatus.Draft,
                    DepartmentId = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-(i * 3))
                });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.IsStructuringPatternAsync(1, 1, 85_000_000);

            Assert.True(result);
        }

        [Fact]
        public async Task IsStructuringPatternAsync_FewRequisitionsNearThreshold_ReturnsFalse()
        {
            using var db = CreateDbContext();
            db.Vendors.Add(new Vendor { Id = 1, Name = "Test Vendor", Code = "TST-001" });

            var requester = new AppUser { Id = 1, FullName = "Test", Email = "test@test.com", EmployeeId = "EMP-TST" };
            db.Users.Add(requester);

            for (int i = 0; i < 3; i++)
            {
                db.ExpenseRequisitions.Add(new ExpenseRequisition
                {
                    RequesterId = 1,
                    VendorId = 1,
                    Amount = 80_000_000,
                    Title = $"Requisition {i}",
                    Description = "High-value requisition",
                    RequisitionNumber = $"REQ-HV-{i:D4}",
                    Status = RequisitionStatus.Draft,
                    DepartmentId = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-(i * 3))
                });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.IsStructuringPatternAsync(1, 1, 85_000_000);

            Assert.False(result);
        }

        [Fact]
        public async Task CheckPriceAsync_ZeroBenchmark_ReturnsNotFlagged()
        {
            using var db = CreateDbContext();
            db.Vendors.Add(new Vendor { Id = 1, Name = "Test Vendor", Code = "TST-001" });
            for (int i = 0; i < 5; i++)
            {
                db.PriceHistories.Add(new PriceHistory
                {
                    VendorId = 1,
                    ItemDescription = "Free Item",
                    Price = 0,
                    PurchaseDate = DateTime.UtcNow.AddDays(-(i + 1)),
                    RequisitionId = i + 1
                });
            }
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.CheckPriceAsync(1, 100_000, "Free Item");

            Assert.False(result.IsFlagged);
            Assert.Contains("Insufficient price history", result.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
