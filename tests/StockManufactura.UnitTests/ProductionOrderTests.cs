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

    [Theory]
    [InlineData(EstadoOrdenProduccion.Borrador, EstadoOrdenProduccion.EnProceso, -1)]
    [InlineData(EstadoOrdenProduccion.Planificada, EstadoOrdenProduccion.EnProceso, -1)]
    [InlineData(EstadoOrdenProduccion.Planificada, EstadoOrdenProduccion.Finalizada, -1)]
    [InlineData(EstadoOrdenProduccion.EnProceso, EstadoOrdenProduccion.Planificada, 1)]
    [InlineData(EstadoOrdenProduccion.EnProceso, EstadoOrdenProduccion.Borrador, 1)]
    [InlineData(EstadoOrdenProduccion.EnProceso, EstadoOrdenProduccion.Cancelada, 1)]
    [InlineData(EstadoOrdenProduccion.EnProceso, EstadoOrdenProduccion.Finalizada, 0)]
    [InlineData(EstadoOrdenProduccion.Borrador, EstadoOrdenProduccion.Planificada, 0)]
    [InlineData(EstadoOrdenProduccion.Planificada, EstadoOrdenProduccion.Cancelada, 0)]
    [InlineData(EstadoOrdenProduccion.Cancelada, EstadoOrdenProduccion.Borrador, 0)]
    public void MovimientoDeStock_SegunElCambioDeEstado(EstadoOrdenProduccion anterior, EstadoOrdenProduccion nuevo, int esperado)
    {
        Assert.Equal(esperado, OrdenProduccion.MovimientoDeStock(anterior, nuevo));
    }

    [Fact]
    public void PlanificarUnaOrdenEnProceso_YVolverAIniciarla_DescuentaElStockUnaSolaVez()
    {
        var orden = new OrdenProduccion("OP-002", Guid.NewGuid(), 5m, string.Empty);
        var stock = 20m;
        void Aplicar(Action transicion)
        {
            var anterior = orden.Estado;
            transicion();
            stock += OrdenProduccion.MovimientoDeStock(anterior, orden.Estado) * orden.CantidadPlaneada;
        }

        Aplicar(orden.MarcarEnProgreso);
        Aplicar(orden.Planificar);
        Aplicar(orden.MarcarEnProgreso);

        Assert.Equal(15m, stock);
    }
}
