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
            Estado = EstadoOrdenProduccion.Borrador;
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

        public void Planificar()
        {
            if (Estado == EstadoOrdenProduccion.Finalizada || Estado == EstadoOrdenProduccion.Cancelada)
            {
                throw new InvalidOperationException("No se puede planificar una orden finalizada o cancelada.");
            }

            Estado = EstadoOrdenProduccion.Planificada;
            UpdateTimestamp();
        }

        public void MarcarEnProgreso()
        {
            if (Estado == EstadoOrdenProduccion.Finalizada)
            {
                throw new InvalidOperationException("No se puede reabrir una orden completada.");
            }

            if (Estado == EstadoOrdenProduccion.Cancelada)
            {
                throw new InvalidOperationException("No se puede iniciar una orden cancelada.");
            }

            Estado = EstadoOrdenProduccion.EnProceso;
            FechaInicio ??= DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void RegistrarProduccion(decimal cantidad)
        {
            if (Estado == EstadoOrdenProduccion.Finalizada)
            {
                throw new InvalidOperationException("No se puede registrar producción en una orden completada.");
            }

            if (Estado == EstadoOrdenProduccion.Cancelada)
            {
                throw new InvalidOperationException("No se puede registrar producción en una orden cancelada.");
            }

            if (Estado == EstadoOrdenProduccion.Borrador || Estado == EstadoOrdenProduccion.Planificada)
            {
                MarcarEnProgreso();
            }

            CantidadProducida = cantidad;
            UpdateTimestamp();
        }

        public void Completar()
        {
            if (Estado == EstadoOrdenProduccion.Cancelada)
            {
                throw new InvalidOperationException("No se puede completar una orden cancelada.");
            }

            if (Estado == EstadoOrdenProduccion.Borrador || Estado == EstadoOrdenProduccion.Planificada)
            {
                MarcarEnProgreso();
            }

            Estado = EstadoOrdenProduccion.Finalizada;
            FechaFin ??= DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Cancelar(string motivo)
        {
            if (Estado == EstadoOrdenProduccion.Finalizada)
            {
                throw new InvalidOperationException("No se puede cancelar una orden finalizada.");
            }

            Estado = EstadoOrdenProduccion.Cancelada;
            if (!string.IsNullOrWhiteSpace(motivo))
            {
                Observaciones = string.IsNullOrWhiteSpace(Observaciones)
                    ? motivo.Trim()
                    : $"{Observaciones}{Environment.NewLine}[Cancelada] {motivo.Trim()}";
            }

            FechaFin ??= DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void VolverABorrador()
        {
            if (Estado == EstadoOrdenProduccion.Finalizada)
            {
                throw new InvalidOperationException("No se puede volver a borrador una orden finalizada.");
            }

            Estado = EstadoOrdenProduccion.Borrador;
            FechaInicio = null;
            FechaFin = null;
            UpdateTimestamp();
        }

        public void ActualizarObservaciones(string observaciones)
        {
            Observaciones = observaciones ?? string.Empty;
            UpdateTimestamp();
        }
    }

    public enum EstadoOrdenProduccion
    {
        Borrador = 1,
        Planificada = 2,
        EnProceso = 3,
        Finalizada = 4,
        Cancelada = 5,
        Pendiente = Borrador,
        Completada = Finalizada
    }
}
