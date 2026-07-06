using AccessControl.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AccessControl.Infrastructure.Almacenamiento;

/// <summary>
/// Guarda fotos de carnet y capturas de marcación en disco local, bajo la ruta
/// configurada en Almacenamiento:RutaBase (montada como volumen en docker-compose).
/// </summary>
public class FotoStorage : IFotoStorage
{
    private readonly string _rutaBase;

    public FotoStorage(IConfiguration configuracion)
    {
        _rutaBase = configuracion["Almacenamiento:RutaBase"] ?? "./data";
        Directory.CreateDirectory(Path.Combine(_rutaBase, "fotos"));
        Directory.CreateDirectory(Path.Combine(_rutaBase, "capturas"));
    }

    public async Task<string> GuardarFotoCarnet(int empleadoId, byte[] contenido, string extension, CancellationToken ct = default)
    {
        var relativa = Path.Combine("fotos", $"empleado-{empleadoId}-{Guid.NewGuid():N}{NormalizarExtension(extension)}");
        await File.WriteAllBytesAsync(Path.Combine(_rutaBase, relativa), contenido, ct);
        return relativa;
    }

    public async Task<string> GuardarCaptura(byte[] contenido, string extension, CancellationToken ct = default)
    {
        var carpeta = Path.Combine("capturas", DateTime.UtcNow.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(Path.Combine(_rutaBase, carpeta));
        var relativa = Path.Combine(carpeta, $"captura-{Guid.NewGuid():N}{NormalizarExtension(extension)}");
        await File.WriteAllBytesAsync(Path.Combine(_rutaBase, relativa), contenido, ct);
        return relativa;
    }

    public async Task<byte[]?> Leer(string rutaRelativa, CancellationToken ct = default)
    {
        var completa = Path.Combine(_rutaBase, rutaRelativa);
        return File.Exists(completa) ? await File.ReadAllBytesAsync(completa, ct) : null;
    }

    private static string NormalizarExtension(string extension)
    {
        var ext = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" ? ext : ".jpg";
    }
}
