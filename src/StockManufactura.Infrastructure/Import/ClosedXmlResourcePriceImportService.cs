using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Import
{
    public sealed class ClosedXmlResourcePriceImportService : IResourcePriceImportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IResourcePricingService _resourcePricingService;

        public ClosedXmlResourcePriceImportService(IUnitOfWork unitOfWork, IResourcePricingService resourcePricingService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _resourcePricingService = resourcePricingService ?? throw new ArgumentNullException(nameof(resourcePricingService));
        }

        public async Task<IReadOnlyList<ResourcePriceImportRow>> ParsearAsync(Stream excelStream, Guid proveedorId, CancellationToken cancellationToken = default)
        {
            var resultado = new List<ResourcePriceImportRow>();

            using var workbook = new XLWorkbook(excelStream);
            var hoja = workbook.Worksheets.First();
            var filasUsadas = hoja.RowsUsed().Skip(1);

            foreach (var fila in filasUsadas)
            {
                var codigo = fila.Cell(1).GetString().Trim();
                if (string.IsNullOrEmpty(codigo))
                {
                    continue;
                }

                var precioTexto = fila.Cell(2).GetString().Trim();
                var row = new ResourcePriceImportRow
                {
                    Fila = fila.RowNumber(),
                    CodigoProveedor = codigo,
                    PrecioTexto = precioTexto
                };

                if (!TryParsePrecio(fila.Cell(2), precioTexto, out var precio) || precio < 0)
                {
                    row.Status = ResourcePriceImportRowStatus.PrecioInvalido;
                    row.Mensaje = "Precio inválido.";
                    resultado.Add(row);
                    continue;
                }

                row.Precio = precio;

                var recursoProveedor = await _unitOfWork.RecursoProveedores.GetByProveedorAndCodigoAsync(proveedorId, codigo);
                if (recursoProveedor is null)
                {
                    row.Status = ResourcePriceImportRowStatus.NoEncontrado;
                    row.Mensaje = "No hay ningún insumo con ese código para el proveedor seleccionado.";
                    resultado.Add(row);
                    continue;
                }

                row.RecursoProveedorId = recursoProveedor.Id;
                row.RecursoId = recursoProveedor.RecursoId;
                row.RecursoNombre = recursoProveedor.Recurso.Nombre;
                row.PrecioActual = recursoProveedor.Precio;

                if (recursoProveedor.Precio == precio)
                {
                    row.Status = ResourcePriceImportRowStatus.SinCambios;
                    row.Mensaje = "El precio no cambió.";
                }
                else
                {
                    row.Status = ResourcePriceImportRowStatus.Ok;
                }

                resultado.Add(row);
            }

            return resultado;
        }

        public async Task<ResourcePriceImportResult> AplicarAsync(IReadOnlyList<ResourcePriceImportRow> filas, string usuario, CancellationToken cancellationToken = default)
        {
            var result = new ResourcePriceImportResult();

            foreach (var fila in filas.Where(x => x.Status == ResourcePriceImportRowStatus.Ok))
            {
                try
                {
                    if (!fila.RecursoProveedorId.HasValue || !fila.Precio.HasValue)
                    {
                        result.Omitidos++;
                        continue;
                    }

                    var recursoProveedor = await _unitOfWork.RecursoProveedores.GetByIdAsync(fila.RecursoProveedorId.Value);
                    if (recursoProveedor is null)
                    {
                        result.Omitidos++;
                        continue;
                    }

                    recursoProveedor.Precio = fila.Precio.Value;
                    _unitOfWork.RecursoProveedores.Update(recursoProveedor);
                    await _unitOfWork.SaveChangesAsync();

                    if (recursoProveedor.EsPrioritario)
                    {
                        var recurso = await _unitOfWork.Recursos.GetByIdAsync(recursoProveedor.RecursoId);
                        if (recurso is not null)
                        {
                            var request = new ResourceUpsertRequest
                            {
                                ResourceId = recurso.Id,
                                Codigo = recurso.Codigo,
                                Nombre = recurso.Nombre,
                                Descripcion = recurso.Descripcion,
                                Categoria = recurso.Categoria,
                                UnidadMedida = recurso.UnidadMedida,
                                EsServicio = recurso.EsServicio,
                                StockActual = recurso.StockActual,
                                StockMinimo = recurso.StockMinimo,
                                Precio = fila.Precio.Value,
                                Moneda = recurso.Moneda,
                                ProveedorHabitualId = recurso.ProveedorHabitualId,
                                MotivoCambio = "Carga masiva de precios (Excel)",
                                Observaciones = recurso.Observaciones,
                                Activo = recurso.Activo,
                                Usuario = usuario
                            };
                            await _resourcePricingService.UpsertResourceAsync(request, cancellationToken);
                        }
                    }

                    result.Actualizados++;
                }
                catch
                {
                    result.Omitidos++;
                }
            }

            return result;
        }

        private static bool TryParsePrecio(IXLCell cell, string texto, out decimal precio)
        {
            if (cell.TryGetValue(out precio))
            {
                return true;
            }

            return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out precio)
                || decimal.TryParse(texto, NumberStyles.Number, CultureInfo.CurrentCulture, out precio);
        }
    }
}
