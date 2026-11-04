using AgendaDrive.Infra;
using AgendaDrive.Models;
using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaDrive.Controllers;

// Telas de corrida do cliente e do motorista. As ações do motorista têm [Authorize] com papel.
[Authorize]
public class CorridasController : Controller
{
    private readonly CorridaNegocio _corridaNegocio;

    public CorridasController(CorridaNegocio corridaNegocio)
    {
        _corridaNegocio = corridaNegocio;
    }

    public IActionResult Nova()
    {
        return View(new SolicitacaoViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nova(SolicitacaoViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var erro = await _corridaNegocio.SolicitarAsync(modelo.ParaCorrida(), User.IdUsuario());
        if (erro != null)
        {
            ModelState.AddModelError(string.Empty, erro);
            return View(modelo);
        }

        TempData["Sucesso"] = "Solicitação enviada. Agora é só aguardar a resposta do motorista.";
        return RedirectToAction(nameof(Minhas));
    }

    public async Task<IActionResult> Minhas()
    {
        return View(await _corridaNegocio.ListarDoClienteAsync(User.IdUsuario()));
    }

    // Cliente só abre a própria corrida; o motorista abre qualquer uma.
    public async Task<IActionResult> Detalhes(int id)
    {
        var corrida = await _corridaNegocio.ObterAsync(id, User.EhMotorista() ? null : User.IdUsuario());
        if (corrida == null)
            return NotFound();

        return View(corrida);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var erro = await _corridaNegocio.CancelarAsync(id, User.IdUsuario(), User.EhMotorista());
        MostrarResultado(erro, "Corrida cancelada.");
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // Lista do motorista e relatório por período (RF18).
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Solicitacoes(string? status, DateTime? inicio, DateTime? fim)
    {
        ViewBag.Status = status;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;
        return View(await _corridaNegocio.ListarAsync(status, inicio, fim));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Aprovar(int id)
    {
        var erro = await _corridaNegocio.AprovarAsync(id, User.IdUsuario());
        MostrarResultado(erro, "Corrida aprovada. O horário está reservado.");
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Recusar(int id)
    {
        var erro = await _corridaNegocio.RecusarAsync(id, User.IdUsuario());
        MostrarResultado(erro, "Solicitação recusada.");
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Concluir(int id)
    {
        var erro = await _corridaNegocio.ConcluirAsync(id, User.IdUsuario());
        MostrarResultado(erro, "Corrida concluída.");
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AdicionarItem(int id, [Bind("Tipo,Descricao,Valor,Incluso")] ItemAdicional item)
    {
        if (!ModelState.IsValid)
        {
            TempData["Erro"] = "Confira o tipo e o valor do item.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        item.IDCorrida = id;
        var erro = await _corridaNegocio.AdicionarItemAsync(item);
        MostrarResultado(erro, "Item adicionado.");
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> RemoverItem(int id, int idItem)
    {
        var erro = await _corridaNegocio.RemoverItemAsync(id, idItem);
        MostrarResultado(erro, "Item removido.");
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    private void MostrarResultado(string? erro, string mensagemSucesso)
    {
        if (erro == null)
            TempData["Sucesso"] = mensagemSucesso;
        else
            TempData["Erro"] = erro;
    }
}
