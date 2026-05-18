using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Api.Configuration;
using WeatherApp.Api.DTOs;
using WeatherApp.Application.DTOs;
using WeatherApp.Infrastructure.Data;
using Xunit;

namespace WeatherApp.Tests.Integration;

public class WeatherApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public WeatherApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureFlags:UseFakeProvider"] = "true"
                });
            });
            
            var databaseName = $"TestDb_{Guid.NewGuid()}";
            
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));

                services.Configure<BruteForceOptions>(options =>
                {
                    options.MaxTentativas = 1;
                    options.JanelaSegundos = 60;
                    options.BloqueioSegundos = 60;
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task POST_login_DeveRetornarToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
    }


    [Fact]
    public async Task POST_cadastro_DeveRetornar201()
    {
        var usuario = $"usuario_{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/auth/cadastro", new CadastroRequest(usuario, "senha123"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CadastroResponse>();
        result.Should().NotBeNull();
        result!.Usuario.Should().Be(usuario);
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_cadastro_UsuarioRepetido_DeveRetornar409()
    {
        var usuario = $"usuario_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/auth/cadastro", new CadastroRequest(usuario, "senha123"));

        var response = await _client.PostAsJsonAsync("/api/auth/cadastro", new CadastroRequest(usuario, "senha123"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_login_AposCadastro_DeveRetornarToken()
    {
        var usuario = $"usuario_{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/auth/cadastro", new CadastroRequest(usuario, "senha123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(usuario, "senha123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POST_cidade_SemToken_DeveRetornar401()
    {
        var response = await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Cascavel"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_cidade_ComToken_DeveRetornar201ComTemperatura()
    {
        // Arrange
        await AutenticarAsync();
        var request = new RegistrarPorCidadeRequest("Cascavel");

        // Act
        var response = await _client.PostAsJsonAsync("/api/clima/cidade", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TemperaturaResponse>();
        result.Should().NotBeNull();
        result!.Cidade.Should().Be("Cascavel");
        result.TemperaturaCelsius.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_cidade_ComFonteSimulada_DeveRetornar201ComTemperatura()
    {
        await AutenticarAsync();
        var request = new RegistrarPorCidadeRequest("Maringá", FonteClima.Simulado);

        var response = await _client.PostAsJsonAsync("/api/clima/cidade", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TemperaturaResponse>();
        result.Should().NotBeNull();
        result!.Cidade.Should().Be("Maringá");
        result.DescricaoTempo.Should().Contain("simulado");
    }

    [Fact]
    public async Task GET_cidades_AposRegistrar_DeveRetornarCidadesSalvas()
    {
        await AutenticarAsync();
        await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Curitiba", FonteClima.Simulado));

        var response = await _client.GetAsync("/api/clima/cidades");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<string>>();
        result.Should().NotBeNull();
        result!.Should().Contain("Curitiba");
    }

    [Fact]
    public async Task GET_health_DeveRetornar200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_historico_AposRegistrar_DeveRetornarRegistro()
    {
        await AutenticarAsync();

        var postResponse = await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Londrina"));
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registroCriado = await postResponse.Content.ReadFromJsonAsync<TemperaturaResponse>();
        registroCriado.Should().NotBeNull();
        registroCriado!.Cidade.Should().Be("Londrina");

        var response = await _client.GetAsync("/api/clima/historico/cidade/Londrina");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<IEnumerable<TemperaturaResponse>>();
        var historico = result.Should().NotBeNull().And.Subject!.ToList();
        historico.Should().NotBeEmpty();
        historico.Should().Contain(r => r.Id == registroCriado.Id);
        historico.Should().Contain(r => r.Cidade == "Londrina" && r.TemperaturaCelsius > 0);
    }

    [Fact]
    public async Task GET_historico_NaoDeveContabilizarTentativasDoFiltroBruteForce()
    {
        var primeiraConsultaHistorico = await _client.GetAsync("/api/clima/historico/cidade/Cascavel");
        var segundaConsultaHistorico = await _client.GetAsync("/api/clima/historico/cidade/Cascavel");

        primeiraConsultaHistorico.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        segundaConsultaHistorico.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var primeiraCriacao = await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Cascavel"));

        primeiraCriacao.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_criacao_DeveRetornar429AposExcederMaxTentativas()
    {
        var primeiraTentativa = await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Cascavel"));
        var segundaTentativa = await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Cascavel"));

        primeiraTentativa.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        segundaTentativa.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task POST_criacaoPorCidadeECoordenadas_DevemUsarChavesSeparadasPorRotaEIp()
    {
        var primeiraTentativaCidade = await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Cascavel"));
        var primeiraTentativaCoordenadas = await _client.PostAsJsonAsync("/api/clima/coordenadas", new RegistrarPorCoordenadasRequest(-24.9555, -53.4552));
        var segundaTentativaCidade = await _client.PostAsJsonAsync("/api/clima/cidade", new RegistrarPorCidadeRequest("Cascavel"));
        var segundaTentativaCoordenadas = await _client.PostAsJsonAsync("/api/clima/coordenadas", new RegistrarPorCoordenadasRequest(-24.9555, -53.4552));

        primeiraTentativaCidade.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        primeiraTentativaCoordenadas.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        segundaTentativaCidade.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        segundaTentativaCoordenadas.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private async Task AutenticarAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin123"));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
    }
}
