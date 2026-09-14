#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using StockManufactura.Domain.Entities;
using StockManufactura.Infrastructure.Cloud;
using Xunit;

namespace StockManufactura.UnitTests;

public class FolderCloudBackupSyncServiceTests : IDisposable
{
    private readonly string _root;
    private readonly string _localFolder;
    private readonly string _cloudFolder;
    private readonly FolderCloudBackupSyncService _service = new();

    public FolderCloudBackupSyncServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "sm-cloud-tests", Guid.NewGuid().ToString("N"));
        _localFolder = Path.Combine(_root, "local");
        _cloudFolder = Path.Combine(_root, "nube");
        Directory.CreateDirectory(_localFolder);
        Directory.CreateDirectory(_cloudFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task SynchronizeAsync_DoesNothing_WhenCloudIsDisabled()
    {
        var result = await _service.SynchronizeAsync(BuildSettings(enabled: false));

        Assert.False(result.Ejecutada);
        Assert.Contains("deshabilitada", result.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SynchronizeAsync_UploadsLocalBackupsMissingInCloud()
    {
        WriteBackup(_localFolder, "backup-20260810-100000.zip", "contenido-a");

        var result = await _service.SynchronizeAsync(BuildSettings());

        Assert.True(result.Ejecutada);
        Assert.Equal(new[] { "backup-20260810-100000.zip" }, result.Subidos);
        Assert.True(File.Exists(Path.Combine(_cloudFolder, "backup-20260810-100000.zip")));
    }

    [Fact]
    public async Task SynchronizeAsync_DownloadsRemoteBackupsMissingLocally()
    {
        WriteBackup(_cloudFolder, "backup-20260811-080000.zip", "desde-otro-equipo");

        var result = await _service.SynchronizeAsync(BuildSettings());

        Assert.Equal(new[] { "backup-20260811-080000.zip" }, result.Descargados);
        Assert.True(File.Exists(Path.Combine(_localFolder, "backup-20260811-080000.zip")));
    }

    [Fact]
    public async Task SynchronizeAsync_SkipsDownload_WhenBidirectionalIsDisabled()
    {
        WriteBackup(_cloudFolder, "backup-20260811-080000.zip", "desde-otro-equipo");

        var settings = BuildSettings();
        settings.SincronizacionBidireccional = false;

        var result = await _service.SynchronizeAsync(settings);

        Assert.Empty(result.Descargados);
        Assert.False(File.Exists(Path.Combine(_localFolder, "backup-20260811-080000.zip")));
    }

    [Fact]
    public async Task SynchronizeAsync_PreservesBothCopies_WhenSameNameHasDifferentContent()
    {
        const string nombre = "backup-20260812-090000.zip";
        WriteBackup(_localFolder, nombre, "version-local");
        WriteBackup(_cloudFolder, nombre, "version-remota");

        var result = await _service.SynchronizeAsync(BuildSettings());

        Assert.Single(result.Conflictos);
        Assert.Contains(nombre, result.Conflictos[0]);

        // La copia local gana el nombre canonico y la remota se conserva bajo un nombre alternativo.
        Assert.Equal("version-local", File.ReadAllText(Path.Combine(_cloudFolder, nombre)));

        var preservada = Directory.GetFiles(_cloudFolder, "*-conflicto-*.zip").Single();
        Assert.Equal("version-remota", File.ReadAllText(preservada));
    }

    [Fact]
    public async Task SynchronizeAsync_IsIdempotent_WhenBothSidesAlreadyMatch()
    {
        const string nombre = "backup-20260813-070000.zip";
        WriteBackup(_localFolder, nombre, "identico");
        WriteBackup(_cloudFolder, nombre, "identico");

        var result = await _service.SynchronizeAsync(BuildSettings());

        Assert.Empty(result.Subidos);
        Assert.Empty(result.Descargados);
        Assert.Empty(result.Conflictos);
    }

    [Fact]
    public async Task ListRemoteAsync_ReturnsCloudBackupsWithHash()
    {
        WriteBackup(_cloudFolder, "backup-20260814-070000.zip", "contenido");

        var items = await _service.ListRemoteAsync(BuildSettings());

        var item = Assert.Single(items);
        Assert.Equal("backup-20260814-070000.zip", item.NombreArchivo);
        Assert.NotEmpty(item.Hash);
    }

    [Fact]
    public async Task IsAvailableAsync_IsFalse_WhenCloudFolderIsNotConfigured()
    {
        var settings = BuildSettings();
        settings.CarpetaNube = string.Empty;

        Assert.False(await _service.IsAvailableAsync(settings));
    }

    private BackupSettings BuildSettings(bool enabled = true) => new()
    {
        CarpetaLocal = _localFolder,
        CarpetaNube = _cloudFolder,
        GoogleDriveHabilitado = enabled,
        SincronizacionBidireccional = true,
        ProveedorNube = "Google Drive"
    };

    private static void WriteBackup(string folder, string fileName, string content)
        => File.WriteAllText(Path.Combine(folder, fileName), content);
}
