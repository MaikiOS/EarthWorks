[CmdletBinding()]
param(
    [string] $LocalizationPath = (Join-Path $PSScriptRoot '..\src\EarthWorks\EarthWorksLocalization.cs'),
    [string] $SourceRoot = (Join-Path $PSScriptRoot '..\src\EarthWorks')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$source = Get-Content -LiteralPath $LocalizationPath -Raw
$dictionaryPattern = '(?s)Dictionary<string, string>\s+(?<name>English|Russian).*?=\s*new Dictionary<string, string>\s*\{(?<body>.*?)\n\s*\};'
$entryPattern = '\{\s*"(?<key>[^"]+)"\s*,\s*"(?<value>(?:\\.|[^"])*)"\s*\}'
$dictionaries = @{}

foreach ($match in [regex]::Matches($source, $dictionaryPattern)) {
    $entries = @{}
    foreach ($entry in [regex]::Matches($match.Groups['body'].Value, $entryPattern)) {
        $key = $entry.Groups['key'].Value
        if ($entries.ContainsKey($key)) { throw "Duplicate $($match.Groups['name'].Value) token: $key" }
        $entries[$key] = $entry.Groups['value'].Value
    }
    $dictionaries[$match.Groups['name'].Value] = $entries
}

foreach ($language in 'English', 'Russian') {
    if (-not $dictionaries.ContainsKey($language) -or $dictionaries[$language].Count -eq 0) {
        throw "Unable to parse the $language localization dictionary."
    }
}

$englishKeys = @($dictionaries.English.Keys | Sort-Object)
$russianKeys = @($dictionaries.Russian.Keys | Sort-Object)
$keyDifference = Compare-Object $englishKeys $russianKeys
if ($keyDifference) {
    throw "English/Russian token mismatch:`n$($keyDifference | Out-String)"
}

foreach ($key in $englishKeys) {
    $englishPlaceholders = @([regex]::Matches($dictionaries.English[$key], '\{\d+(?::[^}]*)?\}') | ForEach-Object Value | Sort-Object -Unique)
    $russianPlaceholders = @([regex]::Matches($dictionaries.Russian[$key], '\{\d+(?::[^}]*)?\}') | ForEach-Object Value | Sort-Object -Unique)
    if (Compare-Object $englishPlaceholders $russianPlaceholders) {
        throw "Placeholder mismatch for token '$key': EN=[$($englishPlaceholders -join ', ')] RU=[$($russianPlaceholders -join ', ')]"
    }
}

$literalUsePattern = 'EarthWorksLocalization\.(?:Text|Token)\(\s*"(?<key>[^"]+)"'
foreach ($file in Get-ChildItem -LiteralPath $SourceRoot -Filter '*.cs' -File) {
    $fileSource = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($use in [regex]::Matches($fileSource, $literalUsePattern)) {
        $key = $use.Groups['key'].Value
        if (-not $dictionaries.English.ContainsKey($key)) {
            throw "Unknown localization token '$key' in $($file.Name)."
        }
    }

    if ($file.FullName -ne ([IO.Path]::GetFullPath($LocalizationPath)) -and $fileSource -match '[А-Яа-яЁё]') {
        throw "Cyrillic user-facing text remains outside EarthWorksLocalization.cs: $($file.Name)"
    }
}

$requiredDynamicKeys = @(
    'state_idle', 'state_draw', 'state_geometry', 'state_surface', 'state_review',
    'controls_idle', 'controls_draw', 'controls_geometry', 'controls_surface', 'controls_review',
    'stage_setup', 'stage_marking', 'stage_clearing', 'stage_earthworks', 'stage_surfacing', 'stage_completion', 'stage_completed'
)
foreach ($key in $requiredDynamicKeys) {
    if (-not $dictionaries.English.ContainsKey($key)) { throw "Missing dynamic localization token: $key" }
}

Write-Output "PASS English/Russian key parity: $($englishKeys.Count) tokens"
Write-Output 'PASS English/Russian placeholder parity'
Write-Output 'PASS literal token references'
Write-Output 'PASS dynamic state and stage tokens'
Write-Output 'PASS no Cyrillic text outside EarthWorksLocalization.cs'
