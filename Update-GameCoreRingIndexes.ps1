#Requires -Version 7.0
<#
.SYNOPSIS
Rewrites GameCore-rooted ring ChromaID indexes in generated environment data and scenes to Chroma-boundary values.

.DESCRIPTION
The environment exports currently in this repository were captured through a requirements-free donor level, so the
GameCore scene's gameplay pools were instantiated before the environment scene activated and every dynamically
spawned ring clone received a ~500-offset root index. Real Chroma/Noodle maps load through the modded pipeline
where the environment activates first, so Chroma resolves the same rings at low indexes ([1..N]); LightIdDumper now
reproduces that load order. This script rewrites the stale high indexes in both the Data/*.json exports and the
generated *.unity scenes so existing generated content matches the corrected runtime captures without a full
"Create All from Data" regeneration.

Ring identity is mapped per family: each ring family's old indexes, sorted ascending, map rank-for-rank onto the
same family's captured indexes, sorted ascending, because both orders follow the deterministic spawner loop. Every
mapping is validated against the captures before any file is edited; an environment that fails validation is left
untouched and reported.

Non-ring GameCore-rooted families (for example TubeBloomPrePassLightHitPointRoot) never enter Chroma's matching
universe (Chroma only collects Environment-scene objects and TrackLaneRing components), so they are intentionally
left unchanged.

.PARAMETER GameVersion
Game version whose LightIdDumper captures define the target indexes. Defaults to 1.44.1.

.PARAMETER RuntimeLightDataRoot
LightIdDumper RuntimeLightData root. Defaults to the sibling BeatSaberLightIdDumper repository.

.PARAMETER WhatIf
Reports every planned rewrite without modifying files.
#>
param(
    [ValidateNotNullOrEmpty()]
    [string]$GameVersion = "1.44.1",

    [ValidateNotNullOrEmpty()]
    [string]$RuntimeLightDataRoot,

    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# This script lives at the repository root, so the sibling BeatSaberLightIdDumper repository shares its parent.
$RepoRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($RuntimeLightDataRoot)) {
    $RuntimeLightDataRoot = Join-Path (Split-Path -Parent $RepoRoot) "BeatSaberLightIdDumper" "RuntimeLightData"
}
$CaptureRoot = Join-Path $RuntimeLightDataRoot $GameVersion
$DataRoot = Join-Path $RepoRoot "Assets" "__Scenes" "Environments" "Data"
$SceneRoot = Join-Path $RepoRoot "Assets" "__Scenes" "Environments"

if (-not (Test-Path -LiteralPath $CaptureRoot -PathType Container)) {
    throw "No LightIdDumper captures for game version [$GameVersion] at [$CaptureRoot]."
}

# The ID segment after GameCore.[N] runs to the next child marker ('.'), the JSON string quote, or the YAML line end.
$GameCoreIdPattern = 'GameCore\.\[(\d+)\]([^\.\r\n"]+)'

# Reads a text file while preserving any UTF-8 BOM so rewritten files keep their original encoding.
function Read-TextPreservingBom {
    param([string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($hasBom -and $text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
        $text = $text.Substring(1)
    }
    return [pscustomobject]@{ Text = $text; HasBom = $hasBom }
}

function Write-TextPreservingBom {
    param([string]$Path, [string]$Text, [bool]$HasBom)
    $encoding = [System.Text.UTF8Encoding]::new($HasBom)
    [System.IO.File]::WriteAllText($Path, $Text, $encoding)
}

# Extracts { familyName -> sorted distinct indexes } for every GameCore-rooted ID found in the raw text.
function Get-GameCoreFamiliesFromText {
    param([string]$Text)
    $families = @{}
    foreach ($match in [regex]::Matches($Text, $GameCoreIdPattern)) {
        $familyName = $match.Groups[2].Value
        $index = [int]$match.Groups[1].Value
        if (-not $families.ContainsKey($familyName)) {
            $families[$familyName] = [System.Collections.Generic.SortedSet[int]]::new()
        }
        [void]$families[$familyName].Add($index)
    }
    return $families
}

# Loads the captured target families for one environment from both classified dump files.
function Get-CapturedFamilies {
    param([string]$EnvironmentName)
    $capturedText = [System.Text.StringBuilder]::new()
    foreach ($kind in @("BehaviorLights", "OtherLights")) {
        $capturePath = Join-Path $CaptureRoot "$EnvironmentName`_$kind.json"
        if (Test-Path -LiteralPath $capturePath -PathType Leaf) {
            [void]$capturedText.Append([System.IO.File]::ReadAllText($capturePath))
        }
    }
    if ($capturedText.Length -eq 0) {
        return $null
    }
    return Get-GameCoreFamiliesFromText $capturedText.ToString()
}

# Builds familyName -> index -> newIndex from old families plus captured anchors, validating every assumption.
# Returns $null (with a reason in $Script:LastMappingError) when the environment must be skipped.
$Script:LastMappingError = $null
function New-FamilyIndexMapping {
    param([hashtable]$OldFamilies, [hashtable]$CapturedFamilies)

    $ringFamilies = @($OldFamilies.Keys | Where-Object { $_.EndsWith("(Clone)") } | Sort-Object)
    $foreignFamilies = @($OldFamilies.Keys | Where-Object { -not $_.EndsWith("(Clone)") })

    if ($ringFamilies.Count -eq 0) {
        $Script:LastMappingError = "no ring families present; only non-ring GameCore roots"
        return $null
    }

    $mapping = @{}
    $unvalidatedFamilies = [System.Collections.Generic.List[string]]::new()

    foreach ($familyName in $ringFamilies) {
        $oldIndexes = @($OldFamilies[$familyName])
        if ($null -ne $CapturedFamilies -and $CapturedFamilies.ContainsKey($familyName)) {
            $capturedIndexes = @($CapturedFamilies[$familyName])
            if ($capturedIndexes.Count -ne $oldIndexes.Count) {
                $Script:LastMappingError = "family [$familyName] has [$($oldIndexes.Count)] exported objects but [$($capturedIndexes.Count)] captured"
                return $null
            }

            $sortedOld = $oldIndexes | Sort-Object
            $sortedNew = $capturedIndexes | Sort-Object
            $familyMap = @{}
            for ($i = 0; $i -lt $sortedOld.Count; $i++) {
                $familyMap[[int]$sortedOld[$i]] = [int]$sortedNew[$i]
            }
            $mapping[$familyName] = $familyMap
        }
        else {
            $unvalidatedFamilies.Add($familyName)
        }
    }

    if ($unvalidatedFamilies.Count -gt 0) {
        # Rings occupy [1..totalRingCount] contiguously in the requirements-carrying load, so the captured families
        # anchor where every uncaptured (lightless) family must sit; the leftover slots must fit those families exactly.
        $totalRingCount = ($ringFamilies | ForEach-Object { @($OldFamilies[$_]).Count } | Measure-Object -Sum).Sum
        $occupied = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($familyMap in $mapping.Values) {
            foreach ($newIndex in $familyMap.Values) {
                [void]$occupied.Add($newIndex)
            }
        }

        $remaining = @(1..$totalRingCount | Where-Object { -not $occupied.Contains($_) })
        $unvalidatedCount = ($unvalidatedFamilies | ForEach-Object { @($OldFamilies[$_]).Count } | Measure-Object -Sum).Sum
        if ($unvalidatedFamilies.Count -eq 1) {
            $familyName = $unvalidatedFamilies[0]
            $oldIndexes = @($OldFamilies[$familyName]) | Sort-Object
            if ($remaining.Count -ne $oldIndexes.Count) {
                $Script:LastMappingError = "family [$familyName] needs [$($oldIndexes.Count)] slots but [$($remaining.Count)] remain after captured anchors"
                return $null
            }

            $familyMap = @{}
            for ($i = 0; $i -lt $oldIndexes.Count; $i++) {
                $familyMap[[int]$oldIndexes[$i]] = [int]$remaining[$i]
            }
            $mapping[$familyName] = $familyMap
        }
        elseif ($remaining.Count -ne $unvalidatedCount) {
            $Script:LastMappingError = "[$($unvalidatedFamilies.Count)] uncaptured families need [$unvalidatedCount] slots but [$($remaining.Count)] remain"
            return $null
        }
        else {
            $Script:LastMappingError = "[$($unvalidatedFamilies.Count)] uncaptured families cannot be ordered without a captured anchor"
            return $null
        }
    }

    return @{ Mappings = $mapping; ForeignFamilies = $foreignFamilies }
}

# Single-pass replacement so overlapping old/new index sets cannot chain-rewrite each other.
function Update-GameCoreIdsInText {
    param([string]$Text, [hashtable]$FamilyMappings, [ref]$ReplacementCount)
    $evaluator = {
        param($match)
        $familyName = $match.Groups[2].Value
        if ($FamilyMappings.ContainsKey($familyName)) {
            $familyMap = $FamilyMappings[$familyName]
            $oldIndex = [int]$match.Groups[1].Value
            if ($familyMap.ContainsKey($oldIndex)) {
                $ReplacementCount.Value++
                return "GameCore.[$($familyMap[$oldIndex])]$familyName"
            }
        }
        return $match.Value
    }
    return [regex]::Replace($Text, $GameCoreIdPattern, $evaluator)
}

$results = [System.Collections.Generic.List[object]]::new()
$dataFiles = @(Get-ChildItem -LiteralPath $DataRoot -Filter "*.json" -File | Sort-Object Name)
foreach ($dataFile in $dataFiles) {
    $environmentName = $dataFile.BaseName
    $scenePath = Join-Path $SceneRoot "$environmentName.unity"
    $readData = Read-TextPreservingBom $dataFile.FullName
    $oldFamilies = Get-GameCoreFamiliesFromText $readData.Text
    if ($oldFamilies.Count -eq 0) {
        continue
    }

    $capturedFamilies = Get-CapturedFamilies $environmentName
    if ($null -eq $capturedFamilies -and -not (Test-Path -LiteralPath (Join-Path $CaptureRoot "$environmentName`_BehaviorLights.json") -PathType Leaf)) {
        $results.Add([pscustomobject]@{
            Environment = $environmentName; Status = "Skipped"
            Detail = "no captures for game version [$GameVersion]"
        })
        continue
    }

    $mappingResult = New-FamilyIndexMapping $oldFamilies $capturedFamilies
    if ($null -eq $mappingResult) {
        $results.Add([pscustomobject]@{
            Environment = $environmentName; Status = "Skipped"
            Detail = $Script:LastMappingError
        })
        continue
    }

    $familySummaries = [System.Collections.Generic.List[string]]::new()
    $changedAny = $false
    foreach ($familyName in ($mappingResult.Mappings.Keys | Sort-Object)) {
        $familyMap = $mappingResult.Mappings[$familyName]
        $oldList = @($familyMap.Keys | Sort-Object)
        $newList = @($familyMap.Values | Sort-Object)
        $captured = $null -ne $capturedFamilies -and $capturedFamilies.ContainsKey($familyName)
        $capturedSuffix = ""
        if (-not $captured) {
            $capturedSuffix = " (inferred)"
        }
        $familySummaries.Add("$($familyName): $($oldList[0])..$($oldList[-1]) -> $($newList[0])..$($newList[-1])$capturedSuffix")
        $oldSignature = $oldList -join ","
        $newSignature = $newList -join ","
        if ($oldSignature -ne $newSignature) {
            $changedAny = $true
        }
    }

    $dataReplacementCount = 0
    $updatedDataText = Update-GameCoreIdsInText $readData.Text $mappingResult.Mappings ([ref]$dataReplacementCount)
    $sceneReplacementCount = 0
    $updatedSceneText = $null
    $sceneRead = $null
    if (Test-Path -LiteralPath $scenePath -PathType Leaf) {
        $sceneRead = Read-TextPreservingBom $scenePath
        $sceneFamilies = Get-GameCoreFamiliesFromText $sceneRead.Text
        $sceneRingsMatch = $true
        foreach ($familyName in ($mappingResult.Mappings.Keys)) {
            if (-not $sceneFamilies.ContainsKey($familyName) -or (Compare-Object @($sceneFamilies[$familyName]) @($oldFamilies[$familyName]))) {
                $sceneRingsMatch = $false
                break
            }
        }
        if (-not $sceneRingsMatch) {
            $results.Add([pscustomobject]@{
                Environment = $environmentName; Status = "Skipped"
                Detail = "scene [$environmentName.unity] ring families do not match the data export"
            })
            continue
        }

        $updatedSceneText = Update-GameCoreIdsInText $sceneRead.Text $mappingResult.Mappings ([ref]$sceneReplacementCount)
    }

    if (-not $WhatIf) {
        if ($updatedDataText -ne $readData.Text) {
            Write-TextPreservingBom $dataFile.FullName $updatedDataText $readData.HasBom
        }
        if ($null -ne $updatedSceneText -and $sceneRead -and $updatedSceneText -ne $sceneRead.Text) {
            Write-TextPreservingBom $scenePath $updatedSceneText $sceneRead.HasBom
        }
    }

    $foreignNote = ""
    if ($mappingResult.ForeignFamilies.Count -gt 0) {
        $foreignNote = " (left $($mappingResult.ForeignFamilies -join ', ') unchanged: not TrackLaneRing objects)"
    }
    $results.Add([pscustomobject]@{
        Environment = $environmentName
        Status = if ($WhatIf) { "WouldUpdate" } elseif ($changedAny) { "Updated" } else { "AlreadyCurrent" }
        Detail = "$($familySummaries -join '; ')$foreignNote; data replacements [$dataReplacementCount], scene replacements [$sceneReplacementCount]"
    })
}

$results | Format-Table -AutoSize -Wrap
$skipped = @($results | Where-Object { $_.Status -eq "Skipped" -and $_.Detail -notlike "no ring families present*" })
if ($skipped.Count -gt 0) {
    Write-Warning "[$($skipped.Count)] environment(s) were skipped and remain on stale ring indexes; resolve their captures or regenerate them from data."
}
