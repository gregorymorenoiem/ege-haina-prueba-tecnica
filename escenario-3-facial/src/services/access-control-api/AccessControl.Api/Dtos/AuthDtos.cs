using System.ComponentModel.DataAnnotations;

namespace AccessControl.Api.Dtos;

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record LoginResponse(string Token, string Nombre, string Email, string Rol, DateTime ExpiraEnUtc);
