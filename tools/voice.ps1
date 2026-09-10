param([ValidateSet('listen','speak','check')][string]$Mode='check',[string]$TextFile='')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
if($Mode -eq 'check') {
    [System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers() | Select-Object Id,Name,@{n='Culture';e={.Culture.Name}} | ConvertTo-Json -Compress
    exit 0
}
if($Mode -eq 'speak') {
    $speaker=New-Object System.Speech.Synthesis.SpeechSynthesizer
    try {$speaker.Speak([IO.File]::ReadAllText($TextFile))}finally{$speaker.Dispose()}
    exit 0
}
$recognizers=[System.Speech.Recognition.SpeechRecognitionEngine]::InstalledRecognizers()
if($recognizers.Count -eq 0){throw 'No offline Windows speech recognizer installed. Install an English speech language pack in Windows Settings.'}
$recognizer=New-Object System.Speech.Recognition.SpeechRecognitionEngine($recognizers[0])
try {
    $recognizer.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
    $recognizer.SetInputToDefaultAudioDevice()
    $result=$recognizer.Recognize([TimeSpan]::FromSeconds(12))
    if($null -eq $result){throw 'No speech recognized before timeout'}
    @{text=$result.Text;confidence=$result.Confidence;source='windows_offline_microphone'}|ConvertTo-Json -Compress
} finally {$recognizer.Dispose()}

