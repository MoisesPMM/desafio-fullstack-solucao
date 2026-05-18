using Microsoft.EntityFrameworkCore;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Interfaces;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Infrastructure.Repositories;

public class TemperatureRepository : ITemperatureRepository
{
    private readonly AppDbContext _context;

    public TemperatureRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RegistroTemperatura registro, CancellationToken ct = default)
    {
        await _context.TemperatureRecords.AddAsync(registro, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<RegistroTemperatura>> ObterHistoricoPorCidadeAsync(
        string cidade, int dias = 30, CancellationToken ct = default)
    {
        var desde = DateTime.UtcNow.AddDays(-dias);
        return await _context.TemperatureRecords
            .Where(r => r.Cidade.ToLower() == cidade.ToLower() && r.RegistradoEm >= desde)
            .OrderByDescending(r => r.RegistradoEm)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<RegistroTemperatura>> ObterHistoricoPorCoordenadasAsync(
        double latitude, double longitude, int dias = 30, CancellationToken ct = default)
    {
        var desde = DateTime.UtcNow.AddDays(-dias);
        const double tolerancia = 0.01; // ~1km radius
        return await _context.TemperatureRecords
            .Where(r =>
                r.Latitude.HasValue && r.Longitude.HasValue &&
                Math.Abs(r.Latitude.Value - latitude) <= tolerancia &&
                Math.Abs(r.Longitude.Value - longitude) <= tolerancia &&
                r.RegistradoEm >= desde)
            .OrderByDescending(r => r.RegistradoEm)
            .AsNoTracking()
            .ToListAsync(ct);
    }
    public async Task<IEnumerable<string>> ObterCidadesRegistradasAsync(CancellationToken ct = default)
    {
        return await _context.TemperatureRecords
            .Where(r => r.Cidade != string.Empty)
            .Select(r => r.Cidade)
            .Distinct()
            .OrderBy(cidade => cidade)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
