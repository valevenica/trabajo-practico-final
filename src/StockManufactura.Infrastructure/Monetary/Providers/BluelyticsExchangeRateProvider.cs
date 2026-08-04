using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Monetary.Providers
{
    public sealed class BluelyticsExchangeRateProvider : IExchangeRateProvider
    {
        private static readonly HttpClient HttpClient = new();

        public string Key => "bluelytics";
        public string DisplayName => "Bluelytics";

        public async Task<ExchangeRate> GetCurrentRateAsync(string usuario, CancellationToken cancellationToken = default)
        {
            using var response = await HttpClient.GetAsync("https://api.bluelytics.com.ar/v2/latest", cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            decimal value;
            if (json.RootElement.TryGetProperty("blue", out var blueElement) && blueElement.TryGetProperty("value_avg", out var valueElement))
            {
                value = valueElement.GetDecimal();
            }
            else if (json.RootElement.TryGetProperty("oficial", out var officialElement) && officialElement.TryGetProperty("value_sell", out var sellElement))
            {
                value = sellElement.GetDecimal();
            }
            else
            {
                throw new InvalidOperationException("Respuesta inválida de Bluelytics para cotización.");
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
