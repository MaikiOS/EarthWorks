[CmdletBinding()]
param(
    [string] $AssemblyPath = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll',
    [string] $TerrainApplierPath = (Join-Path $PSScriptRoot '..\src\EarthWorks\RoadTerrainApplier.cs'),
    [string] $CecilPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $CecilPath) {
    $nugetLine = & dotnet nuget locals global-packages --list
    if ($LASTEXITCODE -ne 0 -or $nugetLine -notmatch '^global-packages:\s*(.+)$') {
        throw 'Unable to locate the NuGet global-packages folder.'
    }
    $CecilPath = Join-Path $Matches[1].Trim() 'coverlet.collector\6.0.4\build\netstandard2.0\Mono.Cecil.dll'
}
Add-Type -Path $CecilPath
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)

function Get-TypeDefinition([string] $name) {
    $type = $assembly.MainModule.Types | Where-Object FullName -CEQ $name | Select-Object -First 1
    if ($null -eq $type) { throw "Missing type: $name" }
    return $type
}

function Assert-Field([string] $typeName, [string] $fieldName, [string] $fieldType) {
    $type = Get-TypeDefinition $typeName
    $field = $type.Fields | Where-Object Name -CEQ $fieldName | Select-Object -First 1
    if ($null -eq $field) { throw "Missing field: $typeName.$fieldName" }
    if ($field.FieldType.FullName -cne $fieldType) {
        throw "Wrong field type: $typeName.$fieldName is $($field.FieldType.FullName), expected $fieldType"
    }
    Write-Host "PASS field $typeName.$fieldName : $fieldType"
}

function Assert-Method(
    [string] $typeName,
    [string] $methodName,
    [string] $returnType,
    [string[]] $parameterTypes
) {
    $type = Get-TypeDefinition $typeName
    $method = $type.Methods | Where-Object {
        $_.Name -ceq $methodName -and
        $_.ReturnType.FullName -ceq $returnType -and
        $_.Parameters.Count -eq $parameterTypes.Count -and
        -not (Compare-Object @($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) $parameterTypes -SyncWindow 0)
    } | Select-Object -First 1
    if ($null -eq $method) {
        throw "Missing method: $typeName.$methodName($($parameterTypes -join ', ')) : $returnType"
    }
    Write-Host "PASS method $typeName.$methodName($($parameterTypes -join ', ')) : $returnType"
    return $method
}

try {
    Assert-Method 'Hoverable' 'GetHoverOffset' 'System.Single' @() | Out-Null
    Assert-Field 'Sign' 'm_hoverOffset' 'System.Single'
    $signHover = Assert-Method 'Sign' 'GetHoverOffset' 'System.Single' @()
    $signBody = @($signHover.Body.Instructions.OpCode.Name) -join ','
    if ($signBody -cne 'ldarg.0,ldfld,ret' -or
        $signHover.Body.Instructions[1].Operand.FullName -cne 'System.Single Sign::m_hoverOffset') {
        throw 'Sign.GetHoverOffset no longer returns Sign.m_hoverOffset directly.'
    }
    Write-Output 'PASS vanilla Sign.GetHoverOffset returns Sign.m_hoverOffset'

    Assert-Field 'Player' 'm_placementGhost' 'UnityEngine.GameObject'
    Assert-Field 'GameCamera' 'm_camera' 'UnityEngine.Camera'
    Assert-Field 'GameCamera' 'm_skyCamera' 'UnityEngine.Camera'
    Assert-Field 'TerrainComp' 'm_width' 'System.Int32'
    Assert-Field 'TerrainComp' 'm_modifiedHeight' 'System.Boolean[]'
    Assert-Field 'TerrainComp' 'm_levelDelta' 'System.Single[]'
    Assert-Field 'TerrainComp' 'm_smoothDelta' 'System.Single[]'
    Assert-Field 'TerrainComp' 'm_modifiedPaint' 'System.Boolean[]'
    Assert-Field 'TerrainComp' 'm_paintMask' 'UnityEngine.Color[]'
    Assert-Field 'TerrainComp' 'm_operations' 'System.Int32'
    Assert-Field 'TerrainComp' 'm_lastOpPoint' 'UnityEngine.Vector3'
    Assert-Field 'TerrainComp' 'm_lastOpRadius' 'System.Single'
    Assert-Field 'TerrainComp' 'm_nview' 'ZNetView'

    Assert-Method 'TerrainComp' 'Save' 'System.Void' @('System.Boolean') | Out-Null
    Assert-Method 'Heightmap' 'GetWorldBaseHeight' 'System.Boolean' @('UnityEngine.Vector3', 'System.Single&') | Out-Null
    Assert-Method 'Heightmap' 'Poke' 'System.Void' @('System.Int32', 'System.Boolean') | Out-Null
    Assert-Method 'Player' 'Update' 'System.Void' @() | Out-Null
    Assert-Method 'Player' 'TryPlacePiece' 'System.Boolean' @('Piece') | Out-Null
    Assert-Method 'GameCamera' 'LateUpdate' 'System.Void' @() | Out-Null
    Assert-Method 'GameCamera' 'UpdateMouseCapture' 'System.Void' @() | Out-Null
    Assert-Method 'Player' 'TakeInput' 'System.Boolean' @() | Out-Null
    Assert-Method 'Player' 'OnDamaged' 'System.Void' @('HitData') | Out-Null
    Assert-Method 'Player' 'GetPlayer' 'Player' @('System.Int64') | Out-Null
    Assert-Method 'ZNet' 'GetPeer' 'ZNetPeer' @('System.Int64') | Out-Null
    Assert-Method 'ZNet' 'GetUID' 'System.Int64' @() | Out-Null
    Assert-Field 'ZNetPeer' 'm_playerID' 'System.Int64'
    Assert-Method 'PrivateArea' 'IsEnabled' 'System.Boolean' @() | Out-Null
    Assert-Method 'PrivateArea' 'IsInside' 'System.Boolean' @('UnityEngine.Vector3', 'System.Single') | Out-Null
    Assert-Method 'PrivateArea' 'IsPermitted' 'System.Boolean' @('System.Int64') | Out-Null
    Assert-Field 'PrivateArea' 'm_allAreas' 'System.Collections.Generic.List`1<PrivateArea>'

    $terrainSource = Get-Content -LiteralPath $TerrainApplierPath -Raw
    $saveCalls = ([regex]::Matches($terrainSource, 'SaveMethod\.Invoke\(batch\.Compiler, new object\[\] \{ false \}\);')).Count
    if ($saveCalls -ne 2 -or $terrainSource.Contains('SaveMethod.Invoke(batch.Compiler, null)')) {
        throw 'TerrainComp.Save(bool) reflection calls must both pass false.'
    }
    Write-Output 'PASS EarthWorks passes false to both TerrainComp.Save(bool) reflection calls'

    Write-Output 'All EarthWorks Valheim API contracts passed.'
}
finally {
    $assembly.Dispose()
}
