using System.Data;
using AgendaDrive.Infra;
using AgendaDrive.Models.Entidades;
using Microsoft.EntityFrameworkCore;

namespace AgendaDrive.Models.Negocio;

// Só início e fim: o cliente nunca vê de quem é o horário nem o motivo do bloqueio (RN11).
public record PeriodoOcupado(DateTime Inicio, DateTime Fim);

// Responde "este período está livre?" (RF05, RF12, RN11 a RN15).
public class AgendaNegocio
{
    private readonly AgendaDriveContext _contexto;

    public AgendaNegocio(AgendaDriveContext contexto)
    {
        _contexto = contexto;
    }

    // Período ocupado por uma corrida (RN14): duração da rota + margem de segurança da tarifa.
    public static DateTime CalcularFim(DateTime inicio, int duracaoMin, int margemSegurancaMin) =>
        inicio.AddMinutes(duracaoMin + margemSegurancaMin);

    // Dois períodos A e B se sobrepõem quando A começa antes de B acabar E B começa antes de A acabar.
    public static bool SeSobrepoem(DateTime inicioA, DateTime fimA, DateTime inicioB, DateTime fimB) =>
        inicioA < fimB && inicioB < fimA;

    // Só corridas Confirmadas ocupam a agenda (RN15).
    public async Task<bool> ExisteCorridaConfirmadaAsync(DateTime inicio, DateTime fim, int? ignorarCorridaId = null)
    {
        var ocupados = await ListarCorridasConfirmadasAsync(inicio, fim, ignorarCorridaId);
        return ocupados.Any(c => SeSobrepoem(inicio, fim, c.Inicio, c.Fim));
    }

    public async Task<bool> PeriodoLivreAsync(DateTime inicio, DateTime fim, int? ignorarCorridaId = null)
    {
        var bloqueado = await _contexto.Bloqueios
            .AnyAsync(b => b.Ativo && b.Inicio < fim && b.Fim > inicio);

        return !bloqueado && !await ExisteCorridaConfirmadaAsync(inicio, fim, ignorarCorridaId);
    }

    public async Task<List<PeriodoOcupado>> ListarOcupadosAsync(DateOnly data)
    {
        var inicio = data.ToDateTime(TimeOnly.MinValue);
        var fim = inicio.AddDays(1);

        var corridas = await ListarCorridasConfirmadasAsync(inicio, fim, null);
        var bloqueios = await _contexto.Bloqueios
            .Where(b => b.Ativo && b.Inicio < fim && b.Fim > inicio)
            .Select(b => new PeriodoOcupado(b.Inicio, b.Fim))
            .ToListAsync();

        return corridas
            .Where(c => SeSobrepoem(inicio, fim, c.Inicio, c.Fim))
            .Concat(bloqueios)
            .OrderBy(p => p.Inicio)
            .ToList();
    }

    // Roda uma operação que confere a agenda e depois grava, sem deixar outra requisição entrar no meio.
    // Serializable: dois motoristas (ou duas abas) aprovando o mesmo horário ficam em fila, e só o primeiro passa.
    // Se o banco escolher uma das duas como vítima de deadlock, o EnableRetryOnFailure repete o bloco.
    public async Task<string?> ExecutarExclusivoAsync(Func<Task<string?>> operacao)
    {
        var estrategia = _contexto.Database.CreateExecutionStrategy();
        return await estrategia.ExecuteAsync(async () =>
        {
            // Numa repetição, descarta o que a tentativa anterior deixou carregado.
            _contexto.ChangeTracker.Clear();

            await using var transacao = await _contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var erro = await operacao();

            if (erro == null)
                await transacao.CommitAsync();

            return erro;
        });
    }

    private async Task<List<PeriodoOcupado>> ListarCorridasConfirmadasAsync(DateTime inicio, DateTime fim, int? ignorarCorridaId)
    {
        // O fim depende da margem de cada tarifa, então o filtro fino é feito em C#.
        // O banco só corta o que começa depois do período ou muito antes dele.
        var limiteAnterior = inicio.AddDays(-2);

        var corridas = await _contexto.Corridas
            .Where(c => c.Status == "Confirmada"
                && c.ID != ignorarCorridaId
                && c.DataHora < fim
                && c.DataHora > limiteAnterior)
            .Select(c => new { c.DataHora, c.DuracaoMin, c.Tarifa.MargemSegurancaMin })
            .ToListAsync();

        return corridas
            .Select(c => new PeriodoOcupado(c.DataHora, CalcularFim(c.DataHora, c.DuracaoMin, c.MargemSegurancaMin)))
            .ToList();
    }
}
