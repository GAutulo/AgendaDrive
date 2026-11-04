using System.Globalization;
using System.Threading.RateLimiting;
using AgendaDrive.Infra;
using AgendaDrive.Models.Entidades;
using AgendaDrive.Models.Negocio;
using AgendaDrive.Models.Negocio.Rotas;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Credenciais ----------
// Connection string e chave do OpenRouteService ficam em App_Secrets/secrets.json (fora do Git e fora do wwwroot).
// optional: false -> sem o arquivo o site nem sobe, em vez de rodar sem banco.
builder.Configuration.AddJsonFile("App_Secrets/secrets.json", optional: false, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("AgendaDrive")
    ?? throw new InvalidOperationException("Connection string 'AgendaDrive' não encontrada em App_Secrets/secrets.json.");

// ---------- Serviços (injeção de dependência) ----------
builder.Services.AddControllersWithViews(opcoes =>
{
    // Rede de segurança: todo POST exige o token anti-falsificação, mesmo se faltar o [ValidateAntiForgeryToken].
    opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Mensagens de validação do model binding em português.
    var mensagens = opcoes.ModelBindingMessageProvider;
    mensagens.SetValueMustNotBeNullAccessor(_ => "Preencha este campo.");
    mensagens.SetMissingBindRequiredValueAccessor(campo => $"Informe {campo}.");
    mensagens.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"O valor '{valor}' não é válido para {campo}.");
    mensagens.SetValueIsInvalidAccessor(valor => $"O valor '{valor}' não é válido.");
    mensagens.SetUnknownValueIsInvalidAccessor(campo => $"Valor inválido para {campo}.");
    mensagens.SetValueMustBeANumberAccessor(campo => $"{campo}: use só números, com vírgula nos decimais (ex.: 2,50).");
});

// O context é criado uma vez por requisição (Scoped) e entregue a quem pedir no construtor.
// EnableRetryOnFailure: se a conexão cair por um instante (comum em banco compartilhado), o EF tenta de novo sozinho.
builder.Services.AddDbContext<AgendaDriveContext>(opcoes =>
    opcoes.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

// Classes de negócio: uma por requisição, recebendo o context.
builder.Services.AddScoped<TarifaNegocio>();
builder.Services.AddScoped<UsuarioNegocio>();
builder.Services.AddScoped<AgendaNegocio>();
builder.Services.AddScoped<BloqueioNegocio>();
builder.Services.AddScoped<PrecoNegocio>();
builder.Services.AddScoped<CorridaNegocio>();

// Serviço de rotas: AddHttpClient entrega um HttpClient pronto para a classe que implementa a interface.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IRotaServico, OpenRouteServiceRotaServico>();

// Login por cookie. Quem chama a API sem permissão recebe 401/403, não um redirecionamento para a tela de login.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/Conta/Entrar";
        opcoes.AccessDeniedPath = "/Conta/AcessoNegado";
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Lax;
        opcoes.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opcoes.Events.OnRedirectToLogin = contexto => ResponderApiOuRedirecionar(contexto, StatusCodes.Status401Unauthorized);
        opcoes.Events.OnRedirectToAccessDenied = contexto => ResponderApiOuRedirecionar(contexto, StatusCodes.Status403Forbidden);
    });

// Guarda as chaves que assinam cookies e tokens em App_Data (fora do wwwroot).
// Sem isso, cada reinício do site na hospedagem derrubaria o login de todo mundo.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

// Freia tentativas de adivinhar senha: no máximo 10 envios de login/cadastro por minuto por IP.
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy("login", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// Log de exceções em App_Data/logs.
builder.Services.AddSingleton<ArquivoLog>();
builder.Services.AddExceptionHandler<LogExcecaoHandler>();

var app = builder.Build();

// ---------- Pipeline (a ordem importa: cada linha embrulha as de baixo) ----------

// Toda exceção: LogExcecaoHandler grava o log e o usuário vê /Home/Erro (inclusive em Development,
// para testar o log local exatamente como vai funcionar na hospedagem).
app.UseExceptionHandler("/Home/Erro");

// 404 e outros códigos de erro sem conteúdo também mostram a página amigável.
app.UseStatusCodePagesWithReExecute("/Home/Erro", "?codigo={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cabeçalhos de segurança simples em todas as respostas.
app.Use(async (contexto, proximo) =>
{
    contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
    contexto.Response.Headers["X-Frame-Options"] = "DENY";
    contexto.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await proximo();
});

// Sempre pt-BR, independente do servidor: "2,50" vira 2.50, datas em dd/MM/yyyy, moeda em R$.
// Sem isso, numa hospedagem em inglês "2,50" seria lido como 250.
var ptBR = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBR),
    SupportedCultures = [ptBR],
    SupportedUICultures = [ptBR]
});

app.UseRouting();

app.UseRateLimiter();

// Primeiro descobre quem é o usuário (cookie), depois confere se ele pode entrar ([Authorize]).
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static Task ResponderApiOuRedirecionar(RedirectContext<CookieAuthenticationOptions> contexto, int statusApi)
{
    if (contexto.Request.Path.StartsWithSegments("/api"))
        contexto.Response.StatusCode = statusApi;
    else
        contexto.Response.Redirect(contexto.RedirectUri);

    return Task.CompletedTask;
}
