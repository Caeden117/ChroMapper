![This is ChroMapper.](https://i.imgur.com/fnhMWRe.png)

# This is ChroMapper.
ChroMapper is an in-development, Unity-based map editor for Beat Saber, specializing in modded map creation.

ChroMapper will allow you to create stunning lightshows with Chroma RGB lighting, unique gameplay maps with ChromaToggle notes, powerful JSON editing with a built-in Node Editor, and more!

## Why?
*(Inspiration from [BeatMapper](https://github.com/joshwcomeau/beatmapper))*

Why would you choose ChroMapper over an already existing map editor, or even the official editor? Here's a simple table that lets you compare and contrast what makes each editor unique.

|Feature|ChroMapper|MediocreMapper|EditSaber|BeatMapper|Official Editor|
|-|-|-|-|-|-|
|**Engine/Framework**|Unity|Unreal|Unreal|React (JavaScript)|Unity|
|**Availability**|Standalone|Standalone|Standalone|Web Browser|Bundled w/PCVR Beat Saber|
|**Platform**|Windows|Windows|Windows|Fuckin' Anywhere|Windows|
|**Perspective**|3D|3D|3D|3D/2D|2D|
|**Mapping Extensions**|🗹|🗹|🗷|🗷|🗷|
|**Chroma RGB Lightmapping**|🗹|🗹|🗷|🗷|🗷|
|**ChromaToggle**|🗹|🗷|🗷|🗷|🗷|
|**Beatmap v2 Support**|🗹|🗹|🗷|🗹|🗹|

With that big chart out of the way, here are some reasons why ChroMapper might better appeal to you than the other editors.
- Being built in **Unity** means ChroMapper has the potential to be visually closer to Beat Saber than any other map editor, and [the results do show.](https://streamable.com/a0lh6)
- Being **designed for mods** means you can easily design maps that utilize Chroma RGB lighting, ChromaToggle note types, and improvements brought from Mapping Extensions.
- **Custom Platforms** outside of the base game set means you can preview your map with other platforms without needing to test in Beat Saber, as well as develop maps that utilize Custom Events.
- The **Node Editor** allows you to manually input custom data into map objects, and give you a degree of control that no other editor has. Want to make a map that utilizes a new mod that uses CustomJSONData? ChroMapper has your back!
- **Beatmap v2 Support** means you do not have to run your song through a conversion tool in order to play it in Beat Saber, upload it to Beat Saver, or load it onto your Oculus Quest.
- **Open Source** means that you too can contribute to this ever expanding project.

# Releases

Previously, I guestimated that ChroMapper should enter open beta on October 19th. Obviously, that did not happen. Bugs are still ary, and I'd much rather give out something complete and of quality rather than a broken mess, especially with Open Beta being a first impression for many people. As for when ChroMapper Open Beta will be pushed back to, I really cannot give a date, however do expect it by the end of the year *at the latest.*

Some expectations I'd like to set before hand: ChroMapper is still not *release*. While this may be a viable mapping tool, features one might call a "necessity" might not be implemented yet. Bugs should also be expected, as a team as small as myself and the closed alpha team probably haven't swept through everything.

If you wish to follow ChroMapper development, [here is the Trello board for ChroMapper.](https://trello.com/b/j2ikcHZh/chromapper-development) Here, you can figure out all that has been suggested by the closed alpha testing crew, what we are currently working on, and what has been done.

## Patreon

If you'd like to donate to the project and get some sweet perks, you can [support SkyKiwi and Chroma development on Patreon](https://www.patreon.com/Chroma). The ChromaToggle Sabers tier ($5+) will get you access to ChroMapper closed alpha testing, as well as access to release, pre-release, and hotfix builds earlier than everyone else.

# For Developers
ChroMapper was being developed with Unity 5.6.6f2, however has now been upgraded to Unity **2018.3.14f1**.

This GitHub repository should come with the assets and scripts you need to easily open it up in Unity. Feel free to fork the repository if you plan on contributing to ChroMapper in any way. Even though ChroMapper is in closed alpha, if you are able to clone or fork and get the project working on your end, I'll allow you to use it as long as you're making contributions to ChroMapper.

Also keep in mind of the [GNU GPL v2 license](https://github.com/Caeden117/ChroMapper/blob/master/LICENSE) that I picked for ChroMapper. If you were to make a fork and build off of that (a la MediocreMapper from EditSaber), your source code must be made public, and changes must be stated, while all being under the same license. Pull requests, however, I'll be more lenient on.
