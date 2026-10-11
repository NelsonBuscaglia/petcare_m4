using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PetCare.Web.Dominio;
using PetCare.Web.Dominio.Entidades;

namespace PetCare.Web.Datos;

public class PetCareDbContext(DbContextOptions<PetCareDbContext> options) : DbContext(options)
{
    public DbSet<Hogar> Hogares => Set<Hogar>();
    public DbSet<Cuidador> Cuidadores => Set<Cuidador>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<Deber> Deberes => Set<Deber>();
    public DbSet<Instancia> Instancias => Set<Instancia>();
    public DbSet<RegistroCumplimiento> RegistrosCumplimiento => Set<RegistroCumplimiento>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite no guarda el Kind: los instantes se leen como UTC, que es como se guardan (R2).
        configurationBuilder.Properties<DateTime>().HaveConversion<ComoUtc>();

        // Enums guardados como texto.
        configurationBuilder.Properties<TipoDeber>().HaveConversion<string>();
        configurationBuilder.Properties<SubtipoVisita>().HaveConversion<string>();
        configurationBuilder.Properties<Modalidad>().HaveConversion<string>();
        configurationBuilder.Properties<Recurrencia>().HaveConversion<string>();
        configurationBuilder.Properties<EstadoInstancia>().HaveConversion<string>();
        configurationBuilder.Properties<TipoNotificacion>().HaveConversion<string>();
        configurationBuilder.Properties<EstadoNotificacion>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hogar>(e =>
        {
            // El Id lo fija la aplicación: siempre 1 (FR-048, R8).
            e.Property(h => h.Id).ValueGeneratedNever();
            e.Property(h => h.Codigo).HasMaxLength(8);
        });

        modelBuilder.Entity<Cuidador>(e =>
        {
            e.HasIndex(c => c.EmailNormalizado).IsUnique();
        });

        modelBuilder.Entity<Mascota>(e =>
        {
            e.HasIndex(m => new { m.HogarId, m.NombreNormalizado }).IsUnique();
        });

        modelBuilder.Entity<Deber>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_Deberes_IntervaloHoras", "IntervaloHoras BETWEEN 1 AND 23"));
        });

        modelBuilder.Entity<Instancia>(e =>
        {
            e.HasIndex(i => new { i.DeberId, i.ProgramadaUtc }).IsUnique();
            e.HasIndex(i => new { i.Estado, i.Dia });
        });

        modelBuilder.Entity<RegistroCumplimiento>(e =>
        {
            e.HasOne(r => r.Instancia).WithOne().HasForeignKey<RegistroCumplimiento>(r => r.InstanciaId);
            e.HasIndex(r => r.InstanciaId).IsUnique();
            e.HasIndex(r => r.Dia);
        });

        modelBuilder.Entity<Notificacion>(e =>
        {
            e.HasIndex(n => n.Clave).IsUnique();
            e.HasIndex(n => n.Estado);
            e.HasOne<Instancia>().WithMany().HasForeignKey(n => n.InstanciaId);
            e.HasOne<Deber>().WithMany().HasForeignKey(n => n.DeberId);
            e.HasOne<Mascota>().WithMany().HasForeignKey(n => n.MascotaId);
        });
    }

    private sealed class ComoUtc() : ValueConverter<DateTime, DateTime>(
        v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
