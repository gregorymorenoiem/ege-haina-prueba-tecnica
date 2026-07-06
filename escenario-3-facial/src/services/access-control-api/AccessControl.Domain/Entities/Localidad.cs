namespace AccessControl.Domain.Entities;

public class Localidad
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;

    /// <summary>"planta" u "oficina".</summary>
    public string Tipo { get; set; } = null!;

    /// <summary>Pedernales opera como sistema aislado.</summary>
    public bool EsSistemaAislado { get; set; }

    public ICollection<Terminal> Terminales { get; set; } = new List<Terminal>();
}
