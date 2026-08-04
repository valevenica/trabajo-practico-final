using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Db;

namespace StockManufactura.Infrastructure.Repositories
{
    public sealed class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
    {
        private readonly StockManufacturaDbContext _context;

        public AuditLogRepository(StockManufacturaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<AuditLog>> QueryAsync(DateTime? from, DateTime? to, string? usuario, string? modulo, string? accion)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(x => x.FechaHora >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.FechaHora <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                query = query.Where(x => x.Usuario == usuario);
            }

            if (!string.IsNullOrWhiteSpace(modulo))
            {
                query = query.Where(x => x.Modulo == modulo);
            }

            if (!string.IsNullOrWhiteSpace(accion))
            {
                query = query.Where(x => x.Accion == accion);
            }

            return await query.OrderByDescending(x => x.FechaHora).ToListAsync();
        }
    }
}
