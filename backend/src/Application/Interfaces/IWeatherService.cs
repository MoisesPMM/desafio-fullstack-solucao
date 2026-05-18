using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Interfaces;

public record DadosClima(string Cidade, double TemperaturaCelsius, string Descricao);

public interface IWeatherProvider
{
    FonteClima Fonte { get; }
    Task<DadosClima> ObterPorCidadeAsync(string cidade, CancellationToken ct = default);
    Task<DadosClima> ObterPorCoordenadasAsync(double latitude, double longitude, CancellationToken ct = default);
}

public interface IWeatherProviderResolver
{
    IWeatherProvider ObterProvider(FonteClima? fonteSolicitada = null);
}

public interface IWeatherService
{
    Task<TemperaturaResponse> RegistrarPorCidadeAsync(string cidade, FonteClima? fonte = null, CancellationToken ct = default);
    Task<TemperaturaResponse> RegistrarPorCoordenadasAsync(double latitude, double longitude, FonteClima? fonte = null, CancellationToken ct = default);
    Task<IEnumerable<TemperaturaResponse>> ObterHistoricoPorCidadeAsync(string cidade, CancellationToken ct = default);
    Task<IEnumerable<TemperaturaResponse>> ObterHistoricoPorCoordenadasAsync(double latitude, double longitude, CancellationToken ct = default);
    Task<IEnumerable<string>> ObterCidadesRegistradasAsync(CancellationToken ct = default);
}
