using Zentic.Domain.Enums;

namespace Zentic.Domain.Entities
{
    /// <summary>
    /// Sequence entity for generating unique numbers with concurrency control.
    /// Uses database row-level locking to prevent race conditions.
    /// </summary>
    public class NumberSequence : BaseEntity
    {
        public string SequenceName { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public int CurrentValue { get; set; } = 0;
        public int IncrementBy { get; set; } = 1;
        public int MinValue { get; set; } = 1;
        public int MaxValue { get; set; } = 999999;
        public bool Cycle { get; set; } = false;
        public DateTime? LastUsedAt { get; set; }
        public string? YearMonth { get; set; } // For monthly sequences like EXP-202601-0001
    }
}