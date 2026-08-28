using Zentic.Infrastructure.Helpers;
using Zentic.Domain.Enums;
using Xunit;

namespace Zentic.Tests.UnitTests
{
    public class BadgeHelperTests
    {
        [Theory]
        [InlineData(RequisitionStatus.Draft, "badge bg-secondary")]
        [InlineData(RequisitionStatus.Submitted, "badge bg-primary")]
        [InlineData(RequisitionStatus.UnderReview, "badge bg-warning text-dark")]
        [InlineData(RequisitionStatus.Approved, "badge bg-success")]
        [InlineData(RequisitionStatus.Rejected, "badge bg-danger")]
        [InlineData(RequisitionStatus.Cancelled, "badge bg-dark")]
        [InlineData(RequisitionStatus.Paid, "badge bg-info text-dark")]
        public void GetBadgeClass_ReturnsCorrectBadgeForStatus(RequisitionStatus status, string expected)
        {
            var result = BadgeHelper.GetBadgeClass(status);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetBadgeClass_UnknownStatus_ReturnsDefaultBadge()
        {
            var result = BadgeHelper.GetBadgeClass((RequisitionStatus)999);
            Assert.Equal("badge bg-secondary", result);
        }
    }
}
