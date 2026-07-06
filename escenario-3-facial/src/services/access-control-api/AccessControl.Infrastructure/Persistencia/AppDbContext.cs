using AccessControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistencia;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Localidad> Localidades => Set<Localidad>();
    public DbSet<Terminal> Terminales => Set<Terminal>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Marcacion> Marcaciones => Set<Marcacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(200);
            e.Property(u => u.Nombre).HasMaxLength(200);
            e.Property(u => u.Rol).HasMaxLength(30);
        });

        modelBuilder.Entity<Localidad>(e =>
        {
            e.ToTable("localidades");
            e.HasIndex(l => l.Nombre).IsUnique();
            e.Property(l => l.Nombre).HasMaxLength(120);
            e.Property(l => l.Tipo).HasMaxLength(20);
        });

        modelBuilder.Entity<Terminal>(e =>
        {
            e.ToTable("terminales");
            e.Property(t => t.Nombre).HasMaxLength(120);
            e.Property(t => t.UbicacionDescripcion).HasMaxLength(300);
            e.HasOne(t => t.Localidad)
                .WithMany(l => l.Terminales)
                .HasForeignKey(t => t.LocalidadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Empleado>(e =>
        {
            e.ToTable("empleados");
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasIndex(x => x.Cedula).IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(20);
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Apellido).HasMaxLength(120);
            e.Property(x => x.Cedula).HasMaxLength(20);
            e.Property(x => x.Cargo).HasMaxLength(120);
            e.Property(x => x.FotoCarnetPath).HasMaxLength(300);
            // jsonb: array de 512 floats. Búsqueda lineal en memoria (padrón ≤673);
            // pgvector queda documentado como evolución de producción.
            e.Property(x => x.EmbeddingJson).HasColumnType("jsonb");
            e.HasOne(x => x.Localidad)
                .WithMany()
                .HasForeignKey(x => x.LocalidadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Marcacion>(e =>
        {
            e.ToTable("marcaciones");
            e.Property(m => m.Resultado).HasMaxLength(15);
            e.Property(m => m.CapturaPath).HasMaxLength(300);
            e.HasIndex(m => m.TimestampUtc);
            e.HasOne(m => m.Empleado)
                .WithMany()
                .HasForeignKey(m => m.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Terminal)
                .WithMany()
                .HasForeignKey(m => m.TerminalId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Localidad)
                .WithMany()
                .HasForeignKey(m => m.LocalidadId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
