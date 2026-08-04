using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.DTOs;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> AuthenticateDetailedAsync(string userOrEmail, string password, CancellationToken cancellationToken = default);
        Task<Usuario?> AuthenticateAsync(string userOrEmail, string password, CancellationToken cancellationToken = default);
        Task<Usuario> RegisterLoginAsync(Usuario usuario, CancellationToken cancellationToken = default);
    }
}
