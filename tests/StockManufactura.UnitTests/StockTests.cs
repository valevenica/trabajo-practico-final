using System;
using StockManufactura.Domain.Entities;
using Xunit;

namespace StockManufactura.UnitTests;

public class StockTests
{
    [Fact]
    public void RegistrarEntrada_AumentaStockDisponibleYActualizaFecha()
    {
        var stock = new Stock(Guid.NewGuid(), Guid.NewGuid(), 10m, 2m);

        stock.RegistrarEntrada(5m);

        Assert.Equal(15m, stock.CantidadDisponible);
        Assert.Equal(7m, stock.CantidadReservada);
        Assert.True(stock.UltimaActualizacion > DateTime.UtcNow.AddMinutes(-1));
    }
}
