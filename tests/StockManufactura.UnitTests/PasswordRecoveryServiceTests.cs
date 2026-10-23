#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StockManufactura.Application.Email;
using StockManufactura.Application.Interfaces;
using StockManufactura.Application.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.UnitTests.TestDoubles;
using Xunit;

namespace StockManufactura.UnitTests;

public class PasswordRecoveryServiceTests
{
    private const string Email = "operario@test.com";

    [Fact]
    public async Task RequestCodeAsync_SendsSixDigitCodeToProfileEmail()
    {
        var (service, sender, _, _, _) = Build();

        await service.RequestCodeAsync(Email);

        var enviado = Assert.Single(sender.Sent);
        Assert.Equal(Email, enviado.To);
        Assert.Matches(@"\b\d{6}\b", enviado.Body);
    }

    [Fact]
    public async Task RequestCodeAsync_UnknownEmail_SendsNothingAndDoesNotFail()
    {
        var (service, sender, _, _, _) = Build();

        await service.RequestCodeAsync("nadie@test.com");

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task RequestCodeAsync_WithoutEmailConfiguration_Throws()
    {
        var (service, _, _, _, settings) = Build();
        settings.Settings = new EmailSettings();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RequestCodeAsync(Email));
    }

    [Fact]
    public async Task ResetWithCodeAsync_ValidCode_ChangesPasswordAndUnlocksAccount()
    {
        var (service, sender, usuario, _, _) = Build();
        for (var i = 0; i < 5; i++)
        {
            usuario.RegistrarIntentoFallido(DateTime.UtcNow, 5, TimeSpan.FromMinutes(15));
        }
        Assert.True(usuario.EstaBloqueado(DateTime.UtcNow));

        await service.RequestCodeAsync(Email);
        var codigo = ExtractCode(sender.Sent.Single().Body);
        await service.ResetWithCodeAsync(Email, codigo, "NuevaClave123");

        Assert.True(BCrypt.Net.BCrypt.Verify("NuevaClave123", usuario.PasswordHash));
        Assert.False(usuario.EstaBloqueado(DateTime.UtcNow));
    }

    [Fact]
    public async Task ResetWithCodeAsync_WrongCode_KeepsPassword()
    {
        var (service, _, usuario, _, _) = Build();
        var hashOriginal = usuario.PasswordHash;

        await service.RequestCodeAsync(Email);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetWithCodeAsync(Email, "000000x", "NuevaClave123"));
        Assert.Equal(hashOriginal, usuario.PasswordHash);
    }

    [Fact]
    public async Task ResetWithCodeAsync_CodeIsSingleUse()
    {
        var (service, sender, _, _, _) = Build();
        await service.RequestCodeAsync(Email);
        var codigo = ExtractCode(sender.Sent.Single().Body);

        await service.ResetWithCodeAsync(Email, codigo, "NuevaClave123");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetWithCodeAsync(Email, codigo, "OtraClave456"));
    }

    [Fact]
    public async Task ResetWithCodeAsync_ExpiredCode_IsRejected()
    {
        var ahora = DateTime.UtcNow;
        var (service, sender, _, _, _) = Build(() => ahora);
        await service.RequestCodeAsync(Email);
        var codigo = ExtractCode(sender.Sent.Single().Body);

        ahora = ahora.Add(PasswordRecoveryCodeStore.Vigencia).AddSeconds(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetWithCodeAsync(Email, codigo, "NuevaClave123"));
    }

    [Fact]
    public async Task ResetWithCodeAsync_TooManyWrongAttempts_InvalidatesCode()
    {
        var (service, sender, _, _, _) = Build();
        await service.RequestCodeAsync(Email);
        var codigo = ExtractCode(sender.Sent.Single().Body);
        var incorrecto = codigo == "111111" ? "222222" : "111111";

        for (var i = 0; i < PasswordRecoveryCodeStore.MaxIntentos; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetWithCodeAsync(Email, incorrecto, "NuevaClave123"));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResetWithCodeAsync(Email, codigo, "NuevaClave123"));
    }

    private static string ExtractCode(string body) => Regex.Match(body, @"\b\d{6}\b").Value;

    private static (PasswordRecoveryService Service, FakeEmailSender Sender, Usuario Usuario, FakeUnitOfWork UnitOfWork, FakeEmailSettingsStore Settings)
        Build(Func<DateTime>? utcNow = null)
    {
        var rol = new Rol("Operario", "Rol operativo");
        var usuario = new Usuario("Operario", Email, BCrypt.Net.BCrypt.HashPassword("ClaveVieja1"), rol.Id);
        usuario.AsignarRol(rol);

        var unitOfWork = new FakeUnitOfWork();
        unitOfWork.RolesFake.Seed(rol);
        unitOfWork.UsuariosFake.Seed(usuario);

        var sender = new FakeEmailSender();
        var settings = new FakeEmailSettingsStore();
        var store = utcNow is null ? new PasswordRecoveryCodeStore() : new PasswordRecoveryCodeStore(utcNow);
        var service = new PasswordRecoveryService(unitOfWork, settings, sender, store, new AuditLogService(unitOfWork));
        return (service, sender, usuario, unitOfWork, settings);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = new();

        public Task SendAsync(EmailSettings settings, string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken = default)
        {
            Sent.Add((destinatario, asunto, cuerpo));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailSettingsStore : IEmailSettingsStore
    {
        public EmailSettings Settings { get; set; } = new()
        {
            Host = "smtp.test.com",
            Puerto = 587,
            Remitente = "sistema@test.com"
        };

        public Task<EmailSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);

        public Task SaveAsync(EmailSettings settings, CancellationToken cancellationToken = default)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }
}
