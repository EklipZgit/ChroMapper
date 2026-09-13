#Requires -Version 7.0
param(
    [string]$DecompilationRoot = 'C:\src\BeatSaberStuff\BeatSaberDecompilation\1.44.1',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\Assets\_Graphics\Textures\GLS Event Icons')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The OE sprites are split between two standalone textures and one shared UI atlas, so resolve both sources explicitly.
$assetRoot = Join-Path $DecompilationRoot 'SerializedAssets\AssetRipperOutput\ExportedProject\Assets'
$textureRoot = Join-Path $assetRoot 'Texture2D'
$atlasPath = Join-Path $textureRoot 'sactx-0-1024x1024-BC7-BeatmapEditorUI-1fbc81e1.png'
$instantPath = Join-Path $textureRoot 'LightingOnIcon.png'
$transitionPath = Join-Path $textureRoot 'LightingIcon.png'

# Fail before writing partial output when the serialized export does not contain the 1.44.1 marker sources.
foreach ($requiredPath in @($atlasPath, $instantPath, $transitionPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required OE icon source was not found: $requiredPath"
    }
}

# System.Drawing preserves the exported texture pixels while converting Unity's bottom-left sprite rectangles to image coordinates.
Add-Type -AssemblyName System.Drawing.Common
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Copy-Item -LiteralPath $instantPath -Destination (Join-Path $OutputDirectory 'Instant.png') -Force
Copy-Item -LiteralPath $transitionPath -Destination (Join-Path $OutputDirectory 'Transition.png') -Force

# GeneratedRotationIconsUsePerfectMirroredArcs gives direction filenames to their mathematical generator; this extractor keeps only OE references.
$sprites = @(
    [pscustomobject]@{ Name = 'EaseIn'; X = 521; Y = 776; Width = 102; Height = 102 },
    [pscustomobject]@{ Name = 'EaseOut'; X = 719; Y = 579; Width = 102; Height = 102 },
    [pscustomobject]@{ Name = 'EaseInOut'; X = 719; Y = 473; Width = 102; Height = 102 }
)

# Crop each referenced sprite without resampling so this repeatable dump remains an authoritative visual reference.
$atlas = [System.Drawing.Bitmap]::new($atlasPath)
try {
    foreach ($sprite in $sprites) {
        $rectangle = [System.Drawing.Rectangle]::new(
            $sprite.X,
            $atlas.Height - $sprite.Y - $sprite.Height,
            $sprite.Width,
            $sprite.Height)
        $crop = $atlas.Clone($rectangle, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $destination = Join-Path $OutputDirectory "$($sprite.Name).png"
            $crop.Save($destination, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $crop.Dispose()
        }
    }
}
finally {
    $atlas.Dispose()
}

# Record provenance beside the dump so later clean remakes can be audited against the exact game/export version.
$manifest = [ordered]@{
    gameVersion = '1.44.1'
    source = 'Beat Saber Official Editor serialized assets exported with AssetRipper'
    serializedAssetExport = 'SerializedAssets/AssetRipperOutput/ExportedProject/Assets'
    instantTexture = 'Texture2D/LightingOnIcon.png'
    transitionTexture = 'Texture2D/LightingIcon.png'
    atlasTexture = 'Texture2D/sactx-0-1024x1024-BC7-BeatmapEditorUI-1fbc81e1.png'
    atlasSprites = $sprites
    # GeneratedRotationIconsUsePerfectMirroredArcs documents why direction sprites are intentionally absent from OE extraction.
    generatedDirectionManifest = 'rotation-direction-source-manifest.json'
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'oe-source-manifest.json') -Encoding utf8
