# Publicação no MonsterASP.NET (3ª medição, até 05/11)

Passo a passo para colocar o AgendaDrive no ar no plano gratuito do MonsterASP.NET.
Ajuda oficial: https://help.monsterasp.net/books/deploy

Resumo do que vai acontecer:

1. Criar a conta, o site e o banco no painel.
2. Rodar o `database/script.sql` no banco de lá.
3. Publicar o código (Web Deploy pelo Visual Studio ou FTPS).
4. Enviar à mão o `App_Secrets/secrets.json` de produção.
5. Testar pelo link público, inclusive o log de erros.

> **Por que o `secrets.json` vai à mão?** O `AgendaDrive.csproj` tem `CopyToPublishDirectory="Never"` para `App_Secrets` e `App_Data`.
> Assim a publicação nunca leva a connection string do seu computador e nunca apaga a de produção.

---

## 1. Conta, site e banco

1. Crie a conta em https://www.monsterasp.net. A conta é sua; use seus dados.
2. No painel (Control Panel), em **Websites**, crie um site. Anote o endereço `algumacoisa.runasp.net`.
3. Em **Databases**, crie um banco **MSSQL**. Anote:
   - o servidor;
   - o nome do banco;
   - o usuário;
   - a senha.
4. Abra o banco e copie a **connection string "local"**. É a que o site usa dentro do servidor do MonsterASP; a "remote" é para acessar do seu computador.

## 2. Rodar o script no banco da hospedagem

O banco já vem criado e com outro nome, então **apague estas 4 linhas do começo** do script antes de rodar:

```sql
create database AgendaDrive;
go
use AgendaDrive;
go
```

O resto do script (os `set` e todas as tabelas) roda dentro do banco que você criou no painel.

Escolha uma das opções:

- **SSMS (recomendado):**
  1. No painel, ative o **acesso remoto** ao banco.
  2. Conecte o SQL Server Management Studio com o servidor, usuário e senha da connection string **remote**.
  3. Selecione o banco do MonsterASP na lista de bancos.
  4. Abra o script e execute.
- **Pelo painel:** use a opção de importar um script `.sql` ou executar T-SQL (guias "Import a .sql script" e "Run a T-SQL query" na ajuda). Se a ferramenta reclamar do `go`, rode o script em partes, separando nos `go`.

No fim, os `select` devem mostrar os dados de exemplo (2 usuários, 1 tarifa, 3 corridas…).

> Cuidado: este script **cria** tabelas. Se precisar rodá-lo de novo, as tabelas já vão existir e ele vai falhar.
> Nunca rode `drop`/`delete` no banco da hospedagem sem fazer backup antes.

## 3. Publicar o código

### Opção A — Visual Studio (Web Deploy), a mais simples

1. No painel: **Websites** → seu site → **Deploy**.
2. No cartão **Visual Studio access**, coloque **WebDeploy** em **Enabled** e clique em **Download publish profile**.
3. No Visual Studio, abra `AgendaDrive.slnx`. Depois clique com o botão direito no projeto **AgendaDrive** → **Publicar** → **Importar perfil** → escolha o arquivo `.publishSettings`.
4. Clique em **Mostrar todas as configurações** e confira:
   - **Configuração:** `Release`. **Estrutura de destino:** `net10.0`.
   - **Database / Entity Framework Migrations: deixe vazio e desmarcado.** O projeto é Database First (o banco veio do script) e a connection string fica no `App_Secrets`, não no `appsettings.json`.
   - **"Remover arquivos adicionais no destino": deixe desmarcado.** Se marcar, a publicação apaga do servidor o `App_Secrets/secrets.json` e os logs, que não fazem parte do pacote.
5. Clique em **Publicar**.

> O perfil `.publishSettings` contém a senha de publicação. Não o coloque dentro do repositório (ou coloque em `App_Secrets/`, que já é ignorado pelo Git).

### Opção B — `dotnet publish` + FTPS (sem Visual Studio)

1. Gere a pasta de publicação:

   ```bash
   dotnet publish AgendaDrive/AgendaDrive.csproj -c Release -o publicar
   ```

2. No painel, em **Deploy**, pegue os dados do cartão **FTP / SFTP access**: servidor, porta, login e senha.
   - Use **FTPS ou SFTP**, nunca FTP puro.
   - Use o nome do servidor, não o IP.
3. No FileZilla (**Arquivo → Gerenciador de sites**), conecte e entre na pasta remota **`wwwroot`**. Ao conectar você fica um nível acima dela, e o que for enviado lá não aparece no site.
4. **Pare o site** no painel (botão **Stop**), senão o IIS trava os arquivos ("550 file in use").
5. Envie **o conteúdo** da pasta `publicar` para dentro de `wwwroot`.
   - No servidor, a estrutura vai ficar `wwwroot/AgendaDrive.dll`, `wwwroot/web.config`, `wwwroot/wwwroot/css/...` etc.
   - É normal ter um `wwwroot` dentro do outro: o de fora é a raiz do site no IIS e o de dentro é a pasta de arquivos estáticos do ASP.NET Core.
6. Inicie o site de novo (**Start**).

> A pasta `publicar/` não deve ir para o Git. Gere-a fora do repositório, ou apague depois de enviar.

### Sobre o `web.config` gerado

A publicação cria um `web.config` com o `AspNetCoreModuleV2`. Ele **não** é o `Web.config` do MVC 5: é só o arquivo que o IIS do MonsterASP precisa para iniciar uma aplicação ASP.NET Core. Pode deixar como está.

## 4. Enviar o `App_Secrets/secrets.json` de produção

1. Crie no seu computador um arquivo `secrets.json` com a connection string **local** do MonsterASP. Use o `AgendaDrive/App_Secrets/secrets.exemplo.json` como modelo:

   ```json
   {
     "ConnectionStrings": {
       "AgendaDrive": "COLE_AQUI_A_CONNECTION_STRING_LOCAL_DO_PAINEL"
     },
     "OpenRouteService": {
       "Chave": ""
     }
   }
   ```

   - Se a connection string do painel vier sem `TrustServerCertificate=True` e o site der erro de certificado ao conectar, acrescente `;TrustServerCertificate=True` no final.
   - **Não** substitua o `secrets.json` do seu computador por este. O local continua apontando para o LocalDB.
2. Pelo FileZilla ou pelo **WebFTP** do painel, crie a pasta `wwwroot/App_Secrets` no servidor e envie o arquivo para lá. O caminho final fica `wwwroot/App_Secrets/secrets.json`.
3. **Reinicie o site** no painel (Stop → Start). A connection string é lida uma vez, quando o site sobe.

> `App_Secrets` fica fora do `wwwroot` interno, então o site não serve esse arquivo: `https://seusite.runasp.net/App_Secrets/secrets.json` deve dar **404**.
> No computador isso já foi testado; teste também no servidor (passo 5).

## 5. Testar pelo link público

Abra `https://SEUSITE.runasp.net` e confira, de preferência também pelo celular:

- [ ] A página inicial abre.
- [ ] Em **Tarifas**, a tarifa de exemplo aparece como **Ativa**. Isso prova que a **conexão com o banco** funciona.
- [ ] Criar uma tarifa com valores como `6,50`. Depois, em Detalhes, conferir que ficou R$ 6,50 e não R$ 650,00.
- [ ] Ativar a nova tarifa: a antiga fica inativa. Reativar a antiga e excluir a nova.
- [ ] Abrir um endereço que não existe (`/qualquercoisa`): aparece "Página não encontrada".
- [ ] `/App_Secrets/secrets.json` e `/App_Data/logs/` dão 404.

### Testar o log de exceções no servidor (pedido da 3ª medição)

O log só é gravado quando acontece uma exceção. Para provar que funciona na hospedagem:

1. No `wwwroot/App_Secrets/secrets.json` **do servidor**, mude temporariamente o nome do banco na connection string (por exemplo `Database=nome_errado`). Reinicie o site.
2. Abra **Tarifas**: deve aparecer "Ops, algo deu errado" com um código.
3. Pelo FTP/WebFTP, abra `wwwroot/App_Data/logs/AAAA-MM-DD.log`. Deve haver um registro com o mesmo código, a URL `GET /Tarifas`, a mensagem e o stack trace.
4. **Volte o nome certo do banco** e reinicie o site.

Se a pasta `App_Data/logs` não for criada, o site está sem permissão de escrita. Nesse caso, crie a pasta `wwwroot/App_Data/logs` pelo FTP e teste de novo; se ainda assim não gravar, procure "permissions" na ajuda do MonsterASP ou abra um chamado.

Tire prints dessas telas (computador e celular) para o PDF final, **sem dados reais de clientes**.

## Problemas comuns

| Sintoma | Causa provável e solução |
| --- | --- |
| **HTTP Error 500.30 / 500.31** ao abrir o site | O app não conseguiu iniciar. Na maioria das vezes falta o `App_Secrets/secrets.json` no servidor (o `Program.cs` exige o arquivo). Se o arquivo estiver lá, pode ser o runtime do .NET 10: publique como *self-contained* (`dotnet publish ... -r win-x64 --self-contained`). |
| "Ops, algo deu errado" em **Tarifas** | Erro de banco: connection string errada, script não rodado ou certificado. O motivo exato está em `App_Data/logs`. |
| **403 Forbidden** | Os arquivos foram enviados para fora da pasta `wwwroot` do servidor. |
| "550 file in use" no FTP | Pare o site antes de enviar (passo 3, opção B). |
| Valores salvos ×100 (`6,50` virou `650`) | Não deveria acontecer: a cultura pt-BR é fixada no `Program.cs`. Se acontecer, avise com o print. |

## Depois que estiver no ar

- Atualize o `CLAUDE.md` (checklist) e anote o link público. Ele vai no PDF final.
- **Lembrete de segurança:** até a 4ª medição, a área de Tarifas fica aberta para quem tiver o link. Quando o login existir, descomente `[Authorize(Roles = "Administrador")]` no `TarifasController` e publique de novo.
