namespace WeatherApp.Domain.Entities;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }

    private Usuario() { }

    public static Usuario Criar(string nome, string senhaHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(senhaHash);

        return new Usuario
        {
            Nome = nome.Trim(),
            SenhaHash = senhaHash,
            CriadoEm = DateTime.UtcNow
        };
    }
}
