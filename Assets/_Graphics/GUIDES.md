# Graphics asset guide

## Mesh identity and placement

`EnvironmentMeshSO` maps exported mesh hashes to Unity mesh references. These
SHA-256 hashes identify GPU mesh buffers, not FBX file contents. File checksums
cannot substitute for those lookup keys.

Mesh names and bounds alone do not distinguish variants. Source scene references,
vertices, indices, submeshes, UV channels, and vertex colors distinguish meshes
with the same name or dimensions.

- Place a mesh used by one environment in that environment's folder.
- Place meshes shared only by related environments in a suitable family folder.
- Place meshes shared across unrelated environments in `Models/Environments`.
- Move the corresponding `.meta` file with each asset.

## Mesh format and coordinates

Binary FBX is suitable when conversion preserves the mesh data and its imported
Unity references. Native `.asset` meshes remain necessary when conversion cannot
preserve custom bounds or existing references safely.

Environment renderers assign `MeshFilter.sharedMesh` and particle meshes directly.
They do not instantiate the FBX GameObject hierarchy. Scale or rotation on an
FBX model node cannot correct a directly referenced mesh.

The converted environment FBXs use these coordinate rules:

| Property | Required representation |
| --- | --- |
| File format | Binary FBX, with the `Kaydara FBX Binary` header |
| Units | Meters, represented by `UnitScaleFactor = 100` |
| Model scale | Identity: `(1, 1, 1)` |
| Vertex magnitudes | Source mesh units, without a hidden model-node scale |
| Handedness | X-reflected positions and directional vectors, with reversed polygon winding, to compensate for Unity's import conversion |

`UnitScaleFactor` expresses centimeters per file unit. A value of 100 represents
meters. Source-sized coordinates in a centimeter file become 100 times too small
after unit conversion, regardless of a compensating model-node scale.

For these files, Unity reflects X during mesh import. The FBX conversion applies
the inverse reflection to positions, normals, tangents, and binormals. Polygon
winding also reverses. UV and color indices follow the same corner permutation.
Edge references preserve their connectivity and per-polygon materials keep their order.

A 180-degree Y rotation does not correct an X reflection because it also reverses
Z. Symmetric bounds can hide this error. Asymmetric vertices and bounds centers
expose the difference.

### Mesh preservation checks

1. Compare the actual Unity mesh coordinates and bounds with the source mesh.
2. Compare asymmetric vertices to detect axis or sign errors.
3. Preserve every submesh, polygon, UV channel, and vertex-color value.
4. Preserve UV and color associations when polygon winding changes.
5. Preserve tangent direction and handedness when normals or coordinates change.
6. Retain custom bounds instead of replacing them with geometric bounds.
7. Verify the GUID and local fileID of each imported mesh reference.

Normals can be recalculated when the intended surface permits it. Custom bounds
can exceed the geometric bounds to accommodate displacement or animation.
An exporter round trip alone does not prove correct Unity mesh coordinates,
color values, or subasset IDs.

## Textures, sprites, and metadata

Texture imports retain the source image data and relevant importer settings.
These settings include color space, filtering, wrapping, mipmaps, compression,
and sprite definitions. A matching image does not imply matching importer settings.

Native sprite assets also reference textures. Their texture GUIDs must resolve
to project assets, not to GUIDs that exist only in the source export.

1. Preserve the destination asset GUID when replacing its contents.
2. Copy the relevant source importer settings.
3. Preserve referenced sprite fileIDs and sprite definitions.
4. Remap native sprite texture references to destination texture GUIDs.
5. Exclude source `assetBundleName`, `assetBundleVariant`, `timeCreated`, and `licenseType` metadata.
6. Verify that asset references resolve without duplicate GUIDs.

AssetBundle assignments describe project packaging, not visual appearance.
Source packaging metadata does not belong in the destination import settings.

## Shaders and materials

ChroMapper uses render-target alpha in its bloom pipeline. Alpha is not a
universal transparency control. Each shader defines how texture alpha, material
alpha, and output alpha contribute to color, coverage, and bloom.

Transparent particle routes can premultiply RGB by their calculated alpha before
color blending. Their blend state can preserve destination alpha independently.
Dithered opaque routes use different coverage rules.

Material keywords select shader features. Numeric properties alone do not
activate keyword-controlled branches. Shader names, supported keywords, texture
transforms, blend factors, depth state, and render queues form one material contract.

The [shader documentation](Shaders/README.md) describes the mappings and shared
shader behavior.

## Render queues

| Typical queue | Purpose |
| --- | --- |
| `2000` | Opaque geometry, including applicable dithered materials |
| `3000` | Transparent geometry |
| Greater than `3000` | Later transparent draws when the material requires that order |

These values are conventions, not replacements for material-specific queues.
Queue order interacts with depth writes, depth tests, blend factors, and sorting.
Assigning every transparent material the same queue does not guarantee correct results.

## Environment generation

The [environment generation guide](../Editor/Environments/README.md) describes
library population, environment metadata, and scene creation. Imported assets
and library references must agree before scene generation.
