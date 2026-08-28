using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Repositories;
using Zentic.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class VendorServiceTests
    {
        private static (AppDbContext db, VendorService service) CreateService()
        {
            var db = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options);

            var encryption = new Mock<IEncryptionService>();
            encryption.Setup(e => e.MaskEncryptedAccountNumber(It.IsAny<string>()))
                      .Returns((string s) => "***" + s[^4..]);
            encryption.Setup(e => e.MaskAccountNumber(It.IsAny<string>()))
                      .Returns((string s) => new string('*', s.Length - 4) + s[^4..]);
            encryption.Setup(e => e.Decrypt(It.IsAny<string>()))
                      .Returns((string s) => s);
            encryption.Setup(e => e.Encrypt(It.IsAny<string>()))
                      .Returns((string s) => "ENC_" + s);

            var service = new VendorService(db, encryption.Object);
            return (db, service);
        }

        private static async Task SeedVendorsAsync(AppDbContext db)
        {
            db.Vendors.AddRange(
                new Vendor { Id = 1, Name = "Vendor A", Code = "VA", Status = VendorStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Vendor { Id = 2, Name = "Vendor B", Code = "VB", Status = VendorStatus.PendingVerification, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Vendor { Id = 3, Name = "Vendor C", Code = "VC", Status = VendorStatus.Active, IsDeleted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task GetActiveVendorsAsync_ReturnsOnlyActiveVendors()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            var result = await service.GetActiveVendorsAsync();

            Assert.Single(result);
            Assert.Equal("Vendor A", result[0].Name);
        }

        [Fact]
        public async Task GetAllVendorsAsync_ReturnsAllNonDeletedVendors()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            var result = await service.GetAllVendorsAsync();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingVendor_ReturnsVendor()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            var result = await service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Vendor A", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingVendor_ReturnsNull()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            var result = await service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_CreatesVendor()
        {
            var (db, service) = CreateService();

            var dto = new CreateVendorDto
            {
                Name = "New Vendor",
                Code = "NV",
                Email = "vendor@test.com",
                PhoneNumber = "1234567890",
                TaxId = "1234567890"
            };

            var result = await service.CreateAsync(dto, 1);

            Assert.Equal("New Vendor", result.Name);
            Assert.Equal("PendingVerification", result.Status);
            Assert.Equal(1, db.Vendors.Count());
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsInvalidOperationException()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            var dto = new CreateVendorDto
            {
                Name = "Duplicate Code Vendor",
                Code = "VA"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(dto, 1));
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_CreatesSuccessfully()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            var dto = new CreateVendorDto
            {
                Name = "Vendor A",
                Code = "NEWCODE"
            };

            var result = await service.CreateAsync(dto, 1);
            Assert.Equal("Vendor A", result.Name);
        }

        [Fact]
        public async Task VerifyAsync_PendingVendor_SetsToActive()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            await service.VerifyAsync(2, 1);

            var vendor = await db.Vendors.FindAsync(2);
            Assert.Equal(VendorStatus.Active, vendor!.Status);
            Assert.NotNull(vendor.VerifiedAt);
        }

        [Fact]
        public async Task VerifyAsync_NonExistingVendor_ThrowsInvalidOperationException()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.VerifyAsync(999, 1));
        }

        [Fact]
        public async Task SuspendAsync_ActiveVendor_SetsToSuspended()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            await service.SuspendAsync(1, 1);

            var vendor = await db.Vendors.FindAsync(1);
            Assert.Equal(VendorStatus.Suspended, vendor!.Status);
        }

        [Fact]
        public async Task SuspendAsync_AlreadySuspended_SetsToActive()
        {
            var (db, service) = CreateService();
            await SeedVendorsAsync(db);

            // Suspend first
            await service.SuspendAsync(1, 1);
            // Suspend again to toggle back
            await service.SuspendAsync(1, 1);

            var vendor = await db.Vendors.FindAsync(1);
            Assert.Equal(VendorStatus.Active, vendor!.Status);
        }
    }
}
