using FluentAssertions;
using Moq;
using WeatherApp.Application.DTOs;
using WeatherApp.Application.Interfaces;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using WeatherApp.Domain.Interfaces;
using Xunit;

namespace WeatherApp.Tests.Unit;

public class WeatherServiceTests
{
    private readonly Mock<IWeatherProvider> _providerMock = new();
    private readonly Mock<IWeatherProviderResolver> _providerResolverMock = new();
    private readonly Mock<ITemperatureRepository> _repositoryMock = new();
    private readonly WeatherService _sut;

    public WeatherServiceTests()
    {
        _providerResolverMock
            .Setup(r => r.ObterProvider(It.IsAny<FonteClima?>()))
            .Returns(_providerMock.Object);

        _sut = new WeatherService(_providerResolverMock.Object, _repositoryMock.Object);
    }

    [Fact]
    public async Task RegistrarPorCidadeAsync_DevePersistirERetornarTemperatura()
    {
        // Arrange
        _providerMock
            .Setup(p => p.ObterPorCidadeAsync("Cascavel", default))
            .ReturnsAsync(new DadosClima("Cascavel", 25.5, "céu limpo"));

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<RegistroTemperatura>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RegistrarPorCidadeAsync("Cascavel");

        // Assert
        result.Cidade.Should().Be("Cascavel");
        result.TemperaturaCelsius.Should().Be(25.5);
        result.DescricaoTempo.Should().Be("céu limpo");
        _providerResolverMock.Verify(r => r.ObterProvider(null), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<RegistroTemperatura>(), default), Times.Once);
    }

    [Fact]
    public async Task RegistrarPorCidadeAsync_ComFonteInformada_DeveUsarProviderSolicitado()
    {
        // Arrange
        _providerMock
            .Setup(p => p.ObterPorCidadeAsync("Cascavel", default))
            .ReturnsAsync(new DadosClima("Cascavel", 21.0, "céu limpo (simulado)"));

        // Act
        await _sut.RegistrarPorCidadeAsync("Cascavel", FonteClima.Simulado);

        // Assert
        _providerResolverMock.Verify(r => r.ObterProvider(FonteClima.Simulado), Times.Once);
    }

    [Fact]
    public async Task RegistrarPorCoordenadasAsync_DevePersistirERetornarTemperatura()
    {
        // Arrange
        _providerMock
            .Setup(p => p.ObterPorCoordenadasAsync(-24.9, -53.4, default))
            .ReturnsAsync(new DadosClima("Cascavel", 22.0, "nublado"));

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<RegistroTemperatura>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RegistrarPorCoordenadasAsync(-24.9, -53.4);

        // Assert
        result.TemperaturaCelsius.Should().Be(22.0);
        result.Latitude.Should().Be(-24.9);
        result.Longitude.Should().Be(-53.4);
    }

    [Fact]
    public async Task ObterHistoricoPorCidadeAsync_DeveRetornarRegistrosOrdenados()
    {
        // Arrange
        var registros = new List<RegistroTemperatura>
        {
            RegistroTemperatura.CriarPorCidade("Cascavel", 28.0, "sol"),
            RegistroTemperatura.CriarPorCidade("Cascavel", 20.0, "chuva"),
        };

        _repositoryMock
            .Setup(r => r.ObterHistoricoPorCidadeAsync("Cascavel", 30, default))
            .ReturnsAsync(registros);

        // Act
        var result = await _sut.ObterHistoricoPorCidadeAsync("Cascavel");

        // Assert
        result.Should().HaveCount(2);
        result.First().TemperaturaCelsius.Should().Be(28.0);
    }

    [Fact]
    public async Task ObterCidadesRegistradasAsync_DeveRetornarCidadesDoRepositorio()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ObterCidadesRegistradasAsync(default))
            .ReturnsAsync(new[] { "Cascavel", "Londrina" });

        // Act
        var result = await _sut.ObterCidadesRegistradasAsync();

        // Assert
        result.Should().BeEquivalentTo(new[] { "Cascavel", "Londrina" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task RegistrarPorCidadeAsync_QuandoProviderFalhar_DeveLancarExcecao()
    {
        // Arrange
        _providerMock
            .Setup(p => p.ObterPorCidadeAsync(It.IsAny<string>(), default))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        // Act & Assert
        await _sut.Invoking(s => s.RegistrarPorCidadeAsync("InvalidCity"))
            .Should().ThrowAsync<HttpRequestException>();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<RegistroTemperatura>(), default), Times.Never);
    }
}
