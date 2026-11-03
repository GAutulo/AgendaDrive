using System.ComponentModel.DataAnnotations;

namespace AgendaDrive.Models.Entidades;

// Espelha a tabela Bloqueios: periodo em que o motorista nao aceita corridas.
public class Bloqueio
{
    [Key]
    public int ID { get; set; }

    public DateTime Inicio { get; set; }

    public DateTime Fim { get; set; }

    // So o motorista ve o motivo; o cliente ve apenas "horario indisponivel".
    [StringLength(200)]
    public string? MotivoPrivado { get; set; }

    public bool Ativo { get; set; } = true;
}
