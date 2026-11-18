# AgendaDrive — contexto do projeto

Leia este arquivo inteiro antes de qualquer coisa. Ele resume as decisões tomadas numa conversa anterior com o Claude (claude.ai). O histórico completo dessa conversa precisa ir anexado ao PDF final, por exigência da disciplina.

## Quem e o quê

- Aluno: Guilherme Autulo Santana, RA 103844, trabalho individual.
- Disciplina: Tópicos em Linguagem de Programação II (TLP II), UNIFEV. Professor: Fernando Datorre.
- Sistema: **AgendaDrive**, sistema web de agendamento e precificação de corridas particulares para um motorista.
  - O cliente digita origem e destino (com sugestão de endereços), vê a rota no mapa, recebe uma estimativa de preço e envia a solicitação.
  - O motorista (Administrador) aprova ou recusa, registra bloqueios na agenda e configura as tarifas.
- Enunciado oficial: `docs/TLPII_-_Projeto_Final_1.pdf`. **Ele prevalece sobre qualquer coisa neste arquivo.**
- Documentação já entregue nas medições 1 e 2: `docs/AgendaDrive_Documentacao.pdf` (requisitos, regras de negócio, casos de uso, classes, dicionário, arquitetura, pesquisa de mapa). Documento editável: https://claude.ai/artifact/NxHVKWaVEqJY5gV1A9DqAS

## Cronograma

| Data | Entrega | Situação |
| --- | --- | --- |
| 28/10 | 1ª medição: definição, escopo, requisitos, casos de uso | Entregue |
| 29/10 | 2ª medição: classes, dicionário de dados, banco com script rodando | Entregue |
| **05/11 (qui)** | **3ª medição: projeto com as camadas, log de exceções em App_Data/logs, credenciais em App_Secrets, conexão com o banco, 1º CRUD funcionando, primeira versão publicada** | **Próxima** |
| 12/11 | 4ª medição: todos os CRUDs e regras de negócio, login com papéis, layout responsivo testado no celular, sistema completo no ar | |
| 19/11 | Projeto final: sistema no ar + PDF + apresentação (8,0 pontos) | |

## Regras do enunciado que NÃO podem ser quebradas

- **Framework:** ASP.NET Core MVC no **.NET 10** (LTS). Padrão MVC com Controllers e Views Razor. **Proibido:** Razor Pages, Blazor e Minimal APIs. Não misturar nada de MVC 5 (Global.asax, Web.config).
- **Banco:** SQL Server, com Entity Framework Core (`Microsoft.EntityFrameworkCore.SqlServer`), Database First: o banco é criado pelo `database/script.sql`.
- **Camadas:** Controllers e Views nunca acessam o banco.
  - Entidades em `Models/Entidades`.
  - Classes de negócio em `Models/Negocio` (ex.: `TarifaNegocio`).
  - O `DbContext` é recebido pela classe de negócio (injeção de dependência), **nunca** usado direto no Controller.
- **Credenciais** (connection string, chave do OpenRouteService) na pasta `App_Secrets`, fora do `wwwroot` e no `.gitignore`. Não usar User Secrets nem deixar segredos no `appsettings.json`.
- **Log:** desde o início, toda e qualquer exceção gravada em `App_Data/logs`, com data/hora, usuário, URL, mensagem e stack trace. O usuário vê uma página de erro amigável; o detalhe vai só para o log.
- **Dados do cliente** (uploads, exportações) ficam em `App_Data`, nunca em pasta pública.
- **Segurança:**
  - Senha com hash.
  - `[Authorize]` com papéis nas áreas restritas, Administrador e Usuario.
  - `[ValidateAntiForgeryToken]` nos POSTs.
  - Consultas parametrizadas, sem concatenar SQL.
- **Comandos destrutivos** (DROP, DELETE, TRUNCATE): sempre avisar antes e preferir exclusão lógica (campo `Ativo`).
- **Interface:** Bootstrap 5, responsiva e testada no celular.
- **Nomes** em português e sem acento.
- **Banco:** tabelas no plural, PK `ID`, FK `ID` + tabela no singular (`IDUsuario`), padrão usado pelo professor.
- **Hospedagem:** MonsterASP.NET, grátis. Aceita .NET 10 no plano gratuito: 1 site, 1 banco de 1 GB, 256 MB de RAM, subdomínio runasp.net.

## Limite de uso de IA (regra da disciplina)

- Projetos com **mais de 50% de código gerado por IA são desconsiderados**.
- Na apresentação, o professor faz perguntas sobre o código, respondidas sem IA.
- **A IA pode:**
  - Fazer planejamento e documentação: requisitos, diagramas, dicionário, scripts SQL.
  - Explicar conceitos e erros, e revisar o código do aluno.
  - Mostrar **um exemplo de cada padrão**: uma entidade, uma classe de negócio, um Controller e suas Views.
- **A IA não pode** gerar o sistema inteiro nem os demais CRUDs depois do exemplo. Para esses, ela entrega só a estrutura (assinaturas de métodos e comentários do que fazer) e orienta o aluno a implementar.

**Combinado com o Guilherme:** a IA monta a base da 3ª medição.

- A estrutura do projeto e o `Program.cs`.
- O log em `App_Data/logs` e a leitura de `App_Secrets`.
- O `DbContext` e as entidades, espelhando o script.
- O **CRUD de Tarifas**, que é o único exemplo permitido de cada padrão.

Daqui para frente o trabalho é passo a passo, com o Guilherme escrevendo o código. Explique cada arquivo criado de um jeito que ele consiga responder perguntas sobre ele na apresentação.

## Decisões técnicas

- **Projeto único** `AgendaDrive` (ASP.NET Core MVC). Na documentação eu tinha proposto três projetos (Web, Negocio, Dados); isso foi **substituído** pelo formato do enunciado: `Models/Entidades` + `Models/Negocio`.
- **Preparado para Vue ou React no futuro:**
  - Toda regra fica em `Models/Negocio`.
  - Além dos controllers MVC, haverá controllers de API (`[ApiController]`, em `Controllers/Api`, rotas `/api/...`) que chamam as mesmas classes de negócio.
  - Controllers de API são permitidos; Minimal APIs não.
- **Mapa:** Leaflet 1.9.4 no front, com atribuição "© OpenStreetMap contributors".
- **Endereços e rotas:** OpenRouteService, chamado **pelo servidor** (HttpClient), com a chave em App_Secrets e nunca no navegador.
  - Plano gratuito: 2.000 rotas por dia e 1.000 autocompletes por dia.
  - Usar uma interface `IRotaServico` para poder trocar de provedor.
- **Preço:**
  - Subtotal = tarifa-base + km × valor/km + minutos × valor/min + km de deslocamento × valor/km de deslocamento + extras.
  - Preço estimado = o maior valor entre o subtotal e o preço mínimo.
  - O deslocamento é a rota do ponto de partida do motorista (`Tarifas.BaseLatitude`/`BaseLongitude`) até a origem.
  - O servidor sempre recalcula; nunca confiar no preço vindo do navegador.
- **Agenda:**
  - A corrida nasce Pendente e só ocupa horário quando é Confirmada.
  - Período ocupado = duração + `MargemSegurancaMin`.
  - Não pode haver sobreposição entre corridas confirmadas e bloqueios; isso é validado em C#, não no banco.
- **Estados da corrida:** Pendente, Confirmada, Recusada, Cancelada e Concluida. Toda mudança grava um registro em `HistoricosStatus`.
- **Tarifas:** só uma tarifa ativa por vez, garantido por um índice único filtrado no banco e também pela regra de negócio.

## Banco

O script completo está em `database/script.sql` e cria seis tabelas:

- Usuarios
- Tarifas
- Corridas
- Bloqueios
- ItensAdicionais
- HistoricosStatus

O script insere dados de exemplo e termina com um `select` em cada tabela. As entidades podem ser geradas com:

```
Scaffold-DbContext "Name=ConnectionStrings:AgendaDrive" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models/Entidades -Context AgendaDriveContext
```

Antes de rodar o Scaffold, a connection string precisa estar acessível pela configuração, ou seja, com `App_Secrets` já carregado no `Program.cs`. As entidades ficam no singular (Usuario, Tarifa, Corrida, Bloqueio, ItemAdicional, HistoricoStatus).

## Checklist da 3ª medição (05/11)

- [x] Criar o projeto ASP.NET Core MVC (.NET 10) com Bootstrap 5, com as pastas `Models/Entidades`, `Models/Negocio`, `App_Secrets` e `App_Data/logs`.
- [x] Configurar o `.gitignore`: `**/App_Secrets/*` (exceto `secrets.exemplo.json`), `**/App_Data/logs/`, `bin/`, `obj/`. O `**/` é obrigatório porque o projeto fica na subpasta `AgendaDrive/`.
- [x] Criar `App_Secrets/secrets.json` com a connection string e a chave do ORS, carregado no `Program.cs`.
- [x] Log de exceções: `Infra/ArquivoLog.cs` + `Infra/LogExcecaoHandler.cs` (`IExceptionHandler`), gravando em `App_Data/logs/yyyy-MM-dd.log`. Página amigável em `/Home/Erro`, também para 404.
- [x] `AgendaDriveContext` e entidades escritos à mão espelhando o script; não rode o Scaffold por cima (ele gera `ItensAdicionai`, `Idcorrida` e `HasDefaultValue(true)` em bool, que faz o EF gravar 1 quando o C# manda false).
- [x] CRUD de Tarifas: listar, criar, editar, detalhes, ativar/desativar e excluir.
  - Tarifa nasce inativa; só `AtivarAsync` ativa (transação Serializable + `EnableRetryOnFailure`, testado com 90 ativações simultâneas).
  - Tarifa usada em corridas não pode ser editada nem excluída (o motorista cria outra e ativa). Decisão a confirmar com o Guilherme.
  - Exclusão física só de tarifa inativa e nunca usada.
- [x] Rodar o `database/script.sql` local (LocalDB) e testar o CRUD.
- [x] Publicado em https://agendadrive.runasp.net (MonsterASP, banco `db77902`). Para republicar: `publicar-hospedagem.ps1`, com o perfil `.publishSettings` em Downloads. Script do banco foi rodado pelo "Run T-SQL" do painel, porque a conexão remota a partir deste computador falhou.
- [x] HTTPS ativo (Let's Encrypt, renovação automática). O próprio app redireciona http para https.

## 4ª medição

O restante do sistema foi escrito na branch `4a-medicao`. O que falta e o que estudar estão em `docs/ROTEIRO.md`.

## Pendências de negócio

Estas decisões são do aluno; não invente valores. Decididas em 16/11:

- Cancelamento: o cliente cancela até 2 horas antes; depois, só o motorista.
- Prazo de resposta: pendente que chega à hora da corrida vira Recusada.
- Retorno vazio (outra cidade): km da volta até o ponto de partida × valor/km × 50%.

Ainda em aberto: valores reais de tarifa e o endereço do ponto de partida (o próprio motorista cadastra pela tela Tarifas).

## Visual

Estilo profissional (o Guilherme rejeitou um visual temático de estrada por parecer amador). Paleta de 3 cores: petróleo `#0B6E79` (ações), tinta `#0F1E2E` (texto e marca), âmbar `#F5A524` (destaque pontual). Fonte única Plus Jakarta Sans (Google Fonts). Cards brancos com borda e sombra suaves, status como etiquetas suaves. Evitar barras laterais coloridas em cards e topbar com ícones. Bootstrap 5 é obrigatório pelo enunciado: não trocar por outra tecnologia de front.
