using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AccessControl.Tests.Soporte;

/// <summary>Utilidades comunes: DbContext en memoria con datos base y configuración.</summary>
public static class ContextoPruebas
{
    public static AppDbContext CrearDb()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(opciones);

        var centralHaina = new Localidad { Id = 1, Nombre = "Central Haina", Tipo = "planta" };
        var quisqueya = new Localidad { Id = 2, Nombre = "Quisqueya 1", Tipo = "planta" };
        db.Localidades.AddRange(centralHaina, quisqueya);
        db.Terminales.Add(new Terminal { Id = 1, Localidad = centralHaina, LocalidadId = 1, Nombre = "Portón Principal" });
        db.SaveChanges();
        return db;
    }

    public static IConfiguration Configuracion(params (string Clave, string Valor)[] valores)
    {
        var pares = valores.ToDictionary(v => v.Clave, v => (string?)v.Valor);
        return new ConfigurationBuilder().AddInMemoryCollection(pares).Build();
    }
}
