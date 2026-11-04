using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgendaDrive.Infra;
using Microsoft.Extensions.Caching.Memory;

namespace AgendaDrive.Models.Negocio.Rotas;

// Implementa o IRotaServico chamando a API do OpenRouteService (RF06, RF07).
// A chave fica só no servidor: o navegador fala com a nossa API, nunca com o ORS.
// Atenção: o ORS usa coordenadas na ordem [longitude, latitude].
public class OpenRouteServiceRotaServico : IRotaServico
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ArquivoLog _log;
    private readonly string _chave;

    public OpenRouteServiceRotaServico(HttpClient http, IMemoryCache cache, ArquivoLog log, IConfiguration configuracao)
    {
        _http = http;
        _cache = cache;
        _log = log;
        _chave = configuracao["OpenRouteService:Chave"] ?? "";
        _http.BaseAddress = new Uri(configuracao["OpenRouteService:Url"] ?? "https://api.openrouteservice.org/");
        _http.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<List<EnderecoSugerido>> SugerirEnderecosAsync(string texto)
    {
        var chaveCache = $"endereco:{texto.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(chaveCache, out List<EnderecoSugerido>? guardado))
            return guardado!;

        try
        {
            var url = $"geocode/autocomplete?text={Uri.EscapeDataString(texto)}&boundary.country=BR&size=6";
            using var requisicao = NovaRequisicao(HttpMethod.Get, url);
            using var resposta = await _http.SendAsync(requisicao);
            resposta.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var enderecos = json.RootElement.GetProperty("features").EnumerateArray()
                .Select(f => new EnderecoSugerido(
                    f.GetProperty("properties").GetProperty("label").GetString() ?? "",
                    LerCoordenada(f.GetProperty("geometry").GetProperty("coordinates"))))
                .ToList();

            _cache.Set(chaveCache, enderecos, TimeSpan.FromMinutes(30));
            return enderecos;
        }
        catch (Exception excecao) when (excecao is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _log.RegistrarExcecao(excecao, null);
            return [];
        }
    }

    public async Task<RotaCalculada?> CalcularRotaAsync(Coordenada origem, Coordenada destino)
    {
        var chaveCache = $"rota:{Formatar(origem)}:{Formatar(destino)}";
        if (_cache.TryGetValue(chaveCache, out RotaCalculada? guardada))
            return guardada;

        try
        {
            using var requisicao = NovaRequisicao(HttpMethod.Post, "v2/directions/driving-car/geojson");
            var corpo = JsonSerializer.Serialize(new
            {
                coordinates = new[]
                {
                    new[] { origem.Longitude, origem.Latitude },
                    new[] { destino.Longitude, destino.Latitude }
                }
            });
            requisicao.Content = new StringContent(corpo, Encoding.UTF8, "application/json");

            using var resposta = await _http.SendAsync(requisicao);
            resposta.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var rota = json.RootElement.GetProperty("features")[0];
            var resumo = rota.GetProperty("properties").GetProperty("summary");

            // Quando origem e destino são o mesmo ponto, o ORS omite distância e duração.
            var metros = resumo.TryGetProperty("distance", out var distancia) ? distancia.GetDecimal() : 0;
            var segundos = resumo.TryGetProperty("duration", out var duracao) ? duracao.GetDecimal() : 0;

            var calculada = new RotaCalculada(
                Math.Round(metros / 1000, 2, MidpointRounding.AwayFromZero),
                Math.Max(1, (int)Math.Ceiling(segundos / 60)),
                rota.GetProperty("geometry").GetRawText());

            _cache.Set(chaveCache, calculada, TimeSpan.FromHours(1));
            return calculada;
        }
        catch (Exception excecao) when (excecao is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            _log.RegistrarExcecao(excecao, null);
            return null;
        }
    }

    private HttpRequestMessage NovaRequisicao(HttpMethod metodo, string url)
    {
        var requisicao = new HttpRequestMessage(metodo, url);
        requisicao.Headers.TryAddWithoutValidation("Authorization", _chave);
        requisicao.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        requisicao.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/geo+json"));
        return requisicao;
    }

    private static Coordenada LerCoordenada(JsonElement coordenadas) =>
        new(coordenadas[1].GetDecimal(), coordenadas[0].GetDecimal());

    private static string Formatar(Coordenada c) =>
        string.Create(CultureInfo.InvariantCulture, $"{c.Latitude:F6},{c.Longitude:F6}");
}
