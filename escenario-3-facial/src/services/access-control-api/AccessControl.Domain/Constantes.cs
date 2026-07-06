namespace AccessControl.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string RRHH = "RRHH";
    public const string Operaciones = "Operaciones";
    public const string Direccion = "Direccion";

    public static readonly string[] Todos = { Admin, RRHH, Operaciones, Direccion };
}

public static class ResultadoMarcacion
{
    public const string Aceptada = "ACEPTADA";
    public const string Rechazada = "RECHAZADA";
}

public static class TipoLocalidad
{
    public const string Planta = "planta";
    public const string Oficina = "oficina";
}
