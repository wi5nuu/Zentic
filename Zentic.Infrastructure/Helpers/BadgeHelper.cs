using Zentic.Domain.Enums;

namespace Zentic.Infrastructure.Helpers
{
    public static class BadgeHelper
    {
        public static string GetBadgeClass(RequisitionStatus status) => status switch
        {
            RequisitionStatus.Draft => "badge bg-secondary",
            RequisitionStatus.Submitted => "badge bg-primary",
            RequisitionStatus.UnderReview => "badge bg-warning text-dark",
            RequisitionStatus.Approved => "badge bg-success",
            RequisitionStatus.Rejected => "badge bg-danger",
            RequisitionStatus.Cancelled => "badge bg-dark",
            RequisitionStatus.Paid => "badge bg-info text-dark",
            _ => "badge bg-secondary"
        };
    }
}
