using System.ComponentModel.DataAnnotations;
using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio.Rotas;

namespace AgendaDrive.Models;

// O que o cliente envia ao pedir uma corrida. Não existe campo de preço, distância nem duração:
// esses valores o servidor sempre recalcula (RN06). Serve para o formulário e para a API.
public class SolicitacaoViewModel
{
    [Display(Name = "Origem")]
    [Required(ErrorMessage = "Informe a {0}.")]
    [StringLength(250, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string Origem { get; set; } = "";

    [Required(ErrorMessage = "Escolha a origem na lista de sugestões.")]
    [Range(-90, 90, ErrorMessage = "Escolha a origem na lista de sugestões.")]
    public decimal? OrigemLatitude { get; set; }

    [Required(ErrorMessage = "Escolha a origem na lista de sugestões.")]
    [Range(-180, 180, ErrorMessage = "Escolha a origem na lista de sugestões.")]
    public decimal? OrigemLongitude { get; set; }

    [Display(Name = "Destino")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [StringLength(250, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string Destino { get; set; } = "";

    [Required(ErrorMessage = "Escolha o destino na lista de sugestões.")]
    [Range(-90, 90, ErrorMessage = "Escolha o destino na lista de sugestões.")]
    public decimal? DestinoLatitude { get; set; }

    [Required(ErrorMessage = "Escolha o destino na lista de sugestões.")]
    [Range(-180, 180, ErrorMessage = "Escolha o destino na lista de sugestões.")]
    public decimal? DestinoLongitude { get; set; }

    [Display(Name = "Data e hora")]
    [Required(ErrorMessage = "Informe a {0}.")]
    public DateTime? DataHora { get; set; }

    [Display(Name = "Viagem para outra cidade")]
    public bool Intermunicipal { get; set; }

    [Display(Name = "Observações (paradas, bagagem etc.)")]
    [StringLength(500, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string? Observacoes { get; set; }

    public Corrida ParaCorrida() => new()
    {
        Origem = Origem,
        OrigemLatitude = OrigemLatitude!.Value,
        OrigemLongitude = OrigemLongitude!.Value,
        Destino = Destino,
        DestinoLatitude = DestinoLatitude!.Value,
        DestinoLongitude = DestinoLongitude!.Value,
        DataHora = DataHora!.Value,
        Intermunicipal = Intermunicipal,
        Observacoes = Observacoes
    };
}

// Entrada de POST /api/cotacoes.
public record CotacaoRequisicao([Required] Coordenada Origem, [Required] Coordenada Destino, bool Intermunicipal = false);

// Saída da API de corridas: só os campos públicos, sem dados de outros usuários.
public record CorridaResumo(
    int ID,
    string Origem,
    string Destino,
    DateTime DataHora,
    int DuracaoMin,
    decimal DistanciaKm,
    decimal PrecoEstimado,
    string Status)
{
    public static CorridaResumo De(Corrida c) =>
        new(c.ID, c.Origem, c.Destino, c.DataHora, c.DuracaoMin, c.DistanciaKm, c.PrecoEstimado, c.Status);
}
