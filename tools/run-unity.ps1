param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('Author','EditMode','PlayMode','PlayModeVisual','Capture','Build')]
    [string]$Action,
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$repositoryRoot=Split-Path $PSScriptRoot -Parent
$projectPath=Join-Path $repositoryRoot 'simulator\unity\MicroHawkSim'
$outputPath=Join-Path $repositoryRoot 'artifacts'
if(!(Test-Path -LiteralPath $UnityPath)){throw "Unity not found: $UnityPath"}
if((Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion -notlike '6000.6.0f1*'){throw 'Expected pinned Unity 6000.6.0f1.'}
New-Item -ItemType Directory -Force $outputPath | Out-Null
$logPath=Join-Path $outputPath "$Action.log"
$arguments=@('-batchmode','-projectPath',('"'+$projectPath+'"'),'-logFile',('"'+$logPath+'"'))
switch($Action){
    'Author' {$arguments+=@('-nographics','-executeMethod','MicroHawk.Editor.IndustrialSceneBuilder.Build','-quit')}
    'Build' {$arguments+=@('-nographics','-executeMethod','MicroHawk.Editor.FlightDemoBuild.Build','-quit')}
    'Capture' {$arguments+=@('-force-d3d11','-executeMethod','MicroHawk.Editor.IndustrialSceneBuilder.CaptureViews','-quit')}
    'PlayModeVisual' {$arguments+=@('-force-d3d11','-runTests','-testPlatform','PlayMode','-testResults',('"'+(Join-Path $outputPath "$Action.xml")+'"'))}
    default {$arguments+=@('-nographics','-runTests','-testPlatform',$Action,'-testResults',('"'+(Join-Path $outputPath "$Action.xml")+'"'))}
}
$start=[System.Diagnostics.ProcessStartInfo]::new()
$start.FileName=$UnityPath
$start.Arguments=$arguments -join ' '
$start.UseShellExecute=$false
$start.CreateNoWindow=$true
$start.WindowStyle=[System.Diagnostics.ProcessWindowStyle]::Hidden
$process=[System.Diagnostics.Process]::Start($start)
$process.WaitForExit()
$exitCode=$process.ExitCode
Write-Output "Unity $Action exited $exitCode. Log: $logPath"
if($exitCode -ne 0){throw "Unity failed. Inspect $logPath"}
if($Action -in @('EditMode','PlayMode','PlayModeVisual')){
    $resultFile=Join-Path $outputPath "$Action.xml"
    if(!(Test-Path -LiteralPath $resultFile)){throw 'Unity did not produce a test result file.'}
    [xml]$results=Get-Content -LiteralPath $resultFile
    $result=$results.'test-run'
    Write-Output "Tests: $($result.total); passed: $($result.passed); failed: $($result.failed); result: $($result.result)"
    if($result.result -ne 'Passed' -or [int]$result.total -eq 0){throw 'Unity tests did not pass.'}
}


