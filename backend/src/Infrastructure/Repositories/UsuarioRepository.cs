using Microsoft.EntityFrameworkCore;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Interfaces;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> ObterPorNomeAsync(string nome, CancellationToken ct = default) =>
        await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Nome.ToLower() == nome.ToLower(), ct);

    public async Task<bool> ExisteAsync(string nome, CancellationToken ct = default) =>
        await _context.Usuarios.AnyAsync(u => u.Nome.ToLower() == nome.ToLower(), ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default)
    {
        await _context.Usuarios.AddAsync(usuario, ct);
        await _context.SaveChangesAsync(ct);
    }
}
