using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccessControl.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AccessControl.Infrastructure.Auth;

/// <summary>
/// Emisión de JWT. Configuración de firma y validación adaptada de un proyecto
/// previo del autor, simplificada para este MVP.
/// </summary>
public class JwtTokenService
{
    private readonly JwtOptions _opciones;

    public JwtTokenService(IOptions<JwtOptions> opciones) => _opciones = opciones.Value;

    public (string Token, DateTime ExpiraEnUtc) Generar(Usuario usuario)
    {
        var expiraEn = DateTime.UtcNow.AddMinutes(_opciones.ExpiracionMinutos);
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Secret)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: _opciones.Issuer,
            audience: _opciones.Audience,
            claims: claims,
            expires: expiraEn,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
