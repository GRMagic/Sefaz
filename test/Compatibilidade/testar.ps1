<#
.SYNOPSIS
    Testa a compatibilidade do pacote rmdev.sefaz em projetos consumidores limpos, cada um num container Docker.

.DESCRIPTION
    Em cada cenário, o Dockerfile gera o .nupkg a partir do código atual e o consome num projeto novo, com o
    SDK/runtime do cenário e, opcionalmente, com versões próprias das dependências (simulando quem já usa outras
    versões delas). O build do consumidor trata avisos como erros, então falha com conflito de versão
    (NU1605/NU1608), vulnerabilidade conhecida (NU190x) ou aviso de framework sem suporte. A auditoria de
    vulnerabilidades só existe a partir do SDK 8, então não roda no cenário net6.0. Depois, o consumidor
    executa a assinatura de XML e as chamadas WCF contra uma porta local fechada.

    Requer Docker com containers Linux. Os logs de cada cenário ficam na pasta temporária do sistema.

.PARAMETER Cenario
    Roda só os cenários cujo nome contém algum dos textos informados (ex.: -Cenario net8.0).

.EXAMPLE
    pwsh test/Compatibilidade/testar.ps1
    pwsh test/Compatibilidade/testar.ps1 -Cenario "net10.0"
#>
#Requires -Version 7
param([string[]] $Cenario)

$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$dockerfile = Join-Path $PSScriptRoot 'Dockerfile'
$pastaLogs = Join-Path ([System.IO.Path]::GetTempPath()) 'sefaz-compatibilidade'
New-Item -ItemType Directory -Force $pastaLogs | Out-Null

# Extras: pacotes que o consumidor referencia diretamente, no formato Pacote@Versão, separados por espaço.
# Não há cenário de net8.0 com WCF 10.x: o WCF 10 só traz implementação para net10.0 (a de netstandard2.0 serve
# só para compilar), então essa combinação quebra em tempo de execução mesmo sem esta biblioteca.
$cenarios = @(
    @{ Nome = 'net6.0';                                  Sdk = '6.0';  Tfm = 'net6.0';  Extras = '' }
    @{ Nome = 'net8.0';                                  Sdk = '8.0';  Tfm = 'net8.0';  Extras = '' }
    @{ Nome = 'net8.0 com WCF 4.8.1';                    Sdk = '8.0';  Tfm = 'net8.0';  Extras = 'System.ServiceModel.Http@4.8.1' }
    @{ Nome = 'net8.0 com WCF 6.2.0';                    Sdk = '8.0';  Tfm = 'net8.0';  Extras = 'System.ServiceModel.Http@6.2.0' }
    @{ Nome = 'net8.0 com WCF 8.1.2';                    Sdk = '8.0';  Tfm = 'net8.0';  Extras = 'System.ServiceModel.Http@8.1.2' }
    @{ Nome = 'net8.0 com Cryptography.Xml 10.x';        Sdk = '8.0';  Tfm = 'net8.0';  Extras = 'System.Security.Cryptography.Xml@10.0.15' }
    @{ Nome = 'net10.0';                                 Sdk = '10.0'; Tfm = 'net10.0'; Extras = '' }
    @{ Nome = 'net10.0 com Cryptography.Xml e WCF 10.x'; Sdk = '10.0'; Tfm = 'net10.0'; Extras = 'System.Security.Cryptography.Xml@10.0.15 System.ServiceModel.Http@10.1.0' }
)

if ($Cenario) {
    $cenarios = @($cenarios | Where-Object { $nome = $_.Nome; $Cenario | Where-Object { $nome -like "*$_*" } })
    if ($cenarios.Count -eq 0) { throw "Nenhum cenário corresponde a: $($Cenario -join ', ')" }
}

$resultados = foreach ($c in $cenarios) {
    $log = Join-Path $pastaLogs (($c.Nome -replace '[^\w.-]+', '_') + '.log')
    Write-Host "==> $($c.Nome)" -ForegroundColor Cyan

    # --pull usa a imagem atual da tag do SDK, e não uma cópia antiga em cache na máquina.
    # --no-cache-filter refaz sempre a etapa do consumidor (o restore e a auditoria do NuGet dependem do momento),
    # mas reaproveita a geração do pacote entre os cenários. type=cacheonly evita acumular imagens.
    docker build --pull --file $dockerfile `
        --build-arg "SDK_CONSUMIDOR=$($c.Sdk)" --build-arg "TFM=$($c.Tfm)" --build-arg "PACOTES_EXTRAS=$($c.Extras)" `
        --no-cache-filter consumidor --output type=cacheonly --progress plain $raiz *> $log
    $passou = $LASTEXITCODE -eq 0

    if ($passou) {
        Select-String -Path $log -Pattern '\[(OK|FALHA)\]|Runtime:' | ForEach-Object { '    ' + ($_.Line -replace '^#\d+ [\d.]+ ', '') } | Write-Host
    } else {
        Write-Host "    Falhou. Últimas linhas do log ($log):" -ForegroundColor Red
        Get-Content $log -Tail 25 | ForEach-Object { "    $_" } | Write-Host
    }
    [pscustomobject]@{ Cenario = $c.Nome; Resultado = $(if ($passou) { 'OK' } else { 'FALHOU' }); Log = $log }
}

$resultados | Format-Table -AutoSize | Out-String | Write-Host
if ($resultados | Where-Object Resultado -ne 'OK') { exit 1 }
