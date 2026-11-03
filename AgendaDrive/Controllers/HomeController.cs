using AgendaDrive.Models;
using Microsoft.AspNetCore.Mvc;

namespace AgendaDrive.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Página de erro amigável. O Program.cs manda para cá:
    // - toda exceção (UseExceptionHandler), depois que o LogExcecaoHandler já gravou o log;
    // - respostas de erro sem conteúdo, como 404 (UseStatusCodePagesWithReExecute, com ?codigo=404).
    // IgnoreAntiforgeryToken: se o erro aconteceu num POST, esta página também é chamada como POST.
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Erro(int? codigo)
    {
        return View(new ErroViewModel
        {
            Codigo = HttpContext.TraceIdentifier,
            StatusCode = codigo
        });
    }
}
