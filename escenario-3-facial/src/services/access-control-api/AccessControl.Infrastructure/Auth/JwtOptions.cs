namespace AccessControl.Infrastructure.Auth;

public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = "EgeHaina.AccessControl";
    public string Audience { get; set; } = "EgeHaina.AccessControl";
    public int ExpiracionMinutos { get; set; } = 480;
}
