using Microsoft.EntityFrameworkCore;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RegistroTemperatura> TemperatureRecords => Set<RegistroTemperatura>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroTemperatura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cidade).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DescricaoTempo).HasMaxLength(500);
            entity.Property(e => e.RegistradoEm).IsRequired();
            entity.HasIndex(e => e.Cidade);
            entity.HasIndex(e => e.RegistradoEm);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SenhaHash).IsRequired().HasMaxLength(300);
            entity.Property(e => e.CriadoEm).IsRequired();
            entity.HasIndex(e => e.Nome).IsUnique();
        });
    }
}
