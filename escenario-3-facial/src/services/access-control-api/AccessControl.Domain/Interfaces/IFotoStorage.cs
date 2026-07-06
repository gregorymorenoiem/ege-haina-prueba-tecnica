namespace AccessControl.Domain.Interfaces;

/// <summary>
/// Almacenamiento de imágenes en disco local (volumen Docker). MinIO/objeto queda
/// como evolución de producción.
/// </summary>
public interface IFotoStorage
{
    /// <summary>Guarda la foto de carnet de un empleado y devuelve la ruta relativa.</summary>
    Task<string> GuardarFotoCarnet(int empleadoId, byte[] contenido, string extension, CancellationToken ct = default);

    /// <summary>Guarda la captura de una marcación y devuelve la ruta relativa.</summary>
    Task<string> GuardarCaptura(byte[] contenido, string extension, CancellationToken ct = default);

    Task<byte[]?> Leer(string rutaRelativa, CancellationToken ct = default);
}
