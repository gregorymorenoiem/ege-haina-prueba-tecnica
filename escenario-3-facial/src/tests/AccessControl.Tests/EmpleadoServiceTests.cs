using AccessControl.Domain;
using AccessControl.Domain.Interfaces;
using AccessControl.Infrastructure.Servicios;
using AccessControl.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace AccessControl.Tests;

public class EmpleadoServiceTests
{
    private static readonly byte[] FotoFalsa = { 9, 9, 9 };
    private static readonly float[] EmbeddingFalso = { 0.5f, 0.5f, 0.5f };

    private static DatosEmpleado Datos(string codigo = "EH-0100", string cedula = "001-0100") =>
        new(codigo, "Pedro", "Santana", cedula, "Operador", LocalidadId: 1);

    private static (EmpleadoService Servicio, IFacialService Facial) CrearServicio(
        Infrastructure.Persistencia.AppDbContext db)
    {
        var facial = Substitute.For<IFacialService>();
        facial.GenerarEmbedding(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(EmbeddingFalso);
        var fotos = Substitute.For<IFotoStorage>();
        fotos.GuardarFotoCarnet(Arg.Any<int>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(llamada => $"fotos/empleado-{llamada.ArgAt<int>(0)}.jpg");
        return (new EmpleadoService(db, facial, fotos), facial);
    }

    [Fact]
    public async Task AltaConFoto_GeneraYPersisteEmbedding()
    {
        using var db = ContextoPruebas.CrearDb();
        var (servicio, facial) = CrearServicio(db);

        var empleado = await servicio.Crear(Datos(), FotoFalsa, ".jpg");

        await facial.Received(1).GenerarEmbedding(FotoFalsa, Arg.Any<CancellationToken>());
        var guardado = await db.Empleados.SingleAsync(e => e.Id == empleado.Id);
        Assert.Equal(EmbeddingFalso, guardado.GetEmbedding());
        Assert.NotNull(guardado.FotoCarnetPath);
    }

    [Fact]
    public async Task AltaSinFoto_QuedaSinEnrolar()
    {
        using var db = ContextoPruebas.CrearDb();
        var (servicio, facial) = CrearServicio(db);

        var empleado = await servicio.Crear(Datos(), foto: null, extension: null);

        Assert.Null(empleado.EmbeddingJson);
        Assert.Null(empleado.FotoCarnetPath);
        await facial.DidNotReceive().GenerarEmbedding(Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EdicionConNuevaFoto_RegeneraElEmbedding()
    {
        using var db = ContextoPruebas.CrearDb();
        var (servicio, facial) = CrearServicio(db);
        var empleado = await servicio.Crear(Datos(), FotoFalsa, ".jpg");

        var otroEmbedding = new[] { -0.1f, 0.7f, 0.2f };
        facial.GenerarEmbedding(Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(otroEmbedding);

        var nuevaFoto = new byte[] { 7, 7, 7 };
        await servicio.Actualizar(empleado.Id, Datos(), nuevaFoto, ".png");

        var guardado = await db.Empleados.SingleAsync(e => e.Id == empleado.Id);
        Assert.Equal(otroEmbedding, guardado.GetEmbedding());
    }

    [Fact]
    public async Task EdicionSinFoto_ConservaElEmbedding()
    {
        using var db = ContextoPruebas.CrearDb();
        var (servicio, _) = CrearServicio(db);
        var empleado = await servicio.Crear(Datos(), FotoFalsa, ".jpg");

        await servicio.Actualizar(empleado.Id, Datos() with { Cargo = "Supervisor" }, foto: null, extension: null);

        var guardado = await db.Empleados.SingleAsync(e => e.Id == empleado.Id);
        Assert.Equal(EmbeddingFalso, guardado.GetEmbedding());
        Assert.Equal("Supervisor", guardado.Cargo);
    }

    [Fact]
    public async Task CodigoDuplicado_LanzaReglaNegocio()
    {
        using var db = ContextoPruebas.CrearDb();
        var (servicio, _) = CrearServicio(db);
        await servicio.Crear(Datos(codigo: "EH-0100", cedula: "001-A"), null, null);

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.Crear(Datos(codigo: "EH-0100", cedula: "001-B"), null, null));
    }

    [Fact]
    public async Task Desactivar_EsBajaLogica()
    {
        using var db = ContextoPruebas.CrearDb();
        var (servicio, _) = CrearServicio(db);
        var empleado = await servicio.Crear(Datos(), null, null);

        var resultado = await servicio.Desactivar(empleado.Id);

        Assert.True(resultado);
        var guardado = await db.Empleados.SingleAsync(e => e.Id == empleado.Id);
        Assert.False(guardado.Activo);
    }
}
