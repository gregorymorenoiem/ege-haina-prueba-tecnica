using AccessControl.Domain;
using AccessControl.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Persistencia;

/// <summary>
/// Datos de demostración para el primer arranque: 15 localidades reales de EGE Haina,
/// terminales de ejemplo, un usuario por rol y 3 empleados sin foto (el evaluador los
/// enrola con su propia foto; ver README). Idempotente: si ya hay localidades no hace nada.
/// </summary>
public static class DbSeeder
{
    public static async Task Ejecutar(AppDbContext db, IPasswordHasher<Usuario> hasher, ILogger logger)
    {
        if (await db.Localidades.AnyAsync())
            return;

        logger.LogInformation("Base de datos vacía: cargando datos de demostración...");

        // (nombre, tipo, es sistema aislado) — padrón de ejemplo, ver README.
        var localidades = new (string Nombre, string Tipo, bool Aislado)[]
        {
            ("Oficinas Centrales (Santo Domingo)", TipoLocalidad.Oficina, false),
            ("Central Haina", TipoLocalidad.Planta, false),
            ("Barahona Carbón", TipoLocalidad.Planta, false),
            ("Sultana del Este", TipoLocalidad.Planta, false),
            ("Quisqueya 1", TipoLocalidad.Planta, false),
            ("Quisqueya 2", TipoLocalidad.Planta, false),
            ("Quisqueya Solar", TipoLocalidad.Planta, false),
            ("SIBA", TipoLocalidad.Planta, false),
            ("Pedernales", TipoLocalidad.Planta, true),
            ("Larimar 1", TipoLocalidad.Planta, false),
            ("Larimar 2", TipoLocalidad.Planta, false),
            ("Los Cocos 1", TipoLocalidad.Planta, false),
            ("Los Cocos 2", TipoLocalidad.Planta, false),
            ("Esperanza", TipoLocalidad.Planta, false),
            ("Girasol", TipoLocalidad.Planta, false)
        };

        var entidades = localidades
            .Select(l => new Localidad { Nombre = l.Nombre, Tipo = l.Tipo, EsSistemaAislado = l.Aislado })
            .ToList();
        db.Localidades.AddRange(entidades);
        await db.SaveChangesAsync();

        var centralHaina = entidades.Single(l => l.Nombre == "Central Haina");
        var oficinas = entidades.Single(l => l.Nombre.StartsWith("Oficinas Centrales"));

        db.Terminales.AddRange(
            new Terminal { Localidad = centralHaina, Nombre = "Portón Principal", UbicacionDescripcion = "Acceso vehicular y peatonal principal" },
            new Terminal { Localidad = centralHaina, Nombre = "Puerta Administrativa", UbicacionDescripcion = "Entrada del edificio administrativo" },
            new Terminal { Localidad = oficinas, Nombre = "Recepción", UbicacionDescripcion = "Lobby de Oficinas Centrales" });

        // Un usuario por rol; contraseñas solo para demo (documentadas en el README).
        var usuarios = new (string Email, string Nombre, string Rol, string Password)[]
        {
            ("admin@egehaina.com", "Administrador del Sistema", Roles.Admin, "Admin123!"),
            ("rrhh@egehaina.com", "Recursos Humanos", Roles.RRHH, "Rrhh123!"),
            ("operaciones@egehaina.com", "Operaciones", Roles.Operaciones, "Operaciones123!"),
            ("direccion@egehaina.com", "Dirección", Roles.Direccion, "Direccion123!")
        };
        foreach (var (email, nombre, rol, password) in usuarios)
        {
            var usuario = new Usuario { Email = email, Nombre = nombre, Rol = rol };
            usuario.PasswordHash = hasher.HashPassword(usuario, password);
            db.Usuarios.Add(usuario);
        }

        // Empleados demo SIN foto: el evaluador sube su propia foto como carnet
        // y valida con una segunda foto suya (demo E2E honesta, ver README).
        db.Empleados.AddRange(
            new Empleado { Codigo = "EH-0001", Nombre = "Ana", Apellido = "Pérez", Cedula = "001-0000001-1", Cargo = "Operadora de planta", Localidad = centralHaina },
            new Empleado { Codigo = "EH-0002", Nombre = "José", Apellido = "Rodríguez", Cedula = "001-0000002-2", Cargo = "Técnico de mantenimiento", Localidad = centralHaina },
            new Empleado { Codigo = "EH-0003", Nombre = "María", Apellido = "Gómez", Cedula = "001-0000003-3", Cargo = "Analista de RRHH", Localidad = oficinas });

        await db.SaveChangesAsync();
        logger.LogInformation("Seed completado: {Localidades} localidades, 3 terminales, 4 usuarios, 3 empleados demo.",
            entidades.Count);
    }
}
