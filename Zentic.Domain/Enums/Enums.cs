namespace Zentic.Domain.Enums
{
    public enum UserRole
    {
        Staff = 1,
        Manager = 2,
        Finance = 3,
        Admin = 4,
        Auditor = 5
    }

    public enum ExpenseCategory
    {
        OfficeSupplies = 1,
        Travel = 2,
        ITEquipment = 3,
        Training = 4,
        Utilities = 5,
        Maintenance = 6,
        Entertainment = 7,
        Other = 99
    }

    public enum RequisitionStatus
    {
        Draft = 1,
        Submitted = 2,
        UnderReview = 3,
        Approved = 10,
        Rejected = 11,
        Cancelled = 12,
        Paid = 20
    }

    public enum VendorStatus
    {
        PendingVerification = 1,
        Active = 2,
        Suspended = 3,
        Blacklisted = 4
    }

    public enum ApprovalAction
    {
        Approve = 1,
        Reject = 2
    }

    public enum AuditEventType
    {
        Create = 1,
        Update = 2,
        Delete = 3,
        StatusChange = 4,
        Approve = 5,
        Reject = 6,
        View = 7,
        Export = 8,
        Login = 9,
        Logout = 10
    }
}
