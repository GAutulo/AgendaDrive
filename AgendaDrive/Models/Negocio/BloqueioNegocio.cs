using AgendaDrive.Models.Entidades;

namespace AgendaDrive.Models.Negocio;

// ESTRUTURA PARA O GUILHERME IMPLEMENTAR (RF13, RN16). É o CRUD mais parecido com o de Tarifas: bom para começar.
public class BloqueioNegocio
{
    private readonly AgendaDriveContext _contexto;
    private readonly AgendaNegocio _agendaNegocio;

    public BloqueioNegocio(AgendaDriveContext contexto, AgendaNegocio agendaNegocio)
    {
        _contexto = contexto;
        _agendaNegocio = agendaNegocio;
    }

    // TODO: só os Ativos, ordenados por Inicio.
    public Task<List<Bloqueio>> ListarAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Bloqueio?> ObterAsync(int id)
    {
        throw new NotImplementedException();
    }

    // TODO:
    // - recusar se Fim <= Inicio;
    // - RN16: recusar se o período sobrepõe corrida Confirmada (use o AgendaNegocio) e dizer o motivo.
    public Task<string?> CriarAsync(Bloqueio bloqueio)
    {
        throw new NotImplementedException();
    }

    // TODO: mesmas validações do CriarAsync. Copie campo a campo, como a TarifaNegocio.AtualizarAsync.
    public Task<string?> AtualizarAsync(Bloqueio dados)
    {
        throw new NotImplementedException();
    }

    // TODO: exclusão lógica (Ativo = false), nunca Remove.
    public Task<string?> CancelarAsync(int id)
    {
        throw new NotImplementedException();
    }
}
