using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    public class VendorService : IVendorService
    {
        private readonly AppDbContext _db;
        private readonly IEncryptionService _encryption;

        public VendorService(AppDbContext db, IEncryptionService encryption)
        {
            _db = db;
            _encryption = encryption;
        }

        public async Task<List<VendorDto>> GetActiveVendorsAsync()
        {
            var vendors = await _db.Vendors
                .Where(v => v.Status == VendorStatus.Active && !v.IsDeleted)
                .OrderBy(v => v.Name)
                .ToListAsync();

            return vendors.Select(v => MapToDto(v)).ToList();
        }

        public async Task<List<VendorDto>> GetAllVendorsAsync()
        {
            var vendors = await _db.Vendors
                .Include(v => v.VerifiedBy)
                .Where(v => !v.IsDeleted)
                .OrderBy(v => v.Name)
                .ToListAsync();

            return vendors.Select(v => MapToDto(v, v.VerifiedBy?.FullName)).ToList();
        }

        public async Task<VendorDto?> GetByIdAsync(int id)
        {
            var v = await _db.Vendors
                .Include(v => v.VerifiedBy)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (v == null) return null;

            return MapToDto(v, v.VerifiedBy?.FullName);
        }

        public async Task<VendorDto> CreateAsync(CreateVendorDto dto, int createdByUserId)
        {
            if (await _db.Vendors.AnyAsync(v => v.Code == dto.Code && !v.IsDeleted))
                throw new InvalidOperationException($"Vendor code '{dto.Code}' already exists.");

            var vendor = new Vendor
            {
                Name = dto.Name,
                Code = dto.Code.ToUpper(),
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                TaxId = dto.TaxId,
                BankName = dto.BankName,
                BankAccountHolderName = dto.BankAccountHolderName,
                BankAccountNumber = dto.BankAccountNumber != null
                    ? _encryption.Encrypt(dto.BankAccountNumber)
                    : null,
                Status = VendorStatus.PendingVerification,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Vendors.Add(vendor);
            await _db.SaveChangesAsync();
            return MapToDto(vendor);
        }

        public async Task<VendorDto> UpdateAsync(int id, CreateVendorDto dto, int updatedByUserId)
        {
            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted)
                ?? throw new InvalidOperationException("Vendor not found.");

            if (await _db.Vendors.AnyAsync(v => v.Code == dto.Code && v.Id != id && !v.IsDeleted))
                throw new InvalidOperationException($"Vendor code '{dto.Code}' already in use.");

            vendor.Name = dto.Name;
            vendor.Code = dto.Code.ToUpper();
            vendor.Address = dto.Address;
            vendor.PhoneNumber = dto.PhoneNumber;
            vendor.Email = dto.Email;
            vendor.TaxId = dto.TaxId;
            vendor.BankName = dto.BankName;
            vendor.BankAccountHolderName = dto.BankAccountHolderName;

            if (dto.BankAccountNumber != null)
                vendor.BankAccountNumber = _encryption.Encrypt(dto.BankAccountNumber);

            vendor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MapToDto(vendor);
        }

        public async Task VerifyAsync(int id, int verifiedByUserId)
        {
            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted)
                ?? throw new InvalidOperationException("Vendor not found.");

            if (vendor.Status != VendorStatus.PendingVerification)
                throw new InvalidOperationException("Only pending vendors can be verified.");

            vendor.Status = VendorStatus.Active;
            vendor.VerifiedById = verifiedByUserId;
            vendor.VerifiedAt = DateTime.UtcNow;
            vendor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task SuspendAsync(int id, int suspendedByUserId)
        {
            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted)
                ?? throw new InvalidOperationException("Vendor not found.");

            if (vendor.Status == VendorStatus.Blacklisted)
                throw new InvalidOperationException("Blacklisted vendors cannot be suspended.");

            vendor.Status = vendor.Status == VendorStatus.Suspended
                ? VendorStatus.Active   // toggle: re-activate
                : VendorStatus.Suspended;
            vendor.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private VendorDto MapToDto(Vendor v, string? verifiedByName = null) => new VendorDto
        {
            Id = v.Id,
            Name = v.Name,
            Code = v.Code,
            Address = v.Address,
            PhoneNumber = v.PhoneNumber,
            Email = v.Email,
            TaxId = v.TaxId,
            Status = v.Status.ToString(),
            BankName = v.BankName,
            BankAccountHolderName = v.BankAccountHolderName,
            MaskedAccountNumber = v.BankAccountNumber != null
                ? _encryption.MaskAccountNumber(_encryption.Decrypt(v.BankAccountNumber))
                : null,
            VerifiedByName = verifiedByName,
            VerifiedAt = v.VerifiedAt
        };
    }
}
