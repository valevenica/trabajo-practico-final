using System.Threading;
using System.Threading.Tasks;

namespace StockManufactura.Application.Interfaces
{
    /// <summary>
    /// Copia de la base de datos en uso. La base permanece abierta mientras la aplicación funciona, por lo que no puede
    /// copiarse ni reemplazarse como un archivo cualquiera: se usa el mecanismo de copia en línea del motor.
    /// </summary>
    public interface IDatabaseSnapshotService
    {
        /// <summary>Genera en <paramref name="destinationPath"/> una copia consistente de la base en uso.</summary>
        Task ExportAsync(string destinationPath, CancellationToken cancellationToken = default);

        /// <summary>Reemplaza el contenido de la base en uso por el de la base en <paramref name="sourcePath"/>.</summary>
        Task ImportAsync(string sourcePath, CancellationToken cancellationToken = default);
    }
}
