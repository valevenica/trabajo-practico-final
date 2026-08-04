using StockManufactura.Application.DTOs;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<IReadOnlyList<UsuarioDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Rol>> GetRolesAsync(CancellationToken cancellationToken = default);
        Task<UsuarioDto> CreateAsync(UpsertUsuarioRequest request, string plainPassword, string actor, CancellationToken cancellationToken = default);
        Task<UsuarioDto> UpdateAsync(Guid userId, UpsertUsuarioRequest request, string actor, CancellationToken cancellationToken = default);
        Task<UsuarioDto> SetActiveAsync(Guid userId, bool active, string actor, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string actor, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(Guid userId, string newPassword, string actor, CancellationToken cancellationToken = default);
    }
}
