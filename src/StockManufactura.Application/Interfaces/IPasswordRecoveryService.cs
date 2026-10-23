using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Email;

namespace StockManufactura.Application.Interfaces
{
    public interface IPasswordRecoveryService
    {
        Task<EmailSettings> GetEmailSettingsAsync(CancellationToken cancellationToken = default);
        Task SaveEmailSettingsAsync(EmailSettings settings, string actor, CancellationToken cancellationToken = default);
        Task SendTestEmailAsync(string destinatario, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envia un codigo de un solo uso al correo del perfil del usuario. Si el correo no corresponde a un
        /// usuario activo no se envia nada, pero el resultado es el mismo, para no revelar que cuentas existen.
        /// </summary>
        Task RequestCodeAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida el codigo y reemplaza la contraseña; tambien desbloquea la cuenta. Devuelve el nombre de usuario,
        /// que es la credencial de ingreso.
        /// </summary>
        Task<string> ResetWithCodeAsync(string email, string codigo, string nuevaPassword, CancellationToken cancellationToken = default);
    }
}
