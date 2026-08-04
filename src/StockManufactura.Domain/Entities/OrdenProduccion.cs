using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class OrdenProduccion : BaseEntity
    {
        public OrdenProduccion(string codigo, Guid productoId, decimal cantidadPlaneada, string observaciones)
        {
            Codigo = codigo;
            ProductoId = productoId;
            CantidadPlaneada = cantidadPlaneada;
            Observaciones = observaciones;
            Estado = EstadoOrdenProduccion.Pendiente;
        }

        private OrdenProduccion() { }

        public string Codigo { get; private set; } = string.Empty;
        public Guid ProductoId { get; private set; }
        public decimal CantidadPlaneada { get; private set; }
        public decimal CantidadProducida { get; private set; }
        public EstadoOrdenProduccion Estado { get; private set; }
        public string Observaciones { get; private set; } = string.Empty;
        public DateTime? FechaInicio { get; private set; }
        public DateTime? FechaFin { get; private set; }

        public void MarcarEnProgreso()
        {
            if (Estado == EstadoOrdenProduccion.Completada)
            {
                throw new InvalidOperationException("No se puede reabrir una orden completada.");
            }

            Estado = EstadoOrdenProduccion.EnProceso;
            FechaInicio ??= DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void RegistrarProduccion(decimal cantidad)
        {
            if (Estado == EstadoOrdenProduccion.Completada)
            {
                throw new InvalidOperationException("No se puede registrar producción en una orden completada.");
            }

            if (Estado == EstadoOrdenProduccion.Pendiente)
            {
                MarcarEnProgreso();
            }

            CantidadProducida = cantidad;
            UpdateTimestamp();
        }

        public void Completar()
        {
            if (Estado == EstadoOrdenProduccion.Pendiente)
            {
                MarcarEnProgreso();
            }

            Estado = EstadoOrdenProduccion.Completada;
            FechaFin ??= DateTime.UtcNow;
            UpdateTimestamp();
        }
    }

    public enum EstadoOrdenProduccion
    {
        Pendiente = 1,
        EnProceso = 2,
        Completada = 3
    }
}
