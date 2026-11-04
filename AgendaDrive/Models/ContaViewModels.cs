using System.ComponentModel.DataAnnotations;

namespace AgendaDrive.Models;

// ViewModels em vez da entidade Usuario: a tela nunca recebe campos como Papel ou SenhaHash.
public class LoginViewModel
{
    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = "";

    [Display(Name = "Senha")]
    [Required(ErrorMessage = "Informe a {0}.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = "";
}

public class CadastroViewModel
{
    [Display(Name = "Nome completo")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [StringLength(100, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string Nome { get; set; } = "";

    [Display(Name = "E-mail")]
    [Required(ErrorMessage = "Informe o {0}.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string Email { get; set; } = "";

    [Display(Name = "Telefone (WhatsApp)")]
    [StringLength(20, ErrorMessage = "{0} pode ter no máximo {1} caracteres.")]
    public string? Telefone { get; set; }

    [Display(Name = "Senha")]
    [Required(ErrorMessage = "Informe a {0}.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha precisa ter pelo menos {2} caracteres.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = "";

    [Display(Name = "Confirmar senha")]
    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = "";
}
