using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Interfaces;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Infrastructure.Monetary.Providers
{
    public sealed class DolarHoyProvider : IExchangeRateProvider
    {
        public string Key => "dolar-hoy";
        public string DisplayName => "DolarHoy";

        public Task<ExchangeRate> GetCurrentRateAsync(string usuario, CancellationToken cancellationToken = default)
        {
            // Placeholder provider for first phase. Real integration will be added later.
            const string fallbackRate = "1300";
            var value = decimal.Parse(fallbackRate, CultureInfo.InvariantCulture);

            var result = new ExchangeRate
            {
                Valor = value,
                Fecha = DateTime.UtcNow,
                Fuente = DisplayName,
                Usuario = usuario,
                Automatica = true
            };

            return Task.FromResult(result);
        }
    }
}
