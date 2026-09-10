param([switch]$Unity,[switch]$Live)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
& ./.venv/Scripts/python.exe -m pytest backend/tests -q -p no:cacheprovider
if($LASTEXITCODE -ne 0){throw 'Python tests failed'}
if($Unity){
    & pwsh -NoProfile -File tools/run-unity.ps1 -Action EditMode
    if($LASTEXITCODE -ne 0){throw 'Unity EditMode failed'}
    & pwsh -NoProfile -File tools/run-unity.ps1 -Action PlayModeVisual
    if($LASTEXITCODE -ne 0){throw 'Unity PlayMode failed'}
}
if($Live){& ./.venv/Scripts/python.exe tools/verify-live.py;if($LASTEXITCODE -ne 0){throw 'Live demonstration failed'}}
