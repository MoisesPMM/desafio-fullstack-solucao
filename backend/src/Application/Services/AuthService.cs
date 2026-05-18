using System.Security.Cryptography;
using WeatherApp.Application.Interfaces;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Interfaces;

namespace WeatherApp.Application.Services;

public class AuthService : IAuthService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const char Separador = ':';

    private readonly IUsuarioRepository _repository;

    public AuthService(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task<Usuario?> CadastrarAsync(string usuario, string senha, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
        {
            return null;
        }

        var nome = usuario.Trim();
        if (await _repository.ExisteAsync(nome, ct))
        {
            return null;
        }

        var novoUsuario = Usuario.Criar(nome, GerarHash(senha));
        await _repository.AddAsync(novoUsuario, ct);

        return novoUsuario;
    }

    public async Task<bool> CredenciaisValidasAsync(string usuario, string senha, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
        {
            return false;
        }

        var usuarioEncontrado = await _repository.ObterPorNomeAsync(usuario.Trim(), ct);

        return usuarioEncontrado is not null && SenhaValida(senha, usuarioEncontrado.SenhaHash);
    }

    private static string GerarHash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return string.Join(Separador, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    private static bool SenhaValida(string senha, string senhaHash)
    {
        var partes = senhaHash.Split(Separador);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteracoes))
        {
            return false;
        }

        var salt = Convert.FromBase64String(partes[1]);
        var hashSalvo = Convert.FromBase64String(partes[2]);
        var hashInformado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, hashSalvo.Length);

        return CryptographicOperations.FixedTimeEquals(hashSalvo, hashInformado);
    }
}
