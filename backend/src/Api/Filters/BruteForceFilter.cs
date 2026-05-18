using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherApp.Api.Configuration;

namespace WeatherApp.Api.Filters;

public class BruteForceFilter : IMiddleware
{
    private readonly IMemoryCache _cache;
    private readonly IOptions<BruteForceOptions> _options;

    public BruteForceFilter(IMemoryCache cache, IOptions<BruteForceOptions> options)
    {
        _cache = cache;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!DeveAplicarFiltro(context))
        {
            await next(context);
            return;
        }

        var opcoes = _options.Value;
        var chave = CriarChave(context);
        var tentativa = _cache.GetOrCreate(chave, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(opcoes.JanelaSegundos);
            return new TentativaBruteForce(0, null);
        })!;

        if (tentativa.BloqueadoAte.HasValue && tentativa.BloqueadoAte.Value > DateTimeOffset.UtcNow)
        {
            await ResponderBloqueioAsync(context, tentativa.BloqueadoAte.Value);
            return;
        }

        var totalTentativas = tentativa.TotalTentativas + 1;
        DateTimeOffset? bloqueadoAte = null;

        if (totalTentativas > opcoes.MaxTentativas)
        {
            bloqueadoAte = DateTimeOffset.UtcNow.AddSeconds(opcoes.BloqueioSegundos);
            _cache.Set(chave, new TentativaBruteForce(totalTentativas, bloqueadoAte), bloqueadoAte.Value);
            await ResponderBloqueioAsync(context, bloqueadoAte.Value);
            return;
        }

        _cache.Set(
            chave,
            new TentativaBruteForce(totalTentativas, bloqueadoAte),
            TimeSpan.FromSeconds(opcoes.JanelaSegundos));

        await next(context);
    }

    private static bool DeveAplicarFiltro(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            return false;
        }
        
        var path = context.Request.Path;

        return path.StartsWithSegments("/api/clima/cidade")
               || path.StartsWithSegments("/api/clima/coordenadas");

    }

    private static string CriarChave(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido";
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? context.Request.GetEncodedPathAndQuery().ToLowerInvariant();

        return $"bruteforce:{ip}:{path}";
    }

    private static async Task ResponderBloqueioAsync(HttpContext context, DateTimeOffset bloqueadoAte)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = Math.Max(1, (int)(bloqueadoAte - DateTimeOffset.UtcNow).TotalSeconds).ToString();

        await context.Response.WriteAsJsonAsync(new
        {
            mensagem = "Muitas tentativas foram realizadas para este endpoint. Tente novamente mais tarde.",
            bloqueadoAte
        });
    }

    private sealed record TentativaBruteForce(int TotalTentativas, DateTimeOffset? BloqueadoAte);
}
