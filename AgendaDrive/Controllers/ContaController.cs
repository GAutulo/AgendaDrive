using System.Security.Claims;
using AgendaDrive.Infra;
using AgendaDrive.Models;
using AgendaDrive.Models.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AgendaDrive.Controllers;

// Cadastro, entrada e saída. Quem confere senha é a UsuarioNegocio; aqui só se cria o cookie de login.
public class ContaController : Controller
{
    private readonly UsuarioNegocio _usuarioNegocio;

    public ContaController(UsuarioNegocio usuarioNegocio)
    {
        _usuarioNegocio = usuarioNegocio;
    }

    public IActionResult Entrar(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Entrar(LoginViewModel modelo, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
            return View(modelo);

        var usuario = await _usuarioNegocio.AutenticarAsync(modelo.Email, modelo.Senha);
        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
            return View(modelo);
        }

        // Claims: o que fica gravado (assinado) no cookie. O papel é o que o [Authorize(Roles = ...)] confere.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.ID.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Papel)
        };
        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identidade));

        // Só volta para endereços do próprio site, para ninguém usar o login como trampolim para um site falso.
        if (Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Cadastrar()
    {
        return View(new CadastroViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Cadastrar(CadastroViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var erro = await _usuarioNegocio.CadastrarAsync(modelo.Nome, modelo.Email, modelo.Telefone, modelo.Senha, modelo.PalavraPasse);
        if (erro != null)
        {
            ModelState.AddModelError(string.Empty, erro);
            return View(modelo);
        }

        TempData["Sucesso"] = "Conta criada. Agora é só entrar.";
        return RedirectToAction(nameof(Entrar));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult EsqueciSenha()
    {
        return View(new RedefinirSenhaViewModel());
    }

    // Mesmo limite de tentativas do login: impede adivinhar a palavra-passe por força bruta.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> EsqueciSenha(RedefinirSenhaViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var erro = await _usuarioNegocio.RedefinirSenhaAsync(modelo.Email, modelo.PalavraPasse, modelo.NovaSenha);
        if (erro != null)
        {
            ModelState.AddModelError(string.Empty, erro);
            return View(modelo);
        }

        TempData["Sucesso"] = "Senha redefinida. Entre com a nova senha.";
        return RedirectToAction(nameof(Entrar));
    }

    [Authorize]
    public async Task<IActionResult> MinhaConta()
    {
        ViewBag.TemPalavraPasse = await _usuarioNegocio.TemPalavraPasseAsync(User.IdUsuario());
        return View(new PalavraPasseViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> MinhaConta(PalavraPasseViewModel modelo)
    {
        if (ModelState.IsValid)
        {
            var erro = await _usuarioNegocio.DefinirPalavraPasseAsync(User.IdUsuario(), modelo.SenhaAtual, modelo.PalavraPasse);
            if (erro == null)
            {
                TempData["Sucesso"] = "Palavra-passe salva. Use-a se esquecer a senha.";
                return RedirectToAction(nameof(MinhaConta));
            }
            ModelState.AddModelError(string.Empty, erro);
        }

        ViewBag.TemPalavraPasse = await _usuarioNegocio.TemPalavraPasseAsync(User.IdUsuario());
        return View(modelo);
    }

    public IActionResult AcessoNegado()
    {
        return View();
    }
}
