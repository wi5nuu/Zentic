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
        PendingManagerApproval = 3,
        PendingFinanceApproval = 4,
        UnderReview = 5,
        Approved = 10,
        Rejected = 11,
        Cancelled = 12,
        AwaitingPayment = 19,
        Paid = 20,
        PaymentFailed = 21
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
        Logout = 10,
        LoginFailed = 11,
        PasswordChange = 12,
        PasswordReset = 13,
        AccountLocked = 14,
        AccountUnlocked = 15,
        EmailVerified = 16,
        DocumentUpload = 17,
        DocumentDelete = 18,
        BudgetExceeded = 19,
        PaymentProcessed = 20,
        NotificationSent = 21,
        MfaEnabled = 22,
        MfaDisabled = 23
    }

    public enum DocumentType
    {
        Invoice = 1,
        Quotation = 2,
        PurchaseOrder = 3,
        DeliveryReceipt = 4,
        PaymentProof = 5,
        VendorRegistration = 6,
        TaxDocument = 7,
        Contract = 8,
        Other = 99
    }

    public enum NotificationType
    {
        Email = 1,
        SMS = 2,
        InApp = 3,
        Push = 4
    }

    public enum NotificationPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Urgent = 4
    }

    public enum PaymentMethod
    {
        BankTransfer = 1,
        Check = 2,
        Cash = 3,
        CreditCard = 4,
        PaymentGateway = 5
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Refunded = 6
    }

    public enum BudgetPeriodType
    {
        Monthly = 1,
        Quarterly = 2,
        Annual = 3,
        Custom = 4
    }

    public enum ApprovalLevel
    {
        L0_AutoApproved = 0,
        L1_Manager = 1,
        L2_Finance = 2,
        L3_Director = 3,
        L4_CEO = 4
    }

    public enum Currency
    {
        IDR = 1,
        USD = 2,
        EUR = 3,
        SGD = 4,
        MYR = 5
    }

    public enum ApproverType
    {
        SpecificUser = 1,
        Role = 2,
        Manager = 3,
        DepartmentHead = 4,
        Finance = 5,
        Admin = 6
    }

    public enum EscalationTrigger
    {
        NoAction = 1,
        Rejected = 2,
        AmountExceeded = 3,
        FlaggedForReview = 4
    }
}
