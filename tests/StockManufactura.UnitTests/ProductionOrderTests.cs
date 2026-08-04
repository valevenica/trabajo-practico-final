using System;
using StockManufactura.Domain.Entities;
using Xunit;

namespace StockManufactura.UnitTests;

public class ProductionOrderTests
{
    [Fact]
    public void OrdenProduccion_CambiaEstadoYActualizaCantidadProducida()
    {
        var orden = new OrdenProduccion("OP-001", Guid.NewGuid(), 10m, "Orden de prueba");

        orden.MarcarEnProgreso();
        Assert.Equal(EstadoOrdenProduccion.EnProceso, orden.Estado);

        orden.RegistrarProduccion(8m);
        Assert.Equal(8m, orden.CantidadProducida);
        Assert.Equal(EstadoOrdenProduccion.EnProceso, orden.Estado);

        orden.Completar();
        Assert.Equal(EstadoOrdenProduccion.Completada, orden.Estado);
    }
}
