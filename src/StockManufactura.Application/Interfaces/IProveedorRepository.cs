using System.Threading.Tasks;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Application.Interfaces
{
    public interface IProveedorRepository : IRepository<Proveedor>
    {
        Task<Proveedor?> GetByCuitAsync(string cuit);
    }
}
