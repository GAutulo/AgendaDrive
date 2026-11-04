using AgendaDrive.Models.Entidades;

namespace AgendaDrive.Models.Negocio;

// ESTRUTURA PARA O GUILHERME IMPLEMENTAR (RF05, RF12, RN11 a RN15).
// Uma única classe sabe responder "este período está livre?"; quem precisa da resposta chama aqui.
public class AgendaNegocio
{
    private readonly AgendaDriveContext _contexto;

    public AgendaNegocio(AgendaDriveContext contexto)
    {
        _contexto = contexto;
    }

    // true se [inicio, fim) não bate com nenhuma corrida Confirmada nem Bloqueio Ativo.
    // TODO:
    // - dois períodos A e B se sobrepõem quando A.inicio < B.fim E B.inicio < A.fim;
    // - fim de uma corrida = DataHora + DuracaoMin + MargemSegurancaMin da tarifa DELA (RN14);
    // - Pendente, Recusada, Cancelada e Concluida não ocupam a agenda (RN15);
    // - ignorarCorridaId existe para a aprovação não conflitar com a própria corrida.
    public Task<bool> PeriodoLivreAsync(DateTime inicio, DateTime fim, int? ignorarCorridaId = null)
    {
        throw new NotImplementedException();
    }

    // Períodos ocupados do dia, para a tela do cliente.
    // TODO: RN11: devolver só início e fim. Nada de nome de cliente nem MotivoPrivado.
    public Task<List<(DateTime Inicio, DateTime Fim)>> ListarOcupadosAsync(DateOnly data)
    {
        throw new NotImplementedException();
    }
}
