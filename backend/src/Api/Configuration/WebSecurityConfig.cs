using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using WeatherApp.Api.Filters;

namespace WeatherApp.Api.Configuration;

public static class WebSecurityConfig
{
    public static IServiceCollection AddWebSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfig = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Configuração Jwt não encontrada.");

        if (string.IsNullOrWhiteSpace(jwtConfig.Key) || Encoding.UTF8.GetByteCount(jwtConfig.Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key deve possuir pelo menos 32 bytes.");
        }

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<LoginOptions>(configuration.GetSection(LoginOptions.SectionName));
        services.Configure<BruteForceOptions>(configuration.GetSection(BruteForceOptions.SectionName));
        services.AddMemoryCache();
        services.AddSingleton<BruteForceFilter>();

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IApplicationBuilder UseWebSecurity(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseMiddleware<BruteForceFilter>();
        app.UseAuthorization();

        return app;
    }

    public static void AddSwaggerSecurity(this SwaggerGenOptions options)
    {
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Informe o token JWT no formato: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = JwtBearerDefaults.AuthenticationScheme
            }
        };

        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [securityScheme] = Array.Empty<string>()
        });
    }
}

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 60;
}

public class LoginOptions
{
    public const string SectionName = "WebSecurity:Login";

    public string Usuario { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;
}

public class BruteForceOptions
{
    public const string SectionName = "WebSecurity:BruteForce";

    public int MaxTentativas { get; set; } = 20;
    public int JanelaSegundos { get; set; } = 60;
    public int BloqueioSegundos { get; set; } = 300;
}
