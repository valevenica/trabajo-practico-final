namespace StockManufactura.Domain.Entities
{
    public sealed class Rol : BaseEntity
    {
        public string Nombre { get; private set; } = string.Empty;
        public string Descripcion { get; private set; } = string.Empty;

        private Rol() { }

        public Rol(string nombre, string descripcion)
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }

        public void ActualizarDescripcion(string descripcion)
        {
            Descripcion = descripcion;
            UpdateTimestamp();
        }
    }
}
