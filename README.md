![This is ChroMapper.](https://i.imgur.com/fnhMWRe.png)

# This is ChroMapper.
ChroMapper is an in-development, Unity-based map editor for Beat Saber, specializing in modded map creation (particularly in the Chroma suite of mods).

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
- Being built in **Unity** means ChroMapper has the potential to be visually closer to Beat Saber than any other map editor, and [the results do show.](https://youtu.be/ybLWma2IMyA?t=50)
- Being **designed for mods** means you can easily design maps that utilize Chroma RGB lighting, ChromaToggle note types, and improvements brought from Mapping Extensions.
- **Custom Platforms** outside of the base game set means you can preview your map with other platforms without needing to test in Beat Saber, as well as develop maps that utilize Custom Events.
- The **Node Editor** allows you to manually input custom data into map objects, and give you a degree of control that no other editor has. Want to make a map that utilizes a new mod that uses CustomJSONData? ChroMapper has your back!
- **Beatmap v2 Support** means you do not have to run your song through a conversion tool in order to play it in Beat Saber, upload it to Beat Saver, or load it onto your Oculus Quest.
- **Open Source** means that you too can contribute to this ever expanding project.

# Releases
ChroMapper is currently in closed alpha. This means that no public builds are available, and public videos of ChroMapper will be rare, if any. For development purposes, a select group of Chroma lightmappers are in the closed alpha team to hunt bugs and give suggestions. This is *not* like the "Closed" Beta Testing of old Chroma, I will only add people based on recommendations from the other closed alpha testers.

Closed alpha means that not all features are in the mapper yet. While this may be a viable mapping tool, features one might call a "necessity" might not be implemented yet.

If you wish to follow ChroMapper development, [here is the Trello board for ChroMapper.](https://trello.com/b/j2ikcHZh/chromapper-development) Here, you can figure out all that has been suggested by the closed alpha testing crew, what we are currently working on, and what has been done.

Once ChroMapper has reached a development point where most basic features are implemented, I'll consider:
- Switching to Closed Beta, where a larger selection of people will be invited to test ChroMapper, similar to Chroma's *"Closed Beta Testing"*.
- Switching to Open Beta, where anyone and everyone can download a ChroMapper build and try it out for themselves.

## Patreon

Remember how I said I would only add new members recommended by the closed alpha testers? I lied, there is one exception to that rule.

You can [support SkyKiwi and Chroma development on Patreon](https://www.patreon.com/Chroma). The Pentachrome Sabers tier ($15+) will get you access to ChroMapper closed alpha testing, as well as access to release, pre-release, and hotfix builds earlier than everyone else.

# For Developers
ChroMapper was being developed with Unity 5.6.6f2, however has now been upgraded to Unity **2018.3.14f1**.

This GitHub repository should come with the assets and scripts you need to easily open it up in Unity. Feel free to fork the repository if you plan on contributing to ChroMapper in any way. Even though ChroMapper is in closed alpha, if you are able to clone or fork and get the project working on your end, I'll allow you to use it as long as you're making contributions to ChroMapper.

Also keep in mind of the [GNU GPL v2 license](https://github.com/Caeden117/ChroMapper/blob/master/LICENSE) that I picked for ChroMapper. If you were to make a fork and build off of that (a la MediocreMapper from EditSaber), your source code must be made public, and changes must be stated, while all being under the same license. Pull requests, however, I'll be more lenient on.

## Format Changes
ChroMapper saves time using a weird way. It is easily visible by opening an object using the Node Editor, where an event might look like this in JSON:

```json
{
  "_time": "40.25",
  "type": 0,
  "value": 3
}```

Of course, time shouldn't really be stored a string. What's going on here? The reason is to circumvent what is known as "Pixelguy Syndrome", where everything in your map is reset to Beat 0 after saving. Why? [.NET Cultures](https://docs.microsoft.com/en-us/dotnet/api/system.single.tostring?view=netframework-4.8#System_Single_ToString_System_IFormatProvider_).

Some countries use a comma (`,`) instead of a period (`.`) to separate whole from decimals when writing numbers. In .NET, and by extension ChroMapper, this carries when `_time` is converted from a decimal number into JSON:

```json
{
  "_time": 40,25,
  "type": 0,
  "value": 3
}```

And this is where Pixelguy Syndrome occurs. There is no other way to go about this, even with the JSON library ChroMapper uses.

### What does this mean?

*ChroMapper*, *BeatMapper*, and *Beat Saber* still read just fine. **However, EditSaber and related forks such as MediocreMapper and Edit Saber Enhanced do not.** This will mean you'd get the same "Pixelguy Syndrome" effect in these editors.
