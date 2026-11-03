using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaDrive.Models.Entidades;

// Espelha a tabela HistoricosStatus: cada mudanca de status de uma corrida gera um registro.
public class HistoricoStatus
{
    [Key]
    public int ID { get; set; }

    public int IDCorrida { get; set; }

    // Quem fez a mudanca (cliente ou motorista).
    public int IDUsuario { get; set; }

    [StringLength(20)]
    public string? StatusAnterior { get; set; }

    [StringLength(20)]
    public string StatusNovo { get; set; } = null!;

    public DateTime DataHora { get; set; }

    [ForeignKey(nameof(IDCorrida))]
    [InverseProperty(nameof(Corrida.HistoricosStatus))]
    public virtual Corrida Corrida { get; set; } = null!;

    [ForeignKey(nameof(IDUsuario))]
    [InverseProperty(nameof(Usuario.HistoricosStatus))]
    public virtual Usuario Usuario { get; set; } = null!;
}
