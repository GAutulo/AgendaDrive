using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio.Rotas;

namespace AgendaDrive.Models.Negocio;

// Resultado de uma cotação: cada componente separado para a tela mostrar o preço detalhado (RF09).
public record Cotacao(
    int IDTarifa,
    decimal DistanciaKm,
    int DuracaoMin,
    decimal DeslocamentoKm,
    decimal ValorBase,
    decimal ValorDistancia,
    decimal ValorTempo,
    decimal ValorDeslocamento,
    decimal PrecoEstimado,
    string RotaGeoJson);

// ESTRUTURA PARA O GUILHERME IMPLEMENTAR (RF07, RF09, RN03 a RN07).
public class PrecoNegocio
{
    private readonly TarifaNegocio _tarifaNegocio;
    private readonly IRotaServico _rotaServico;

    public PrecoNegocio(TarifaNegocio tarifaNegocio, IRotaServico rotaServico)
    {
        _tarifaNegocio = tarifaNegocio;
        _rotaServico = rotaServico;
    }

    // TODO:
    // - pegar a tarifa com ObterAtivaAsync; sem tarifa ativa, não há cotação;
    // - pedir duas rotas: base do motorista -> origem (deslocamento) e origem -> destino (corrida);
    // - se alguma rota falhar, nenhuma cotação (RN07): devolva null;
    // - subtotal = base + km*valorKm + min*valorMin + kmDesloc*valorKmDesloc + extras;
    // - preço = maior valor entre o subtotal e o PrecoMinimo (Math.Max) e arredondar para 2 casas;
    // - conferir com o exemplo da documentação: 5 + 20*2 + 30*0,20 + 3*0,90 = R$ 53,70.
    public Task<Cotacao?> CotarAsync(Coordenada origem, Coordenada destino)
    {
        throw new NotImplementedException();
    }
}
