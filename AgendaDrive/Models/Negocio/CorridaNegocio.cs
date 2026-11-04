using AgendaDrive.Infra;
using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio.Rotas;
using Microsoft.EntityFrameworkCore;

namespace AgendaDrive.Models.Negocio;

// Ciclo de vida da corrida (RF10 a RF16, RN06, RN10, RN12, RN13, RN17, RN18).
public class CorridaNegocio
{
    // Transições válidas. Recusada, Cancelada e Concluida são finais.
    private static readonly Dictionary<string, string[]> Transicoes = new()
    {
        ["Pendente"] = ["Confirmada", "Recusada", "Cancelada"],
        ["Confirmada"] = ["Concluida", "Cancelada"]
    };

    public static readonly string[] Status = ["Pendente", "Confirmada", "Recusada", "Cancelada", "Concluida"];
    public static readonly string[] TiposItem = ["Pedagio", "Estacionamento", "Espera", "RetornoVazio", "Outro"];

    private readonly AgendaDriveContext _contexto;
    private readonly AgendaNegocio _agendaNegocio;
    private readonly PrecoNegocio _precoNegocio;

    public CorridaNegocio(AgendaDriveContext contexto, AgendaNegocio agendaNegocio, PrecoNegocio precoNegocio)
    {
        _contexto = contexto;
        _agendaNegocio = agendaNegocio;
        _precoNegocio = precoNegocio;
    }

    // Recebe só o que o cliente informa. Rota, preço e tarifa são sempre recalculados aqui (RN06);
    // assim, alterar o preço no navegador não muda nada. Os valores ficam gravados na corrida (RN10).
    public async Task<string?> SolicitarAsync(Corrida dados, int idUsuario)
    {
        if (dados.DataHora <= Relogio.Agora)
            return "Escolha uma data e hora no futuro.";

        var cotacao = await _precoNegocio.CotarAsync(
            new Coordenada(dados.OrigemLatitude, dados.OrigemLongitude),
            new Coordenada(dados.DestinoLatitude, dados.DestinoLongitude));

        if (cotacao == null)
            return "Não foi possível calcular a rota agora. Confira os endereços ou tente novamente.";

        var tarifa = await _contexto.Tarifas.AsNoTracking().FirstAsync(t => t.ID == cotacao.IDTarifa);
        var fim = AgendaNegocio.CalcularFim(dados.DataHora, cotacao.DuracaoMin, tarifa.MargemSegurancaMin);

        if (!await _agendaNegocio.PeriodoLivreAsync(dados.DataHora, fim))
            return "Este horário não está disponível. Escolha outro.";

        var corrida = new Corrida
        {
            IDUsuario = idUsuario,
            IDTarifa = cotacao.IDTarifa,
            Origem = dados.Origem,
            OrigemLatitude = dados.OrigemLatitude,
            OrigemLongitude = dados.OrigemLongitude,
            Destino = dados.Destino,
            DestinoLatitude = dados.DestinoLatitude,
            DestinoLongitude = dados.DestinoLongitude,
            DataHora = dados.DataHora,
            DuracaoMin = cotacao.DuracaoMin,
            DistanciaKm = cotacao.DistanciaKm,
            DeslocamentoKm = cotacao.DeslocamentoKm,
            Intermunicipal = dados.Intermunicipal,
            PrecoEstimado = cotacao.PrecoEstimado,
            Observacoes = dados.Observacoes,
            CriadoEm = Relogio.Agora
        };
        corrida.HistoricosStatus.Add(NovoHistorico(null, "Pendente", idUsuario));

        _contexto.Corridas.Add(corrida);
        await _contexto.SaveChangesAsync();
        return null;
    }

    public async Task<List<Corrida>> ListarDoClienteAsync(int idUsuario)
    {
        return await _contexto.Corridas
            .AsNoTracking()
            .Where(c => c.IDUsuario == idUsuario)
            .OrderByDescending(c => c.DataHora)
            .ToListAsync();
    }

    // Visão do motorista, também usada como relatório por período (RF18).
    public async Task<List<Corrida>> ListarAsync(string? status, DateTime? inicio, DateTime? fim)
    {
        var consulta = _contexto.Corridas.AsNoTracking().Include(c => c.Usuario).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            consulta = consulta.Where(c => c.Status == status);
        if (inicio != null)
            consulta = consulta.Where(c => c.DataHora >= inicio);
        if (fim != null)
            consulta = consulta.Where(c => c.DataHora < fim.Value.AddDays(1));

        return await consulta.OrderBy(c => c.DataHora).ToListAsync();
    }

    // Com idUsuario preenchido, só devolve se a corrida for dele: outra pessoa recebe "não encontrada" (RN18).
    public async Task<Corrida?> ObterAsync(int id, int? idUsuario)
    {
        return await _contexto.Corridas
            .AsNoTracking()
            .Include(c => c.Usuario)
            .Include(c => c.ItensAdicionais)
            .Include(c => c.HistoricosStatus.OrderBy(h => h.DataHora)).ThenInclude(h => h.Usuario)
            .FirstOrDefaultAsync(c => c.ID == id && (idUsuario == null || c.IDUsuario == idUsuario));
    }

    // Revalida a agenda e confirma numa transação exclusiva (RN12, RN13):
    // de dois pedidos para o mesmo horário, só o primeiro a ser aprovado passa.
    public async Task<string?> AprovarAsync(int id, int idMotorista)
    {
        return await _agendaNegocio.ExecutarExclusivoAsync(async () =>
        {
            var corrida = await _contexto.Corridas.Include(c => c.Tarifa).FirstOrDefaultAsync(c => c.ID == id);
            if (corrida == null)
                return "Corrida não encontrada.";

            var fim = AgendaNegocio.CalcularFim(corrida.DataHora, corrida.DuracaoMin, corrida.Tarifa.MargemSegurancaMin);
            if (corrida.Status == "Pendente" && !await _agendaNegocio.PeriodoLivreAsync(corrida.DataHora, fim, corrida.ID))
                return "O horário desta corrida não está mais livre. Recuse a solicitação.";

            var erro = MudarStatus(corrida, "Confirmada", idMotorista);
            if (erro != null)
                return erro;

            await _contexto.SaveChangesAsync();
            return null;
        });
    }

    public Task<string?> RecusarAsync(int id, int idMotorista) => AlterarStatusAsync(id, "Recusada", idMotorista);

    public Task<string?> ConcluirAsync(int id, int idMotorista) => AlterarStatusAsync(id, "Concluida", idMotorista);

    // O cliente só cancela a própria corrida (RN18); o motorista cancela qualquer uma.
    // A antecedência mínima depende da política de cancelamento, que ainda é uma pendência de negócio.
    public async Task<string?> CancelarAsync(int id, int idUsuario, bool ehMotorista)
    {
        var corrida = await _contexto.Corridas.FirstOrDefaultAsync(c => c.ID == id && (ehMotorista || c.IDUsuario == idUsuario));
        if (corrida == null)
            return "Corrida não encontrada.";

        var erro = MudarStatus(corrida, "Cancelada", idUsuario);
        if (erro != null)
            return erro;

        await _contexto.SaveChangesAsync();
        return null;
    }

    // Pedágio, espera, retorno vazio etc. (RN08, RN09). Só o que está incluso entra no preço.
    public async Task<string?> AdicionarItemAsync(ItemAdicional item)
    {
        if (!TiposItem.Contains(item.Tipo))
            return "Tipo de item inválido.";

        var corrida = await ObterParaItensAsync(item.IDCorrida);
        if (corrida == null)
            return "Só é possível alterar itens de corridas pendentes ou confirmadas.";

        corrida.ItensAdicionais.Add(new ItemAdicional
        {
            Tipo = item.Tipo,
            Descricao = item.Descricao,
            Valor = item.Valor,
            Incluso = item.Incluso
        });
        RecalcularPreco(corrida);
        await _contexto.SaveChangesAsync();
        return null;
    }

    public async Task<string?> RemoverItemAsync(int idCorrida, int idItem)
    {
        var corrida = await ObterParaItensAsync(idCorrida);
        var item = corrida?.ItensAdicionais.FirstOrDefault(i => i.ID == idItem);
        if (corrida == null || item == null)
            return "Item não encontrado nesta corrida.";

        corrida.ItensAdicionais.Remove(item);
        _contexto.ItensAdicionais.Remove(item);
        RecalcularPreco(corrida);
        await _contexto.SaveChangesAsync();
        return null;
    }

    private async Task<string?> AlterarStatusAsync(int id, string statusNovo, int idUsuario)
    {
        var corrida = await _contexto.Corridas.FirstOrDefaultAsync(c => c.ID == id);
        if (corrida == null)
            return "Corrida não encontrada.";

        var erro = MudarStatus(corrida, statusNovo, idUsuario);
        if (erro != null)
            return erro;

        await _contexto.SaveChangesAsync();
        return null;
    }

    // Único lugar que muda o status: confere a transição e registra o histórico.
    private string? MudarStatus(Corrida corrida, string statusNovo, int idUsuario)
    {
        if (!Transicoes.TryGetValue(corrida.Status, out var permitidos) || !permitidos.Contains(statusNovo))
            return $"Uma corrida {corrida.Status} não pode passar para {statusNovo}.";

        var historico = NovoHistorico(corrida.Status, statusNovo, idUsuario);
        historico.IDCorrida = corrida.ID;
        _contexto.HistoricosStatus.Add(historico);

        corrida.Status = statusNovo;
        return null;
    }

    private static HistoricoStatus NovoHistorico(string? anterior, string novo, int idUsuario) => new()
    {
        IDUsuario = idUsuario,
        StatusAnterior = anterior,
        StatusNovo = novo,
        DataHora = Relogio.Agora
    };

    private async Task<Corrida?> ObterParaItensAsync(int idCorrida)
    {
        return await _contexto.Corridas
            .Include(c => c.Tarifa)
            .Include(c => c.ItensAdicionais)
            .FirstOrDefaultAsync(c => c.ID == idCorrida && (c.Status == "Pendente" || c.Status == "Confirmada"));
    }

    // Usa a tarifa gravada na corrida: mudar a tarifa ativa não altera corridas já solicitadas (RN02).
    private static void RecalcularPreco(Corrida corrida)
    {
        var extras = corrida.ItensAdicionais.Where(i => i.Incluso).Sum(i => i.Valor);
        corrida.PrecoEstimado = PrecoNegocio.CalcularPreco(corrida.Tarifa, corrida.DistanciaKm, corrida.DuracaoMin, corrida.DeslocamentoKm, extras);
    }
}
