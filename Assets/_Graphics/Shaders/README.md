# Beat Saber Shader Mapping

This document records the current mapping from Beat Saber game shader names to the ChroMapper shader names used by the environment material system.

## Current mapping

| Game shader name | ChroMapper shader name |
| --- | --- |
| `Custom/CustomParticles` | `ChroMapper/Particles` |
| `Custom/Mirror` | `ChroMapper/Mirror` |
| `Custom/OpaqueNeonLight` | `ChroMapper/Parametric Box Opaque` |
| `Custom/Parametric3SliceSprite` | `ChroMapper/Parametric Slice Billboard` |
| `Custom/ParametricBoxFakeGlow` | `ChroMapper/Parametric Box Fake Glow` |
| `Custom/Rain` | None (Shader) |
| `Custom/SetDepthOnly` | None (Shader) |
| `Custom/SimpleLightning` | `ChroMapper/Lightning` |
| `Custom/SimpleLit` | `ChroMapper/Lit` |
| `Custom/SimpleStencil` | None (Shader) |
| `Custom/Spectrogram` | `ChroMapper/Spectrogram` |
| `Custom/TransparentNeonLight` | `ChroMapper/Parametric Box Transparent` |
| `Custom/UnlitSpectrogram` | `ChroMapper/Spectrogram Unlit` |
| `Custom/WaterLit` | `ChroMapper/Water Lit` |

## How to find or modify the mapping

The authoritative mapping is the `Shaders` list in `EnvironmentLibrarySO` in the Unity Editor:

1. Open the ChroMapper project in Unity.
2. Select `Assets/Editor/Environments/EnvironmentLibrarySO.asset`.
3. Inspect the `Shaders` list.
4. Each entry's `name` is the Beat Saber game shader name.
5. Each entry's `shader` reference is the ChroMapper replacement shader.
6. Modify the corresponding entry there when changing a mapping.

The `name` field intentionally preserves the original game shader name for lookup, so it may differ from the actual `Shader "..."` declaration in the ChroMapper shader asset. For example, `Custom/OpaqueNeonLight` maps to the ChroMapper shader asset declared as `ChroMapper/Parametric Box Opaque`.

This list was transcribed from the current Unity Editor mapping shown in the shader configuration.
