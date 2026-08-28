using Zentic.Application.DTOs;
using Zentic.Domain.Enums;
using System.Threading;

namespace Zentic.Application.Interfaces
{
    public interface IVendorService
    {
        Task<List<VendorDto>> GetActiveVendorsAsync(CancellationToken cancellationToken = default);
        Task<List<VendorDto>> GetAllVendorsAsync(CancellationToken cancellationToken = default);
        Task<VendorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<VendorDto> CreateAsync(CreateVendorDto dto, int createdByUserId, CancellationToken cancellationToken = default);
        Task<VendorDto> UpdateAsync(int id, CreateVendorDto dto, int updatedByUserId, CancellationToken cancellationToken = default);
        Task VerifyAsync(int id, int verifiedByUserId, CancellationToken cancellationToken = default);
        Task SuspendAsync(int id, int suspendedByUserId, CancellationToken cancellationToken = default);
    }
}
