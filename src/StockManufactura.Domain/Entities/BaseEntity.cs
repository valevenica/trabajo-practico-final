using System;

namespace StockManufactura.Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
        public bool IsDeleted { get; protected set; }

        public void MarkAsDeleted() => IsDeleted = true;

        public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;
    }
}
