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
    decimal PrecoMinimo,
    decimal PrecoEstimado,
    string RotaGeoJson);

// Pede as rotas e aplica a fórmula de preço (RF07, RF09, RN03 a RN07).
public class PrecoNegocio
{
    private readonly TarifaNegocio _tarifaNegocio;
    private readonly IRotaServico _rotaServico;

    public PrecoNegocio(TarifaNegocio tarifaNegocio, IRotaServico rotaServico)
    {
        _tarifaNegocio = tarifaNegocio;
        _rotaServico = rotaServico;
    }

    // Subtotal = base + km*valorKm + min*valorMin + kmDesloc*valorDesloc + extras.
    // Preço = o maior valor entre o subtotal e o preço mínimo (RN03).
    public static decimal CalcularPreco(Tarifa tarifa, decimal distanciaKm, int duracaoMin, decimal deslocamentoKm, decimal extras = 0)
    {
        var subtotal = tarifa.TarifaBase
            + distanciaKm * tarifa.ValorKm
            + duracaoMin * tarifa.ValorMinuto
            + deslocamentoKm * tarifa.ValorKmDeslocamento
            + extras;

        return Math.Round(Math.Max(subtotal, tarifa.PrecoMinimo), 2, MidpointRounding.AwayFromZero);
    }

    // null quando não há tarifa ativa ou o serviço de rotas falhou: nenhuma cotação é exibida (RN07).
    public async Task<Cotacao?> CotarAsync(Coordenada origem, Coordenada destino)
    {
        var tarifa = await _tarifaNegocio.ObterAtivaAsync();
        if (tarifa == null)
            return null;

        var pontoDePartida = new Coordenada(tarifa.BaseLatitude, tarifa.BaseLongitude);
        var deslocamento = await _rotaServico.CalcularRotaAsync(pontoDePartida, origem);
        var corrida = await _rotaServico.CalcularRotaAsync(origem, destino);

        if (deslocamento == null || corrida == null)
            return null;

        return new Cotacao(
            tarifa.ID,
            corrida.DistanciaKm,
            corrida.DuracaoMin,
            deslocamento.DistanciaKm,
            tarifa.TarifaBase,
            Math.Round(corrida.DistanciaKm * tarifa.ValorKm, 2, MidpointRounding.AwayFromZero),
            Math.Round(corrida.DuracaoMin * tarifa.ValorMinuto, 2, MidpointRounding.AwayFromZero),
            Math.Round(deslocamento.DistanciaKm * tarifa.ValorKmDeslocamento, 2, MidpointRounding.AwayFromZero),
            tarifa.PrecoMinimo,
            CalcularPreco(tarifa, corrida.DistanciaKm, corrida.DuracaoMin, deslocamento.DistanciaKm),
            corrida.GeoJson);
    }
}
