using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaDrive.Models.Entidades;

// Espelha a tabela Corridas. Status: Pendente, Confirmada, Recusada, Cancelada ou Concluida.
public class Corrida
{
    [Key]
    public int ID { get; set; }

    public int IDUsuario { get; set; }

    public int IDTarifa { get; set; }

    [StringLength(250)]
    public string Origem { get; set; } = null!;

    [Column(TypeName = "decimal(9, 6)")]
    public decimal OrigemLatitude { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal OrigemLongitude { get; set; }

    [StringLength(250)]
    public string Destino { get; set; } = null!;

    [Column(TypeName = "decimal(9, 6)")]
    public decimal DestinoLatitude { get; set; }

    [Column(TypeName = "decimal(9, 6)")]
    public decimal DestinoLongitude { get; set; }

    public DateTime DataHora { get; set; }

    public int DuracaoMin { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal DistanciaKm { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal DeslocamentoKm { get; set; }

    public bool Intermunicipal { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal PrecoEstimado { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Pendente";

    [StringLength(500)]
    public string? Observacoes { get; set; }

    public DateTime CriadoEm { get; set; }

    [ForeignKey(nameof(IDUsuario))]
    [InverseProperty(nameof(Usuario.Corridas))]
    public virtual Usuario Usuario { get; set; } = null!;

    [ForeignKey(nameof(IDTarifa))]
    [InverseProperty(nameof(Tarifa.Corridas))]
    public virtual Tarifa Tarifa { get; set; } = null!;

    [InverseProperty(nameof(ItemAdicional.Corrida))]
    public virtual ICollection<ItemAdicional> ItensAdicionais { get; set; } = new List<ItemAdicional>();

    [InverseProperty(nameof(HistoricoStatus.Corrida))]
    public virtual ICollection<HistoricoStatus> HistoricosStatus { get; set; } = new List<HistoricoStatus>();
}
