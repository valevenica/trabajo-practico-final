using System;
using System.Collections.Generic;
using System.Linq;
using StockManufactura.Application.Resources;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Services
{
    public static class TramoPrecioCalculator
    {
        public static TramoCalculationResult Calcular(IReadOnlyList<RecursoTramoPrecio> tramosOrdenados, decimal consumoTotal)
        {
            if (consumoTotal <= 0 || tramosOrdenados.Count == 0)
            {
                return new TramoCalculationResult
                {
                    ConsumoTotal = consumoTotal,
                    CostoTotal = 0,
                    PrecioPromedio = 0,
                    Detalle = Array.Empty<TramoCalculationDetalle>()
                };
            }

            var restante = consumoTotal;
            var pisoAnterior = 0m;
            var costoTotal = 0m;
            var detalles = new List<TramoCalculationDetalle>();
            RecursoTramoPrecio? ultimoTramo = null;

            foreach (var tramo in tramosOrdenados.OrderBy(x => x.Orden))
            {
                ultimoTramo = tramo;

                if (restante <= 0)
                {
                    detalles.Add(new TramoCalculationDetalle
                    {
                        Orden = tramo.Orden,
                        CantidadHasta = tramo.CantidadHasta,
                        PrecioPorUnidad = tramo.PrecioPorUnidad,
                        CantidadConsumida = 0,
                        Subtotal = 0
                    });
                    pisoAnterior = tramo.CantidadHasta ?? pisoAnterior;
                    continue;
                }

                var capacidad = tramo.CantidadHasta.HasValue
                    ? Math.Max(0, tramo.CantidadHasta.Value - pisoAnterior)
                    : restante;

                var consumidoEnTramo = Math.Min(restante, capacidad);
                var subtotal = consumidoEnTramo * tramo.PrecioPorUnidad;

                detalles.Add(new TramoCalculationDetalle
                {
                    Orden = tramo.Orden,
                    CantidadHasta = tramo.CantidadHasta,
                    PrecioPorUnidad = tramo.PrecioPorUnidad,
                    CantidadConsumida = consumidoEnTramo,
                    Subtotal = subtotal
                });

                costoTotal += subtotal;
                restante -= consumidoEnTramo;
                pisoAnterior = tramo.CantidadHasta ?? pisoAnterior;
            }

            if (restante > 0 && ultimoTramo is not null)
            {
                var subtotalExcedente = restante * ultimoTramo.PrecioPorUnidad;
                costoTotal += subtotalExcedente;

                var last = detalles[^1];
                detalles[^1] = new TramoCalculationDetalle
                {
                    Orden = last.Orden,
                    CantidadHasta = last.CantidadHasta,
                    PrecioPorUnidad = last.PrecioPorUnidad,
                    CantidadConsumida = last.CantidadConsumida + restante,
                    Subtotal = last.Subtotal + subtotalExcedente
                };
            }

            return new TramoCalculationResult
            {
                ConsumoTotal = consumoTotal,
                CostoTotal = costoTotal,
                PrecioPromedio = costoTotal / consumoTotal,
                Detalle = detalles
            };
        }
    }
}
