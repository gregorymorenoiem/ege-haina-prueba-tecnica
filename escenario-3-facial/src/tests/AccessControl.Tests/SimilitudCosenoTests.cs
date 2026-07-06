using AccessControl.Domain.Servicios;

namespace AccessControl.Tests;

public class SimilitudCosenoTests
{
    [Fact]
    public void VectoresIdenticos_DevuelveUno()
    {
        var vector = new[] { 0.5f, -0.3f, 0.8f, 0.1f };

        var similitud = SimilitudCoseno.Calcular(vector, vector);

        Assert.Equal(1.0, similitud, precision: 6);
    }

    [Fact]
    public void VectoresOrtogonales_DevuelveCero()
    {
        var a = new[] { 1f, 0f, 0f };
        var b = new[] { 0f, 1f, 0f };

        Assert.Equal(0.0, SimilitudCoseno.Calcular(a, b), precision: 6);
    }

    [Fact]
    public void VectoresOpuestos_DevuelveMenosUno()
    {
        var a = new[] { 0.4f, -0.2f, 0.9f };
        var b = new[] { -0.4f, 0.2f, -0.9f };

        Assert.Equal(-1.0, SimilitudCoseno.Calcular(a, b), precision: 6);
    }

    [Fact]
    public void EscalarUnVector_NoCambiaLaSimilitud()
    {
        var a = new[] { 0.3f, 0.7f, -0.2f };
        var escalado = a.Select(x => x * 5f).ToArray();

        Assert.Equal(1.0, SimilitudCoseno.Calcular(a, escalado), precision: 6);
    }

    [Fact]
    public void VectorCero_DevuelveCero()
    {
        var a = new[] { 0f, 0f, 0f };
        var b = new[] { 1f, 2f, 3f };

        Assert.Equal(0.0, SimilitudCoseno.Calcular(a, b));
    }

    [Fact]
    public void DimensionesDistintas_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            SimilitudCoseno.Calcular(new[] { 1f, 2f }, new[] { 1f, 2f, 3f }));
    }

    [Fact]
    public void VectoresVacios_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            SimilitudCoseno.Calcular(Array.Empty<float>(), Array.Empty<float>()));
    }
}
