using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio;
using Microsoft.AspNetCore.Mvc;

namespace AgendaDrive.Controllers;

// CRUD de Tarifas (área do motorista). O Controller só recebe a requisição, chama a TarifaNegocio
// e escolhe a View; nenhuma regra e nenhum acesso ao banco ficam aqui.
// TODO (4a medição, quando existir o login): descomentar a linha abaixo para só o motorista entrar.
// [Authorize(Roles = "Administrador")]
public class TarifasController : Controller
{
    // Campos que o formulário pode enviar. Ativo, CriadoEm e ID ficam de fora de propósito (proteção contra overposting).
    private const string CamposFormulario =
        "TarifaBase,ValorKm,ValorMinuto,ValorKmDeslocamento,PrecoMinimo,MargemSegurancaMin,BaseEndereco,BaseLatitude,BaseLongitude";

    private readonly TarifaNegocio _tarifaNegocio;

    public TarifasController(TarifaNegocio tarifaNegocio)
    {
        _tarifaNegocio = tarifaNegocio;
    }

    // GET /Tarifas
    public async Task<IActionResult> Index()
    {
        return View(await _tarifaNegocio.ListarAsync());
    }

    // GET /Tarifas/Detalhes/5
    public async Task<IActionResult> Detalhes(int id)
    {
        var tarifa = await _tarifaNegocio.ObterAsync(id);
        if (tarifa == null)
            return NotFound();

        ViewBag.QuantidadeCorridas = await _tarifaNegocio.ContarCorridasAsync(id);
        return View(tarifa);
    }

    // GET /Tarifas/Criar
    public IActionResult Criar()
    {
        return View(new Tarifa());
    }

    // POST /Tarifas/Criar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar([Bind(CamposFormulario)] Tarifa tarifa)
    {
        if (!ModelState.IsValid)
            return View(tarifa);

        await _tarifaNegocio.CriarAsync(tarifa);
        TempData["Sucesso"] = "Tarifa criada. Ela está inativa: clique em Ativar para passar a usá-la.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Tarifas/Editar/5
    public async Task<IActionResult> Editar(int id)
    {
        var tarifa = await _tarifaNegocio.ObterAsync(id);
        if (tarifa == null)
            return NotFound();

        if (await _tarifaNegocio.ContarCorridasAsync(id) > 0)
        {
            TempData["Erro"] = "Esta tarifa já foi usada em corridas e não pode ser alterada. Crie uma nova tarifa e ative-a.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        return View(tarifa);
    }

    // POST /Tarifas/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [Bind(CamposFormulario)] Tarifa tarifa)
    {
        // O ID vem só da rota, nunca do corpo do formulário.
        tarifa.ID = id;

        if (!ModelState.IsValid)
            return View(tarifa);

        var erro = await _tarifaNegocio.AtualizarAsync(tarifa);
        if (erro != null)
        {
            ModelState.AddModelError(string.Empty, erro);
            return View(tarifa);
        }

        TempData["Sucesso"] = "Tarifa atualizada.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Tarifas/Ativar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ativar(int id)
    {
        var erro = await _tarifaNegocio.AtivarAsync(id);
        MostrarResultado(erro, "Tarifa ativada. As outras foram desativadas.");
        return RedirectToAction(nameof(Index));
    }

    // POST /Tarifas/Desativar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desativar(int id)
    {
        var erro = await _tarifaNegocio.DesativarAsync(id);
        MostrarResultado(erro, "Tarifa desativada. Sem tarifa ativa, o sistema não calcula preço de novas corridas.");
        return RedirectToAction(nameof(Index));
    }

    // GET /Tarifas/Excluir/5 (página de confirmação)
    public async Task<IActionResult> Excluir(int id)
    {
        var tarifa = await _tarifaNegocio.ObterAsync(id);
        if (tarifa == null)
            return NotFound();

        ViewBag.QuantidadeCorridas = await _tarifaNegocio.ContarCorridasAsync(id);
        return View(tarifa);
    }

    // POST /Tarifas/Excluir/5
    [HttpPost, ActionName("Excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirConfirmado(int id)
    {
        var erro = await _tarifaNegocio.ExcluirAsync(id);
        MostrarResultado(erro, "Tarifa excluída.");
        return RedirectToAction(nameof(Index));
    }

    // TempData sobrevive ao redirecionamento e é lido pelo _Layout (partial _Mensagens).
    private void MostrarResultado(string? erro, string mensagemSucesso)
    {
        if (erro == null)
            TempData["Sucesso"] = mensagemSucesso;
        else
            TempData["Erro"] = erro;
    }
}
