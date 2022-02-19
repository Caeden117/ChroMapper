# Build guide

ChroMapper is developed with Unity **2021.1.15f1** in C#.

This GitHub repository comes with the assets and scripts you need to easily open it up in Unity, no dependency bullshit required.

## Development Environment Setup
* Clone the project from GitHub to your local work folder.
* Download and install [Unity Hub](https://unity3d.com/get-unity/download).
* Activate your license within Unity Hub. Most people should be eligible for a free Personal license.
* Install the Unity version (mentioned in the header of this page) from within Unity Hub in the "Installs" section. The project might not be using the latest release; in this case, you should consult the "download archive" (from within Unity Hub). The installer will take care of the required dependencies as well.
* Add the project in the "Projects" section. Select your main folder you cloned from GitHub.
* Open the project. The Unity editor should launch, and the project dependencies should download automatically.

## Running the project
* Select "File" -> "Build and Run" within Unity.
  * It is recommended to always build with Mono; building with IL2CPP will cause issues in areas that utilize [Harmony](https://github.com/pardeike/Harmony) patches, including post processing and input.
* Most errors, including "Missing Project ID" and "Discord RPC error", can be ignored.

## Repository layout
The project uses the master / development branch setup.

* The `master` branch is the latest stable version.
  * In most cases, any commits to this branch will be part of a stable version update.
* The `dev` branch contains the latest, unstable code.
  * This is where most development takes place.
* Additionally, there might be some feature or bugfix branches.

## Contributing
Please follow the [Contributing guidelines](CONTRIBUTING.md) as you are making contributions to ChroMapper.
