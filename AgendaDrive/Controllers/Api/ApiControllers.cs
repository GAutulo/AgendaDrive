using AgendaDrive.Infra;
using AgendaDrive.Models;
using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio;
using AgendaDrive.Models.Negocio.Rotas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgendaDrive.Controllers.Api;

// API em JSON (RNF05): usa as mesmas classes de negócio das telas, então nenhuma regra fica duplicada.
// O cookie de login vale aqui também. POST, PUT e DELETE exigem o cabeçalho RequestVerificationToken.

[ApiController]
[Authorize]
[Route("api/enderecos")]
public class EnderecosApiController : ControllerBase
{
    private readonly IRotaServico _rotaServico;

    public EnderecosApiController(IRotaServico rotaServico)
    {
        _rotaServico = rotaServico;
    }

    [HttpGet]
    public async Task<ActionResult<List<EnderecoSugerido>>> Sugerir([FromQuery] string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3)
            return new List<EnderecoSugerido>();

        return await _rotaServico.SugerirEnderecosAsync(texto.Trim());
    }
}

[ApiController]
[Authorize]
[Route("api/cotacoes")]
public class CotacoesApiController : ControllerBase
{
    private readonly PrecoNegocio _precoNegocio;

    public CotacoesApiController(PrecoNegocio precoNegocio)
    {
        _precoNegocio = precoNegocio;
    }

    [HttpPost]
    public async Task<ActionResult<Cotacao>> Cotar(CotacaoRequisicao requisicao)
    {
        var cotacao = await _precoNegocio.CotarAsync(requisicao.Origem, requisicao.Destino);
        if (cotacao == null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { erro = "Não foi possível calcular a rota agora." });

        return cotacao;
    }
}

[ApiController]
[Authorize]
[Route("api/agenda")]
public class AgendaApiController : ControllerBase
{
    private readonly AgendaNegocio _agendaNegocio;

    public AgendaApiController(AgendaNegocio agendaNegocio)
    {
        _agendaNegocio = agendaNegocio;
    }

    // Só início e fim dos períodos ocupados: nada sobre quem ocupou nem o motivo (RN11).
    [HttpGet("disponibilidade")]
    public async Task<List<PeriodoOcupado>> Disponibilidade([FromQuery] DateOnly data)
    {
        return await _agendaNegocio.ListarOcupadosAsync(data);
    }
}

[ApiController]
[Authorize]
[Route("api/corridas")]
public class CorridasApiController : ControllerBase
{
    private readonly CorridaNegocio _corridaNegocio;

    public CorridasApiController(CorridaNegocio corridaNegocio)
    {
        _corridaNegocio = corridaNegocio;
    }

    [HttpPost]
    public async Task<IActionResult> Solicitar(SolicitacaoViewModel modelo)
    {
        return Responder(await _corridaNegocio.SolicitarAsync(modelo.ParaCorrida(), User.IdUsuario()));
    }

    [HttpGet("minhas")]
    public async Task<List<CorridaResumo>> Minhas()
    {
        var corridas = await _corridaNegocio.ListarDoClienteAsync(User.IdUsuario());
        return corridas.Select(CorridaResumo.De).ToList();
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        return Responder(await _corridaNegocio.CancelarAsync(id, User.IdUsuario(), User.EhMotorista()));
    }

    [HttpPost("{id:int}/aprovar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Aprovar(int id)
    {
        return Responder(await _corridaNegocio.AprovarAsync(id, User.IdUsuario()));
    }

    [HttpPost("{id:int}/recusar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Recusar(int id)
    {
        return Responder(await _corridaNegocio.RecusarAsync(id, User.IdUsuario()));
    }

    [HttpPost("{id:int}/concluir")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Concluir(int id)
    {
        return Responder(await _corridaNegocio.ConcluirAsync(id, User.IdUsuario()));
    }

    private IActionResult Responder(string? erro) =>
        erro == null ? NoContent() : BadRequest(new { erro });
}

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/bloqueios")]
public class BloqueiosApiController : ControllerBase
{
    private readonly BloqueioNegocio _bloqueioNegocio;

    public BloqueiosApiController(BloqueioNegocio bloqueioNegocio)
    {
        _bloqueioNegocio = bloqueioNegocio;
    }

    [HttpGet]
    public async Task<List<Bloqueio>> Listar()
    {
        return await _bloqueioNegocio.ListarAsync();
    }

    [HttpPost]
    public async Task<IActionResult> Criar(Bloqueio bloqueio)
    {
        return Responder(await _bloqueioNegocio.CriarAsync(bloqueio));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, Bloqueio bloqueio)
    {
        bloqueio.ID = id;
        return Responder(await _bloqueioNegocio.AtualizarAsync(bloqueio));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id)
    {
        return Responder(await _bloqueioNegocio.CancelarAsync(id));
    }

    private IActionResult Responder(string? erro) =>
        erro == null ? NoContent() : BadRequest(new { erro });
}

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/tarifas")]
public class TarifasApiController : ControllerBase
{
    private readonly TarifaNegocio _tarifaNegocio;

    public TarifasApiController(TarifaNegocio tarifaNegocio)
    {
        _tarifaNegocio = tarifaNegocio;
    }

    [HttpGet("ativa")]
    public async Task<ActionResult<Tarifa>> Ativa()
    {
        var tarifa = await _tarifaNegocio.ObterAtivaAsync();
        return tarifa == null ? NotFound() : tarifa;
    }

    [HttpPut("ativa")]
    public async Task<IActionResult> Substituir(Tarifa dados)
    {
        var erro = await _tarifaNegocio.SubstituirAtivaAsync(dados);
        return erro == null ? NoContent() : BadRequest(new { erro });
    }
}
