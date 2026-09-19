# Third-party asset notice

Beat Saber and its assets belong to their respective rights holders.
ChroMapper is not affiliated with or endorsed by Beat Games or Meta.

Attribution does not grant permission to redistribute these assets.
You are responsible for confirming that your use and distribution are permitted.
This notice is not legal advice.

## Graphics documentation

- [Asset ownership notice](BEAT_SABER_ASSETS.md)
- [Mesh, texture, sprite, and material guidance](GUIDES.md)
- [Shader contracts and mappings](Shaders/README.md)
- [Environment generation](../Editor/Environments/README.md).

## Asset layout

| Directory | Contents |
| --- | --- |
| `Models/Environments` | Imported environment meshes, including binary FBX and native mesh assets |
| `Textures/Environments` | Environment textures and sprites |
| `Materials/Environments` | Environment materials |
| `Shaders` | Project shaders and shared shader includes |

Environment-specific meshes use their environment folder. Related environments
can share a family folder. Meshes shared across unrelated environments use the
environment-model root. Asset GUIDs preserve references when files move with
their `.meta` files.
