namespace WeatherApp.Domain.Entities;

public class RegistroTemperatura
{
    public int Id { get; private set; }
    public string Cidade { get; private set; } = string.Empty;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double TemperaturaCelsius { get; private set; }
    public string DescricaoTempo { get; private set; } = string.Empty;
    public DateTime RegistradoEm { get; private set; }

    // EF Core constructor
    private RegistroTemperatura() { }

    public static RegistroTemperatura CriarPorCidade(
        string cidade,
        double temperaturaCelsius,
        string descricaoTempo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cidade);

        return new RegistroTemperatura
        {
            Cidade = cidade.Trim(),
            TemperaturaCelsius = temperaturaCelsius,
            DescricaoTempo = descricaoTempo,
            RegistradoEm = DateTime.UtcNow
        };
    }

    public static RegistroTemperatura CriarPorCoordenadas(
        double latitude,
        double longitude,
        string cidade,
        double temperaturaCelsius,
        string descricacaoTempo)
    {
        return new RegistroTemperatura
        {
            Cidade = cidade,
            Latitude = latitude,
            Longitude = longitude,
            TemperaturaCelsius = temperaturaCelsius,
            DescricaoTempo = descricacaoTempo,
            RegistradoEm = DateTime.UtcNow
        };
    }
}
