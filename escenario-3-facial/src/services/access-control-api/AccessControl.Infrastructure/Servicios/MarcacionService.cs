using AccessControl.Domain;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Interfaces;
using AccessControl.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AccessControl.Infrastructure.Servicios;

public record ResultadoValidacion(
    Marcacion Marcacion,
    Empleado? Empleado,
    double Umbral,
    string Terminal,
    string Localidad);

/// <summary>
/// Validación de marcaciones: embedding de la captura → comparación contra la galería
/// de empleados de la localidad del terminal (reflejo del diseño edge: cada nodo solo
/// conoce su planta) → mejor score (argmax) contra el umbral.
/// </summary>
public class MarcacionService
{
    /// <summary>Umbral por defecto para ArcFace/buffalo_l; ver README para calibración.</summary>
    public const double UmbralPorDefecto = 0.40;

    private readonly AppDbContext _db;
    private readonly IFacialService _facial;
    private readonly IFotoStorage _fotos;
    private readonly double _umbral;

    public MarcacionService(AppDbContext db, IFacialService facial, IFotoStorage fotos, IConfiguration configuracion)
    {
        _db = db;
        _facial = facial;
        _fotos = fotos;
        _umbral = configuracion.GetValue("Facial:UmbralSimilitud", UmbralPorDefecto);
    }

    public double Umbral => _umbral;

    public async Task<ResultadoValidacion> Validar(int terminalId, byte[] imagen, string? extension, CancellationToken ct = default)
    {
        var terminal = await _db.Terminales.Include(t => t.Localidad)
            .SingleOrDefaultAsync(t => t.Id == terminalId && t.Activo, ct);
        if (terminal is null)
            throw new ReglaNegocioException($"El terminal {terminalId} no existe o está inactivo.");

        // Si la imagen no tiene exactamente una cara, IFacialService lanza
        // FacialException (NO_FACE_DETECTED) y no se registra marcación.
        var embeddingCaptura = await _facial.GenerarEmbedding(imagen, ct);

        // Galería de la planta del terminal: solo empleados activos y enrolados.
        var galeria = await _db.Empleados
            .Where(e => e.LocalidadId == terminal.LocalidadId && e.Activo && e.EmbeddingJson != null)
            .ToListAsync(ct);

        Empleado? mejorEmpleado = null;
        var mejorScore = 0.0;
        foreach (var candidato in galeria)
        {
            var score = await _facial.Comparar(embeddingCaptura, candidato.GetEmbedding()!, ct);
            if (score > mejorScore)
            {
                mejorScore = score;
                mejorEmpleado = candidato;
            }
        }

        var aceptada = mejorEmpleado is not null && mejorScore >= _umbral;

        // La captura se guarda siempre, también en rechazos, para auditoría.
        var capturaPath = await _fotos.GuardarCaptura(imagen, extension ?? ".jpg", ct);

        var marcacion = new Marcacion
        {
            EmpleadoId = aceptada ? mejorEmpleado!.Id : null,
            TerminalId = terminal.Id,
            LocalidadId = terminal.LocalidadId,
            Resultado = aceptada ? ResultadoMarcacion.Aceptada : ResultadoMarcacion.Rechazada,
            ScoreSimilitud = mejorScore,
            CapturaPath = capturaPath,
            TimestampUtc = DateTime.UtcNow
        };
        _db.Marcaciones.Add(marcacion);
        await _db.SaveChangesAsync(ct);

        return new ResultadoValidacion(
            marcacion,
            aceptada ? mejorEmpleado : null,
            _umbral,
            terminal.Nombre,
            terminal.Localidad.Nombre);
    }
}
