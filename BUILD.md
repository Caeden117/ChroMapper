# Build guide

ChroMapper is developed with Unity **2021.3.45f1** in C#.

This GitHub repository comes with the assets and scripts you need to easily open it up in Unity, no dependency bullshit required.

## Development Environment Setup
* Clone the project from GitHub to your local work folder.
* Download and install [Unity Hub](https://unity3d.com/get-unity/download).
* Activate your license within Unity Hub. Most people should be eligible for a free Personal license.
* Use Unity's [build archive](https://unity.com/releases/editor/archive) to locate and install ChroMapper's version of Unity (see above).
* Add the project in the "Projects" section. Select your main folder you cloned from GitHub.
* Open the project. Project dependencies should download automatically.

## Running the project
* Select "File" -> "Build and Run" within Unity.
  * It is recommended to always build with Mono; building with IL2CPP will cause issues in areas that utilize [Harmony](https://github.com/pardeike/Harmony) patches, including post processing and input.
* Most errors, including "Missing Project ID" and "Discord RPC error", can be ignored.

## Contributing
Please follow the [Contributing guidelines](CONTRIBUTING.md) as you are making contributions to ChroMapper.
