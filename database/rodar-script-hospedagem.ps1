# Roda o script.sql no banco da hospedagem, sem o "create database" e o "use" do comeco.
# Tudo acontece numa transacao: se algo falhar, nada fica gravado e da para rodar de novo.
# A senha e pedida na hora, nao aparece na tela e nao fica salva em nenhum arquivo.
#
# Uso: powershell -ExecutionPolicy Bypass -File database\rodar-script-hospedagem.ps1
# -AutenticacaoWindows serve so para testar no LocalDB, sem senha.

param(
    [string]$Servidor,
    [string]$Banco,
    [string]$Login,
    [switch]$AutenticacaoWindows
)

$ErrorActionPreference = 'Stop'

$texto = Get-Content (Join-Path $PSScriptRoot 'script.sql') -Raw

# Tudo antes deste comentario so vale para o banco local.
$marcador = '-- exigido pelo indice filtrado'
$posicao = $texto.IndexOf($marcador)
if ($posicao -lt 0) {
    throw "Trecho '$marcador' nao encontrado no script.sql."
}

# "go" nao e SQL: e um separador do SSMS. Aqui cada lote e enviado separadamente.
$lotes = [regex]::Split($texto.Substring($posicao), '(?im)^\s*go\s*$') | Where-Object { $_.Trim() }

if (-not $Servidor) { $Servidor = Read-Host 'Servidor (connection string REMOTE, depois de Server=)' }
if (-not $Banco) { $Banco = Read-Host 'Nome do banco' }

$dados = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$dados['Data Source'] = $Servidor
$dados['Initial Catalog'] = $Banco

if ($AutenticacaoWindows) {
    $dados['Integrated Security'] = $true
    $conexao = New-Object System.Data.SqlClient.SqlConnection($dados.ConnectionString)
}
else {
    # Conexao pela internet: sempre criptografada. O LocalDB nao suporta isso, por isso fica so aqui.
    $dados['Encrypt'] = $true
    $dados['TrustServerCertificate'] = $true

    if (-not $Login) { $Login = Read-Host 'Login' }
    $senha = Read-Host 'Cole a senha NOVA do banco (nao aparece na tela)' -AsSecureString
    $senha.MakeReadOnly()
    $credencial = New-Object System.Data.SqlClient.SqlCredential($Login, $senha)
    $conexao = New-Object System.Data.SqlClient.SqlConnection($dados.ConnectionString, $credencial)
}

function Consultar([string]$sql) {
    $comando = $conexao.CreateCommand()
    $comando.CommandText = $sql
    return $comando.ExecuteScalar()
}

$conexao.Open()
try {
    Write-Host "Conectado ao banco: $(Consultar 'select db_name()')"

    if ((Consultar "select count(*) from sys.tables where name = 'Usuarios'") -gt 0) {
        Write-Host 'As tabelas ja existem neste banco. Nada foi alterado.' -ForegroundColor Yellow
        return
    }

    $transacao = $conexao.BeginTransaction()
    try {
        foreach ($lote in $lotes) {
            $comando = $conexao.CreateCommand()
            $comando.Transaction = $transacao
            $comando.CommandText = $lote
            $comando.CommandTimeout = 120
            [void]$comando.ExecuteNonQuery()
        }
        $transacao.Commit()
    }
    catch {
        $transacao.Rollback()
        Write-Host 'Erro: nada foi gravado no banco.' -ForegroundColor Red
        throw
    }

    Write-Host 'Script executado. Registros por tabela:' -ForegroundColor Green
    foreach ($tabela in 'Usuarios', 'Tarifas', 'Corridas', 'ItensAdicionais', 'Bloqueios', 'HistoricosStatus') {
        Write-Host ("  {0,-18} {1}" -f $tabela, (Consultar "select count(*) from $tabela"))
    }
}
finally {
    $conexao.Dispose()
}
