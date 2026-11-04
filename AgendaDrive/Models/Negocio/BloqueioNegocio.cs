using AgendaDrive.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AgendaDrive.Models.Negocio;

// Períodos em que o motorista não aceita corridas (RF13, RN16).
public class BloqueioNegocio
{
    private readonly AgendaDriveContext _contexto;
    private readonly AgendaNegocio _agendaNegocio;

    public BloqueioNegocio(AgendaDriveContext contexto, AgendaNegocio agendaNegocio)
    {
        _contexto = contexto;
        _agendaNegocio = agendaNegocio;
    }

    public async Task<List<Bloqueio>> ListarAsync()
    {
        return await _contexto.Bloqueios
            .AsNoTracking()
            .Where(b => b.Ativo)
            .OrderBy(b => b.Inicio)
            .ToListAsync();
    }

    public async Task<Bloqueio?> ObterAsync(int id)
    {
        return await _contexto.Bloqueios.AsNoTracking().FirstOrDefaultAsync(b => b.ID == id && b.Ativo);
    }

    public async Task<string?> CriarAsync(Bloqueio dados)
    {
        return await _agendaNegocio.ExecutarExclusivoAsync(async () =>
        {
            var erro = await ValidarAsync(dados);
            if (erro != null)
                return erro;

            _contexto.Bloqueios.Add(new Bloqueio
            {
                Inicio = dados.Inicio,
                Fim = dados.Fim,
                MotivoPrivado = dados.MotivoPrivado,
                Ativo = true
            });
            await _contexto.SaveChangesAsync();
            return null;
        });
    }

    public async Task<string?> AtualizarAsync(Bloqueio dados)
    {
        return await _agendaNegocio.ExecutarExclusivoAsync(async () =>
        {
            var bloqueio = await _contexto.Bloqueios.FirstOrDefaultAsync(b => b.ID == dados.ID && b.Ativo);
            if (bloqueio == null)
                return "Bloqueio não encontrado.";

            var erro = await ValidarAsync(dados);
            if (erro != null)
                return erro;

            bloqueio.Inicio = dados.Inicio;
            bloqueio.Fim = dados.Fim;
            bloqueio.MotivoPrivado = dados.MotivoPrivado;
            await _contexto.SaveChangesAsync();
            return null;
        });
    }

    // Exclusão lógica: o registro continua no banco, só deixa de bloquear a agenda.
    public async Task<string?> CancelarAsync(int id)
    {
        var alterados = await _contexto.Bloqueios
            .Where(b => b.ID == id && b.Ativo)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Ativo, false));

        return alterados == 0 ? "Bloqueio não encontrado." : null;
    }

    private async Task<string?> ValidarAsync(Bloqueio dados)
    {
        if (dados.Fim <= dados.Inicio)
            return "O fim precisa ser depois do início.";

        if (await _agendaNegocio.ExisteCorridaConfirmadaAsync(dados.Inicio, dados.Fim))
            return "Já existe corrida confirmada nesse período. Cancele a corrida antes de bloquear o horário.";

        return null;
    }
}
