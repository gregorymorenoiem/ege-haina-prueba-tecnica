using System.ComponentModel.DataAnnotations;

namespace AccessControl.Api.Dtos;

public class ValidarMarcacionForm
{
    [Required] public int TerminalId { get; set; }

    /// <summary>Imagen capturada en el terminal (jpg/png).</summary>
    [Required] public IFormFile Imagen { get; set; } = null!;
}

public record EmpleadoResumenDto(int Id, string Nombre, string Codigo);

public record ValidacionResponse(
    string Resultado,
    EmpleadoResumenDto? Empleado,
    double ScoreSimilitud,
    double Umbral,
    string Terminal,
    string Localidad,
    DateTime TimestampUtc);

public record MarcacionDto(
    int Id,
    string Resultado,
    double ScoreSimilitud,
    string? Empleado,
    string? CodigoEmpleado,
    string Terminal,
    string Localidad,
    DateTime TimestampUtc);
