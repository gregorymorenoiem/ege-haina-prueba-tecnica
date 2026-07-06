namespace AccessControl.Domain.Entities;

public class Terminal
{
    public int Id { get; set; }
    public int LocalidadId { get; set; }
    public Localidad Localidad { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? UbicacionDescripcion { get; set; }
    public bool Activo { get; set; } = true;
}
