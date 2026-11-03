using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaDrive.Models.Entidades;

// Espelha a tabela ItensAdicionais. Tipo: Pedagio, Estacionamento, Espera, RetornoVazio ou Outro.
// Incluso = true quando o valor entra no preco estimado.
public class ItemAdicional
{
    [Key]
    public int ID { get; set; }

    public int IDCorrida { get; set; }

    [StringLength(30)]
    public string Tipo { get; set; } = null!;

    [StringLength(200)]
    public string? Descricao { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Valor { get; set; }

    public bool Incluso { get; set; } = true;

    [ForeignKey(nameof(IDCorrida))]
    [InverseProperty(nameof(Corrida.ItensAdicionais))]
    public virtual Corrida Corrida { get; set; } = null!;
}
