namespace AgendaDrive.Models;

// Dados da página de erro amigável. Nada de mensagem técnica aqui: o detalhe vai só para o log.
public class ErroViewModel
{
    // Mesmo código gravado no log (TraceIdentifier), para achar o registro quando alguém relatar o erro.
    public string? Codigo { get; set; }

    // 404, 403... ou null quando foi uma exceção (500).
    public int? StatusCode { get; set; }
}
