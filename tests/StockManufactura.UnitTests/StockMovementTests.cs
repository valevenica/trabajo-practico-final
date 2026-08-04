using System;
using StockManufactura.Domain.Entities;
using Xunit;

namespace StockManufactura.UnitTests;

public class StockMovementTests
{
    [Fact]
    public void StockPuedeRegistrarEntradaYSalidaConReserva()
    {
        var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), 10m, 0m);

        stock.RegistrarEntrada(5m);
        Assert.Equal(15m, stock.CantidadDisponible);

        stock.Reservar(4m);
        Assert.Equal(11m, stock.CantidadDisponible);
        Assert.Equal(4m, stock.CantidadReservada);

        stock.LiberarReserva(2m);
        Assert.Equal(13m, stock.CantidadDisponible);
        Assert.Equal(2m, stock.CantidadReservada);

        stock.RegistrarSalida(8m);
        Assert.Equal(5m, stock.CantidadDisponible);
    }
}
