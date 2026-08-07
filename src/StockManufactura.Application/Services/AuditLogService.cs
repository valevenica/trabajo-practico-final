using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.AuditLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        public Task<IReadOnlyList<AuditLog>> QueryAsync(DateTime? from, DateTime? to, string? usuario, string? modulo, string? accion, CancellationToken cancellationToken = default)
        {
            return _unitOfWork.AuditLogs.QueryAsync(from, to, usuario, modulo, accion);
        }
    }
}
