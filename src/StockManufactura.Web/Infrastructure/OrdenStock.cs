using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Web.Infrastructure
{
    /// <summary>
    /// Movimiento de stock de producto terminado que acompaña a un cambio de estado de una orden. Lo usan la pantalla de
    /// órdenes y el panel principal, para que avanzar un pedido desde cualquiera de los dos tenga el mismo efecto.
    /// </summary>
    public static class OrdenStock
    {
        public static async Task AplicarMovimientoAsync(IUnitOfWork unitOfWork, EstadoOrdenProduccion anterior, OrdenProduccion orden)
        {
            var movimiento = OrdenProduccion.MovimientoDeStock(anterior, orden.Estado);
            if (movimiento == 0) { return; }

            var producto = await unitOfWork.Productos.GetByIdAsync(orden.ProductoId)
                ?? throw new InvalidOperationException("No se encontró el producto de la orden.");
            if (movimiento < 0)
            {
                if (producto.StockActual < orden.CantidadPlaneada)
                {
                    throw new InvalidOperationException($"Stock insuficiente de {producto.Nombre} para completar el pedido. Faltan {orden.CantidadPlaneada - producto.StockActual:0.##} unidades.");
                }

                producto.StockActual -= orden.CantidadPlaneada;
            }
            else
            {
                producto.StockActual += orden.CantidadPlaneada;
            }

            unitOfWork.Productos.Update(producto);
        }
    }
}
