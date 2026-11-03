using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaDrive.Models.Entidades;

// Espelha a tabela Tarifas. So uma tarifa pode estar ativa por vez:
// o banco garante com o indice UX_Tarifas_Ativa e a TarifaNegocio garante na regra.
// As anotacoes [Display], [Required] e [Range] servem para os formularios (validacao no navegador e no servidor).
// Os textos entre aspas aparecem na tela, por isso tem acento; os nomes no codigo nao.
public class Tarifa
{
    [Key]
    public int ID { get; set; }

    [Display(Name = "Tarifa-base (R$)")]
    [Required(ErrorMessage = "Informe a {0}.")]
    [Range(0, 99999999.99, ErrorMessage = "{0} deve ser zero ou positiva.")]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal TarifaBase { get; set; }

    [Display(Name = "Valor por km (R$)")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [Range(0, 99999999.99, ErrorMessage = "{0} deve ser zero ou positivo.")]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal ValorKm { get; set; }

    [Display(Name = "Valor por minuto (R$)")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [Range(0, 99999999.99, ErrorMessage = "{0} deve ser zero ou positivo.")]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal ValorMinuto { get; set; }

    [Display(Name = "Valor por km de deslocamento (R$)")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [Range(0, 99999999.99, ErrorMessage = "{0} deve ser zero ou positivo.")]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal ValorKmDeslocamento { get; set; }

    [Display(Name = "Preço mínimo (R$)")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [Range(0, 99999999.99, ErrorMessage = "{0} deve ser zero ou positivo.")]
    [Column(TypeName = "decimal(10, 2)")]
    public decimal PrecoMinimo { get; set; }

    [Display(Name = "Margem de segurança (min)")]
    [Required(ErrorMessage = "Informe a {0}.")]
    [Range(0, 1440, ErrorMessage = "{0} deve ficar entre {1} e {2} minutos.")]
    public int MargemSegurancaMin { get; set; } = 30;

    [Display(Name = "Endereço de partida")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [StringLength(250, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string BaseEndereco { get; set; } = null!;

    [Display(Name = "Latitude da partida")]
    [Required(ErrorMessage = "Informe a {0}.")]
    [Range(-90, 90, ErrorMessage = "{0} deve ficar entre {1} e {2}.")]
    [Column(TypeName = "decimal(9, 6)")]
    public decimal BaseLatitude { get; set; }

    [Display(Name = "Longitude da partida")]
    [Required(ErrorMessage = "Informe a {0}.")]
    [Range(-180, 180, ErrorMessage = "{0} deve ficar entre {1} e {2}.")]
    [Column(TypeName = "decimal(9, 6)")]
    public decimal BaseLongitude { get; set; }

    [Display(Name = "Ativa")]
    public bool Ativo { get; set; }

    [Display(Name = "Criada em")]
    public DateTime CriadoEm { get; set; }

    [InverseProperty(nameof(Corrida.Tarifa))]
    public virtual ICollection<Corrida> Corridas { get; set; } = new List<Corrida>();
}
