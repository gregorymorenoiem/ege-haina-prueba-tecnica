using System.Text.Json;

namespace AccessControl.Domain.Entities;

public class Empleado
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Cedula { get; set; } = null!;
    public string? Cargo { get; set; }
    public int LocalidadId { get; set; }
    public Localidad Localidad { get; set; } = null!;
    public string? FotoCarnetPath { get; set; }

    /// <summary>
    /// Embedding facial de referencia (512 floats, ArcFace) serializado como JSON.
    /// Se persiste en una columna jsonb; con un padrón de ≤673 empleados la búsqueda
    /// lineal en memoria es suficiente (pgvector queda como evolución de producción).
    /// </summary>
    public string? EmbeddingJson { get; set; }

    /// <summary>Soft delete: la baja marca Activo=false, nunca se borra la fila.</summary>
    public bool Activo { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public float[]? GetEmbedding() =>
        EmbeddingJson is null ? null : JsonSerializer.Deserialize<float[]>(EmbeddingJson);

    public void SetEmbedding(float[]? embedding) =>
        EmbeddingJson = embedding is null ? null : JsonSerializer.Serialize(embedding);
}
