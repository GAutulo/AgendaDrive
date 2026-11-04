using AgendaDrive.Models.Entidades;

namespace AgendaDrive.Models.Negocio;

// ESTRUTURA PARA O GUILHERME IMPLEMENTAR (RF01, RF02, RNF06).
// Siga o padrão da TarifaNegocio: context pelo construtor, métodos async, string? para recusas.
public class UsuarioNegocio
{
    private readonly AgendaDriveContext _contexto;

    public UsuarioNegocio(AgendaDriveContext contexto)
    {
        _contexto = contexto;
    }

    // Devolve null se deu certo, ou o motivo da recusa.
    // TODO:
    // - normalizar o e-mail (Trim + minúsculas) antes de comparar e gravar;
    // - recusar e-mail que já exista;
    // - gerar SenhaHash com PasswordHasher<Usuario>.HashPassword (nunca guardar a senha);
    // - Papel SEMPRE "Usuario": nunca vem do formulário, senão qualquer um vira Administrador.
    public Task<string?> CadastrarAsync(string nome, string email, string? telefone, string senha)
    {
        throw new NotImplementedException();
    }

    // Devolve o usuário se e-mail e senha conferem, ou null.
    // TODO:
    // - buscar pelo e-mail normalizado e só usuário Ativo;
    // - conferir com PasswordHasher<Usuario>.VerifyHashedPassword;
    // - usar a MESMA resposta para "e-mail não existe" e "senha errada" (não revelar quem tem conta).
    public Task<Usuario?> AutenticarAsync(string email, string senha)
    {
        throw new NotImplementedException();
    }
}
