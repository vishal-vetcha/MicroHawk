$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
if(-not (Test-Path artifacts/build/MicroHawk.exe)){throw 'Build first: .\tools\run-unity.ps1 -Action Build'}
& ./.venv/Scripts/python.exe -c 'from microhawk.config.settings import TOKEN_PATH'
$env:MICROHAWK_TOKEN=(Get-Content runtime-data/operator.token -Raw).Trim()
$player=Start-Process -FilePath "$repoRoot/artifacts/build/MicroHawk.exe" -ArgumentList '-screen-width','1280','-screen-height','800','-screen-fullscreen','0','-logFile',"$repoRoot/artifacts/player.log" -WindowStyle Hidden -PassThru
Write-Output "MicroHawk simulator PID: $($player.Id)"
