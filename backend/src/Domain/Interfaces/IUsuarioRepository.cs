using WeatherApp.Domain.Entities;

namespace WeatherApp.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorNomeAsync(string nome, CancellationToken ct = default);
    Task<bool> ExisteAsync(string nome, CancellationToken ct = default);
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
}
