param([string]$Python = "")
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
if(-not (Test-Path .venv/Scripts/python.exe)) {
    if(-not $Python) { $Python=(Get-Command python -ErrorAction Stop).Source }
    & $Python -m venv .venv
    if($LASTEXITCODE -ne 0){throw 'Python 3.12 is required'}
}
& ./.venv/Scripts/python.exe -m pip install -r backend/requirements.lock
if($LASTEXITCODE -ne 0){throw 'Dependency installation failed'}
& ./.venv/Scripts/python.exe -m pip install --no-deps -e ./backend
if($LASTEXITCODE -ne 0){throw 'Backend installation failed'}
Write-Output 'Setup complete. Ollama with qwen2.5vl:3b and Unity 6000.6.0f1 are separate prerequisites.'
