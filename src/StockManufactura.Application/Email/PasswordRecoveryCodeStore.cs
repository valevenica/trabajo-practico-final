using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace StockManufactura.Application.Email
{
    /// <summary>
    /// Guarda en memoria los codigos de recuperacion pendientes. Solo se conserva la huella SHA-256 del codigo,
    /// nunca el codigo en claro, y cada codigo vence, admite un numero limitado de intentos y es de un solo uso.
    /// Debe registrarse como singleton para que el codigo sobreviva entre el envio y la validacion.
    /// </summary>
    public sealed class PasswordRecoveryCodeStore
    {
        public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan EsperaEntreEnvios = TimeSpan.FromSeconds(60);
        public const int MaxIntentos = 5;

        private readonly ConcurrentDictionary<Guid, Entry> _pendientes = new();
        private readonly Func<DateTime> _utcNow;

        public PasswordRecoveryCodeStore()
            : this(() => DateTime.UtcNow)
        {
        }

        public PasswordRecoveryCodeStore(Func<DateTime> utcNow)
        {
            _utcNow = utcNow;
        }

        public TimeSpan? TiempoRestanteParaReenviar(Guid usuarioId)
        {
            if (!_pendientes.TryGetValue(usuarioId, out var entry))
            {
                return null;
            }

            var restante = entry.EnviadoUtc + EsperaEntreEnvios - _utcNow();
            return restante > TimeSpan.Zero ? restante : null;
        }

        public string Generar(Guid usuarioId)
        {
            var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var ahora = _utcNow();
            _pendientes[usuarioId] = new Entry(Huella(usuarioId, codigo), ahora, ahora + Vigencia, 0);
            return codigo;
        }

        public void Descartar(Guid usuarioId) => _pendientes.TryRemove(usuarioId, out _);

        public CodeValidation Validar(Guid usuarioId, string codigo)
        {
            if (!_pendientes.TryGetValue(usuarioId, out var entry))
            {
                return CodeValidation.Inexistente;
            }

            if (_utcNow() > entry.VenceUtc)
            {
                _pendientes.TryRemove(usuarioId, out _);
                return CodeValidation.Vencido;
            }

            var esperado = Convert.FromHexString(entry.Huella);
            var recibido = Convert.FromHexString(Huella(usuarioId, (codigo ?? string.Empty).Trim()));
            if (CryptographicOperations.FixedTimeEquals(esperado, recibido))
            {
                _pendientes.TryRemove(usuarioId, out _);
                return CodeValidation.Valido;
            }

            var intentos = entry.Intentos + 1;
            if (intentos >= MaxIntentos)
            {
                _pendientes.TryRemove(usuarioId, out _);
                return CodeValidation.IntentosAgotados;
            }

            _pendientes[usuarioId] = entry with { Intentos = intentos };
            return CodeValidation.Incorrecto;
        }

        private static string Huella(Guid usuarioId, string codigo)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{usuarioId:N}:{codigo}")));

        private sealed record Entry(string Huella, DateTime EnviadoUtc, DateTime VenceUtc, int Intentos);
    }

    public enum CodeValidation
    {
        Valido,
        Incorrecto,
        Vencido,
        IntentosAgotados,
        Inexistente
    }
}
