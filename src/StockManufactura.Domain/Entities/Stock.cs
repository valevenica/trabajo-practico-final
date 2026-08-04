using System;

namespace StockManufactura.Domain.Entities
{
    public sealed class Stock : BaseEntity
    {
        public Guid ProductoId { get; private set; }
        public Producto? Producto { get; private set; }
        public Guid UbicacionId { get; private set; }
        public decimal CantidadDisponible { get; private set; }
        public decimal CantidadReservada { get; private set; }
        public DateTime UltimaActualizacion { get; private set; }

        private Stock()
        {
        }

        public Stock(Guid productoId, Guid ubicacionId, decimal cantidadDisponible, decimal cantidadReservada)
        {
            ProductoId = productoId;
            UbicacionId = ubicacionId;
            CantidadDisponible = cantidadDisponible;
            CantidadReservada = cantidadReservada;
            UltimaActualizacion = DateTime.UtcNow;
        }

        public void RegistrarEntrada(decimal cantidad)
        {
            if (cantidad <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
            }

            CantidadDisponible += cantidad;
            UltimaActualizacion = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void RegistrarSalida(decimal cantidad)
        {
            if (cantidad <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
            }

            if (cantidad > CantidadDisponible)
            {
                throw new InvalidOperationException("No hay suficiente stock disponible para la salida solicitada.");
            }

            CantidadDisponible -= cantidad;
            UltimaActualizacion = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void Reservar(decimal cantidad)
        {
            if (cantidad <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
            }

            if (cantidad > CantidadDisponible)
            {
                throw new InvalidOperationException("No hay suficiente stock disponible para reservar.");
            }

            CantidadDisponible -= cantidad;
            CantidadReservada += cantidad;
            UltimaActualizacion = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void LiberarReserva(decimal cantidad)
        {
            if (cantidad <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");
            }

            if (cantidad > CantidadReservada)
            {
                throw new InvalidOperationException("La cantidad a liberar no puede exceder la reserva actual.");
            }

            CantidadReservada -= cantidad;
            CantidadDisponible += cantidad;
            UltimaActualizacion = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
