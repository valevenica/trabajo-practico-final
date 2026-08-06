using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Monetary.Providers
{
    public sealed class DolarHoyProvider : IExchangeRateProvider
    {
        private static readonly HttpClient HttpClient = new();

        public string Key => "dolar-hoy";
        public string DisplayName => "DolarHoy (Blue)";

        public async Task<ExchangeRate> GetCurrentRateAsync(string usuario, CancellationToken cancellationToken = default)
        {
            using var response = await HttpClient.GetAsync("https://dolarapi.com/v1/dolares/blue", cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            decimal value;
            if (json.RootElement.TryGetProperty("venta", out var ventaEl) && ventaEl.TryGetDecimal(out var venta))
            {
                value = venta;
            }
            else if (json.RootElement.TryGetProperty("compra", out var compraEl) && compraEl.TryGetDecimal(out var compra))
            {
                value = compra;
            }
            else
            {
                throw new InvalidOperationException("Respuesta inv\u00e1lida de dolarapi.com para d\u00f3lar blue.");
            }

            return new ExchangeRate
            {
                Valor = value,
                Fecha = DateTime.UtcNow,
                Fuente = DisplayName,
                Usuario = usuario,
                Automatica = true
            };
        }
    }
}
