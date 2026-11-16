using AgendaDrive.Models.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AgendaDrive.Models.Negocio;

// Cadastro e login (RF01, RF02). A senha nunca é guardada: só o hash gerado pelo PasswordHasher.
public class UsuarioNegocio
{
    private readonly AgendaDriveContext _contexto;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public UsuarioNegocio(AgendaDriveContext contexto)
    {
        _contexto = contexto;
    }

    // Devolve null se deu certo, ou o motivo da recusa.
    public async Task<string?> CadastrarAsync(string nome, string email, string? telefone, string senha, string palavraPasse)
    {
        var emailNormalizado = NormalizarEmail(email);

        if (await _contexto.Usuarios.AnyAsync(u => u.Email == emailNormalizado))
            return "Este e-mail já está cadastrado.";

        // Papel fixo: nunca vem do formulário, senão qualquer pessoa viraria Administrador.
        var usuario = new Usuario
        {
            Nome = nome.Trim(),
            Email = emailNormalizado,
            Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
            Papel = "Usuario"
        };
        usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
        usuario.PalavraPasseHash = _hasher.HashPassword(usuario, NormalizarPalavraPasse(palavraPasse));

        _contexto.Usuarios.Add(usuario);
        try
        {
            await _contexto.SaveChangesAsync();
        }
        catch (DbUpdateException erro) when (erro.InnerException is SqlException { Number: 2627 or 2601 })
        {
            // Dois envios do mesmo formulário ao mesmo tempo (clique duplo): os dois passam pela
            // verificação acima, mas o índice único do banco só deixa o primeiro gravar.
            return "Este e-mail já está cadastrado.";
        }
        return null;
    }

    // Devolve o usuário se e-mail e senha conferem. O Controller mostra a mesma mensagem
    // para "e-mail não existe" e "senha errada", para não revelar quem tem conta.
    public async Task<Usuario?> AutenticarAsync(string email, string senha)
    {
        var emailNormalizado = NormalizarEmail(email);
        var usuario = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado && u.Ativo);

        if (usuario == null)
            return null;

        try
        {
            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
            return resultado == PasswordVerificationResult.Failed ? null : usuario;
        }
        catch (FormatException)
        {
            // Os usuários de exemplo do script têm um texto no lugar do hash: não entram.
            return null;
        }
    }

    // Redefinição de senha: e-mail + palavra-passe certos autorizam gravar a nova senha.
    // Mesma mensagem para e-mail inexistente, conta sem palavra-passe e palavra-passe errada,
    // para não revelar quem tem conta (o mesmo cuidado do login).
    public async Task<string?> RedefinirSenhaAsync(string email, string palavraPasse, string novaSenha)
    {
        var emailNormalizado = NormalizarEmail(email);
        var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado && u.Ativo);

        if (usuario == null || !Confere(usuario, usuario.PalavraPasseHash, NormalizarPalavraPasse(palavraPasse)))
            return "E-mail ou palavra-passe incorretos.";

        usuario.SenhaHash = _hasher.HashPassword(usuario, novaSenha);
        await _contexto.SaveChangesAsync();
        return null;
    }

    // Define ou troca a palavra-passe de quem está logado; exige a senha atual.
    public async Task<string?> DefinirPalavraPasseAsync(int idUsuario, string senhaAtual, string palavraPasse)
    {
        var usuario = await _contexto.Usuarios.FirstOrDefaultAsync(u => u.ID == idUsuario && u.Ativo);
        if (usuario == null || !Confere(usuario, usuario.SenhaHash, senhaAtual))
            return "Senha atual incorreta.";

        usuario.PalavraPasseHash = _hasher.HashPassword(usuario, NormalizarPalavraPasse(palavraPasse));
        await _contexto.SaveChangesAsync();
        return null;
    }

    public async Task<bool> TemPalavraPasseAsync(int idUsuario) =>
        await _contexto.Usuarios.AnyAsync(u => u.ID == idUsuario && u.PalavraPasseHash != null);

    // Compara um texto com um hash; hash ausente ou inválido nunca confere.
    private bool Confere(Usuario usuario, string? hash, string texto)
    {
        if (string.IsNullOrEmpty(hash))
            return false;
        try
        {
            return _hasher.VerifyHashedPassword(usuario, hash, texto) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // A palavra-passe é lembrada "de cabeça": maiúsculas e espaços nas pontas não contam.
    private static string NormalizarPalavraPasse(string palavraPasse) => palavraPasse.Trim().ToLowerInvariant();

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
