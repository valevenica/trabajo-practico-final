using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Email;
using StockManufactura.Application.Interfaces;
using StockManufactura.Shared;

namespace StockManufactura.Infrastructure.Email
{
    /// <summary>
    /// Persiste la cuenta de correo saliente en un archivo JSON junto a la base de datos. La contraseña se
    /// cifra con DPAPI (alcance del usuario de Windows), de modo que el archivo copiado a otro equipo o
    /// leido por otro usuario no expone la credencial.
    /// </summary>
    public sealed class JsonEmailSettingsStore : IEmailSettingsStore
    {
        private static readonly byte[] Entropia = Encoding.UTF8.GetBytes("StockManufactura.EmailSettings");
        private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

        private readonly string _path;

        public JsonEmailSettingsStore()
            : this(Path.Combine(AppPaths.DataDirectory, "correo.json"))
        {
        }

        public JsonEmailSettingsStore(string path)
        {
            _path = path;
        }

        public async Task<EmailSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_path))
            {
                return new EmailSettings();
            }

            await using var stream = File.OpenRead(_path);
            var archivo = await JsonSerializer.DeserializeAsync<Archivo>(stream, Opciones, cancellationToken) ?? new Archivo();
            return new EmailSettings
            {
                Host = archivo.Host,
                Puerto = archivo.Puerto,
                UsarSsl = archivo.UsarSsl,
                Usuario = archivo.Usuario,
                Password = Descifrar(archivo.PasswordCifrada),
                Remitente = archivo.Remitente,
                NombreRemitente = archivo.NombreRemitente
            };
        }

        public async Task SaveAsync(EmailSettings settings, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var archivo = new Archivo
            {
                Host = settings.Host.Trim(),
                Puerto = settings.Puerto,
                UsarSsl = settings.UsarSsl,
                Usuario = settings.Usuario.Trim(),
                PasswordCifrada = Cifrar(settings.Password),
                Remitente = settings.Remitente.Trim(),
                NombreRemitente = settings.NombreRemitente.Trim()
            };

            await using var stream = File.Create(_path);
            await JsonSerializer.SerializeAsync(stream, archivo, Opciones, cancellationToken);
        }

        private static string Cifrar(string valor)
        {
            if (string.IsNullOrEmpty(valor) || !OperatingSystem.IsWindows())
            {
                return string.Empty;
            }

            var cifrado = ProtectedData.Protect(Encoding.UTF8.GetBytes(valor), Entropia, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(cifrado);
        }

        private static string Descifrar(string valor)
        {
            if (string.IsNullOrEmpty(valor) || !OperatingSystem.IsWindows())
            {
                return string.Empty;
            }

            try
            {
                var plano = ProtectedData.Unprotect(Convert.FromBase64String(valor), Entropia, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plano);
            }
            catch (CryptographicException)
            {
                // Archivo generado por otro usuario de Windows o en otro equipo: hay que volver a cargar la contraseña.
                return string.Empty;
            }
        }

        private sealed class Archivo
        {
            public string Host { get; set; } = string.Empty;
            public int Puerto { get; set; } = 587;
            public bool UsarSsl { get; set; } = true;
            public string Usuario { get; set; } = string.Empty;
            public string PasswordCifrada { get; set; } = string.Empty;
            public string Remitente { get; set; } = string.Empty;
            public string NombreRemitente { get; set; } = "Integra Manufacturing";
        }
    }
}
