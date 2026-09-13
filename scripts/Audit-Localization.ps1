[CmdletBinding()]
param(
    [string] $TranslationRoot = (Join-Path $PSScriptRoot '..\Translations\EarthWorks'),
    [string] $SourceRoot = (Join-Path $PSScriptRoot '..\src\EarthWorks')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$dictionaries = @{}
foreach ($language in 'English', 'Russian') {
    $path = Join-Path $TranslationRoot "$language\translations.json"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing $language translations: $path"
    }
    try {
        $dictionaries[$language] = Get-Content -LiteralPath $path -Raw |
            ConvertFrom-Json -AsHashtable
    }
    catch {
        throw "Unable to parse $language translations: $($_.Exception.Message)"
    }
    if ($dictionaries[$language].Count -eq 0) {
        throw "$language translations are empty."
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

    if ($fileSource -match '[А-Яа-яЁё]') {
        throw "Cyrillic user-facing text remains in source code: $($file.Name)"
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
Write-Output 'PASS no Cyrillic text in C# source files'
