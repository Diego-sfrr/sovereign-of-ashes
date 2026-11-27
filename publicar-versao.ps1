<#
.SYNOPSIS
  Gera uma nova versão do Sovereign of Ashes para os amigos em um comando só.

.EXAMPLE
  .\publicar-versao.ps1 -Version 0.4.0 -Notes "Novo chefe e ajustes de equilibrio"
  .\publicar-versao.ps1 -Version 0.4.0 -DownloadUrl "https://www.mediafire.com/file/xxxx/SovereignOfAshes-Windows.zip/file"

.DESCRIPTION
  1. grava o número da versão no projeto;
  2. roda os testes (para se algum falhar);
  3. gera o build do Windows;
  4. copia para a área de trabalho e cria o SovereignOfAshes-Windows.zip;
  5. escreve o version.json (o jogo lê esse arquivo e avisa "nova versão disponível");
  6. se houver um gist configurado (tools\gist-id.txt), atualiza o version.json publicado.

  Depois é só enviar o zip para o MediaFire (ou outro lugar) e, se o link mudou, rodar de novo com -DownloadUrl
  ou editar o link no version.json publicado.
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$DownloadUrl = "",
    [string]$Notes = "",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$project = $PSScriptRoot
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.8f1-x86_64\Editor\Unity.exe"
$desktop = [Environment]::GetFolderPath("Desktop")
$oneDriveDesktop = Join-Path $env:USERPROFILE "OneDrive\Desktop"
if (Test-Path $oneDriveDesktop) { $desktop = $oneDriveDesktop }
$logs = Join-Path $project "Logs"
New-Item -ItemType Directory -Force $logs | Out-Null

if ($Version -notmatch '^\d+(\.\d+){0,3}$') { throw "Versão inválida: use números separados por ponto, ex.: 0.4.0" }

# 1) versão no projeto
$settingsFile = Join-Path $project "ProjectSettings\ProjectSettings.asset"
$text = [IO.File]::ReadAllText($settingsFile)
$text = [regex]::Replace($text, '(?m)^(\s*bundleVersion:\s*).*$', "`${1}$Version")
[IO.File]::WriteAllText($settingsFile, $text)
Write-Host "Versão do projeto: $Version"

# O Unity Hub precisa estar aberto para o modo batch funcionar.
Start-Process "unityhub://"
Start-Sleep -Seconds 5

# 2) testes
if (-not $SkipTests) {
    $results = Join-Path $logs "testes.xml"
    & $unity -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testResults $results -logFile (Join-Path $logs "testes.log") | Out-Null
    $run = Select-String -Path $results -Pattern '<test-run ' | Select-Object -First 1
    if ($run.Line -notmatch 'result="Passed"') { throw "Testes falharam. Veja Logs\testes.xml. Nada foi publicado." }
    Write-Host "Testes: OK"
}

# 3) build
& $unity -batchmode -nographics -projectPath $project -executeMethod SovereignOfAshes.EditorTools.SovereignSetup.BuildWindows -quit -logFile (Join-Path $logs "build.log") | Out-Null
if (-not (Select-String -Path (Join-Path $logs "build.log") -Pattern 'Build Finished, Result: Success' -Quiet)) { throw "O build falhou. Veja Logs\build.log." }
Write-Host "Build: OK"

# 4) cópia e zip
$source = Join-Path $project "Builds\Windows"
$target = Join-Path $desktop "SovereignOfAshes"
robocopy $source $target /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
$zip = Join-Path $desktop "SovereignOfAshes-Windows.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path $target -DestinationPath $zip -CompressionLevel Optimal
Write-Host ("Zip: {0} ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB))

# 5) version.json
$manifestFile = Join-Path $project "tools\version.json"
New-Item -ItemType Directory -Force (Join-Path $project "tools") | Out-Null
if (-not $DownloadUrl -and (Test-Path $manifestFile)) {
    $old = Get-Content $manifestFile -Raw | ConvertFrom-Json
    $DownloadUrl = $old.url
}
if (-not $DownloadUrl) { $DownloadUrl = "https://www.mediafire.com/" }
$manifest = [ordered]@{ version = $Version; url = $DownloadUrl; notes = $Notes } | ConvertTo-Json
[IO.File]::WriteAllText($manifestFile, $manifest, (New-Object Text.UTF8Encoding($false)))
Write-Host "version.json gravado em tools\version.json"

# 6) gist (opcional)
$gistFile = Join-Path $project "tools\gist-id.txt"
if (Test-Path $gistFile) {
    $gistId = (Get-Content $gistFile -Raw).Trim()
    gh gist edit $gistId -f version.json $manifestFile
    Write-Host "version.json publicado no gist."
}
else {
    Write-Host "Sem gist configurado: publique tools\version.json no endereço que o jogo consulta."
}

Write-Host ""
Write-Host "Pronto! Agora envie o zip para o MediaFire e avise o pessoal no Discord."
