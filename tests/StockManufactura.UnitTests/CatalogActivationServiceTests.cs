#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.UnitTests.TestDoubles;
using Xunit;

namespace StockManufactura.UnitTests;

/// <summary>
/// Productos e insumos no se eliminan: se desactivan (quedan ocultos y fuera de las altas nuevas) y se pueden reactivar.
/// </summary>
public class CatalogActivationServiceTests
{
    [Fact]
    public async Task DesactivarProducto_LoConservaYLoSacaDeLosActivos()
    {
        var uow = new FakeUnitOfWork();
        var producto = new Producto { Codigo = "P1", Nombre = "Mesa" };
        uow.ProductosFake.Seed(producto);
        var audit = new RecordingAuditLogService();
        var service = new CatalogActivationService(uow, audit);

        var result = await service.SetProductoActivoAsync(producto.Id, false, "admin", "PC");

        Assert.False(result.Activo);
        Assert.Contains(producto, uow.ProductosFake.Items);
        Assert.Empty(await uow.Productos.ListActivosAsync());
        Assert.True(uow.SaveCalled);
        Assert.Equal("Desactivar", Assert.Single(audit.Logs).Accion);
    }

    [Fact]
    public async Task ReactivarInsumo_VuelveAListarlo()
    {
        var uow = new FakeUnitOfWork();
        var recurso = new Recurso { Codigo = "R1", Nombre = "Madera", Activo = false };
        uow.RecursosFake.Seed(recurso);
        var audit = new RecordingAuditLogService();
        var service = new CatalogActivationService(uow, audit);

        var result = await service.SetRecursoActivoAsync(recurso.Id, true, "admin", "PC");

        Assert.True(result.Activo);
        Assert.Single(await uow.Recursos.ListActivosAsync());
        Assert.Equal("Reactivar", Assert.Single(audit.Logs).Accion);
    }

    [Fact]
    public async Task DesactivarInsumoUsadoEnReceta_AvisaPeroNoLoBloquea()
    {
        var uow = new FakeUnitOfWork();
        var recurso = new Recurso { Codigo = "R1", Nombre = "Madera" };
        uow.RecursosFake.Seed(recurso);
        uow.RecetaProductoItemsFake.Seed(new RecetaProductoItem { ProductoId = Guid.NewGuid(), RecursoId = recurso.Id, Cantidad = 2m });
        var service = new CatalogActivationService(uow, new RecordingAuditLogService());

        var result = await service.SetRecursoActivoAsync(recurso.Id, false, "admin", "PC");

        Assert.False(recurso.Activo);
        Assert.Contains("1 receta", result.Mensaje);
    }

    [Fact]
    public async Task MismoEstado_NoGuardaNiAudita()
    {
        var uow = new FakeUnitOfWork();
        var producto = new Producto { Codigo = "P1", Nombre = "Mesa" };
        uow.ProductosFake.Seed(producto);
        var audit = new RecordingAuditLogService();
        var service = new CatalogActivationService(uow, audit);

        await service.SetProductoActivoAsync(producto.Id, true, "admin", "PC");

        Assert.False(uow.SaveCalled);
        Assert.Empty(audit.Logs);
    }

    private sealed class RecordingAuditLogService : IAuditLogService
    {
        public List<AuditLog> Logs { get; } = new();

        public Task RegisterAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            Logs.Add(log);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLog>> QueryAsync(DateTime? from, DateTime? to, string? usuario, string? modulo, string? accion, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AuditLog>>(Logs.ToArray());
    }
}
