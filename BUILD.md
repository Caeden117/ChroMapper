# Build guide

ChroMapper is developed with Unity **2021.1.15f1** in C#.

This GitHub repository comes with the assets and scripts you need to easily open it up in Unity. Feel free to fork the repository if you plan on contributing to ChroMapper in any way. Even though ChroMapper is in closed alpha, if you are able to clone or fork and get the project working on your end, I'll allow you to use it as long as you're making contributions to ChroMapper. The setup is straight forward.

## Development Environment Setup
* Clone the project from GitHub to your local work folder
* Download and install [Unity Hub](https://unity3d.com/get-unity/download)
* Activate your license within Unity Hub, most people should be eligible for a free Personal license
* If you want to select your Visual Studio installation location install it upfront, otherwise the installer will put it into the default location
* Install the correct Unity version mentioned in the header of this page from within Unity Hub in the "Installs" section. The project might not be using the latest release. In this case you should consult the "download archive" (from within Unity Hub). The installer will take care of the required dependencies as well.
* Add the project in the "Projects" section. Select your main folder you cloned from GitHub.

## Running the project
* Select "File" -> "Build and Run" within Unity
* Warning "Missing Project ID" will appear. Ignore it and select "Yes". 
* You can ignore the "Discord RPC error"

## Repository layout

The project uses the master / development branch setup.

* The `master` branch is the latest stable branch (as far you can call it stable within an alpha)
* The `dev` branch contains the latest code. 
* Additionally, there might be some feature or bugfix branches.

## Pull requests
If you want to raise a pull request please target the `dev` branch.
