namespace AccessControl.Domain.Servicios;

public static class SimilitudCoseno
{
    /// <summary>
    /// Similitud de coseno entre dos vectores. Con embeddings normalizados de ArcFace
    /// (normed_embedding) equivale al producto punto; se calcula la forma completa para
    /// no depender de que la entrada venga normalizada.
    /// </summary>
    public static double Calcular(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException(
                $"Los vectores deben tener la misma dimensión ({a.Length} vs {b.Length}).");
        if (a.Length == 0)
            throw new ArgumentException("Los vectores no pueden estar vacíos.");

        double punto = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            punto += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return punto / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
