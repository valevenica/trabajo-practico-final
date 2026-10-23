using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Email;
using StockManufactura.Application.Interfaces;

namespace StockManufactura.Infrastructure.Email
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private const int TimeoutMilisegundos = 20000;

        public async Task SendAsync(EmailSettings settings, string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken = default)
        {
            using var client = new SmtpClient(settings.Host, settings.Puerto)
            {
                EnableSsl = settings.UsarSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = TimeoutMilisegundos
            };

            if (!string.IsNullOrWhiteSpace(settings.Usuario))
            {
                client.Credentials = new NetworkCredential(settings.Usuario, settings.Password);
            }

            using var mensaje = new MailMessage(
                new MailAddress(settings.Remitente, settings.NombreRemitente),
                new MailAddress(destinatario))
            {
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false
            };

            await client.SendMailAsync(mensaje, cancellationToken);
        }
    }
}
