using System.Text;
using AccessControl.Api.Middleware;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Interfaces;
using AccessControl.Infrastructure.Almacenamiento;
using AccessControl.Infrastructure.Auth;
using AccessControl.Infrastructure.Facial;
using AccessControl.Infrastructure.Persistencia;
using AccessControl.Infrastructure.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// --- Persistencia ---
builder.Services.AddDbContext<AppDbContext>(opciones => opciones
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

// --- Autenticación y autorización ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Falta la sección de configuración Jwt.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones => opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    });
builder.Services.AddAuthorization();

// --- Motor facial: proceso hijo Python persistente ---
builder.Services.AddSingleton<FacialProcessService>();
builder.Services.AddSingleton<IFacialService>(sp => sp.GetRequiredService<FacialProcessService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<FacialProcessService>());

// --- Servicios de aplicación ---
builder.Services.AddSingleton<IFotoStorage, FotoStorage>();
builder.Services.AddScoped<EmpleadoService>();
builder.Services.AddScoped<MarcacionService>();

builder.Services.AddControllers();

// --- Swagger con esquema Bearer ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EGE Haina — Control de Acceso Facial",
        Version = "v1",
        Description = "MVP del Escenario 3: enrolamiento de empleados y validación facial de marcaciones."
    });
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token JWT obtenido en /api/auth/login."
    });
    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

// Frontend estático: en desarrollo se sirve directo desde la carpeta frontend/ del
// repo (Frontend:Ruta); en Docker esa carpeta se copia a wwwroot y no hace falta config.
var rutaFrontend = app.Configuration["Frontend:Ruta"];
if (!string.IsNullOrEmpty(rutaFrontend))
{
    var rutaCompleta = Path.GetFullPath(rutaFrontend, app.Environment.ContentRootPath);
    if (Directory.Exists(rutaCompleta))
    {
        var proveedor = new PhysicalFileProvider(rutaCompleta);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = proveedor });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = proveedor });
    }
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Migraciones al arrancar (con reintentos: en docker-compose Postgres puede tardar).
await AplicarMigraciones(app);

app.Run();

static async Task AplicarMigraciones(WebApplication app)
{
    using var alcance = app.Services.CreateScope();
    var db = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Database.IsRelational())
        return;

    for (var intento = 1; ; intento++)
    {
        try
        {
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (intento < 10)
        {
            app.Logger.LogWarning("Base de datos no disponible (intento {Intento}/10): {Error}",
                intento, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
