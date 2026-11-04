using System.Security.Claims;

namespace AgendaDrive.Infra;

// Atalhos para ler o usuário do cookie de login. O ID vem sempre daqui, nunca do formulário.
public static class UsuarioLogado
{
    public static int IdUsuario(this ClaimsPrincipal usuario) =>
        int.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static bool EhMotorista(this ClaimsPrincipal usuario) =>
        usuario.IsInRole("Administrador");
}
