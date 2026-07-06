using AccessControl.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Authorize]
public class CatalogosController : ControllerBase
{
    private readonly AppDbContext _db;

    public CatalogosController(AppDbContext db) => _db = db;

    [HttpGet("api/localidades")]
    public async Task<IActionResult> Localidades(CancellationToken ct) =>
        Ok(await _db.Localidades.AsNoTracking()
            .OrderBy(l => l.Nombre)
            .Select(l => new { l.Id, l.Nombre, l.Tipo, l.EsSistemaAislado })
            .ToListAsync(ct));

    [HttpGet("api/terminales")]
    public async Task<IActionResult> Terminales(CancellationToken ct) =>
        Ok(await _db.Terminales.AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Localidad.Nombre).ThenBy(t => t.Nombre)
            .Select(t => new
            {
                t.Id,
                t.Nombre,
                t.UbicacionDescripcion,
                LocalidadId = t.LocalidadId,
                Localidad = t.Localidad.Nombre
            })
            .ToListAsync(ct));
}
