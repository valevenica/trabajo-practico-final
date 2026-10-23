namespace StockManufactura.Application.Email
{
    /// <summary>
    /// Cuenta de correo saliente (SMTP) con la que el sistema envia los codigos de recuperacion de contraseña.
    /// </summary>
    public sealed class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Puerto { get; set; } = 587;
        public bool UsarSsl { get; set; } = true;
        public string Usuario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Remitente { get; set; } = string.Empty;
        public string NombreRemitente { get; set; } = "Integra Manufacturing";

        public bool EstaCompleta =>
            !string.IsNullOrWhiteSpace(Host)
            && Puerto > 0
            && !string.IsNullOrWhiteSpace(Remitente);
    }
}
