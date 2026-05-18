using WeatherApp.Application.DTOs;
using WeatherApp.Application.Interfaces;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Interfaces;

namespace WeatherApp.Application.Services;

public class WeatherService : IWeatherService
{
    private readonly IWeatherProviderResolver _weatherProviderResolver;
    private readonly ITemperatureRepository _repository;

    public WeatherService(IWeatherProviderResolver weatherProviderResolver, ITemperatureRepository repository)
    {
        _weatherProviderResolver = weatherProviderResolver;
        _repository = repository;
    }

    public async Task<TemperaturaResponse> RegistrarPorCidadeAsync(string cidade, FonteClima? fonte = null, CancellationToken ct = default)
    {
        var provider = _weatherProviderResolver.ObterProvider(fonte);
        var dadosClima = await provider.ObterPorCidadeAsync(cidade, ct);

        var registro = RegistroTemperatura.CriarPorCidade(
            dadosClima.Cidade,
            dadosClima.TemperaturaCelsius,
            dadosClima.Descricao);

        await _repository.AddAsync(registro, ct);
        return MapToResponse(registro);
    }

    public async Task<TemperaturaResponse> RegistrarPorCoordenadasAsync(double latitude, double longitude, FonteClima? fonte = null, CancellationToken ct = default)
    {
        var provider = _weatherProviderResolver.ObterProvider(fonte);
        var dadosClima = await provider.ObterPorCoordenadasAsync(latitude, longitude, ct);

        var registro = RegistroTemperatura.CriarPorCoordenadas(
            latitude, longitude,
            dadosClima.Cidade,
            dadosClima.TemperaturaCelsius,
            dadosClima.Descricao);

        await _repository.AddAsync(registro, ct);
        return MapToResponse(registro);
    }

    public async Task<IEnumerable<TemperaturaResponse>> ObterHistoricoPorCidadeAsync(string cidade, CancellationToken ct = default)
    {
        var registros = await _repository.ObterHistoricoPorCidadeAsync(cidade, 30, ct);
        return registros.Select(MapToResponse);
    }

    public async Task<IEnumerable<TemperaturaResponse>> ObterHistoricoPorCoordenadasAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var registros = await _repository.ObterHistoricoPorCoordenadasAsync(latitude, longitude, 30, ct);
        return registros.Select(MapToResponse);
    }

    public Task<IEnumerable<string>> ObterCidadesRegistradasAsync(CancellationToken ct = default) =>
        _repository.ObterCidadesRegistradasAsync(ct);

    private static TemperaturaResponse MapToResponse(RegistroTemperatura r) => new(
        r.Id, r.Cidade, r.Latitude, r.Longitude,
        r.TemperaturaCelsius, r.DescricaoTempo, r.RegistradoEm);
}
