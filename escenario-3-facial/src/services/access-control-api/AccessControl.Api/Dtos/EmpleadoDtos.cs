using System.ComponentModel.DataAnnotations;
using AccessControl.Domain.Entities;

namespace AccessControl.Api.Dtos;

public class EmpleadoForm
{
    [Required, MaxLength(20)] public string Codigo { get; set; } = null!;
    [Required, MaxLength(120)] public string Nombre { get; set; } = null!;
    [Required, MaxLength(120)] public string Apellido { get; set; } = null!;
    [Required, MaxLength(20)] public string Cedula { get; set; } = null!;
    [MaxLength(120)] public string? Cargo { get; set; }
    [Required] public int LocalidadId { get; set; }

    /// <summary>Foto de carnet (jpg/png). Al guardarla se genera el embedding de referencia.</summary>
    public IFormFile? Foto { get; set; }
}

public record EmpleadoDto(
    int Id,
    string Codigo,
    string Nombre,
    string Apellido,
    string Cedula,
    string? Cargo,
    int LocalidadId,
    string Localidad,
    bool TieneFoto,
    bool Enrolado,
    bool Activo,
    DateTime CreatedAt)
{
    public static EmpleadoDto DesdeEntidad(Empleado e) => new(
        e.Id, e.Codigo, e.Nombre, e.Apellido, e.Cedula, e.Cargo,
        e.LocalidadId, e.Localidad?.Nombre ?? string.Empty,
        e.FotoCarnetPath is not null, e.EmbeddingJson is not null,
        e.Activo, e.CreatedAt);
}

public record PaginaDto<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina);
