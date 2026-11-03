using System.Data;
using AgendaDrive.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AgendaDrive.Models.Negocio;

// Regras de negócio das tarifas. É a ÚNICA camada que conversa com o banco sobre tarifas:
// o Controller chama estes métodos e nunca usa o AgendaDriveContext diretamente.
// O context chega pelo construtor (injeção de dependência, registrada no Program.cs com AddScoped).
//
// Os métodos que podem ser recusados por uma regra devolvem string?:
// null = deu certo; texto = o motivo da recusa, para o Controller mostrar ao usuário.
public class TarifaNegocio
{
    private readonly AgendaDriveContext _contexto;

    public TarifaNegocio(AgendaDriveContext contexto)
    {
        _contexto = contexto;
    }

    // A ativa aparece primeiro; depois as mais novas.
    public async Task<List<Tarifa>> ListarAsync()
    {
        return await _contexto.Tarifas
            .AsNoTracking()
            .OrderByDescending(t => t.Ativo)
            .ThenByDescending(t => t.CriadoEm)
            .ToListAsync();
    }

    public async Task<Tarifa?> ObterAsync(int id)
    {
        return await _contexto.Tarifas.AsNoTracking().FirstOrDefaultAsync(t => t.ID == id);
    }

    // Tarifa usada para calcular o preço das novas corridas (null se nenhuma estiver ativa).
    public async Task<Tarifa?> ObterAtivaAsync()
    {
        return await _contexto.Tarifas.AsNoTracking().FirstOrDefaultAsync(t => t.Ativo);
    }

    public async Task<int> ContarCorridasAsync(int id)
    {
        return await _contexto.Corridas.CountAsync(c => c.IDTarifa == id);
    }

    // Toda tarifa nasce inativa: para valer, o motorista clica em "Ativar".
    // Assim a regra de "uma ativa por vez" fica concentrada em um único lugar (AtivarAsync).
    public async Task CriarAsync(Tarifa tarifa)
    {
        tarifa.ID = 0;
        tarifa.Ativo = false;
        _contexto.Tarifas.Add(tarifa);
        await _contexto.SaveChangesAsync();
    }

    // Uma tarifa que já foi usada em corridas não muda: cada corrida aponta para a tarifa com que foi calculada.
    // Para mudar valores, o motorista cria uma nova tarifa e a ativa.
    public async Task<string?> AtualizarAsync(Tarifa dados)
    {
        var tarifa = await _contexto.Tarifas.FirstOrDefaultAsync(t => t.ID == dados.ID);
        if (tarifa == null)
            return "Tarifa não encontrada.";

        if (await ContarCorridasAsync(tarifa.ID) > 0)
            return "Esta tarifa já foi usada em corridas e não pode ser alterada. Crie uma nova tarifa e ative-a.";

        // Copia campo a campo: Ativo e CriadoEm nunca vêm do formulário.
        tarifa.TarifaBase = dados.TarifaBase;
        tarifa.ValorKm = dados.ValorKm;
        tarifa.ValorMinuto = dados.ValorMinuto;
        tarifa.ValorKmDeslocamento = dados.ValorKmDeslocamento;
        tarifa.PrecoMinimo = dados.PrecoMinimo;
        tarifa.MargemSegurancaMin = dados.MargemSegurancaMin;
        tarifa.BaseEndereco = dados.BaseEndereco;
        tarifa.BaseLatitude = dados.BaseLatitude;
        tarifa.BaseLongitude = dados.BaseLongitude;

        await _contexto.SaveChangesAsync();
        return null;
    }

    // Ativa uma tarifa e desativa as outras, tudo numa transação (ou faz as duas coisas, ou nenhuma).
    // A ordem importa: primeiro desativa as outras, senão o índice único UX_Tarifas_Ativa
    // recusaria ter duas tarifas ativas ao mesmo tempo.
    public async Task<string?> AtivarAsync(int id)
    {
        if (!await _contexto.Tarifas.AnyAsync(t => t.ID == id))
            return "Tarifa não encontrada.";

        // Com EnableRetryOnFailure (Program.cs), uma transação precisa rodar dentro da "estratégia de execução":
        // se a conexão cair no meio, o EF repete o bloco inteiro, e não só um pedaço dele.
        var estrategia = _contexto.Database.CreateExecutionStrategy();
        await estrategia.ExecuteAsync(async () =>
        {
            // Serializable: se dois cliques em "Ativar" chegarem juntos, o segundo espera o primeiro terminar
            // (no isolamento padrão eles se cruzavam e o índice único recusava a segunda ativação com erro).
            await using var transacao = await _contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            await _contexto.Tarifas
                .Where(t => t.Ativo && t.ID != id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Ativo, false));

            await _contexto.Tarifas
                .Where(t => t.ID == id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Ativo, true));

            await transacao.CommitAsync();
        });

        return null;
    }

    // Exclusão lógica: a tarifa continua no banco, só deixa de ser usada nos novos cálculos.
    public async Task<string?> DesativarAsync(int id)
    {
        var alteradas = await _contexto.Tarifas
            .Where(t => t.ID == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Ativo, false));

        return alteradas == 0 ? "Tarifa não encontrada." : null;
    }

    // Exclusão física só para tarifa que nunca foi usada (ex.: cadastrada por engano) e que não está ativa.
    // Tarifa usada em corridas faz parte do histórico e nunca é apagada.
    public async Task<string?> ExcluirAsync(int id)
    {
        var tarifa = await _contexto.Tarifas.FirstOrDefaultAsync(t => t.ID == id);
        if (tarifa == null)
            return "Tarifa não encontrada.";

        if (tarifa.Ativo)
            return "Desative a tarifa antes de excluí-la.";

        if (await ContarCorridasAsync(id) > 0)
            return "Esta tarifa já foi usada em corridas e não pode ser excluída.";

        _contexto.Tarifas.Remove(tarifa);
        await _contexto.SaveChangesAsync();
        return null;
    }
}
