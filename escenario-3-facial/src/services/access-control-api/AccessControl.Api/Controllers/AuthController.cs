using AccessControl.Api.Dtos;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Auth;
using AccessControl.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokens;
    private readonly IPasswordHasher<Usuario> _hasher;

    public AuthController(AppDbContext db, JwtTokenService tokens, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _tokens = tokens;
        _hasher = hasher;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await _db.Usuarios
            .SingleOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant() && u.Activo);

        if (usuario is null ||
            _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales inválidas"
            });
        }

        var (token, expiraEn) = _tokens.Generar(usuario);
        return new LoginResponse(token, usuario.Nombre, usuario.Email, usuario.Rol, expiraEn);
    }
}
