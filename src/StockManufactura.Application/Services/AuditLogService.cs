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
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AuditLogService(IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
        {
            _auditLogRepository = auditLogRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            await _auditLogRepository.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }

        public Task<IReadOnlyList<AuditLog>> QueryAsync(DateTime? from, DateTime? to, string? usuario, string? modulo, string? accion, CancellationToken cancellationToken = default)
        {
            return _auditLogRepository.QueryAsync(from, to, usuario, modulo, accion);
        }
    }
}
