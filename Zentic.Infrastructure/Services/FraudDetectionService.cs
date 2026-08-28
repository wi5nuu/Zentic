using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        private readonly FraudDetectionOptions _options;

        public FraudDetectionService(AppDbContext db, IOptions<FraudDetectionOptions> options)
        {
            _db = db;
            _options = options.Value;
        }

        public async Task<PriceCheckResult> CheckPriceAsync(int vendorId, decimal requestedAmount, string itemDescription, CancellationToken cancellationToken = default)
        {
            var history = await _db.PriceHistories
                .AsNoTracking()
                .Where(p => p.VendorId == vendorId
                    && p.ItemDescription == itemDescription
                    && p.Price > 0) // Exclude zero prices
                .OrderByDescending(p => p.PurchaseDate)
                .Take(_options.MaxHistoryRecords)
                .ToListAsync(cancellationToken);

            if (history.Count < _options.MinHistoryCount)
            {
                // Not enough data to benchmark — don't flag, but note it
                return new PriceCheckResult
                {
                    IsFlagged = false,
                    Message = "Insufficient price history for benchmarking. Manual review recommended for first-time vendors."
                };
            }

            // Use median for more robust benchmark against outliers
            var sortedPrices = history.Select(p => p.Price).OrderBy(p => p).ToList();
            var benchmark = sortedPrices.Count % 2 == 0
                ? (sortedPrices[sortedPrices.Count / 2 - 1] + sortedPrices[sortedPrices.Count / 2]) / 2
                : sortedPrices[sortedPrices.Count / 2];

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

            // Only flag if deviation is significantly above threshold
            var isFlagged = deviation > _options.PriceDeviationThreshold;

            // Calculate confidence based on sample size and variance
            var variance = sortedPrices.Select(p => (double)(p - benchmark) * (double)(p - benchmark)).Average();
            var stdDev = Math.Sqrt(variance);
            var coefficientOfVariation = benchmark > 0 ? (decimal)stdDev / benchmark : 0;
            
            // Lower confidence if high variance in historical prices
            var confidence = coefficientOfVariation < 0.3m ? 0.9m : 0.6m;

            return new PriceCheckResult
            {
                IsFlagged = isFlagged,
                BenchmarkPrice = benchmark,
                DeviationPercent = deviation * 100,
                Message = isFlagged
                    ? $"Requested amount is {deviation * 100:F1}% above the historical median of Rp {benchmark:N0} for this vendor (based on {history.Count} records). Finance review required."
                    : null,
                ConfidenceScore = confidence
            };
        }

        public async Task<bool> IsStructuringPatternAsync(int requesterId, int vendorId, decimal amount, CancellationToken cancellationToken = default)
        {
            // Structuring: multiple transactions just below approval threshold
            // to avoid triggering higher-level approval.
            // Only flag if the current amount is near the L2 approval threshold.
            if (amount < _options.L2Threshold * _options.StructuringThresholdRatio)
                return false;

            var windowStart = DateTime.UtcNow.AddDays(-_options.StructuringWindowDays);

            // Count recent requisitions near threshold
            var recentCount = await _db.ExpenseRequisitions
                .AsNoTracking()
                .Where(r => r.RequesterId == requesterId
                    && r.VendorId == vendorId
                    && r.Amount >= _options.L2Threshold * _options.StructuringThresholdRatio
                    && r.Amount < _options.L2Threshold
                    && r.CreatedAt >= windowStart
                    && !r.IsDeleted)
                .CountAsync(cancellationToken);

            if (recentCount < _options.StructuringMaxCount)
                return false;

            // Additional check: look for pattern of increasing frequency
            var recentRequisitions = await _db.ExpenseRequisitions
                .AsNoTracking()
                .Where(r => r.RequesterId == requesterId
                    && r.VendorId == vendorId
                    && r.CreatedAt >= windowStart
                    && !r.IsDeleted)
                .OrderBy(r => r.CreatedAt)
                .Select(r => new { r.Amount, r.CreatedAt })
                .ToListAsync(cancellationToken);

            // Check if frequency is increasing (more requisitions in recent half of window)
            var midPoint = windowStart.AddDays(_options.StructuringWindowDays / 2.0);
            var firstHalfCount = recentRequisitions.Count(r => r.CreatedAt < midPoint);
            var secondHalfCount = recentRequisitions.Count(r => r.CreatedAt >= midPoint);

            // Flag if second half has significantly more transactions
            return secondHalfCount > firstHalfCount * 1.5 && secondHalfCount >= _options.StructuringMaxCount;
        }
    }

    public class FraudDetectionOptions
    {
        public decimal PriceDeviationThreshold { get; set; } = 0.25m; // 25%
        public int MinHistoryCount { get; set; } = 5;
        public int MaxHistoryRecords { get; set; } = 50;
        
        // Structuring detection
        public decimal L2Threshold { get; set; } = 100_000_000m;
        public decimal StructuringThresholdRatio { get; set; } = 0.7m; // 70% of L2 threshold
        public int StructuringWindowDays { get; set; } = 30;
        public int StructuringMaxCount { get; set; } = 5;
    }
}
