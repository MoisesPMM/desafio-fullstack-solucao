using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Interfaces;

public interface IAuthService
{
    Task<Usuario?> CadastrarAsync(
        string usuario, 
        string senha, 
        CancellationToken ct = default
        );
    Task<bool> CredenciaisValidasAsync(
        string usuario, 
        string senha,
        CancellationToken ct = default
        );
}
