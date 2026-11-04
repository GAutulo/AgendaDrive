# Publica o AgendaDrive no MonsterASP com um comando so:
# compila, junta o secrets.json de producao, envia por Web Deploy e testa o site.
#
# Antes de rodar: painel -> Websites -> agendadrive -> Deploy -> WebDeploy Enabled -> Download publish profile.
# Uso: powershell -ExecutionPolicy Bypass -File publicar-hospedagem.ps1
#
# A senha de publicacao fica so no arquivo .publishSettings e quem a usa e o msdeploy.
# A connection string e pedida na hora e o arquivo temporario e apagado no fim.
# -SemEnviar monta o pacote e para antes do envio (serve para testar o script).

param(
    [string]$Perfil,
    [string]$ConnectionString,
    [switch]$SemEnviar
)

$ErrorActionPreference = 'Stop'

$msdeploy = 'C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe'
$raiz = $PSScriptRoot

if (-not (Test-Path $msdeploy)) { throw "msdeploy nao encontrado em $msdeploy." }

# O perfil mais recente da pasta Downloads.
if (-not $Perfil) {
    $encontrado = Get-ChildItem (Join-Path $env:USERPROFILE 'Downloads') -Filter '*.publishSettings' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $encontrado) {
        throw 'Nenhum .publishSettings em Downloads. Baixe o perfil no painel (Deploy -> Download publish profile).'
    }
    $Perfil = $encontrado.FullName
}

# Do perfil, leio so o nome do site e o endereco, que nao sao segredos.
$xml = [xml](Get-Content $Perfil -Raw)
$dadosPerfil = $xml.publishData.publishProfile | Where-Object { $_.publishMethod -eq 'MSDeploy' } | Select-Object -First 1
if (-not $dadosPerfil) { throw 'O arquivo nao tem um perfil MSDeploy.' }
$site = $dadosPerfil.msdeploySite
$endereco = $dadosPerfil.destinationAppUrl
Write-Host "Perfil: $(Split-Path $Perfil -Leaf)"
Write-Host "Destino: $site ($endereco)"

if (-not $ConnectionString) {
    $seguro = Read-Host 'Cole a connection string LOCAL do painel (nao aparece na tela)' -AsSecureString
    $ConnectionString = (New-Object System.Net.NetworkCredential('', $seguro)).Password
}
if (-not $ConnectionString.Trim()) { throw 'Connection string vazia.' }

$pacote = Join-Path $env:TEMP ('agendadrive-' + [guid]::NewGuid().ToString('N'))
try {
    Write-Host 'Compilando...'
    & dotnet publish (Join-Path $raiz 'AgendaDrive\AgendaDrive.csproj') -c Release -o $pacote --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'A compilacao falhou.' }

    # O csproj nao publica App_Secrets; aqui entra o arquivo de producao, com ConvertTo-Json escapando o que precisar.
    $pastaSegredos = Join-Path $pacote 'App_Secrets'
    New-Item -ItemType Directory -Path $pastaSegredos | Out-Null
    $segredos = @{
        ConnectionStrings = @{ AgendaDrive = $ConnectionString.Trim() }
        OpenRouteService  = @{ Chave = '' }
    } | ConvertTo-Json
    [IO.File]::WriteAllText((Join-Path $pastaSegredos 'secrets.json'), $segredos, (New-Object Text.UTF8Encoding $false))

    if ($SemEnviar) {
        $lido = Get-Content (Join-Path $pastaSegredos 'secrets.json') -Raw | ConvertFrom-Json
        Write-Host "Pacote OK: $((Get-ChildItem $pacote -Recurse -File).Count) arquivos; secrets.json valido: $($lido.ConnectionStrings.AgendaDrive.Length) caracteres."
        return
    }

    # DoNotDeleteRule: nao apaga o que ja esta no servidor (logs em App_Data).
    # AppOffline: tira o site do ar durante a copia, para o IIS nao travar os arquivos.
    Write-Host 'Enviando (pode levar alguns minutos)...'
    & $msdeploy -verb:sync "-source:contentPath=$pacote" "-dest:contentPath=$site,publishSettings=$Perfil" -enableRule:DoNotDeleteRule -enableRule:AppOffline
    if ($LASTEXITCODE -ne 0) { throw 'O envio falhou. Veja a mensagem acima.' }
}
finally {
    if (Test-Path $pacote) { Remove-Item $pacote -Recurse -Force }
    $ConnectionString = $null
}

# Teste real: a pagina de Tarifas so mostra "Ativa" se site, secrets.json e banco estiverem funcionando.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$url = $endereco.TrimEnd('/') + '/Tarifas'
Write-Host "Testando $url ..."
foreach ($tentativa in 1..6) {
    Start-Sleep -Seconds 10
    try {
        $resposta = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 60
        if ($resposta.Content -match 'text-bg-success') {
            Write-Host "NO AR: $url" -ForegroundColor Green
            exit 0
        }
        Write-Host "Tentativa ${tentativa}: a pagina abriu, mas sem tarifa ativa."
    }
    catch {
        Write-Host "Tentativa ${tentativa}: $($_.Exception.Message)"
    }
}
Write-Host 'Publicado, mas o teste nao passou. Abra o site e me diga o que aparece.' -ForegroundColor Yellow
exit 1
