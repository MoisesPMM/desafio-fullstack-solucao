using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Api.Configuration;
using WeatherApp.Application.Interfaces;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Interfaces;
using WeatherApp.Infrastructure.Data;
using WeatherApp.Infrastructure.Providers;
using WeatherApp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registra os dois providers. A feature flag continua definindo a fonte padrão,
// mas cada requisição pode informar se quer usar dados simulados ou OpenWeatherMap.
builder.Services.AddSingleton<IWeatherProvider, FakeWeatherProvider>();
builder.Services.AddHttpClient<IWeatherProvider, OpenWeatherMapProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IWeatherProviderResolver, WeatherProviderResolver>();

// Application
builder.Services.AddScoped<ITemperatureRepository, TemperatureRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// API
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddWebSecurity(builder.Configuration);
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WeatherApp API", Version = "v1", Description = "Desafio C# — Registro de temperaturas" });
    c.AddSwaggerSecurity();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

//Isso é para fazer um auto-migrate ao subir a imagem
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        if (db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseWebSecurity();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { } // For integration tests
