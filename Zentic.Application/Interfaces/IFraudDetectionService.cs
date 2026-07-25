using Zentic.Application.DTOs;

namespace Zentic.Application.Interfaces
{
    /// <summary>
    /// Anti-fraud: price benchmarking + anomaly detection
    /// </summary>
    public interface IFraudDetectionService
    {
        /// <summary>
        /// Cek apakah harga yang diajukan jauh melebihi benchmark historis.
        /// Return null jika tidak ada histori cukup untuk dibandingkan.
        /// </summary>
        Task<PriceCheckResult> CheckPriceAsync(int vendorId, decimal requestedAmount, string itemDescription);

        /// <summary>
        /// Deteksi pola structuring: requisition berulang ke vendor sama
        /// dengan nominal mendekati threshold approval.
        /// </summary>
        Task<bool> IsStructuringPatternAsync(int requesterId, int vendorId, decimal amount);
    }
}
