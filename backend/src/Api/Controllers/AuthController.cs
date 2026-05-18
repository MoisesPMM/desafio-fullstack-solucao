using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeatherApp.Api.Configuration;
using WeatherApp.Api.DTOs;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;
    private readonly LoginOptions _loginOptions;
    private readonly IAuthService _authService;

    public AuthController(
        IOptions<JwtOptions> jwtOptions,
        IOptions<LoginOptions> loginOptions,
        IAuthService authService)
    {
        _jwtOptions = jwtOptions.Value;
        _loginOptions = loginOptions.Value;
        _authService = authService;
    }

    /// <summary>Cadastra um novo usuário para acessar a aplicação</summary>
    [AllowAnonymous]
    [HttpPost("cadastro")]
    [ProducesResponseType(typeof(CadastroResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastrar([FromBody] CadastroRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Senha))
        {
            return BadRequest(new { mensagem = "Usuário e senha são obrigatórios." });
        }

        var usuario = await _authService.CadastrarAsync(request.Usuario, request.Senha, ct);
        if (usuario is null)
        {
            return Conflict(new { mensagem = "Usuário já cadastrado." });
        }

        var response = new CadastroResponse(usuario.Id, usuario.Nome, usuario.CriadoEm);
        return CreatedAtAction(nameof(Cadastrar), new { id = usuario.Id }, response);
    }

    /// <summary>Autentica o usuário configurado ou cadastrado e retorna um JWT</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!await CredenciaisValidasAsync(request, ct))
        {
            return Unauthorized(new { mensagem = "Usuário ou senha inválidos." });
        }

        return Ok(GerarToken(request.Usuario));
    }

    private async Task<bool> CredenciaisValidasAsync(LoginRequest request, CancellationToken ct) =>
        CredenciaisConfiguradasValidas(request)
        || await _authService.CredenciaisValidasAsync(request.Usuario, request.Senha, ct);

    private bool CredenciaisConfiguradasValidas(LoginRequest request) =>
        string.Equals(request.Usuario, _loginOptions.Usuario, StringComparison.Ordinal)
        && string.Equals(request.Senha, _loginOptions.Senha, StringComparison.Ordinal);

    private LoginResponse GerarToken(string usuario)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, usuario)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: signingCredentials);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
