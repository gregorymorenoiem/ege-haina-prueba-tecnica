namespace AccessControl.Domain.Interfaces;

/// <summary>
/// Fachada del motor facial. Nada más del sistema conoce Python: la implementación
/// habla con un proceso hijo persistente que corre InsightFace/ArcFace.
/// </summary>
public interface IFacialService
{
    /// <summary>
    /// Genera el embedding (512 floats) de la única cara presente en la imagen.
    /// Lanza <see cref="FacialException"/> si no hay exactamente una cara.
    /// </summary>
    Task<float[]> GenerarEmbedding(byte[] imagen, CancellationToken ct = default);

    /// <summary>Similitud de coseno entre dos embeddings.</summary>
    Task<double> Comparar(float[] a, float[] b, CancellationToken ct = default);
}

/// <summary>Error controlado del motor facial (p. ej. NO_FACE_DETECTED).</summary>
public class FacialException : Exception
{
    public string Codigo { get; }

    public FacialException(string codigo, string? mensaje = null)
        : base(mensaje ?? codigo) => Codigo = codigo;
}
