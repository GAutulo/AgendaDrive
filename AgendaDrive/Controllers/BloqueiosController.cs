using AgendaDrive.Infra;
using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaDrive.Controllers;

// Agenda do motorista: períodos em que ele não aceita corridas.
[Authorize(Roles = "Administrador")]
public class BloqueiosController : Controller
{
    private const string CamposFormulario = "Inicio,Fim,MotivoPrivado";

    private readonly BloqueioNegocio _bloqueioNegocio;

    public BloqueiosController(BloqueioNegocio bloqueioNegocio)
    {
        _bloqueioNegocio = bloqueioNegocio;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _bloqueioNegocio.ListarAsync());
    }

    public IActionResult Criar()
    {
        var amanha = Relogio.Agora.Date.AddDays(1);
        return View(new Bloqueio { Inicio = amanha.AddHours(8), Fim = amanha.AddHours(12) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar([Bind(CamposFormulario)] Bloqueio bloqueio)
    {
        if (!ModelState.IsValid)
            return View(bloqueio);

        var erro = await _bloqueioNegocio.CriarAsync(bloqueio);
        if (erro != null)
        {
            ModelState.AddModelError(string.Empty, erro);
            return View(bloqueio);
        }

        TempData["Sucesso"] = "Bloqueio criado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var bloqueio = await _bloqueioNegocio.ObterAsync(id);
        if (bloqueio == null)
            return NotFound();

        return View(bloqueio);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [Bind(CamposFormulario)] Bloqueio bloqueio)
    {
        bloqueio.ID = id;

        if (!ModelState.IsValid)
            return View(bloqueio);

        var erro = await _bloqueioNegocio.AtualizarAsync(bloqueio);
        if (erro != null)
        {
            ModelState.AddModelError(string.Empty, erro);
            return View(bloqueio);
        }

        TempData["Sucesso"] = "Bloqueio atualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var erro = await _bloqueioNegocio.CancelarAsync(id);
        if (erro == null)
            TempData["Sucesso"] = "Bloqueio cancelado.";
        else
            TempData["Erro"] = erro;

        return RedirectToAction(nameof(Index));
    }
}
