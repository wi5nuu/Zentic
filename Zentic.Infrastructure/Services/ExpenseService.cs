using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zentic.Infrastructure.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IRepository<ExpenseRequisition> _requisitionRepo;
        private readonly IRepository<AppUser> _userRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IFraudDetectionService _fraudDetection;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;

        public ExpenseService(
            IRepository<ExpenseRequisition> requisitionRepo,
            IRepository<AppUser> userRepo,
            IRepository<Department> departmentRepo,
            IFraudDetectionService fraudDetection,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            AppDbContext db)
        {
            _requisitionRepo = requisitionRepo;
            _userRepo = userRepo;
            _departmentRepo = departmentRepo;
            _fraudDetection = fraudDetection;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _db = db;
        }

        public async Task<PaginatedResult<ExpenseRequisitionDto>> GetPagedAsync(ExpenseFilterDto filter, int userId, string role)
        {
            var query = _requisitionRepo.Query()
                .Include(r => r.Requester)
                .Include(r => r.Department)
                .Include(r => r.Approver)
                .Include(r => r.Vendor)
                .Where(r => !r.IsDeleted);

            if (role == UserRole.Staff.ToString())
                query = query.Where(r => r.RequesterId == userId);

            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<RequisitionStatus>(filter.Status, out var status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(r => r.Title.Contains(filter.Search) || r.RequisitionNumber.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.Category) && Enum.TryParse<ExpenseCategory>(filter.Category, out var category))
                query = query.Where(r => r.Category == category);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(r => new ExpenseRequisitionDto
                {
                    Id = r.Id,
                    RequisitionNumber = r.RequisitionNumber,
                    Title = r.Title,
                    Description = r.Description,
                    Amount = r.Amount,
                    Category = r.Category.ToString(),
                    Status = r.Status.ToString(),
                    StatusBadgeClass = GetBadgeClass(r.Status),
                    IsFlaggedForReview = r.IsFlaggedForReview,
                    FlagReason = r.FlagReason,
                    PriceDeviation = r.PriceDeviation,
                    CreatedAt = r.CreatedAt,
                    SubmittedAt = r.SubmittedAt,
                    ApprovedAt = r.ApprovedAt,
                    RequesterName = r.Requester!.FullName,
                    RequesterEmail = r.Requester!.Email,
                    ApproverName = r.Approver != null ? r.Approver.FullName : null,
                    DepartmentName = r.Department!.Name,
                    VendorName = r.Vendor != null ? r.Vendor.Name : null
                })
                .ToListAsync();

            return new PaginatedResult<ExpenseRequisitionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<ExpenseRequisitionDto?> GetByIdAsync(int id, int userId, string role)
        {
            var query = _requisitionRepo.Query()
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .Include(r => r.Approver)
                .Include(r => r.Department)
                .Include(r => r.Vendor)
                .Include(r => r.ApprovalHistory).ThenInclude(h => h.Actor)
                .Where(r => r.Id == id && !r.IsDeleted);

            if (role == UserRole.Staff.ToString())
                query = query.Where(r => r.RequesterId == userId);

            var req = await query.FirstOrDefaultAsync();
            if (req == null) return null;

            return new ExpenseRequisitionDto
            {
                Id = req.Id,
                RequesterId = req.RequesterId,
                RequisitionNumber = req.RequisitionNumber,
                Title = req.Title,
                Description = req.Description,
                Amount = req.Amount,
                Category = req.Category.ToString(),
                Status = req.Status.ToString(),
                StatusBadgeClass = GetBadgeClass(req.Status),
                IsFlaggedForReview = req.IsFlaggedForReview,
                FlagReason = req.FlagReason,
                PriceDeviation = req.PriceDeviation,
                CreatedAt = req.CreatedAt,
                SubmittedAt = req.SubmittedAt,
                ApprovedAt = req.ApprovedAt,
                RejectedAt = req.RejectedAt,
                ApprovalNotes = req.ApprovalNotes,
                RejectionReason = req.RejectionReason,
                RequesterName = req.Requester!.FullName,
                RequesterEmail = req.Requester!.Email,
                ApproverName = req.Approver?.FullName,
                DepartmentName = req.Department!.Name,
                VendorName = req.Vendor?.Name,
                ApprovalHistory = req.ApprovalHistory.OrderBy(h => h.Timestamp).Select(h => new ApprovalHistoryDto
                {
                    ActorName = h.Actor!.FullName,
                    Action = h.Action.ToString(),
                    FromStatus = h.FromStatus.ToString(),
                    ToStatus = h.ToStatus.ToString(),
                    Notes = h.Notes,
                    Timestamp = h.Timestamp
                }).ToList()
            };
        }

        public async Task<ExpenseRequisitionDto> CreateAsync(CreateExpenseDto dto, int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
                throw new InvalidOperationException("User not found or inactive.");

            var department = await _departmentRepo.GetByIdAsync(dto.DepartmentId);
            if (department == null)
                throw new InvalidOperationException("Department not found.");

            var requisition = new ExpenseRequisition
            {
                RequisitionNumber = await GenerateRequisitionNumberAsync(),
                Title = dto.Title,
                Description = dto.Description,
                Amount = dto.Amount,
                Category = Enum.Parse<ExpenseCategory>(dto.Category),
                DepartmentId = dto.DepartmentId,
                RequesterId = userId,
                VendorId = dto.VendorId,
                Status = RequisitionStatus.Draft,
                HasConflictOfInterest = dto.HasConflictOfInterest,
                ConflictOfInterestDetails = dto.ConflictOfInterestDetails,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (dto.VendorId.HasValue)
            {
                var priceCheck = await _fraudDetection.CheckPriceAsync(dto.VendorId.Value, dto.Amount, dto.Title);
                if (priceCheck.IsFlagged)
                {
                    requisition.IsFlaggedForReview = true;
                    requisition.FlagReason = priceCheck.Message;
                    requisition.BenchmarkPrice = priceCheck.BenchmarkPrice;
                    requisition.PriceDeviation = priceCheck.DeviationPercent;
                }

                if (await _fraudDetection.IsStructuringPatternAsync(userId, dto.VendorId.Value, dto.Amount))
                {
                    requisition.IsFlaggedForReview = true;
                    requisition.FlagReason = (requisition.FlagReason ?? "") + " Possible structuring pattern detected.";
                }
            }

            var created = await _requisitionRepo.AddAsync(requisition);
            await _unitOfWork.SaveChangesAsync();

            // Persist conflict-of-interest declaration when a vendor is selected
            if (dto.VendorId.HasValue)
            {
                var signature = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes($"{userId}:{created.Id}:{created.CreatedAt:O}")));

                var declaration = new ConflictOfInterestDeclaration
                {
                    RequisitionId = created.Id,
                    DeclarantId = userId,
                    HasConflict = dto.HasConflictOfInterest,
                    ConflictDescription = dto.ConflictOfInterestDetails,
                    SignatureHash = signature,
                    DeclaredAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.ConflictOfInterestDeclarations.Add(declaration);
                await _db.SaveChangesAsync();
            }

            await _auditService.LogAsync(AuditEventType.Create, nameof(ExpenseRequisition), created.Id.ToString(),
                userId, null, null, System.Text.Json.JsonSerializer.Serialize(new { created.Id, created.RequisitionNumber, created.Amount }), "Requisition created", created.Id);

            return new ExpenseRequisitionDto
            {
                Id = created.Id,
                RequisitionNumber = created.RequisitionNumber,
                Title = created.Title,
                Description = created.Description,
                Amount = created.Amount,
                Category = created.Category.ToString(),
                Status = created.Status.ToString(),
                StatusBadgeClass = GetBadgeClass(created.Status),
                IsFlaggedForReview = created.IsFlaggedForReview,
                FlagReason = created.FlagReason,
                CreatedAt = created.CreatedAt,
                RequesterName = user.FullName,
                RequesterEmail = user.Email,
                DepartmentName = department.Name
            };
        }

        public async Task SubmitAsync(int id, int userId)
        {
            var req = await _requisitionRepo.GetByIdAsync(id);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId != userId) throw new UnauthorizedAccessException("You can only submit your own requisition.");
            if (req.Status != RequisitionStatus.Draft) throw new InvalidOperationException("Only draft requisitions can be submitted.");

            var oldStatus = req.Status;
            req.Status = RequisitionStatus.Submitted;
            req.SubmittedAt = DateTime.UtcNow;
            req.UpdatedAt = DateTime.UtcNow;

            await _requisitionRepo.UpdateAsync(req);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAsync(AuditEventType.StatusChange, nameof(ExpenseRequisition), id.ToString(),
                userId, null, oldStatus.ToString(), req.Status.ToString(), "Submitted for approval", id);
        }

        public async Task ApproveAsync(int id, int approverId, string? notes)
        {
            var req = await _requisitionRepo.GetByIdAsync(id);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId == approverId) throw new InvalidOperationException("You cannot approve your own requisition.");
            if (req.Status != RequisitionStatus.Submitted && req.Status != RequisitionStatus.UnderReview)
                throw new InvalidOperationException("Requisition cannot be approved at this stage.");

            var oldStatus = req.Status;
            req.Status = RequisitionStatus.Approved;
            req.ApproverId = approverId;
            req.ApprovedAt = DateTime.UtcNow;
            req.ApprovalNotes = notes;
            req.UpdatedAt = DateTime.UtcNow;

            await _requisitionRepo.UpdateAsync(req);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAsync(AuditEventType.Approve, nameof(ExpenseRequisition), id.ToString(),
                approverId, null, oldStatus.ToString(), req.Status.ToString(), notes, id);
        }

        public async Task RejectAsync(int id, int approverId, string reason)
        {
            var req = await _requisitionRepo.GetByIdAsync(id);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId == approverId) throw new InvalidOperationException("You cannot reject your own requisition.");
            if (req.Status != RequisitionStatus.Submitted && req.Status != RequisitionStatus.UnderReview)
                throw new InvalidOperationException("Requisition cannot be rejected at this stage.");

            var oldStatus = req.Status;
            req.Status = RequisitionStatus.Rejected;
            req.ApproverId = approverId;
            req.RejectedAt = DateTime.UtcNow;
            req.RejectionReason = reason;
            req.UpdatedAt = DateTime.UtcNow;

            await _requisitionRepo.UpdateAsync(req);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAsync(AuditEventType.Reject, nameof(ExpenseRequisition), id.ToString(),
                approverId, null, oldStatus.ToString(), req.Status.ToString(), reason, id);
        }

        public async Task CancelAsync(int id, int userId)
        {
            var req = await _requisitionRepo.GetByIdAsync(id);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId != userId) throw new UnauthorizedAccessException("You can only cancel your own requisition.");
            if (req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Paid)
                throw new InvalidOperationException("Cannot cancel an approved or paid requisition.");

            var oldStatus = req.Status;
            req.Status = RequisitionStatus.Cancelled;
            req.UpdatedAt = DateTime.UtcNow;

            await _requisitionRepo.UpdateAsync(req);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogAsync(AuditEventType.StatusChange, nameof(ExpenseRequisition), id.ToString(),
                userId, null, oldStatus.ToString(), req.Status.ToString(), "Cancelled by requester", id);
        }

        private async Task<string> GenerateRequisitionNumberAsync()
        {
            var now = DateTime.UtcNow;
            // Use a DB-level sequence via MAX+1 with row lock to avoid race condition
            var maxNumber = await _db.ExpenseRequisitions
                .Where(r => r.CreatedAt.Year == now.Year && r.CreatedAt.Month == now.Month)
                .Select(r => r.RequisitionNumber)
                .ToListAsync();

            var count = maxNumber.Count + 1;
            var candidate = $"EXP-{now.Year}{now.Month:D2}-{count:D4}";

            // Ensure uniqueness — increment if collision exists (handles concurrent inserts)
            while (await _db.ExpenseRequisitions
                       .AnyAsync(r => r.RequisitionNumber == candidate))
            {
                count++;
                candidate = $"EXP-{now.Year}{now.Month:D2}-{count:D4}";
            }

            return candidate;
        }

        private static string GetBadgeClass(RequisitionStatus status) => status switch
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
