namespace WeatherApp.Api.DTOs;

public record LoginRequest(string Usuario, string Senha);

public record LoginResponse(string Token, DateTime ExpiraEm, string Tipo = "Bearer");

public record CadastroRequest(string Usuario, string Senha);

public record CadastroResponse(int Id, string Usuario, DateTime CriadoEm);
