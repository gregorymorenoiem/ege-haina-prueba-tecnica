using System.ComponentModel.DataAnnotations;

namespace AccessControl.Api.Dtos;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record LoginResponse(string Token, string Nombre, string Email, string Rol, DateTime ExpiraEnUtc);
