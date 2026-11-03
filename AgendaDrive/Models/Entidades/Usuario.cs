using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgendaDrive.Models.Entidades;

// Espelha a tabela Usuarios. Papel: "Administrador" (o motorista) ou "Usuario" (cliente).
public class Usuario
{
    [Key]
    public int ID { get; set; }

    [StringLength(100)]
    public string Nome { get; set; } = null!;

    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? Telefone { get; set; }

    // Nunca guardar a senha em texto puro: aqui vai so o hash (PasswordHasher na 4a medicao).
    [StringLength(255)]
    public string SenhaHash { get; set; } = null!;

    [StringLength(20)]
    public string Papel { get; set; } = null!;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; }

    [InverseProperty(nameof(Corrida.Usuario))]
    public virtual ICollection<Corrida> Corridas { get; set; } = new List<Corrida>();

    [InverseProperty(nameof(HistoricoStatus.Usuario))]
    public virtual ICollection<HistoricoStatus> HistoricosStatus { get; set; } = new List<HistoricoStatus>();
}
