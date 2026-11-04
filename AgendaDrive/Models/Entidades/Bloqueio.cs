using System.ComponentModel.DataAnnotations;

namespace AgendaDrive.Models.Entidades;

// Espelha a tabela Bloqueios: periodo em que o motorista nao aceita corridas.
public class Bloqueio
{
    [Key]
    public int ID { get; set; }

    [Display(Name = "Início")]
    [Required(ErrorMessage = "Informe o {0}.")]
    public DateTime Inicio { get; set; }

    [Display(Name = "Fim")]
    [Required(ErrorMessage = "Informe o {0}.")]
    public DateTime Fim { get; set; }

    // So o motorista ve o motivo; o cliente ve apenas "horario indisponivel".
    [Display(Name = "Motivo (só você vê)")]
    [StringLength(200, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string? MotivoPrivado { get; set; }

    public bool Ativo { get; set; } = true;
}
