using AccessControl.Domain;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Interfaces;
using AccessControl.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Servicios;

public record DatosEmpleado(
    string Codigo,
    string Nombre,
    string Apellido,
    string Cedula,
    string? Cargo,
    int LocalidadId);

/// <summary>
/// Alta, edición y baja (soft delete) de empleados. Enrolar = generar y guardar el
/// embedding de la foto del carnet; nunca se reentrena la red.
/// </summary>
public class EmpleadoService
{
    private readonly AppDbContext _db;
    private readonly IFacialService _facial;
    private readonly IFotoStorage _fotos;

    public EmpleadoService(AppDbContext db, IFacialService facial, IFotoStorage fotos)
    {
        _db = db;
        _facial = facial;
        _fotos = fotos;
    }

    public async Task<Empleado> Crear(DatosEmpleado datos, byte[]? foto, string? extension, CancellationToken ct = default)
    {
        await ValidarDatos(datos, empleadoId: null, ct);

        // El embedding se genera antes de persistir para fallar temprano si la foto no sirve.
        float[]? embedding = null;
        if (foto is not null)
            embedding = await _facial.GenerarEmbedding(foto, ct);

        var empleado = new Empleado
        {
            Codigo = datos.Codigo.Trim(),
            Nombre = datos.Nombre.Trim(),
            Apellido = datos.Apellido.Trim(),
            Cedula = datos.Cedula.Trim(),
            Cargo = datos.Cargo?.Trim(),
            LocalidadId = datos.LocalidadId
        };
        empleado.SetEmbedding(embedding);
        _db.Empleados.Add(empleado);
        await _db.SaveChangesAsync(ct);

        if (foto is not null)
        {
            empleado.FotoCarnetPath = await _fotos.GuardarFotoCarnet(empleado.Id, foto, extension ?? ".jpg", ct);
            await _db.SaveChangesAsync(ct);
        }

        return empleado;
    }

    public async Task<Empleado?> Actualizar(int id, DatosEmpleado datos, byte[]? foto, string? extension, CancellationToken ct = default)
    {
        var empleado = await _db.Empleados.SingleOrDefaultAsync(e => e.Id == id && e.Activo, ct);
        if (empleado is null)
            return null;

        await ValidarDatos(datos, id, ct);

        empleado.Codigo = datos.Codigo.Trim();
        empleado.Nombre = datos.Nombre.Trim();
        empleado.Apellido = datos.Apellido.Trim();
        empleado.Cedula = datos.Cedula.Trim();
        empleado.Cargo = datos.Cargo?.Trim();
        empleado.LocalidadId = datos.LocalidadId;

        // Si cambia la foto del carnet se regenera el embedding de referencia.
        if (foto is not null)
        {
            empleado.SetEmbedding(await _facial.GenerarEmbedding(foto, ct));
            empleado.FotoCarnetPath = await _fotos.GuardarFotoCarnet(empleado.Id, foto, extension ?? ".jpg", ct);
        }

        empleado.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return empleado;
    }

    public async Task<bool> Desactivar(int id, CancellationToken ct = default)
    {
        var empleado = await _db.Empleados.SingleOrDefaultAsync(e => e.Id == id && e.Activo, ct);
        if (empleado is null)
            return false;

        empleado.Activo = false;
        empleado.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task ValidarDatos(DatosEmpleado datos, int? empleadoId, CancellationToken ct)
    {
        if (!await _db.Localidades.AnyAsync(l => l.Id == datos.LocalidadId, ct))
            throw new ReglaNegocioException($"La localidad {datos.LocalidadId} no existe.");

        var codigo = datos.Codigo.Trim();
        if (await _db.Empleados.AnyAsync(e => e.Codigo == codigo && e.Id != empleadoId, ct))
            throw new ReglaNegocioException($"Ya existe un empleado con el código {codigo}.");

        var cedula = datos.Cedula.Trim();
        if (await _db.Empleados.AnyAsync(e => e.Cedula == cedula && e.Id != empleadoId, ct))
            throw new ReglaNegocioException($"Ya existe un empleado con la cédula {cedula}.");
    }
}
