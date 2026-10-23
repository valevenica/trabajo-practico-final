using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Email;

namespace StockManufactura.Application.Interfaces
{
    public interface IEmailSettingsStore
    {
        Task<EmailSettings> LoadAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(EmailSettings settings, CancellationToken cancellationToken = default);
    }
}
