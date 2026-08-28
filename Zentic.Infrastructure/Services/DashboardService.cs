using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DashboardDto> GetDashboardAsync(int userId, string role, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var isStaff = role == UserRole.Staff.ToString();
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var baseQuery = _db.ExpenseRequisitions
                .AsNoTracking()
                .Where(r => !r.IsDeleted);

            if (isStaff)
                baseQuery = baseQuery.Where(r => r.RequesterId == userId);

            // Count queries — no ToList(), pure SQL aggregation
            var totalRequisitions = await baseQuery.CountAsync(cancellationToken);
            var pendingApproval = await baseQuery.CountAsync(r =>
                r.Status == RequisitionStatus.Submitted || r.Status == RequisitionStatus.UnderReview, cancellationToken);
            var approvedThisMonth = await baseQuery.CountAsync(r =>
                r.CreatedAt >= monthStart &&
                (r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.Paid), cancellationToken);
            var rejectedThisMonth = await baseQuery.CountAsync(r =>
                r.CreatedAt >= monthStart && r.Status == RequisitionStatus.Rejected, cancellationToken);
            var flaggedForReview = await baseQuery.CountAsync(r => r.IsFlaggedForReview, cancellationToken);
            var totalAmountThisMonth = await baseQuery
                .Where(r => r.CreatedAt >= monthStart)
                .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;
            var totalAmountApproved = await baseQuery
                .Where(r => r.Status == RequisitionStatus.Approved || r.Status == RequisitionStatus.Paid)
                .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

            // Recent 8 — minimal columns, no Include needed
            var recentRequisitions = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Take(8)
                .Select(r => new RequisitionSummaryDto
                {
                    Id = r.Id,
                    RequisitionNumber = r.RequisitionNumber,
                    Title = r.Title,
                    Amount = r.Amount,
                    Status = r.Status.ToString(),
                    StatusBadgeClass = BadgeHelper.GetBadgeClass(r.Status),
                    IsFlagged = r.IsFlaggedForReview,
                    CreatedAt = r.CreatedAt,
                    RequesterName = r.Requester != null ? r.Requester.FullName : "Unknown"
                })
                .ToListAsync(cancellationToken);

            // Category breakdown — aggregated in SQL
            var categoryBreakdown = await baseQuery
                .GroupBy(r => r.Category)
                .Select(g => new CategorySummaryDto
                {
                    Category = g.Key.ToString(),
                    Count = g.Count(),
                    TotalAmount = g.Sum(r => r.Amount)
                })
                .ToListAsync(cancellationToken);

            return new DashboardDto
            {
                TotalRequisitions = totalRequisitions,
                PendingApproval = pendingApproval,
                ApprovedThisMonth = approvedThisMonth,
                RejectedThisMonth = rejectedThisMonth,
                FlaggedForReview = flaggedForReview,
                TotalAmountThisMonth = totalAmountThisMonth,
                TotalAmountApproved = totalAmountApproved,
                RecentRequisitions = recentRequisitions,
                CategoryBreakdown = categoryBreakdown
            };
        }
    }
}
