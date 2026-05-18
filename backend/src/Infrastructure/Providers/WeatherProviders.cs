using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WeatherApp.Application.DTOs;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Infrastructure.Providers;

public class OpenWeatherMapProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public FonteClima Fonte => FonteClima.OpenWeatherMap;

    public OpenWeatherMapProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["WeatherProvider:ApiKey"]
            ?? throw new InvalidOperationException("WeatherProvider:ApiKey não configurada.");
    }

    public async Task<DadosClima> ObterPorCidadeAsync(string cidade, CancellationToken ct = default)
    {
        var url = $"weather?q={Uri.EscapeDataString(cidade)}&appid={_apiKey}&units=metric&lang=pt_br";
        return await BuscarClimaAsync(url, ct);
    }

    public async Task<DadosClima> ObterPorCoordenadasAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var latitudeFormatada = latitude.ToString(CultureInfo.InvariantCulture);
        var longitudeFormatada = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"weather?lat={latitudeFormatada}&lon={longitudeFormatada}&appid={_apiKey}&units=metric&lang=pt_br";
        return await BuscarClimaAsync(url, ct);
    }

    private async Task<DadosClima> BuscarClimaAsync(string url, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var cidade = root.GetProperty("name").GetString() ?? "Desconhecida";
        var temperatura = root.GetProperty("main").GetProperty("temp").GetDouble();
        var descricao = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? string.Empty;

        return new DadosClima(cidade, temperatura, descricao);
    }
}

public class FakeWeatherProvider : IWeatherProvider
{
    private static readonly Random _rng = new();

    public FonteClima Fonte => FonteClima.Simulado;

    public Task<DadosClima> ObterPorCidadeAsync(string cidade, CancellationToken ct = default) =>
        Task.FromResult(new DadosClima(cidade, Math.Round(_rng.NextDouble() * 35 + 5, 1), "céu limpo (simulado)"));

    public Task<DadosClima> ObterPorCoordenadasAsync(double latitude, double longitude, CancellationToken ct = default) =>
        Task.FromResult(new DadosClima($"Lat:{latitude:F2}/Lon:{longitude:F2}", Math.Round(_rng.NextDouble() * 35 + 5, 1), "nublado (simulado)"));
}

public class WeatherProviderResolver : IWeatherProviderResolver
{
    private readonly IReadOnlyDictionary<FonteClima, IWeatherProvider> _providers;
    private readonly FonteClima _fontePadrao;

    public WeatherProviderResolver(IEnumerable<IWeatherProvider> providers, IConfiguration configuration)
    {
        _providers = providers.ToDictionary(provider => provider.Fonte);

        // Mantém compatibilidade com a feature flag já existente: quando a fonte não é enviada
        // pelo dashboard, a API usa o provider configurado para o ambiente.
        _fontePadrao = configuration["FeatureFlags:UseFakeProvider"] == "true"
            ? FonteClima.Simulado
            : FonteClima.OpenWeatherMap;
    }

    public IWeatherProvider ObterProvider(FonteClima? fonteSolicitada = null)
    {
        var fonte = fonteSolicitada ?? _fontePadrao;

        if (_providers.TryGetValue(fonte, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException($"Provider de clima '{fonte}' não foi registrado.");
    }
}
