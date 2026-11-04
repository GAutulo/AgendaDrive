using AgendaDrive.Models.Entidades;

namespace AgendaDrive.Models.Negocio;

// ESTRUTURA PARA O GUILHERME IMPLEMENTAR (RF10 a RF16, RN06, RN10, RN12, RN13, RN18).
// Transições válidas: Pendente -> Confirmada, Recusada, Cancelada; Confirmada -> Concluida, Cancelada.
// Recusada, Cancelada e Concluida são finais. Toda mudança grava um HistoricoStatus.
public class CorridaNegocio
{
    private readonly AgendaDriveContext _contexto;
    private readonly AgendaNegocio _agendaNegocio;
    private readonly PrecoNegocio _precoNegocio;

    public CorridaNegocio(AgendaDriveContext contexto, AgendaNegocio agendaNegocio, PrecoNegocio precoNegocio)
    {
        _contexto = contexto;
        _agendaNegocio = agendaNegocio;
        _precoNegocio = precoNegocio;
    }

    // TODO:
    // - RN06: chamar _precoNegocio.CotarAsync de novo; nunca aceitar preço, km ou minutos do navegador;
    // - recusar DataHora no passado;
    // - RN12: conferir a agenda com PeriodoLivreAsync;
    // - gravar Status "Pendente", IDTarifa e os valores da cotação (RN10);
    // - gravar o primeiro HistoricoStatus (StatusAnterior = null) no mesmo SaveChanges.
    public Task<string?> SolicitarAsync(Corrida dados, int idUsuario)
    {
        throw new NotImplementedException();
    }

    // TODO: RN18: filtrar por idUsuario. Traga Tarifa e ItensAdicionais só se a tela usar.
    public Task<List<Corrida>> ListarDoClienteAsync(int idUsuario)
    {
        throw new NotImplementedException();
    }

    // TODO: visão do motorista, com filtros opcionais por status e período (RF18).
    public Task<List<Corrida>> ListarAsync(string? status, DateTime? inicio, DateTime? fim)
    {
        throw new NotImplementedException();
    }

    // TODO: RN18: se idUsuario vier preenchido, só devolve a corrida se for dele (null = não encontrada).
    public Task<Corrida?> ObterAsync(int id, int? idUsuario)
    {
        throw new NotImplementedException();
    }

    // TODO:
    // - só de Pendente;
    // - RN12/RN13: revalidar a agenda E confirmar dentro de uma transação Serializable, como a
    //   TarifaNegocio.AtivarAsync; é o que garante "dois clientes, mesmo horário, só uma confirmada".
    public Task<string?> AprovarAsync(int id, int idMotorista)
    {
        throw new NotImplementedException();
    }

    // TODO: só de Pendente.
    public Task<string?> RecusarAsync(int id, int idMotorista)
    {
        throw new NotImplementedException();
    }

    // TODO:
    // - de Pendente ou Confirmada;
    // - o cliente só cancela a própria corrida (RN18), o motorista cancela qualquer uma;
    // - antecedência mínima: depende da sua política de cancelamento (pendência de negócio).
    public Task<string?> CancelarAsync(int id, int idUsuario, bool ehMotorista)
    {
        throw new NotImplementedException();
    }

    // TODO: só de Confirmada.
    public Task<string?> ConcluirAsync(int id, int idMotorista)
    {
        throw new NotImplementedException();
    }

    // TODO: método privado único usado por todas as mudanças acima: confere a transição,
    // muda o Status e adiciona o HistoricoStatus. Evita repetir a regra cinco vezes.
    private void MudarStatus(Corrida corrida, string statusNovo, int idUsuario)
    {
        throw new NotImplementedException();
    }
}
