using AgendaDrive.Models.Entidades;
using Microsoft.AspNetCore.Identity;
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
    public async Task<string?> CadastrarAsync(string nome, string email, string? telefone, string senha)
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

        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();
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

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
