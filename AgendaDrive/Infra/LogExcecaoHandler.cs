using Microsoft.AspNetCore.Diagnostics;

namespace AgendaDrive.Infra;

// Recebe TODA exceção não tratada do site (o ASP.NET Core chama este handler através do UseExceptionHandler).
// Grava no log e devolve false: "não respondi nada", então o ASP.NET Core mostra a página de erro amigável (/Home/Erro).
public class LogExcecaoHandler : IExceptionHandler
{
    private readonly ArquivoLog _log;

    public LogExcecaoHandler(ArquivoLog log)
    {
        _log = log;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext http, Exception excecao, CancellationToken cancelamento)
    {
        _log.RegistrarExcecao(excecao, http);
        return ValueTask.FromResult(false);
    }
}
