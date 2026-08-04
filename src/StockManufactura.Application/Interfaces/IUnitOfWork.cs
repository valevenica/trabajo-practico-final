using System.Threading.Tasks;

namespace StockManufactura.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IRolRepository Roles { get; }
        IUsuarioRepository Usuarios { get; }
        Task<int> SaveChangesAsync();
    }
}
