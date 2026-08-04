using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<Usuario?> GetByEmailAsync(string email, bool includeRole = false);
        Task<Usuario?> GetByIdWithRoleAsync(Guid id);
        Task<IReadOnlyList<Usuario>> ListWithRoleAsync();
    }
}
