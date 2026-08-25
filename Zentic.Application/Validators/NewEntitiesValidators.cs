using FluentValidation;
using Zentic.Application.DTOs;

namespace Zentic.Application.Validators
{
    public class CreateVendorValidator : AbstractValidator<CreateVendorDto>
    {
        public CreateVendorValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Vendor name is required")
                .MaximumLength(200).WithMessage("Vendor name cannot exceed 200 characters");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Vendor code is required")
                .MaximumLength(50).WithMessage("Vendor code cannot exceed 50 characters")
                .Matches("^[A-Z0-9-]+$").WithMessage("Vendor code must contain only uppercase letters, numbers, and hyphens");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Invalid email format");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(\+62|62|0)[0-9]{8,13}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Invalid phone number format. Must be Indonesian format");

            RuleFor(x => x.TaxId)
                .Matches(@"^\d{15}$").When(x => !string.IsNullOrEmpty(x.TaxId))
                .WithMessage("Tax ID (NPWP) must be 15 digits");

            RuleFor(x => x.BankAccountNumber)
                .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.BankAccountNumber))
                .WithMessage("Bank account number cannot exceed 50 characters");
        }
    }

    public class CreateDocumentValidator : AbstractValidator<CreateDocumentDto>
    {
        private static readonly string[] AllowedMimeTypes = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/msword",
            "application/vnd.ms-excel"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

        public CreateDocumentValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("File name is required")
                .MaximumLength(255).WithMessage("File name cannot exceed 255 characters");

            RuleFor(x => x.OriginalFileName)
                .NotEmpty().WithMessage("Original file name is required")
                .MaximumLength(255).WithMessage("Original file name cannot exceed 255 characters");

            RuleFor(x => x.FilePath)
                .NotEmpty().WithMessage("File path is required");

            RuleFor(x => x.FileSizeBytes)
                .GreaterThan(0).WithMessage("File size must be greater than 0")
                .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage($"File size cannot exceed {MaxFileSizeBytes / 1024 / 1024}MB");

            RuleFor(x => x.MimeType)
                .NotEmpty().WithMessage("MIME type is required")
                .Must(mime => AllowedMimeTypes.Contains(mime))
                .WithMessage($"File type not allowed. Allowed types: {string.Join(", ", AllowedMimeTypes)}");

            RuleFor(x => x.DocumentType)
                .IsInEnum().WithMessage("Invalid document type");

            RuleFor(x => x)
                .Must(x => x.RequisitionId.HasValue || x.VendorId.HasValue || x.PaymentId.HasValue)
                .WithMessage("Document must be associated with at least one entity (Requisition, Vendor, or Payment)");
        }
    }

    public class CreateNotificationValidator : AbstractValidator<CreateNotificationDto>
    {
        public CreateNotificationValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Notification title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Notification message is required")
                .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid notification type");

            RuleFor(x => x.Priority)
                .IsInEnum().WithMessage("Invalid notification priority");

            RuleFor(x => x.RecipientId)
                .GreaterThan(0).WithMessage("Valid recipient is required");
        }
    }

    public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
    {
        public CreatePaymentValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Payment amount must be greater than 0")
                .LessThanOrEqualTo(999999999999.99m).WithMessage("Payment amount is too large");

            RuleFor(x => x.Currency)
                .IsInEnum().WithMessage("Invalid currency");

            RuleFor(x => x.PaymentMethod)
                .IsInEnum().WithMessage("Invalid payment method");

            RuleFor(x => x.PaymentDate)
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
                .WithMessage("Payment date cannot be in the future");

            RuleFor(x => x.ReferenceNumber)
                .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ReferenceNumber))
                .WithMessage("Reference number cannot exceed 100 characters");

            RuleFor(x => x.RequisitionId)
                .GreaterThan(0).WithMessage("Valid requisition is required");

            RuleFor(x => x.VendorId)
                .GreaterThan(0).WithMessage("Valid vendor is required");

            RuleFor(x => x.BankAccountNumber)
                .NotEmpty().When(x => x.PaymentMethod == Domain.Enums.PaymentMethod.BankTransfer)
                .WithMessage("Bank account number is required for bank transfers");

            RuleFor(x => x.BankName)
                .NotEmpty().When(x => x.PaymentMethod == Domain.Enums.PaymentMethod.BankTransfer)
                .WithMessage("Bank name is required for bank transfers");
        }
    }

    public class CreateBudgetAllocationValidator : AbstractValidator<CreateBudgetAllocationDto>
    {
        public CreateBudgetAllocationValidator()
        {
            RuleFor(x => x.BudgetCode)
                .NotEmpty().WithMessage("Budget code is required")
                .MaximumLength(50).WithMessage("Budget code cannot exceed 50 characters")
                .Matches("^[A-Z0-9-]+$").WithMessage("Budget code must contain only uppercase letters, numbers, and hyphens");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Budget name is required")
                .MaximumLength(200).WithMessage("Budget name cannot exceed 200 characters");

            RuleFor(x => x.AllocatedAmount)
                .GreaterThan(0).WithMessage("Allocated amount must be greater than 0")
                .LessThanOrEqualTo(999999999999.99m).WithMessage("Allocated amount is too large");

            RuleFor(x => x.Currency)
                .IsInEnum().WithMessage("Invalid currency");

            RuleFor(x => x.PeriodType)
                .IsInEnum().WithMessage("Invalid period type");

            RuleFor(x => x.FiscalYear)
                .GreaterThanOrEqualTo(2000).WithMessage("Fiscal year must be 2000 or later")
                .LessThanOrEqualTo(2100).WithMessage("Fiscal year cannot exceed 2100");

            RuleFor(x => x.Quarter)
                .InclusiveBetween(1, 4).When(x => x.Quarter.HasValue)
                .WithMessage("Quarter must be between 1 and 4");

            RuleFor(x => x.Month)
                .InclusiveBetween(1, 12).When(x => x.Month.HasValue)
                .WithMessage("Month must be between 1 and 12");

            RuleFor(x => x.PeriodStartDate)
                .NotEmpty().WithMessage("Period start date is required")
                .LessThan(x => x.PeriodEndDate).WithMessage("Start date must be before end date");

            RuleFor(x => x.PeriodEndDate)
                .NotEmpty().WithMessage("Period end date is required")
                .GreaterThan(x => x.PeriodStartDate).WithMessage("End date must be after start date");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Valid department is required");

            RuleFor(x => x.WarningThresholdPercentage)
                .InclusiveBetween(1, 100).WithMessage("Warning threshold must be between 1 and 100");
        }
    }

    public class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderDto>
    {
        public CreatePurchaseOrderValidator()
        {
            RuleFor(x => x.PODate)
                .NotEmpty().WithMessage("PO date is required")
                .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("PO date cannot be in the future");

            RuleFor(x => x.ExpectedDeliveryDate)
                .GreaterThanOrEqualTo(x => x.PODate).When(x => x.ExpectedDeliveryDate.HasValue)
                .WithMessage("Expected delivery date must be after PO date");

            RuleFor(x => x.TaxAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Tax amount cannot be negative");

            RuleFor(x => x.DiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("Discount amount cannot be negative");

            RuleFor(x => x.Currency)
                .IsInEnum().WithMessage("Invalid currency");

            RuleFor(x => x.RequisitionId)
                .GreaterThan(0).WithMessage("Valid requisition is required");

            RuleFor(x => x.VendorId)
                .GreaterThan(0).WithMessage("Valid vendor is required");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one item is required")
                .Must(items => items.Count > 0).WithMessage("Purchase order must contain at least one item");

            RuleForEach(x => x.Items).SetValidator(new CreatePurchaseOrderItemValidator());
        }
    }

    public class CreatePurchaseOrderItemValidator : AbstractValidator<CreatePurchaseOrderItemDto>
    {
        public CreatePurchaseOrderItemValidator()
        {
            RuleFor(x => x.ItemDescription)
                .NotEmpty().WithMessage("Item description is required")
                .MaximumLength(500).WithMessage("Item description cannot exceed 500 characters");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0")
                .LessThanOrEqualTo(999999).WithMessage("Quantity is too large");

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("Unit is required")
                .MaximumLength(20).WithMessage("Unit cannot exceed 20 characters");

            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage("Unit price must be greater than 0")
                .LessThanOrEqualTo(999999999.99m).WithMessage("Unit price is too large");

            RuleFor(x => x.TaxRate)
                .GreaterThanOrEqualTo(0).WithMessage("Tax rate cannot be negative")
                .LessThanOrEqualTo(100).WithMessage("Tax rate cannot exceed 100%");
        }
    }

    public class UpdateExpenseValidator : AbstractValidator<CreateExpenseDto>
    {
        public UpdateExpenseValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0")
                .LessThanOrEqualTo(999999999999.99m).WithMessage("Amount is too large");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Department is required");

            RuleFor(x => x.DueDate)
                .GreaterThan(DateTime.UtcNow).When(x => x.DueDate.HasValue)
                .WithMessage("Due date must be in the future");
        }
    }
}
