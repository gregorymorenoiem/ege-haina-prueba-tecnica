using AccessControl.Domain;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Interfaces;
using AccessControl.Infrastructure.Servicios;
using AccessControl.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace AccessControl.Tests;

public class MarcacionServiceTests
{
    private static readonly byte[] ImagenFalsa = { 1, 2, 3 };

    private static Empleado EmpleadoEnrolado(int id, int localidadId, string codigo)
    {
        var empleado = new Empleado
        {
            Id = id,
            Codigo = codigo,
            Nombre = "Empleado",
            Apellido = codigo,
            Cedula = "001-" + codigo,
            LocalidadId = localidadId
        };
        empleado.SetEmbedding(new[] { 0.1f, 0.2f, 0.3f });
        return empleado;
    }

    private static MarcacionService CrearServicio(
        Infrastructure.Persistencia.AppDbContext db,
        IFacialService facial,
        double umbral = 0.40)
    {
        var fotos = Substitute.For<IFotoStorage>();
        fotos.GuardarCaptura(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("capturas/prueba.jpg");
        var config = ContextoPruebas.Configuracion(
            ("Facial:UmbralSimilitud", umbral.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return new MarcacionService(db, facial, fotos, config);
    }

    [Fact]
    public async Task ScoreSobreElUmbral_RegistraAceptadaConEmpleado()
    {
        using var db = ContextoPruebas.CrearDb();
        db.Empleados.Add(EmpleadoEnrolado(10, localidadId: 1, "EH-0010"));
        db.SaveChanges();

        var facial = Substitute.For<IFacialService>();
        facial.GenerarEmbedding(ImagenFalsa, Arg.Any<CancellationToken>())
            .Returns(new[] { 0.1f, 0.2f, 0.3f });
        facial.Comparar(Arg.Any<float[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>())
            .Returns(0.87);

        var resultado = await CrearServicio(db, facial).Validar(terminalId: 1, ImagenFalsa, ".jpg");

        Assert.Equal(ResultadoMarcacion.Aceptada, resultado.Marcacion.Resultado);
        Assert.Equal(10, resultado.Marcacion.EmpleadoId);
        Assert.Equal(0.87, resultado.Marcacion.ScoreSimilitud, precision: 6);

        var guardada = await db.Marcaciones.SingleAsync();
        Assert.Equal(ResultadoMarcacion.Aceptada, guardada.Resultado);
        Assert.Equal("capturas/prueba.jpg", guardada.CapturaPath);
    }

    [Fact]
    public async Task ScoreBajoElUmbral_RegistraRechazadaSinEmpleado()
    {
        using var db = ContextoPruebas.CrearDb();
        db.Empleados.Add(EmpleadoEnrolado(10, localidadId: 1, "EH-0010"));
        db.SaveChanges();

        var facial = Substitute.For<IFacialService>();
        facial.GenerarEmbedding(ImagenFalsa, Arg.Any<CancellationToken>())
            .Returns(new[] { 0.9f, 0.1f, 0.0f });
        facial.Comparar(Arg.Any<float[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>())
            .Returns(0.22);

        var resultado = await CrearServicio(db, facial).Validar(terminalId: 1, ImagenFalsa, ".jpg");

        Assert.Equal(ResultadoMarcacion.Rechazada, resultado.Marcacion.Resultado);
        Assert.Null(resultado.Marcacion.EmpleadoId);
        Assert.Null(resultado.Empleado);
        // El score y la captura se guardan igualmente, para auditoría.
        var guardada = await db.Marcaciones.SingleAsync();
        Assert.Equal(0.22, guardada.ScoreSimilitud, precision: 6);
        Assert.NotNull(guardada.CapturaPath);
    }

    [Fact]
    public async Task SinCaraDetectada_PropagaErrorYNoRegistraMarcacion()
    {
        using var db = ContextoPruebas.CrearDb();
        db.Empleados.Add(EmpleadoEnrolado(10, localidadId: 1, "EH-0010"));
        db.SaveChanges();

        var facial = Substitute.For<IFacialService>();
        facial.GenerarEmbedding(ImagenFalsa, Arg.Any<CancellationToken>())
            .Returns<float[]>(_ => throw new FacialException("NO_FACE_DETECTED"));

        var servicio = CrearServicio(db, facial);

        var excepcion = await Assert.ThrowsAsync<FacialException>(
            () => servicio.Validar(terminalId: 1, ImagenFalsa, ".jpg"));
        Assert.Equal("NO_FACE_DETECTED", excepcion.Codigo);
        Assert.Empty(db.Marcaciones);
    }

    [Fact]
    public async Task GaleriaFiltradaPorLocalidad_IgnoraEmpleadosDeOtrasPlantas()
    {
        using var db = ContextoPruebas.CrearDb();
        // Solo existe un empleado enrolado, pero en OTRA localidad (Quisqueya 1).
        db.Empleados.Add(EmpleadoEnrolado(20, localidadId: 2, "EH-0020"));
        db.SaveChanges();

        var facial = Substitute.For<IFacialService>();
        facial.GenerarEmbedding(ImagenFalsa, Arg.Any<CancellationToken>())
            .Returns(new[] { 0.1f, 0.2f, 0.3f });
        facial.Comparar(Arg.Any<float[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>())
            .Returns(0.99);

        var resultado = await CrearServicio(db, facial).Validar(terminalId: 1, ImagenFalsa, ".jpg");

        // Aunque la comparación daría 0.99, el empleado no pertenece a la planta
        // del terminal: nunca se compara y la marcación queda rechazada.
        Assert.Equal(ResultadoMarcacion.Rechazada, resultado.Marcacion.Resultado);
        await facial.DidNotReceive().Comparar(Arg.Any<float[]>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TerminalInexistente_LanzaReglaNegocio()
    {
        using var db = ContextoPruebas.CrearDb();
        var servicio = CrearServicio(db, Substitute.For<IFacialService>());

        await Assert.ThrowsAsync<ReglaNegocioException>(
            () => servicio.Validar(terminalId: 999, ImagenFalsa, ".jpg"));
    }
}
