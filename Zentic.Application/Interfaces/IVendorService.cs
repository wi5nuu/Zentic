using Zentic.Application.DTOs;
using Zentic.Domain.Enums;

namespace Zentic.Application.Interfaces
{
    public interface IVendorService
    {
        Task<List<VendorDto>> GetActiveVendorsAsync();
        Task<List<VendorDto>> GetAllVendorsAsync();
        Task<VendorDto?> GetByIdAsync(int id);
        Task<VendorDto> CreateAsync(CreateVendorDto dto, int createdByUserId);
        Task<VendorDto> UpdateAsync(int id, CreateVendorDto dto, int updatedByUserId);
        Task VerifyAsync(int id, int verifiedByUserId);
        Task SuspendAsync(int id, int suspendedByUserId);
    }
}
