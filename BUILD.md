# Build guide

ChroMapper is developed with Unity **6000.3.13f1** in C#.

This GitHub repository comes with the assets and scripts you need to easily open it up in Unity, no dependency bullshit required.

## Development Environment Setup
* Clone the project from GitHub to your local work folder.
* Download and install [Unity Hub](https://unity3d.com/get-unity/download).
* Activate your license within Unity Hub. Most people should be eligible for a free Personal license.
* Use Unity's [build archive](https://unity.com/releases/editor/archive) to locate and install ChroMapper's version of Unity (see above).
* Add the project in the "Projects" section. Select your main folder you cloned from GitHub.
* Open the project. Project dependencies should download automatically.

## Running the project
* Open scene `00 Bootup` from the Project window before running or building.
  * Hitting the **Play button** in Unity on this scene will launch ChroMapper directly in the editor — much faster to iterate than a full build.
* Select "File" -> "Build and Run" within Unity for a standalone build.
  * It is recommended to always build with Mono; building with IL2CPP will cause issues in areas that utilize [Harmony](https://github.com/pardeike/Harmony) patches, including post processing and input.
* Most errors, including "Missing Project ID" and "Discord RPC error", can be ignored.

## Environment Branch Setup

> Make sure scene `00 Bootup` is open in the Unity editor before running the steps below.

1. Extract the environment assets ZIP (usually from Discord) to `_Scenes/Environments/Data`
2. In Unity, run `Environment/Populate Build Data`
3. In Unity, run `Environment/Create All from Data` (this may take a while and may show some errors, which is fine)
4. In Unity, run `Environment/Update Environment List` (registers scenes and color schemes in `EnvironmentListSO`)
5. Never commit scenes and materials until final commit

## Contributing
Please follow the [Contributing guidelines](CONTRIBUTING.md) as you are making contributions to ChroMapper.
