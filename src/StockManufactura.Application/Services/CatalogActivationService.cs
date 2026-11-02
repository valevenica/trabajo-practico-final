using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class CatalogActivationService : ICatalogActivationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;

        public CatalogActivationService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
        }

        public async Task<CatalogActivationResult> SetProductoActivoAsync(Guid productoId, bool activo, string usuario, string equipo, CancellationToken cancellationToken = default)
        {
            // Lectura sin seguimiento: la pantalla comparte el contexto y FindAsync devolvería su instancia seguida, que puede
            // tener el costo desactualizado (lo recalcula ProductCostService con otro contexto). Update suelta esa instancia.
            var producto = (await _unitOfWork.Productos.ListAsync()).FirstOrDefault(x => x.Id == productoId)
                ?? throw new InvalidOperationException("El producto ya no existe; recargue el listado.");

            if (producto.Activo == activo)
            {
                return new CatalogActivationResult(activo, activo ? "El producto ya estaba activo." : "El producto ya estaba desactivado.");
            }

            producto.Activo = activo;
            producto.UpdateTimestamp();
            _unitOfWork.Productos.Update(producto);
            await _unitOfWork.SaveChangesAsync();

            var mensaje = activo ? "Producto reactivado." : "Producto desactivado: ya no aparece en listados ni en nuevas órdenes.";
            if (!activo)
            {
                var recetas = await _unitOfWork.RecetaProductoItems.ListAsync();
                var usos = recetas.Count(x => x.ComponenteProductoId == productoId);
                if (usos > 0)
                {
                    mensaje += $" Sigue figurando como componente en {usos} receta(s).";
                }
            }

            await RegisterAuditAsync(usuario, equipo, "Productos", "Producto", producto.Id, activo, producto.Nombre);
            return new CatalogActivationResult(activo, mensaje);
        }

        public async Task<CatalogActivationResult> SetRecursoActivoAsync(Guid recursoId, bool activo, string usuario, string equipo, CancellationToken cancellationToken = default)
        {
            var recurso = (await _unitOfWork.Recursos.ListAsync()).FirstOrDefault(x => x.Id == recursoId)
                ?? throw new InvalidOperationException("El insumo ya no existe; recargue el listado.");

            if (recurso.Activo == activo)
            {
                return new CatalogActivationResult(activo, activo ? "El insumo ya estaba activo." : "El insumo ya estaba desactivado.");
            }

            recurso.Activo = activo;
            recurso.UpdateTimestamp();
            _unitOfWork.Recursos.Update(recurso);
            await _unitOfWork.SaveChangesAsync();

            var mensaje = activo ? "Insumo reactivado." : "Insumo desactivado: ya no aparece en listados, recetas nuevas ni ingresos de stock.";
            if (!activo)
            {
                var usos = await _unitOfWork.RecetaProductoItems.ListByResourceIdAsync(recursoId);
                if (usos.Count > 0)
                {
                    mensaje += $" Sigue figurando en {usos.Count} receta(s).";
                }
            }

            await RegisterAuditAsync(usuario, equipo, "Insumos", "Recurso", recurso.Id, activo, recurso.Nombre);
            return new CatalogActivationResult(activo, mensaje);
        }

        private Task RegisterAuditAsync(string usuario, string equipo, string modulo, string entidad, Guid id, bool activo, string nombre)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = usuario,
                Modulo = modulo,
                Accion = activo ? "Reactivar" : "Desactivar",
                Entidad = entidad,
                IdEntidad = id.ToString(),
                Descripcion = $"{(activo ? "Reactivado" : "Desactivado")}: {nombre}",
                Equipo = equipo
            });
        }
    }
}
