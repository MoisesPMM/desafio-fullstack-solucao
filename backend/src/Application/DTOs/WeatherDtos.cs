namespace WeatherApp.Application.DTOs;

/// <summary>
/// Define qual fonte deve ser usada para buscar a temperatura no momento do registro.
/// Quando o campo não é enviado, a API usa o provedor configurado pela feature flag.
/// </summary>
public enum FonteClima
{
    Simulado,
    OpenWeatherMap
}

public record RegistrarPorCidadeRequest(string Cidade, FonteClima? Fonte = null);

public record RegistrarPorCoordenadasRequest(double Latitude, double Longitude, FonteClima? Fonte = null);

public record TemperaturaResponse(
    int Id,
    string Cidade,
    double? Latitude,
    double? Longitude,
    double TemperaturaCelsius,
    string DescricaoTempo,
    DateTime RegistradoEm);
