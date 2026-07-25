using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    /// <summary>
    /// Rule-based fraud detection service.
    /// Intentionally kept simple and rule-based — ML-based anomaly detection
    /// is overkill at this scale and harder to audit/explain to stakeholders.
    /// </summary>
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly AppDbContext _db;

        // Flag if requested price exceeds benchmark by more than this percentage
        private const decimal PriceDeviationThreshold = 0.20m; // 20%

        // Minimum historical records needed before benchmarking is reliable
        private const int MinHistoryCount = 3;

        // Structuring detection: how many requisitions in this window is suspicious
        private const int StructuringWindowDays = 30;
        private const int StructuringMaxCount = 5;

        public FraudDetectionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PriceCheckResult> CheckPriceAsync(int vendorId, decimal requestedAmount, string itemDescription)
        {
            var history = await _db.PriceHistories
                .Where(p => p.VendorId == vendorId
                    && p.ItemDescription == itemDescription)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(20)
                .ToListAsync();

            if (history.Count < MinHistoryCount)
            {
                // Not enough data to benchmark — don't flag, but note it
                return new PriceCheckResult
                {
                    IsFlagged = false,
                    Message = "Insufficient price history for benchmarking. Manual review recommended for first-time vendors."
                };
            }

            var benchmark = history.Average(p => p.Price);
            // Guard against zero benchmark to prevent divide by zero
            if (benchmark == 0)
            {
                return new PriceCheckResult
                {
                    IsFlagged = false,
                    Message = "Benchmark price is zero — manual review recommended."
                };
            }
            var deviation = (requestedAmount - benchmark) / benchmark;

            var isFlagged = deviation > PriceDeviationThreshold;

            return new PriceCheckResult
            {
                IsFlagged = isFlagged,
                BenchmarkPrice = benchmark,
                DeviationPercent = deviation * 100,
                Message = isFlagged
                    ? $"Requested amount is {deviation * 100:F1}% above the historical average of Rp {benchmark:N0} for this vendor. Finance review required."
                    : null
            };
        }

        public async Task<bool> IsStructuringPatternAsync(int requesterId, int vendorId, decimal amount)
        {
            // Structuring: multiple transactions just below approval threshold
            // to avoid triggering higher-level approval.
            // Only flag if the current amount is near the L2 approval threshold.
            const decimal l2Threshold = 100_000_000m; // 100 million IDR
            if (amount < l2Threshold * 0.7m)
                return false;

            var windowStart = DateTime.UtcNow.AddDays(-StructuringWindowDays);

            var recentCount = await _db.ExpenseRequisitions
                .Where(r => r.RequesterId == requesterId
                    && r.VendorId == vendorId
                    && r.CreatedAt >= windowStart
                    && !r.IsDeleted)
                .CountAsync();

            return recentCount >= StructuringMaxCount;
        }
    }
}
