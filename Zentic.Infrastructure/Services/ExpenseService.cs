using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Zentic.Infrastructure.Data;
using Zentic.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
        private readonly IConfiguration _configuration;

        public ExpenseService(
            IRepository<ExpenseRequisition> requisitionRepo,
            IRepository<AppUser> userRepo,
            IRepository<Department> departmentRepo,
            IFraudDetectionService fraudDetection,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            AppDbContext db,
            IConfiguration configuration)
        {
            _requisitionRepo = requisitionRepo;
            _userRepo = userRepo;
            _departmentRepo = departmentRepo;
            _fraudDetection = fraudDetection;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _db = db;
            _configuration = configuration;
        }

        public async Task<PaginatedResult<ExpenseRequisitionDto>> GetPagedAsync(ExpenseFilterDto filter, int userId, string role, CancellationToken cancellationToken = default)
        {
            var query = _requisitionRepo.Query()
                .Where(r => !r.IsDeleted);

            // Role-based filtering
            query = await ApplyRoleBasedFilter(query, userId, role, cancellationToken);

            if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<RequisitionStatus>(filter.Status, out var status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(r => r.Title.Contains(filter.Search) || r.RequisitionNumber.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.Category) && Enum.TryParse<ExpenseCategory>(filter.Category, out var category))
                query = query.Where(r => r.Category == category);

            if (filter.Flagged == true)
                query = query.Where(r => r.IsFlaggedForReview);

            var totalCount = await query.CountAsync(cancellationToken);

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
                    StatusBadgeClass = BadgeHelper.GetBadgeClass(r.Status),
                    IsFlaggedForReview = r.IsFlaggedForReview,
                    FlagReason = r.FlagReason,
                    PriceDeviation = r.PriceDeviation,
                    CreatedAt = r.CreatedAt,
                    SubmittedAt = r.SubmittedAt,
                    ApprovedAt = r.ApprovedAt,
                    PaidAt = r.PaidAt,
                    RequesterName = r.Requester != null ? r.Requester.FullName : "Unknown",
                    RequesterEmail = r.Requester != null ? r.Requester.Email : "",
                    ApproverName = r.Approver != null ? r.Approver.FullName : null,
                    DepartmentName = r.Department != null ? r.Department.Name : "Unknown",
                    VendorName = r.Vendor != null ? r.Vendor.Name : null
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<ExpenseRequisitionDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        private async Task<IQueryable<ExpenseRequisition>> ApplyRoleBasedFilter(IQueryable<ExpenseRequisition> query, int userId, string role, CancellationToken cancellationToken = default)
        {
            if (role == UserRole.Staff.ToString())
            {
                // Staff can only see their own requisitions
                return query.Where(r => r.RequesterId == userId);
            }
            
            if (role == UserRole.Manager.ToString())
            {
                // Manager can see:
                // 1. Their own requisitions
                // 2. Requisitions from their subordinates (direct and indirect)
                // 3. Requisitions from their department
                var manager = await _userRepo.GetByIdAsync(userId, cancellationToken);
                if (manager == null) return query.Where(r => r.RequesterId == userId);

                var subordinateIds = await GetSubordinateIdsAsync(userId, cancellationToken);
                subordinateIds.Add(userId); // Include self
                
                return query.Where(r => 
                    subordinateIds.Contains(r.RequesterId) || 
                    r.DepartmentId == manager.DepartmentId);
            }
            
            if (role == UserRole.Finance.ToString() || role == UserRole.Admin.ToString() || role == UserRole.Auditor.ToString())
            {
                // Finance, Admin, Auditor can see all requisitions
                return query;
            }

            // Default: only own requisitions
            return query.Where(r => r.RequesterId == userId);
        }

        private async Task<List<int>> GetSubordinateIdsAsync(int managerId, CancellationToken cancellationToken = default)
        {
            var allSubordinates = new List<int>();
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(managerId);
            visited.Add(managerId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var subordinates = await _db.Users
                    .Where(u => u.ManagerId == currentId && !u.IsDeleted)
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                foreach (var subId in subordinates)
                {
                    if (visited.Add(subId))
                    {
                        allSubordinates.Add(subId);
                        queue.Enqueue(subId);
                    }
                }
            }

            return allSubordinates;
        }

        public async Task<ExpenseRequisitionDto?> GetByIdAsync(int id, int userId, string role, CancellationToken cancellationToken = default)
        {
            var query = _requisitionRepo.Query()
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .Include(r => r.Approver)
                .Include(r => r.Department)
                .Include(r => r.Vendor)
                .Include(r => r.ApprovalHistory).ThenInclude(h => h.Actor)
                .Include(r => r.Quotations).ThenInclude(q => q.Vendor)
                .Where(r => r.Id == id && !r.IsDeleted);

            // Apply role-based filtering
            query = await ApplyRoleBasedFilter(query, userId, role, cancellationToken);

            var req = await query.FirstOrDefaultAsync(cancellationToken);
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
                StatusBadgeClass = BadgeHelper.GetBadgeClass(req.Status),
                IsFlaggedForReview = req.IsFlaggedForReview,
                FlagReason = req.FlagReason,
                PriceDeviation = req.PriceDeviation,
                CreatedAt = req.CreatedAt,
                SubmittedAt = req.SubmittedAt,
                ApprovedAt = req.ApprovedAt,
                RejectedAt = req.RejectedAt,
                PaidAt = req.PaidAt,
                ApprovalNotes = req.ApprovalNotes,
                RejectionReason = req.RejectionReason,
                RequesterName = req.Requester?.FullName ?? "Unknown",
                RequesterEmail = req.Requester?.Email ?? "Unknown",
                ApproverName = req.Approver?.FullName,
                DepartmentName = req.Department?.Name ?? "Unknown",
                VendorName = req.Vendor?.Name,
                VendorId = req.VendorId,
                ApprovalHistory = req.ApprovalHistory.OrderBy(h => h.Timestamp).Select(h => new ApprovalHistoryDto
                {
                    ActorName = h.Actor?.FullName ?? "Unknown",
                    Action = h.Action.ToString(),
                    FromStatus = h.FromStatus.ToString(),
                    ToStatus = h.ToStatus.ToString(),
                    Notes = h.Notes,
                    Timestamp = h.Timestamp
                }).ToList(),
                Quotations = req.Quotations.OrderBy(q => q.QuotationDate).Select(q => new QuotationDto
                {
                    Id = q.Id,
                    VendorId = q.VendorId,
                    VendorName = q.Vendor != null ? q.Vendor.Name : $"Vendor #{q.VendorId}",
                    QuotedPrice = q.QuotedPrice,
                    QuotationDate = q.QuotationDate,
                    IsSelected = q.IsSelected,
                    SelectionReason = q.SelectionReason
                }).ToList()
            };
        }

        public async Task<ExpenseRequisitionDto> CreateAsync(CreateExpenseDto dto, int userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.IsActive)
                throw new InvalidOperationException("User not found or inactive.");

            var department = await _departmentRepo.GetByIdAsync(dto.DepartmentId, cancellationToken);
            if (department == null)
                throw new InvalidOperationException("Department not found.");

            var now = DateTime.UtcNow;
            var requisition = new ExpenseRequisition
            {
                RequisitionNumber = await GenerateRequisitionNumberAsync(cancellationToken),
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
                CreatedAt = now,
                UpdatedAt = now
            };

            if (dto.VendorId.HasValue)
            {
                var priceCheck = await _fraudDetection.CheckPriceAsync(dto.VendorId.Value, dto.Amount, dto.Title, cancellationToken);
                if (priceCheck.IsFlagged)
                {
                    requisition.IsFlaggedForReview = true;
                    requisition.FlagReason = priceCheck.Message;
                    requisition.BenchmarkPrice = priceCheck.BenchmarkPrice;
                    requisition.PriceDeviation = priceCheck.DeviationPercent;
                }

                if (await _fraudDetection.IsStructuringPatternAsync(userId, dto.VendorId.Value, dto.Amount, cancellationToken))
                {
                    requisition.IsFlaggedForReview = true;
                    requisition.FlagReason = $"{requisition.FlagReason} Possible structuring pattern detected.";
                }
            }

            var created = await _requisitionRepo.AddAsync(requisition, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Persist conflict-of-interest declaration when a vendor is selected
            if (dto.VendorId.HasValue)
            {
                var signature = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes($"{userId}:{created.Id}:{created.CreatedAt:O}")));

                var declarationNow = DateTime.UtcNow;
                var declaration = new ConflictOfInterestDeclaration
                {
                    RequisitionId = created.Id,
                    DeclarantId = userId,
                    HasConflict = dto.HasConflictOfInterest,
                    ConflictDescription = dto.ConflictOfInterestDetails,
                    SignatureHash = signature,
                    DeclaredAt = declarationNow,
                    CreatedAt = declarationNow,
                    UpdatedAt = declarationNow
                };
                _db.ConflictOfInterestDeclarations.Add(declaration);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _auditService.LogAsync(AuditEventType.Create, nameof(ExpenseRequisition), created.Id.ToString(),
                userId, null, null, System.Text.Json.JsonSerializer.Serialize(new { created.Id, created.RequisitionNumber, created.Amount }), "Requisition created", created.Id, cancellationToken);

            return new ExpenseRequisitionDto
            {
                Id = created.Id,
                RequisitionNumber = created.RequisitionNumber,
                Title = created.Title,
                Description = created.Description,
                Amount = created.Amount,
                Category = created.Category.ToString(),
                Status = created.Status.ToString(),
                StatusBadgeClass = BadgeHelper.GetBadgeClass(created.Status),
                IsFlaggedForReview = created.IsFlaggedForReview,
                FlagReason = created.FlagReason,
                CreatedAt = created.CreatedAt,
                RequesterName = user.FullName,
                RequesterEmail = user.Email,
                DepartmentName = department.Name
            };
        }

        public async Task SubmitAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var req = await _requisitionRepo.GetByIdAsync(id, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId != userId) throw new UnauthorizedAccessException("You can only submit your own requisition.");
            if (req.Status != RequisitionStatus.Draft) throw new InvalidOperationException("Only draft requisitions can be submitted.");

            var oldStatus = req.Status;
            var now = DateTime.UtcNow;
            req.Status = req.IsFlaggedForReview ? RequisitionStatus.UnderReview : RequisitionStatus.Submitted;
            req.SubmittedAt = now;
            req.UpdatedAt = now;

            await _requisitionRepo.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(AuditEventType.StatusChange, nameof(ExpenseRequisition), id.ToString(),
                userId, null, oldStatus.ToString(), req.Status.ToString(), "Submitted for approval", id, cancellationToken);
        }

        public async Task ApproveAsync(int id, int approverId, string? notes, CancellationToken cancellationToken = default)
        {
            var req = await _requisitionRepo.GetByIdAsync(id, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId == approverId) throw new InvalidOperationException("You cannot approve your own requisition.");
            if (req.Status != RequisitionStatus.Submitted && req.Status != RequisitionStatus.UnderReview)
                throw new InvalidOperationException("Requisition cannot be approved at this stage.");

            // Authorization check: verify approver has permission to approve this requisition
            var canApprove = await CanUserApproveAsync(req, approverId, cancellationToken);
            if (!canApprove)
                throw new UnauthorizedAccessException("You are not authorized to approve this requisition at this stage.");

            // 3-way matching rule: amount above threshold requires at least 2 vendor quotations
            if (req.VendorId.HasValue && req.Amount >= GetMultipleQuotationThreshold())
            {
                var quotationCount = await _db.RequisitionQuotations.CountAsync(q => q.RequisitionId == id && !q.IsDeleted, cancellationToken);
                if (quotationCount < 2)
                    throw new InvalidOperationException(
                        $"Requisitions of {GetMultipleQuotationThreshold():N0} IDR or more require at least 2 vendor quotations before approval.");
            }

            var oldStatus = req.Status;
            var now = DateTime.UtcNow;
            req.Status = RequisitionStatus.Approved;
            req.ApproverId = approverId;
            req.ApprovedAt = now;
            req.ApprovalNotes = notes;
            req.UpdatedAt = now;

            await _requisitionRepo.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Record price history so future fraud-detection benchmarking has data
            if (req.VendorId.HasValue)
            {
                var priceHistory = new PriceHistory
                {
                    VendorId = req.VendorId.Value,
                    RequisitionId = req.Id,
                    ItemDescription = req.Title,
                    Price = req.Amount,
                    PurchaseDate = now,
                    CreatedAt = now,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.PriceHistories.Add(priceHistory);
                await _db.SaveChangesAsync(cancellationToken);
            }

            await _auditService.LogAsync(AuditEventType.Approve, nameof(ExpenseRequisition), id.ToString(),
                approverId, null, oldStatus.ToString(), req.Status.ToString(), notes, id, cancellationToken);
        }

        public async Task MarkAsPaidAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var req = await _requisitionRepo.GetByIdAsync(id, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.Status != RequisitionStatus.Approved)
                throw new InvalidOperationException("Only approved requisitions can be marked as paid.");

            // Authorization check: only Finance and Admin can mark as paid
            var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
            if (user == null || (user.Role != UserRole.Finance && user.Role != UserRole.Admin))
                throw new UnauthorizedAccessException("Only Finance and Admin users can mark requisitions as paid.");

            var oldStatus = req.Status;
            var now = DateTime.UtcNow;
            req.Status = RequisitionStatus.Paid;
            req.PaidAt = now;
            req.UpdatedAt = now;

            await _requisitionRepo.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(AuditEventType.StatusChange, nameof(ExpenseRequisition), id.ToString(),
                userId, null, oldStatus.ToString(), req.Status.ToString(), "Marked as paid", id, cancellationToken);
        }

        public async Task<List<QuotationDto>> GetQuotationsAsync(int requisitionId, CancellationToken cancellationToken = default)
        {
            var quotations = await _db.RequisitionQuotations
                .Include(q => q.Vendor)
                .Where(q => q.RequisitionId == requisitionId && !q.IsDeleted)
                .OrderBy(q => q.QuotationDate)
                .Select(q => new QuotationDto
                {
                    Id = q.Id,
                    VendorId = q.VendorId,
                    VendorName = q.Vendor != null ? q.Vendor.Name : $"Vendor #{q.VendorId}",
                    QuotedPrice = q.QuotedPrice,
                    QuotationDate = q.QuotationDate,
                    IsSelected = q.IsSelected,
                    SelectionReason = q.SelectionReason
                })
                .ToListAsync(cancellationToken);
            return quotations;
        }

        public async Task AddQuotationAsync(AddQuotationDto dto, int userId, CancellationToken cancellationToken = default)
        {
            if (dto.VendorId <= 0) throw new InvalidOperationException("Please select a vendor for the quotation.");
            if (dto.QuotedPrice <= 0) throw new InvalidOperationException("Quoted price must be greater than zero.");

            var req = await _requisitionRepo.GetByIdAsync(dto.RequisitionId, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Paid ||
                req.Status == RequisitionStatus.Rejected || req.Status == RequisitionStatus.Cancelled)
                throw new InvalidOperationException("Quotations can no longer be added at this stage.");

            var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == dto.VendorId && !v.IsDeleted, cancellationToken);
            if (vendor == null) throw new InvalidOperationException("Vendor not found.");

            var duplicate = await _db.RequisitionQuotations.AnyAsync(
                q => q.RequisitionId == dto.RequisitionId && q.VendorId == dto.VendorId && !q.IsDeleted, cancellationToken);
            if (duplicate) throw new InvalidOperationException("A quotation for this vendor already exists.");

            var now = DateTime.UtcNow;
            var quotation = new RequisitionQuotation
            {
                RequisitionId = dto.RequisitionId,
                VendorId = dto.VendorId,
                QuotedPrice = dto.QuotedPrice,
                QuotationDate = dto.QuotationDate == default ? DateTime.UtcNow.Date : dto.QuotationDate,
                IsSelected = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.RequisitionQuotations.Add(quotation);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(AuditEventType.Create, nameof(RequisitionQuotation), quotation.Id.ToString(),
                userId, null, null, System.Text.Json.JsonSerializer.Serialize(new { quotation.Id, quotation.VendorId, quotation.QuotedPrice }),
                "Quotation added", dto.RequisitionId, cancellationToken);
        }

        public async Task RemoveQuotationAsync(int quotationId, int userId, CancellationToken cancellationToken = default)
        {
            var quotation = await _db.RequisitionQuotations.FirstOrDefaultAsync(q => q.Id == quotationId && !q.IsDeleted, cancellationToken);
            if (quotation == null) throw new InvalidOperationException("Quotation not found.");

            var req = await _requisitionRepo.GetByIdAsync(quotation.RequisitionId, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Paid)
                throw new InvalidOperationException("Quotations cannot be removed after the requisition is approved.");

            _db.RequisitionQuotations.Remove(quotation);
            await _db.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(AuditEventType.Delete, nameof(RequisitionQuotation), quotationId.ToString(),
                userId, null, null, null, "Quotation removed", req.Id, cancellationToken);
        }

        public async Task SelectQuotationAsync(int requisitionId, int quotationId, string? reason, int userId, CancellationToken cancellationToken = default)
        {
            var req = await _requisitionRepo.GetByIdAsync(requisitionId, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Paid ||
                req.Status == RequisitionStatus.Rejected || req.Status == RequisitionStatus.Cancelled)
                throw new InvalidOperationException("Quotation can no longer be selected at this stage.");

            var quotation = await _db.RequisitionQuotations.FirstOrDefaultAsync(q => q.Id == quotationId && q.RequisitionId == requisitionId && !q.IsDeleted, cancellationToken);
            if (quotation == null) throw new InvalidOperationException("Quotation not found.");

            await _db.RequisitionQuotations
                .Where(q => q.RequisitionId == requisitionId)
                .ExecuteUpdateAsync(s => s.SetProperty(q => q.IsSelected, false), cancellationToken);

            quotation.IsSelected = true;
            quotation.SelectionReason = reason;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(AuditEventType.Update, nameof(RequisitionQuotation), quotationId.ToString(),
                userId, null, null, null, $"Quotation selected{(string.IsNullOrWhiteSpace(reason) ? "" : $": {reason}")}", requisitionId, cancellationToken);
        }

        public async Task RejectAsync(int id, int approverId, string reason, CancellationToken cancellationToken = default)
        {
            var req = await _requisitionRepo.GetByIdAsync(id, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId == approverId) throw new InvalidOperationException("You cannot reject your own requisition.");
            if (req.Status != RequisitionStatus.Submitted && req.Status != RequisitionStatus.UnderReview)
                throw new InvalidOperationException("Requisition cannot be rejected at this stage.");

            // Authorization check
            var canApprove = await CanUserApproveAsync(req, approverId, cancellationToken);
            if (!canApprove)
                throw new UnauthorizedAccessException("You are not authorized to reject this requisition.");

            var oldStatus = req.Status;
            var now = DateTime.UtcNow;
            req.Status = RequisitionStatus.Rejected;
            req.ApproverId = approverId;
            req.RejectedAt = now;
            req.RejectionReason = reason;
            req.UpdatedAt = now;

            await _requisitionRepo.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(AuditEventType.Reject, nameof(ExpenseRequisition), id.ToString(),
                approverId, null, oldStatus.ToString(), req.Status.ToString(), reason, id, cancellationToken);
        }

        public async Task CancelAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var req = await _requisitionRepo.GetByIdAsync(id, cancellationToken);
            if (req == null || req.IsDeleted) throw new InvalidOperationException("Requisition not found.");
            if (req.RequesterId != userId) throw new UnauthorizedAccessException("You can only cancel your own requisition.");
            if (req.Status == RequisitionStatus.Approved || req.Status == RequisitionStatus.Paid)
                throw new InvalidOperationException("Cannot cancel an approved or paid requisition.");
            if (req.Status == RequisitionStatus.Submitted || req.Status == RequisitionStatus.UnderReview)
                throw new InvalidOperationException("Cannot cancel a requisition that is already submitted for approval. Contact your approver to reject it.");

            var oldStatus = req.Status;
            req.Status = RequisitionStatus.Cancelled;
            req.UpdatedAt = DateTime.UtcNow;

            await _requisitionRepo.UpdateAsync(req, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(AuditEventType.StatusChange, nameof(ExpenseRequisition), id.ToString(),
                userId, null, oldStatus.ToString(), req.Status.ToString(), "Cancelled by requester", id, cancellationToken);
        }

        private async Task<string> GenerateRequisitionNumberAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var yearMonth = $"{now.Year}{now.Month:D2}";
            const int maxRetries = 3;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Use SELECT FOR UPDATE equivalent with EF Core - lock the row for update
                    var sequence = await _db.NumberSequences
                        .FromSqlRaw(
                            "SELECT * FROM NumberSequences WITH (UPDLOCK, ROWLOCK) WHERE SequenceName = {0} AND YearMonth = {1}",
                            "ExpenseRequisition", yearMonth)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (sequence == null)
                    {
                        var seqNow = DateTime.UtcNow;
                        sequence = new NumberSequence
                        {
                            SequenceName = "ExpenseRequisition",
                            Prefix = "EXP",
                            YearMonth = yearMonth,
                            CurrentValue = 0,
                            IncrementBy = 1,
                            MinValue = 1,
                            MaxValue = 9999,
                            Cycle = false,
                            CreatedAt = seqNow,
                            UpdatedAt = seqNow
                        };
                        _db.NumberSequences.Add(sequence);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }

                    // Increment the sequence
                    var nextValue = sequence.CurrentValue + sequence.IncrementBy;
                    if (nextValue > sequence.MaxValue && !sequence.Cycle)
                    {
                        throw new InvalidOperationException($"Sequence {sequence.SequenceName} for {yearMonth} has reached maximum value.");
                    }

                    sequence.CurrentValue = nextValue;
                    var updateNow = DateTime.UtcNow;
                    sequence.LastUsedAt = updateNow;
                    sequence.UpdatedAt = updateNow;

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return $"{sequence.Prefix}-{yearMonth}-{sequence.CurrentValue:D4}";
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    // Retry on concurrency conflict
                    await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
                }
                catch (Exception)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    throw;
                }
            }

            throw new InvalidOperationException("Failed to generate requisition number after maximum retries due to concurrency conflicts.");
        }

        private async Task<bool> CanUserApproveAsync(ExpenseRequisition req, int approverId, CancellationToken cancellationToken = default)
        {
            var approver = await _userRepo.GetByIdAsync(approverId, cancellationToken);
            if (approver == null || !approver.IsActive) return false;

            var role = approver.Role;

            // Admin can approve anything
            if (role == UserRole.Admin) return true;

            // Finance can approve at L2 (Finance approval stage)
            if (role == UserRole.Finance)
            {
                // Finance can approve if it's at Finance approval stage
                // For now, allow Finance to approve any submitted/under review requisition
                // In a more complex workflow, you'd check CurrentApprovalLevel
                return req.Status == RequisitionStatus.Submitted || req.Status == RequisitionStatus.UnderReview;
            }

            // Manager can approve if they are the manager of the requester or department head
            if (role == UserRole.Manager)
            {
                var requester = await _userRepo.GetByIdAsync(req.RequesterId, cancellationToken);
                if (requester == null) return false;

                // Check if approver is the direct manager of the requester
                if (requester.ManagerId == approverId) return true;

                // Check if approver is the department head
                if (req.DepartmentId == approver.DepartmentId)
                {
                    var dept = await _departmentRepo.GetByIdAsync(req.DepartmentId, cancellationToken);
                    if (dept?.ManagerId == approverId) return true;
                }

                // Check if approver manages the requester's department (for sub-departments)
                if (requester.DepartmentId.HasValue)
                {
                    var requesterDept = await _departmentRepo.GetByIdAsync(requester.DepartmentId.Value, cancellationToken);
                    if (requesterDept != null)
                    {
                        // Check if approver is manager of any parent department
                        var currentDept = requesterDept;
                        while (currentDept.ParentDepartmentId.HasValue)
                        {
                            var parentDept = await _departmentRepo.GetByIdAsync(currentDept.ParentDepartmentId.Value, cancellationToken);
                            if (parentDept == null) break;
                            if (parentDept.ManagerId == approverId) return true;
                            currentDept = parentDept;
                        }
                    }
                }

                return false;
            }

            return false;
        }

        private decimal GetMultipleQuotationThreshold()
        {
            if (decimal.TryParse(_configuration["AppSettings:MultipleQuotationThreshold"], out var threshold) && threshold > 0)
                return threshold;
            return 50_000_000m;
        }
    }
}
