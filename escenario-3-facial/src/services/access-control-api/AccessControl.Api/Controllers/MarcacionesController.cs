using AccessControl.Api.Dtos;
using AccessControl.Domain;
using AccessControl.Infrastructure.Persistencia;
using AccessControl.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/marcaciones")]
public class MarcacionesController : ControllerBase
{
    private const long TamanoMaximoImagen = 8 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly MarcacionService _servicio;

    public MarcacionesController(AppDbContext db, MarcacionService servicio)
    {
        _db = db;
        _servicio = servicio;
    }

    /// <summary>
    /// Valida la imagen capturada en un terminal contra la galería de su localidad
    /// y registra la marcación (aceptada o rechazada).
    /// </summary>
    [HttpPost("validar")]
    [Authorize(Roles = $"{Roles.Operaciones},{Roles.Admin}")]
    [ProducesResponseType(typeof(ValidacionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ValidacionResponse>> Validar([FromForm] ValidarMarcacionForm form, CancellationToken ct)
    {
        if (form.Imagen.Length == 0 || form.Imagen.Length > TamanoMaximoImagen)
            throw new ReglaNegocioException("La imagen debe pesar entre 1 byte y 8 MB.");

        using var memoria = new MemoryStream();
        await form.Imagen.CopyToAsync(memoria, ct);
        var extension = Path.GetExtension(form.Imagen.FileName).ToLowerInvariant();

        var resultado = await _servicio.Validar(form.TerminalId, memoria.ToArray(), extension, ct);

        return new ValidacionResponse(
            resultado.Marcacion.Resultado,
            resultado.Empleado is null
                ? null
                : new EmpleadoResumenDto(
                    resultado.Empleado.Id,
                    $"{resultado.Empleado.Nombre} {resultado.Empleado.Apellido}",
                    resultado.Empleado.Codigo),
            Math.Round(resultado.Marcacion.ScoreSimilitud, 4),
            resultado.Umbral,
            resultado.Terminal,
            resultado.Localidad,
            resultado.Marcacion.TimestampUtc);
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Operaciones},{Roles.RRHH},{Roles.Direccion},{Roles.Admin}")]
    public async Task<ActionResult<PaginaDto<MarcacionDto>>> Listar(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int? localidadId,
        [FromQuery] string? resultado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);

        var consulta = _db.Marcaciones
            .Include(m => m.Empleado)
            .Include(m => m.Terminal)
            .Include(m => m.Localidad)
            .AsNoTracking()
            .AsQueryable();

        if (desde is not null)
            consulta = consulta.Where(m => m.TimestampUtc >= DateTime.SpecifyKind(desde.Value, DateTimeKind.Utc));
        if (hasta is not null)
            consulta = consulta.Where(m => m.TimestampUtc <= DateTime.SpecifyKind(hasta.Value, DateTimeKind.Utc));
        if (localidadId is not null)
            consulta = consulta.Where(m => m.LocalidadId == localidadId);
        if (!string.IsNullOrWhiteSpace(resultado))
            consulta = consulta.Where(m => m.Resultado == resultado.ToUpperInvariant());

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(m => m.TimestampUtc)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(m => new MarcacionDto(
                m.Id,
                m.Resultado,
                Math.Round(m.ScoreSimilitud, 4),
                m.Empleado == null ? null : m.Empleado.Nombre + " " + m.Empleado.Apellido,
                m.Empleado == null ? null : m.Empleado.Codigo,
                m.Terminal.Nombre,
                m.Localidad.Nombre,
                m.TimestampUtc))
            .ToListAsync(ct);

        return new PaginaDto<MarcacionDto>(items, total, pagina, tamanoPagina);
    }
}
