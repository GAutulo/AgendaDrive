using System.Text;
using Microsoft.AspNetCore.Diagnostics;

namespace AgendaDrive.Infra;

// Grava exceções em App_Data/logs/yyyy-MM-dd.log (um arquivo por dia).
// Cada registro tem: data/hora, código do erro, usuário, URL, mensagem e stack trace.
// Registrado no Program.cs como Singleton (uma única instância para o site inteiro).
public class ArquivoLog
{
    // Duas requisições podem dar erro ao mesmo tempo; o lock impede que escrevam no arquivo juntas.
    private static readonly object Trava = new();

    private readonly string _pasta;

    public ArquivoLog(IWebHostEnvironment ambiente)
    {
        _pasta = Path.Combine(ambiente.ContentRootPath, "App_Data", "logs");
    }

    public void RegistrarExcecao(Exception excecao, HttpContext? http)
    {
        try
        {
            var usuario = http?.User.Identity?.IsAuthenticated == true ? http.User.Identity.Name : "anonimo";

            // Quando o erro passa pelo UseExceptionHandler, Request.Path já foi trocado para /Home/Erro;
            // o caminho original que deu o erro fica guardado no IExceptionHandlerPathFeature.
            var caminho = http?.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? http?.Request.Path.Value;
            var url = http == null ? "-" : $"{http.Request.Method} {caminho}{http.Request.QueryString}";

            var texto = new StringBuilder()
                .AppendLine("==================================================")
                .AppendLine($"Data/hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                .AppendLine($"Codigo:    {http?.TraceIdentifier ?? "-"}")
                .AppendLine($"Usuario:   {LimparLinha(usuario)}")
                .AppendLine($"URL:       {LimparLinha(url)}")
                .AppendLine($"Mensagem:  {excecao.Message}")
                // ToString() traz o tipo da exceção, a mensagem, as exceções internas e o stack trace.
                .AppendLine(excecao.ToString())
                .ToString();

            var arquivo = Path.Combine(_pasta, $"{DateTime.Now:yyyy-MM-dd}.log");

            lock (Trava)
            {
                Directory.CreateDirectory(_pasta);
                File.AppendAllText(arquivo, texto, Encoding.UTF8);
            }
        }
        catch
        {
            // Se nem o log conseguir ser gravado (disco cheio, sem permissão), não derruba o site por causa disso.
        }
    }

    // Tira quebras de linha de dados que vêm do usuário (URL, nome), para ninguém forjar linhas falsas no log.
    private static string LimparLinha(string? valor) =>
        (valor ?? "-").Replace('\r', ' ').Replace('\n', ' ');
}
