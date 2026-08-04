using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IAuditLogService
    {
        Task RegisterAsync(AuditLog log, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AuditLog>> QueryAsync(DateTime? from, DateTime? to, string? usuario, string? modulo, string? accion, CancellationToken cancellationToken = default);
    }
}
