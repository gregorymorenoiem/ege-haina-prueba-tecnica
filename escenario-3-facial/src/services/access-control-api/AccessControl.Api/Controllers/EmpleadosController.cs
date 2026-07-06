using AccessControl.Api.Dtos;
using AccessControl.Domain;
using AccessControl.Domain.Interfaces;
using AccessControl.Infrastructure.Persistencia;
using AccessControl.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/empleados")]
[Authorize(Roles = $"{Roles.RRHH},{Roles.Direccion},{Roles.Admin}")]
public class EmpleadosController : ControllerBase
{
    private const long TamanoMaximoFoto = 8 * 1024 * 1024;
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png" };

    private readonly AppDbContext _db;
    private readonly EmpleadoService _servicio;
    private readonly IFotoStorage _fotos;

    public EmpleadosController(AppDbContext db, EmpleadoService servicio, IFotoStorage fotos)
    {
        _db = db;
        _servicio = servicio;
        _fotos = fotos;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaDto<EmpleadoDto>>> Listar(
        [FromQuery] int? localidadId,
        [FromQuery] string? buscar,
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 100);

        var consulta = _db.Empleados.Include(e => e.Localidad).AsNoTracking().AsQueryable();
        if (!incluirInactivos)
            consulta = consulta.Where(e => e.Activo);
        if (localidadId is not null)
            consulta = consulta.Where(e => e.LocalidadId == localidadId);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = $"%{buscar.Trim()}%";
            consulta = consulta.Where(e =>
                EF.Functions.ILike(e.Nombre, texto) ||
                EF.Functions.ILike(e.Apellido, texto) ||
                EF.Functions.ILike(e.Codigo, texto) ||
                EF.Functions.ILike(e.Cedula, texto));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(e => EmpleadoDto.DesdeEntidad(e))
            .ToListAsync(ct);

        return new PaginaDto<EmpleadoDto>(items, total, pagina, tamanoPagina);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpleadoDto>> Obtener(int id, CancellationToken ct)
    {
        var empleado = await _db.Empleados.Include(e => e.Localidad)
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, ct);
        return empleado is null ? NotFound() : EmpleadoDto.DesdeEntidad(empleado);
    }

    [HttpGet("{id:int}/foto")]
    public async Task<IActionResult> Foto(int id, CancellationToken ct)
    {
        var empleado = await _db.Empleados.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, ct);
        if (empleado?.FotoCarnetPath is null)
            return NotFound();

        var contenido = await _fotos.Leer(empleado.FotoCarnetPath, ct);
        if (contenido is null)
            return NotFound();

        var tipo = empleado.FotoCarnetPath.EndsWith(".png") ? "image/png" : "image/jpeg";
        return File(contenido, tipo);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.RRHH},{Roles.Admin}")]
    [ProducesResponseType(typeof(EmpleadoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<EmpleadoDto>> Crear([FromForm] EmpleadoForm form, CancellationToken ct)
    {
        var (foto, extension) = await LeerFoto(form.Foto, ct);
        var empleado = await _servicio.Crear(ADatos(form), foto, extension, ct);
        await _db.Entry(empleado).Reference(e => e.Localidad).LoadAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = empleado.Id }, EmpleadoDto.DesdeEntidad(empleado));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.RRHH},{Roles.Admin}")]
    public async Task<ActionResult<EmpleadoDto>> Actualizar(int id, [FromForm] EmpleadoForm form, CancellationToken ct)
    {
        var (foto, extension) = await LeerFoto(form.Foto, ct);
        var empleado = await _servicio.Actualizar(id, ADatos(form), foto, extension, ct);
        if (empleado is null)
            return NotFound();
        await _db.Entry(empleado).Reference(e => e.Localidad).LoadAsync(ct);
        return EmpleadoDto.DesdeEntidad(empleado);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{Roles.RRHH},{Roles.Admin}")]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct) =>
        await _servicio.Desactivar(id, ct) ? NoContent() : NotFound();

    private static DatosEmpleado ADatos(EmpleadoForm form) => new(
        form.Codigo, form.Nombre, form.Apellido, form.Cedula, form.Cargo, form.LocalidadId);

    private static async Task<(byte[]? Contenido, string? Extension)> LeerFoto(IFormFile? foto, CancellationToken ct)
    {
        if (foto is null || foto.Length == 0)
            return (null, null);

        if (foto.Length > TamanoMaximoFoto)
            throw new ReglaNegocioException("La foto no puede superar 8 MB.");

        var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
            throw new ReglaNegocioException("Formato de foto no soportado (use jpg o png).");

        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria, ct);
        return (memoria.ToArray(), extension);
    }
}
