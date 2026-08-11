using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public sealed class StockIntakeService : IStockIntakeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;

        public StockIntakeService(IUnitOfWork unitOfWork, IAuditLogService auditLogService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        }

        public async Task RegistrarIngresoAsync(IReadOnlyList<StockIntakeLineInput> lineas, string usuario, CancellationToken cancellationToken = default)
        {
            if (lineas is null || lineas.Count == 0)
            {
                throw new InvalidOperationException("Debe agregar al menos un ítem para registrar el ingreso.");
            }

            foreach (var linea in lineas)
            {
                if (linea.Cantidad <= 0)
                {
                    throw new InvalidOperationException("La cantidad debe ser mayor a cero.");
                }

                if (linea.EsProducto)
                {
                    await AplicarIngresoProductoAsync(linea, usuario, cancellationToken);
                }
                else
                {
                    await AplicarIngresoRecursoAsync(linea, usuario, cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        private async Task AplicarIngresoProductoAsync(StockIntakeLineInput linea, string usuario, CancellationToken cancellationToken)
        {
            var producto = await _unitOfWork.Productos.GetByIdAsync(linea.ItemId);
            if (producto is null)
            {
                throw new InvalidOperationException("Producto no encontrado.");
            }

            var recipeItems = await _unitOfWork.RecetaProductoItems.ListByProductIdAsync(producto.Id);
            var insumos = recipeItems.Where(x => x.RecursoId.HasValue).ToList();

            foreach (var item in insumos)
            {
                var recurso = item.Recurso ?? await _unitOfWork.Recursos.GetByIdAsync(item.RecursoId!.Value);
                if (recurso is null)
                {
                    throw new InvalidOperationException($"No se encontró el insumo {item.RecursoId} de la receta de {producto.Nombre}.");
                }

                if (recurso.EsServicio)
                {
                    continue;
                }

                var requerido = item.Cantidad * linea.Cantidad;
                if (recurso.StockActual < requerido)
                {
                    throw new InvalidOperationException(
                        $"Stock insuficiente de '{recurso.Nombre}' para producir {linea.Cantidad:0.##} de '{producto.Nombre}'. Requerido: {requerido:0.##}, disponible: {recurso.StockActual:0.##}.");
                }
            }

            foreach (var item in insumos)
            {
                var recurso = item.Recurso ?? await _unitOfWork.Recursos.GetByIdAsync(item.RecursoId!.Value);
                if (recurso is null || recurso.EsServicio)
                {
                    continue;
                }

                recurso.StockActual -= item.Cantidad * linea.Cantidad;
                recurso.FechaUltimaActualizacion = DateTime.UtcNow;
                _unitOfWork.Recursos.Update(recurso);
            }

            producto.StockActual += linea.Cantidad;
            _unitOfWork.Productos.Update(producto);

            await _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = usuario,
                Modulo = "Stock",
                Accion = "Ingreso",
                Entidad = "Producto",
                IdEntidad = producto.Id.ToString(),
                Descripcion = $"Ingreso de {linea.Cantidad:0.##} unidades de {producto.Nombre} (insumos descontados según receta)",
                Equipo = Environment.MachineName
            }, cancellationToken);
        }

        private async Task AplicarIngresoRecursoAsync(StockIntakeLineInput linea, string usuario, CancellationToken cancellationToken)
        {
            var recurso = await _unitOfWork.Recursos.GetByIdAsync(linea.ItemId);
            if (recurso is null)
            {
                throw new InvalidOperationException("Insumo no encontrado.");
            }

            if (recurso.EsServicio)
            {
                throw new InvalidOperationException($"'{recurso.Nombre}' es un servicio y no maneja stock físico.");
            }

            recurso.StockActual += linea.Cantidad;
            recurso.FechaUltimaActualizacion = DateTime.UtcNow;
            _unitOfWork.Recursos.Update(recurso);

            await _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = usuario,
                Modulo = "Stock",
                Accion = "Ingreso",
                Entidad = "Recurso",
                IdEntidad = recurso.Id.ToString(),
                Descripcion = $"Ingreso de {linea.Cantidad:0.##} {recurso.UnidadMedida} de {recurso.Nombre}",
                Equipo = Environment.MachineName
            }, cancellationToken);
        }
    }
}
