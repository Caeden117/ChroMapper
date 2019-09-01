![This is ChroMapper.](https://i.imgur.com/fnhMWRe.png)

# This is ChroMapper.
ChroMapper is an in-development, Unity-based map editor for Beat Saber, specializing in modded map creation (particularly in the Chroma suite of mods).

ChroMapper will allow you to create stunning lightshows with Chroma RGB lighting, unique gameplay maps with ChromaToggle notes, powerful JSON editing with a built-in Node Editor, and more!

## Why?
*(Inspiration from [BeatMapper](https://github.com/joshwcomeau/beatmapper))*

Why would you choose ChroMapper over an already existing map editor, or even the official editor? Here's a simple table that lets you compare and contrast what makes each editor unique.

|Feature|ChroMapper¹|MediocreMapper|EditSaber|BeatMapper|Official Editor|
|-|-|-|-|-|-|
|**Engine/Framework**|Unity|Unreal|Unreal|React (JavaScript)|Unity|
|**Availability**|Standalone|Standalone|Standalone|Web Browser|Bundled w/Beat Saber|
|**Platform**|Windows|Windows|Windows|Fuckin' Anywhere|Bundled w/Beat Saber|
|**Perspective**|3D|3D|3D|3D/2D|2D|
|**Mapping Extensions**|🗹|🗹|🗷|🗷|🗷|
|**Chroma RGB Lightmapping**|🗹|🗹|🗷|🗷|🗷|
|**ChromaToggle**|🗹|🗷|🗷|🗷|🗷|
|**Beatmap v2 Support**|🗹|🗷¹|🗷|🗹|🗹|

**¹**: Available in private builds, but not in any public releases.

With that big chart out of the way, here are some reasons why ChroMapper might better appeal to you than the other editors.
- Being built in **Unity** means ChroMapper has the potential to be visually closer to Beat Saber than any other map editor, and can be ported to a wide range of devices (Given enough requests).
- Being **designed for mods** means you can easily design maps that utilize Chroma RGB lighting, ChromaToggle note types, and improvements brought from Mapping Extensions.
- **Custom Platforms** outside of the base game set means you can preview your map with other platforms without needing to test in Beat Saber, as well as develop maps that utilize Custom Events.
- The **Node Editor** allows you to manually input custom data into map objects, and give you a degree of control that no other editor has. Want to make a map that utilizes a new mod that uses CustomJSONData? ChroMapper has your back!
- **Beatmap v2 Support** means you do not have to run your song through a conversion tool in order to play it in Beat Saber, upload it to Beat Saver, or load it onto your Oculus Quest.
- **Open Source** means that you too can contribute to this ever expanding project.

# Releases
ChroMapper is currently in closed alpha. This means that no public builds are available, and public videos of ChroMapper will be rare, if any. For development purposes, a select group of Chroma lightmappers are in the closed alpha team to hunt bugs and give suggestions. There are a few methods of which you can enter into ChroMapper closed alpha testing:
- DM me on Discord. I'll determine whether or not you can join ChroMapper testing by recommendations from the other fellow closed alpha testers. Unless you are a well known mapper, you most likely will not get in this way.
- [Support SkyKiwi and Chroma development on Patreon.](https://www.patreon.com/Chroma) The Pentachrome Sabers tier ($15+) will give you access to ChroMapper closed alpha testing as a bonus of supporting SkyKiwi, and future development for the Chroma suite.
- Cheat the system, clone the repository, and load it in Unity. This may not garauntee an up to date build as time goes on, but if you're really desparate for a ChroMapper build, here ya go. If you're not developing, I wont help you with any issues, since you *did* circumvent the closed alpha group.

Closed alpha means that not all features are in the mapper yet. While this may be a viable mapping tool, features one might call a "necessity" might not be implemented yet.

If you wish to follow ChroMapper development, [here is the Trello board for ChroMapper.](https://trello.com/b/j2ikcHZh/chromapper-development) Here, you can figure out all that has been suggested by the closed alpha testing crew, what we are currently working on, and what has been done.

Once ChroMapper has reached a development point where most basic features are implemented, I'll consider:
- Switching to Closed Beta, where a larger selection of people will be invited to test ChroMapper, however it wont be like Chroma's previous *"Closed Beta Testing"*.
- Switching to Open Beta, where anyone and everyone can download a ChroMapper build and try it out for themselves.

Don't worry, current or future Patreon supporter! Once ChroMapper reaches the point of Beta and Release, hotfixes and other pre-release builds will still be available for you, maybe even earlier than everyone else!

# For Developers
ChroMapper was being developed with Unity 5.6.6f2, however has now been upgraded to Unity **2018.3.14f1**.

This GitHub repository should come with the assets and scripts you need to easily open it up in Unity. Feel free to fork the repository if you plan on contributing to ChroMapper in any way. Even though ChroMapper is in closed alpha, if you are able to clone or fork and get the project working on your end, I'll allow you to use it as long as you're making contributions to ChroMapper.

Also keep in mind of the [GNU GPL v2 license](https://github.com/Caeden117/ChroMapper/blob/master/LICENSE) that I picked for ChroMapper. If you were to make a fork and build off of that (a la MediocreMapper from EditSaber), your source code must be made public, and changes must be stated, while all being under the same license.

## Contributing
GitHub for Unity seemed to have lost its compatibility with Unity 5. Now that we are on 2018.3, I'll look into getting GitHub for Unity back and seeing if it still works.
