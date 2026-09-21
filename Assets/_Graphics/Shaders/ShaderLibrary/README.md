# ShaderLibrary Layout

This record describes the current ShaderLibrary layout. The linked ownership
records preserve the historical recovered layout and are not a current-state index.

The layout separates core files from common and family files. It keeps shared files distinct from family files.

## Areas

| Area | Path | Contents |
| --- | --- | --- |
| Core | `ShaderLibrary/Core` | `Camera.hlsl`, `Data.hlsl`, `Tonemapping.hlsl`, `Easings.hlsl` |
| Common | `ShaderLibrary/Common` | `Bloom.hlsl`, `Fog.hlsl`, `Lighting.hlsl`, `ObjectShared.hlsl`, `PostProcess.hlsl`, `Reflection.hlsl`, `Time.hlsl` |
| Families | `ShaderLibrary/Families` | `BloomFogComposition.hlsl`, `ParametricShared.hlsl`, `BloomShared.hlsl`, `SpectrogramShared.hlsl` |
| Root hold | `ShaderLibrary` | `Cutout.hlsl`, `Blurs.hlsl` |
| Shader-local implementations | `Assets/_Graphics/Shaders` | 26 implementations with shader-local HLSL inlined |

## Counts

| Measure | Value |
| --- | --- |
| Core files | 4 |
| Common files | 7 |
| Family files | 4 |
| Root hold files | 2 |
| Shared and hold HLSL files | 17 |
| Shader-local implementations | 26 |
| All ShaderLibrary HLSL files | 17 |

## Include ownership

Keep feature order in the consuming shader when order affects the result.

| Include file | Owner | Responsibility |
| --- | --- | --- |
| `Core/Camera.hlsl` | Core | Stereo-aware camera position and screen coordinates |
| `Core/Data.hlsl` | Core | Pipeline data types and surface defaults |
| `Core/Tonemapping.hlsl` | Core | ACES tone mapping |
| `Core/Easings.hlsl` | Core | Gradient easing curves selected by shader easing ID |
| `Common/Bloom.hlsl` | Common | White boost and bloom composition |
| `Common/Fog.hlsl` | Common | Distance fog, height fog, and color fog |
| `Common/Lighting.hlsl` | Common | Shared diffuse, specular, and falloff lighting |
| `Common/ObjectShared.hlsl` | Common | Shared object-rotation transforms |
| `Common/PostProcess.hlsl` | Common | Blue-noise dither and screen-position helpers |
| `Common/Reflection.hlsl` | Common | Reflection-probe decoding, sampling, and projection |
| `Common/Time.hlsl` | Common | Standard, song, and frozen time vectors |
| `Families/BloomFogComposition.hlsl` | BloomFog family | Prepass sampling and bloom-fog composition |
| `Families/ParametricShared.hlsl` | Parametric family | Height, distance, noise, and fade calculations |
| `Families/BloomShared.hlsl` | PostProcess family | Bloom pyramid filters, merge, exposure, and tone mapping |
| `Families/SpectrogramShared.hlsl` | Spectrogram family | Spectrogram-data index helpers |
| `Cutout.hlsl` | Root hold | Cutout helpers with UNVERIFIED provenance |
| `Blurs.hlsl` | Root hold | Legacy blur variants; no static project consumers are known |

## Fog split

The split keeps `ApplyColorFog` in `Common/Fog.hlsl`.

The split places prepass globals and six composition functions in `Families/BloomFogComposition.hlsl`.

The six functions are `SampleBloomPrePass`, `BlendFogColor`, `ApplyBloomFogCalculatedFactor`, `ApplyBloomFog`, `ApplyBloomHeightFogCalculatedFactor`, and `ApplyBloomHeightFog`.

The two prepass globals are `_CustomFogTextureToScreenRatio` and `_BloomPrePassTexture`.

The family file includes the common file. The family file does not include the camera file directly.

The Fog include contract covers 18 consumers, all of which include the family file directly. Four adapters do not call composition functions: ObjectArc, ParametricBoxFakeGlow, ParametricBoxTransparent, and ParametricSliceBillboard. They retain the family include because a historical comparison found that a generic-only include changed 152 successful D3D11 byte arrays. That result is historical audit evidence, not current compiler-fidelity validation.

`Common/Fog.hlsl` has one direct nested consumer and 18 transitive shader consumers.

## Particles section order

The particle implementation is inline in `Assets/_Graphics/Shaders/Particles.shader`. Its preprocessor regions cross section boundaries.

Preserve the preprocessor section order in `Particles.shader` as a source contract.

## Root hold

`Cutout.hlsl` stays at the root path. Its provenance is UNVERIFIED.

`Blurs.hlsl` stays at the root path. No static project consumers are known;
external ownership is unknown, so this does not prove a total of zero consumers.

A future adoption of `Blurs.hlsl` is a new contract. It needs a new review.

## Validation

Historical audit records report 26 of 26 local splits source-operation-order equivalent and 26 shaders with zero ABI-parser warnings. These are static or historical checks; they do not establish compiler fidelity for the flattened layout.

The Phase 6 compile record is historical investigative evidence. It recorded 654 new Particles failures, 152 successful D3D11 byte differences, and 872 macro warnings.

This record claims no compiler fidelity, visual parity, runtime parity, stereo parity, or MPB parity for the flattened layout.

## Ownership record

The full per-module record is historical. Its recovered paths describe the
historical layout, not the current disk state:

- [`Audit/Recovered/RecoveredShaderModuleOwnership.md`](../Audit/Recovered/RecoveredShaderModuleOwnership.md)
- [`Audit/Recovered/RecoveredShaderModuleOwnership.json`](../Audit/Recovered/RecoveredShaderModuleOwnership.json)

The Phase 1-3 audit baseline stays historical. It is here:

- [`Audit/Recovered/README.md`](../Audit/Recovered/README.md)

## Maintenance

1. Import changed project shader assets in Unity.
2. Read shader import and compile messages.
3. If an include file moves, update the ownership record in the same change.
4. If a consumer adopts a new include file, update the ownership record in the same change.
5. Do not merge contracts that the ownership record marks as prohibited.
