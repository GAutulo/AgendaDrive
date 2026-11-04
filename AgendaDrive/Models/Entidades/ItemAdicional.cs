using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

    [Display(Name = "Descrição")]
    [StringLength(200, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string? Descricao { get; set; }

    [Display(Name = "Valor (R$)")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [Range(0, 99999999.99, ErrorMessage = "{0} deve ser zero ou positivo.")]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal Valor { get; set; }

    [Display(Name = "Incluído no preço")]
    public bool Incluso { get; set; } = true;

    // A navegação nunca vem do formulário, então não entra na validação.
    [ValidateNever]
    [ForeignKey(nameof(IDCorrida))]
    [InverseProperty(nameof(Corrida.ItensAdicionais))]
    public virtual Corrida Corrida { get; set; } = null!;
}
