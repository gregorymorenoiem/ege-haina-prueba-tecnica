using AccessControl.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Api.Middleware;

/// <summary>
/// Manejo global de errores con ProblemDetails (RFC 7807). Adaptado de un proyecto
/// previo del autor, sin la publicación de eventos a mensajería.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FacialException ex)
        {
            _logger.LogWarning("Error controlado del motor facial: {Codigo}", ex.Codigo);
            await EscribirProblema(context, StatusCodes.Status422UnprocessableEntity,
                "Error del motor facial", ex.Codigo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Ruta}", context.Request.Path);
            await EscribirProblema(context, StatusCodes.Status500InternalServerError,
                "Error interno del servidor", "Ocurrió un error inesperado.");
        }
    }

    private static Task EscribirProblema(HttpContext context, int status, string titulo, string detalle)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalle,
            Instance = context.Request.Path
        };
        return context.Response.WriteAsJsonAsync(problema);
    }
}
