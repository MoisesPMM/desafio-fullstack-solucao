using WeatherApp.Domain.Entities;

namespace WeatherApp.Domain.Interfaces;

public interface ITemperatureRepository
{
    Task AddAsync(RegistroTemperatura registro, CancellationToken ct = default);
    Task<IEnumerable<RegistroTemperatura>> ObterHistoricoPorCidadeAsync(string cidade, int dias = 30, CancellationToken ct = default);
    Task<IEnumerable<RegistroTemperatura>> ObterHistoricoPorCoordenadasAsync(double latitude, double longitude, int dias = 30, CancellationToken ct = default);
    Task<IEnumerable<string>> ObterCidadesRegistradasAsync(CancellationToken ct = default);
}
