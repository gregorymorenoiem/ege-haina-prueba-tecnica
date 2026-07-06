namespace AccessControl.Domain;

/// <summary>Violación de una regla de negocio (código duplicado, localidad inexistente, etc.).</summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
