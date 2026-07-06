namespace AccessControl.Domain.Entities;

public class Marcacion
{
    public int Id { get; set; }

    /// <summary>Nulo cuando la marcación fue RECHAZADA (no se identificó al empleado).</summary>
    public int? EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    public int TerminalId { get; set; }
    public Terminal Terminal { get; set; } = null!;

    public int LocalidadId { get; set; }
    public Localidad Localidad { get; set; } = null!;

    /// <summary>ACEPTADA o RECHAZADA (ver ResultadoMarcacion).</summary>
    public string Resultado { get; set; } = null!;

    public double ScoreSimilitud { get; set; }

    /// <summary>Ruta de la imagen capturada, se guarda siempre (auditoría de rechazos).</summary>
    public string? CapturaPath { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
