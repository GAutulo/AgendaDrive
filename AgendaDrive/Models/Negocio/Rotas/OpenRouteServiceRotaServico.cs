namespace AgendaDrive.Models.Negocio.Rotas;

// ESTRUTURA PARA O GUILHERME IMPLEMENTAR (RF06, RF07).
// Registrar no Program.cs com AddHttpClient<IRotaServico, OpenRouteServiceRotaServico>().
public class OpenRouteServiceRotaServico : IRotaServico
{
    private readonly HttpClient _http;
    private readonly string _chave;

    public OpenRouteServiceRotaServico(HttpClient http, IConfiguration configuracao)
    {
        _http = http;
        _chave = configuracao["OpenRouteService:Chave"] ?? "";
    }

    // TODO:
    // - GET no autocomplete do ORS (Pelias), limitando ao Brasil;
    // - a chave fica só aqui, no servidor; nunca vai para o JavaScript;
    // - ler o JSON com System.Text.Json;
    // - atenção: o ORS devolve as coordenadas na ordem [longitude, latitude];
    // - erro de rede ou HTTP: registrar no log e devolver lista vazia.
    public Task<List<EnderecoSugerido>> SugerirEnderecosAsync(string texto)
    {
        throw new NotImplementedException();
    }

    // TODO:
    // - POST em directions/driving-car/geojson com as duas coordenadas (também [lon, lat]);
    // - converter metros para km e segundos para minutos (arredondando para cima);
    // - em qualquer falha, registrar no log e devolver null (RN07).
    public Task<RotaCalculada?> CalcularRotaAsync(Coordenada origem, Coordenada destino)
    {
        throw new NotImplementedException();
    }
}
