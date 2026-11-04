# Roteiro de desenvolvimento (4ª medição, 12/11)

A regra da disciplina é: projeto com mais de 50% de código gerado por IA é desconsiderado, e as perguntas da apresentação são respondidas sem IA.
Por isso, daqui em diante **você escreve o código** e a IA explica, revisa e ajuda com erros.

As classes de `Models/Negocio` já existem só com assinaturas e TODOs. Elas lançam `NotImplementedException` e **não estão registradas** no `Program.cs`: nada muda no site até você registrá-las.
Depois de implementar cada classe, apague o comentário "ESTRUTURA PARA O GUILHERME IMPLEMENTAR".

**Prioridade de hoje:** publicar a 3ª medição seguindo `docs/PUBLICACAO.md`. Ela vale nota amanhã e nada abaixo é necessário para isso.

Como trabalhar comigo em cada etapa: escreva a sua versão, peça revisão, e eu aponto bugs e explico o porquê.

## Etapa 1 — Login e papéis (RF01, RF02). Vem primeiro porque protege todo o resto

**Implementar:** `UsuarioNegocio`.

**Program.cs:**

- `AddScoped<UsuarioNegocio>()`.
- `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` com `LoginPath` e `AccessDeniedPath`.
- `app.UseAuthentication()` **antes** de `app.UseAuthorization()`.

**`Controllers/ContaController.cs`:**

```text
GET  Entrar(string? returnUrl)
POST Entrar(LoginViewModel, string? returnUrl)   -> HttpContext.SignInAsync com claims NameIdentifier, Name e Role
                                                    redirecionar só com LocalRedirect/Url.IsLocalUrl (open redirect)
GET  Cadastrar()
POST Cadastrar(CadastroViewModel)
POST Sair()                                      -> HttpContext.SignOutAsync
GET  AcessoNegado()
```

**ViewModels:** `LoginViewModel` e `CadastroViewModel` em `Models/`. Use ViewModel, não a entidade `Usuario`: a tela nunca pode ter campo `Papel` nem `SenhaHash`.

**Views:** `Views/Conta/Entrar.cshtml` e `Cadastrar.cshtml`. No `_Layout`, mostre Entrar/Sair e os menus conforme `User.IsInRole("Administrador")`.

**Fechar a etapa:**

- Descomente `[Authorize(Roles = "Administrador")]` no `TarifasController`.
- Os usuários do script têm hash falso. Cadastre sua conta pelo site e promova-a uma única vez por SQL, avisando antes de rodar em produção:
  `update Usuarios set Papel = 'Administrador' where Email = '...'`.
- Teste: anônimo em `/Tarifas` vai para o login; cliente em `/Tarifas` recebe acesso negado.

Isto é o que **fecha o buraco** da área de Tarifas aberta no site publicado.

## Etapa 2 — Bloqueios (RF13, RN16)

**Implementar:** `AgendaNegocio.PeriodoLivreAsync` e `BloqueioNegocio`.

**`Controllers/BloqueiosController.cs`** com `[Authorize(Roles = "Administrador")]`, copiando o padrão do `TarifasController`:

```text
Index, Criar (GET/POST), Editar (GET/POST), Cancelar (POST)
```

**Teste:** bloqueio com Fim antes de Inicio; bloqueio em cima da corrida confirmada do script.

**Atenção:** a corrida de exemplo Confirmada é de 03/11, já passada. Crie cenários com datas futuras.

## Etapa 3 — Rotas e cotação (RF06, RF07, RF09)

**Sua parte fora do código:** criar a conta e a chave no OpenRouteService e colocar em `App_Secrets/secrets.json`, no computador e no servidor.

**Implementar:** `OpenRouteServiceRotaServico` e `PrecoNegocio`.

**Program.cs:** `AddHttpClient<IRotaServico, OpenRouteServiceRotaServico>()` e `AddScoped<PrecoNegocio>()`.

**`Controllers/Api/`** com `[ApiController]` e `[Authorize]`:

```text
EnderecosApiController  GET  /api/enderecos?texto=
CotacoesApiController   POST /api/cotacoes
AgendaApiController     GET  /api/agenda/disponibilidade?data=
```

**Cuidado de segurança:** o `AutoValidateAntiforgeryToken` global também vale para POST da API. O `fetch` do `mapa.js` precisa enviar o token no cabeçalho `RequestVerificationToken`.

**Limite do plano grátis:** no JavaScript, espere 3 letras e uma pausa antes de chamar `/api/enderecos`.

## Etapa 4 — Solicitar corrida com mapa (RF04, RF08, RF10, UC03)

**Implementar:** `CorridaNegocio.SolicitarAsync` e `MudarStatus`.

**`Controllers/CorridasController.cs`** com `[Authorize]`:

```text
GET  Nova()
POST Nova(SolicitacaoViewModel)   -> só endereços, coordenadas, data/hora e observações. Nada de preço!
GET  Minhas()
GET  Detalhes(int id)             -> cliente só vê a própria (RN18)
```

**Front:** `wwwroot/js/mapa.js` com Leaflet 1.9.4 (autocomplete, marcadores, linha da rota, resumo do preço). Mantenha a atribuição "© OpenStreetMap contributors".

**Teste:** alterar o preço no DevTools não muda o valor gravado (RN06).

## Etapa 5 — Fluxo do motorista (RF11, RF12, RF15, RF16, UC05, UC06, UC09)

**Implementar:** o resto do `CorridaNegocio`.

**`CorridasController`**, com as ações do motorista em `[Authorize(Roles = "Administrador")]`:

```text
GET  Solicitacoes(status, inicio, fim)
POST Aprovar(int id)
POST Recusar(int id)
POST Concluir(int id)
POST Cancelar(int id)   -> cliente ou motorista
```

**Dica:** o ID do usuário sai sempre do cookie (`User.FindFirstValue(ClaimTypes.NameIdentifier)`), nunca do formulário.

## Etapa 6 — API restante, desejáveis e fechamento

- `CorridasApiController`, `BloqueiosApiController` e `TarifasApiController`, chamando as mesmas classes de negócio.
- Desejáveis: botão do Waze (RF17) e relatório por período (RF18).
- Rodar os cenários de teste da seção 11 da documentação e testar no celular de verdade.

## Antes do PDF final (19/11)

A documentação entregue diverge do que foi construído, e o enunciado prevalece. Ajuste estes pontos:

- Seção 6: três projetos e `Servicos` → projeto único com `Models/Entidades` e `Models/Negocio`.
- Seção 6: hospedagem "[definir]" → MonsterASP.NET; logs com `ILogger` → arquivo em `App_Data/logs`.
- RNF03: classes geradas por Scaffold → entidades escritas espelhando o script.
- RNF08: User Secrets e variáveis de ambiente → `App_Secrets`.
- RNF09: `ILogger` → log em arquivo em `App_Data/logs`.
- Uso de IA: declarar o que a IA gerou (a base da 3ª medição e estes esqueletos) e anexar o histórico das conversas, **incluindo as do Claude Code**.

## Decisões de negócio que travam a implementação

Só você pode decidir. Sem elas, a Etapa 5 fica incompleta:

- Antecedência mínima para o cliente cancelar (política de cancelamento).
- Prazo para o motorista responder uma solicitação pendente.
- Retorno vazio em viagens intermunicipais.
- Valores reais de tarifa e o ponto de partida do motorista.
