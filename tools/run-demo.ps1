param([switch]$NoBrowser)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
& ./.venv/Scripts/python.exe -c 'from microhawk.config.settings import TOKEN_PATH'
$backend=Start-Process -FilePath "$repoRoot/.venv/Scripts/python.exe" -ArgumentList '-m','uvicorn','microhawk.api.app:app','--host','127.0.0.1','--port','8000' -WorkingDirectory $repoRoot -WindowStyle Hidden -RedirectStandardOutput "$repoRoot/artifacts/backend.log" -RedirectStandardError "$repoRoot/artifacts/backend-error.log" -PassThru
& "$PSScriptRoot/run-simulator.ps1"
Write-Output "Backend PID: $($backend.Id). Dashboard: http://127.0.0.1:8000"
if(-not $NoBrowser){Start-Process 'http://127.0.0.1:8000'}
Write-Output 'Use the local operator token file runtime-data/operator.token to unlock controls in the dashboard.'
