using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Email;

namespace StockManufactura.Application.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(EmailSettings settings, string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken = default);
    }
}
