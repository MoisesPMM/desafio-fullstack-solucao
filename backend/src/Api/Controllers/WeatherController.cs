using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.DTOs;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/clima")]
[Produces("application/json")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    /// <summary>Registra temperatura por nome de cidade</summary>
    [HttpPost("cidade")]
    [ProducesResponseType(typeof(TemperaturaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegistrarPorCidade(
        [FromBody] RegistrarPorCidadeRequest request,
        CancellationToken ct)
    {
        var result = await _weatherService.RegistrarPorCidadeAsync(request.Cidade, request.Fonte, ct);
        return CreatedAtAction(nameof(GetHistoricoCidade), new { cidade = result.Cidade }, result);
    }

    /// <summary>Registra temperatura por coordenadas geográficas</summary>
    [HttpPost("coordenadas")]
    [ProducesResponseType(typeof(TemperaturaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegistrarPorCoordenadas(
        [FromBody] RegistrarPorCoordenadasRequest request,
        CancellationToken ct)
    {
        var result = await _weatherService.RegistrarPorCoordenadasAsync(request.Latitude, request.Longitude, request.Fonte, ct);
        return CreatedAtAction(nameof(GetHistoricoPorCoordenadas),
            new { latitude = request.Latitude, longitude = request.Longitude }, result);
    }

    /// <summary>Retorna cidades com registros salvos</summary>
    [HttpGet("cidades")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCidadesRegistradas(CancellationToken ct)
    {
        var result = await _weatherService.ObterCidadesRegistradasAsync(ct);
        return Ok(result);
    }

    /// <summary>Retorna histórico de temperaturas por cidade (últimos 30 dias)</summary>
    [HttpGet("historico/cidade/{cidade}")]
    [ProducesResponseType(typeof(IEnumerable<TemperaturaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistoricoCidade(string cidade, CancellationToken ct)
    {
        var result = await _weatherService.ObterHistoricoPorCidadeAsync(cidade, ct);
        return Ok(result);
    }

    /// <summary>Retorna histórico de temperaturas por coordenadas (últimos 30 dias)</summary>
    [HttpGet("historico/coordenadas")]
    [ProducesResponseType(typeof(IEnumerable<TemperaturaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistoricoPorCoordenadas(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        CancellationToken ct)
    {
        var result = await _weatherService.ObterHistoricoPorCoordenadasAsync(latitude, longitude, ct);
        return Ok(result);
    }
}
