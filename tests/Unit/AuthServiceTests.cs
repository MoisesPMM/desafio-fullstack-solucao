using FluentAssertions;
using Moq;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Interfaces;
using Xunit;

namespace WeatherApp.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CadastrarAsync_DeveSalvarUsuarioComHash()
    {
        _repositoryMock
            .Setup(r => r.ExisteAsync("joao", default))
            .ReturnsAsync(false);

        Usuario? usuarioSalvo = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Usuario>(), default))
            .Callback<Usuario, CancellationToken>((usuario, _) => usuarioSalvo = usuario)
            .Returns(Task.CompletedTask);

        var result = await _sut.CadastrarAsync(" joao ", "senha123");

        result.Should().NotBeNull();
        result!.Nome.Should().Be("joao");
        usuarioSalvo.Should().NotBeNull();
        usuarioSalvo!.SenhaHash.Should().NotBe("senha123");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Usuario>(), default), Times.Once);
    }

    [Fact]
    public async Task CadastrarAsync_QuandoUsuarioJaExiste_DeveRetornarNull()
    {
        _repositoryMock
            .Setup(r => r.ExisteAsync("joao", default))
            .ReturnsAsync(true);

        var result = await _sut.CadastrarAsync("joao", "senha123");

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Usuario>(), default), Times.Never);
    }

    [Fact]
    public async Task CredenciaisValidasAsync_AposCadastrar_DeveValidarSenhaCorreta()
    {
        Usuario? usuarioSalvo = null;
        _repositoryMock
            .Setup(r => r.ExisteAsync("joao", default))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Usuario>(), default))
            .Callback<Usuario, CancellationToken>((usuario, _) => usuarioSalvo = usuario)
            .Returns(Task.CompletedTask);

        await _sut.CadastrarAsync("joao", "senha123");
        _repositoryMock
            .Setup(r => r.ObterPorNomeAsync("joao", default))
            .ReturnsAsync(usuarioSalvo);

        var senhaCorreta = await _sut.CredenciaisValidasAsync("joao", "senha123");
        var senhaIncorreta = await _sut.CredenciaisValidasAsync("joao", "senhaErrada");

        senhaCorreta.Should().BeTrue();
        senhaIncorreta.Should().BeFalse();
    }
}
