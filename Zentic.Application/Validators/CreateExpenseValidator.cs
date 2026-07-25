using FluentValidation;
using Zentic.Application.DTOs;

namespace Zentic.Application.Validators
{
    public class CreateExpenseValidator : AbstractValidator<CreateExpenseDto>
    {
        // Threshold di atas ini wajib multiple quotation
        public const decimal MultipleQuotationThreshold = 50_000_000m; // 50 juta IDR

        // Threshold di atas ini masuk approval level 2
        public const decimal L2ApprovalThreshold = 100_000_000m; // 100 juta IDR

        public CreateExpenseValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MinimumLength(20).WithMessage("Description must be at least 20 characters — be specific.")
                .MaximumLength(2000);

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0.")
                .LessThanOrEqualTo(999_999_999_999m).WithMessage("Amount exceeds maximum allowed.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Department is required.");

            // Jika ada conflict of interest, detail wajib diisi
            RuleFor(x => x.ConflictOfInterestDetails)
                .NotEmpty().WithMessage("Please describe the conflict of interest.")
                .When(x => x.HasConflictOfInterest);
        }
    }
}
