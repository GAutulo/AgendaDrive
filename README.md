# AgendaDrive

Sistema web de agendamento e precificação de corridas particulares para um motorista.

**No ar:** https://agendadrive.runasp.net

Trabalho individual da disciplina **Tópicos em Linguagem de Programação II** (UNIFEV), professor Fernando Datorre.
Aluno: Guilherme Autulo Santana (RA 103844).

## O que o sistema faz

**Cliente**
- Cria conta e entra com e-mail e senha.
- Redefine a senha com a palavra-passe escolhida no cadastro.
- Digita origem e destino com sugestão de endereços e vê a rota no mapa.
- Recebe o preço estimado, detalhado em tarifa-base, distância, tempo e deslocamento.
- Pede a corrida e acompanha a situação.
- Cancela até 2 horas antes.

**Motorista**
- Aprova, recusa, conclui ou cancela corridas.
- Abre a rota no Waze e lança itens adicionais (pedágio, espera, retorno vazio...).
- Bloqueia períodos da agenda.
- Configura as tarifas (só uma fica ativa).
- Consulta o relatório de corridas por situação e período.

**Regras principais**
- **Preço:** tarifa-base + km × valor/km + minutos × valor/min + km de deslocamento × valor/km de deslocamento + extras, nunca abaixo do preço mínimo. O servidor sempre recalcula.
- **Viagem para outra cidade:** soma o retorno vazio (km da volta × valor/km × 50%).
- **Agenda:** só corridas confirmadas ocupam horário (duração + margem de segurança), sem sobreposição com outras corridas ou bloqueios.
- **Prazo de resposta:** pedido pendente que chega à hora da corrida vira Recusada.
- **Histórico:** toda mudança de situação fica registrada.

## Tecnologias

| Camada | Tecnologia |
| --- | --- |
| Back-end | C#, ASP.NET Core MVC (.NET 10), Controllers e Views Razor |
| Dados | SQL Server, Entity Framework Core 10 (Database First) |
| Front-end | Bootstrap 5, Plus Jakarta Sans, jQuery Validation |
| Mapa e rotas | Leaflet + OpenStreetMap, OpenRouteService (chamado pelo servidor) |
| Hospedagem | MonsterASP.NET (Web Deploy, HTTPS com Let's Encrypt) |

## Estrutura

```
AgendaDrive/
├── Controllers/          Controllers MVC (Home, Conta, Corridas, Bloqueios, Tarifas)
│   └── Api/              Controllers de API (/api/...), para um futuro front em Vue ou React
├── Models/
│   ├── Entidades/        AgendaDriveContext e entidades, espelhando o script do banco
│   └── Negocio/          Regras de negócio (o único lugar que acessa o banco)
│       └── Rotas/        IRotaServico e a implementação do OpenRouteService
├── Infra/                Log de exceções, hora de Brasília, usuário logado
├── Views/                Razor
├── wwwroot/              CSS e JavaScript (mapa.js)
├── App_Secrets/          Credenciais (fora do Git; ver secrets.exemplo.json)
└── App_Data/             Logs e chaves de login (fora do Git)
database/                 script.sql e alterações
docs/                     Enunciado, documentação, guias de publicação e roteiro
```

Controllers e Views nunca acessam o banco: o `DbContext` é injetado nas classes de `Models/Negocio`.

## Como rodar

1. Instale o **.NET 10 SDK** e o **SQL Server LocalDB**.
2. Crie o banco:

   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -E -I -i database\script.sql
   ```

3. Copie `AgendaDrive/App_Secrets/secrets.exemplo.json` para `secrets.json` e preencha:
   - a connection string;
   - a chave do [OpenRouteService](https://openrouteservice.org) (plano gratuito).
4. Rode:

   ```
   dotnet run --project AgendaDrive
   ```

5. Crie uma conta pela tela e, para torná-la motorista, rode no banco:

   ```sql
   update Usuarios set Papel = 'Administrador' where Email = 'seu@email';
   ```

> **Banco criado antes de 16/11/2026:** rode `database/alteracao-palavra-passe.sql` uma vez para acrescentar a coluna da palavra-passe.

## Publicação

`publicar-hospedagem.ps1` compila, junta as credenciais de produção, envia por Web Deploy e testa o site. Ele usa o perfil `.publishSettings` do painel do MonsterASP, que fica na pasta Downloads. Detalhes em [docs/PUBLICACAO.md](docs/PUBLICACAO.md).

## Segurança

- Senha e palavra-passe com hash (PBKDF2), nunca em texto.
- Papéis com `[Authorize]`: `Administrador` é o motorista e `Usuario` é o cliente.
- Token antifalsificação em todos os POSTs; consultas só por EF Core (parametrizadas).
- Limite de 10 tentativas por minuto em login, cadastro e redefinição de senha.
- Credenciais em `App_Secrets`, fora do `wwwroot` e do Git; a chave do mapa nunca vai ao navegador.
- Toda exceção é gravada em `App_Data/logs`, e o usuário vê só uma página amigável.
- HTTPS obrigatório com HSTS.

## Uso de IA

O código-base foi gerado com IA (Claude), com autorização do professor, e revisado, testado e corrigido pelo aluno. O histórico das conversas acompanha a entrega final.
