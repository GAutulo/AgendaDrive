namespace AgendaDrive.Models.Negocio.Rotas;

public record Coordenada(decimal Latitude, decimal Longitude);

public record EnderecoSugerido(string Descricao, Coordenada Coordenada);

public record RotaCalculada(decimal DistanciaKm, int DuracaoMin, string GeoJson);

// Contrato com o serviço de mapas. As regras de preço só conhecem esta interface,
// então trocar de provedor muda uma única classe.
public interface IRotaServico
{
    // Lista vazia quando nada foi encontrado.
    Task<List<EnderecoSugerido>> SugerirEnderecosAsync(string texto);

    // null quando o serviço falhou ou não há rota (RN07).
    Task<RotaCalculada?> CalcularRotaAsync(Coordenada origem, Coordenada destino);
}
