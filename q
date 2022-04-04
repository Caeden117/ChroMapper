[33mcommit 25be0096fd86aa3c239c5ae260a7bd6b3742fadc[m[33m ([m[1;36mHEAD -> [m[1;32mdebug[m[33m)[m
Author: CCDV2 <denghaoyuan1100@126.com>
Date:   Sun Mar 13 23:56:17 2022 +0800

    add basic(yet bug) 3.0 support for CM

[33mcommit 4d2b9c9c3b5572e8d35b0f954937719902dc2907[m[33m ([m[1;31morigin/dev[m[33m, [m[1;32mdev[m[33m)[m
Merge: 175f19c6 e5431f5c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Mar 6 21:29:03 2022 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 175f19c6a64dfdeea744771aab7622525836ccbf[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Mar 6 21:28:56 2022 -0800

    Fix OkIgnore preset throwing NullReferenceException

[33mcommit e5431f5c5abaea17dda022326a910c659ebd193d[m
Author: Bullet <68104413+XAce1337manX@users.noreply.github.com>
Date:   Fri Mar 4 11:38:13 2022 +1000

    Shortened fade and flash duration (#379)

[33mcommit 72990f6a0e987eb600982699d068e91006878e8d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Feb 26 22:48:04 2022 -0800

    experimental counters+ optimization

[33mcommit 5eef3bb5ab39bb8e82f5d5ca31e2dc79a9ddba5b[m
Author: SuperSmay <74476397+SuperSmay@users.noreply.github.com>
Date:   Sat Feb 26 16:29:57 2022 -0600

    Add more loading messages (#378)
    
    * Add more loading messages
    
    * Yeet the extra

[33mcommit a733889b60c3a902da1f90eb6c1c2d2f0b54a6de[m
Author: Bullet <68104413+XAce1337manX@users.noreply.github.com>
Date:   Sun Feb 27 08:29:18 2022 +1000

    Don't yeet lightId from env enhancements (#377)
    
    now people can stop complaining about it

[33mcommit 53bb01872ba6a8c41f7e493903c4d59d6d0c164d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 22 20:15:13 2022 -0800

    *ANOTHER* attempted fix for deleting while playing

[33mcommit e583c32c28bfe28b5ce40de0758a27b09a80696e[m
Merge: 0c211587 f125296c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 22 14:07:44 2022 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 0c211587aa49d891e932fe60b8c87f7332221d7f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 22 14:06:09 2022 -0800

    CMUI dialog box and slider fixes

[33mcommit f125296c7e45e687cb350fc4ac9647c430f0cd45[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Feb 17 22:50:02 2022 +0100

    add render scale setting (#373)

[33mcommit 539dc49f58368e892dbd9ad3b18e04db038cb0e5[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Feb 17 22:48:26 2022 +0100

    update max jump duration in HJD calculation (#374)
    
    In a recent game update this was changed from 36 to 35.998, presumably to combat the 36 JD bug

[33mcommit b00a4955ef46ca7dc03a18574cb7dd543040bd5b[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Feb 17 22:48:04 2022 +0100

    fix errors introduced to atsc.CurrentBeat when changing editor scale (#375)
    
    This fixes an annoying bug I've been chasing for a while where for seemingly no reason I was placing notes slightly off-grid
    EditorScaleController calls atsc.MoveToTimeInSeconds, changing this to MoveToTimeInBeats avoids introducing floating point errors

[33mcommit 2cafe87c9d2217236b8b4c1da656715d1c638267[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Feb 12 18:05:56 2022 -0800

    quick small fix

[33mcommit 33a9db3e375cd4a059d916a9aac87329bba983ea[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Feb 12 18:01:17 2022 -0800

    Fix various problems with CMUI slider

[33mcommit 851c91a5c7811c521e47b230a2c0b48eb07c9667[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Feb 12 17:30:00 2022 -0800

    potential fix for crashing bug

[33mcommit 748abace24586edb6596ceeee25dca10b929629a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Feb 12 17:29:48 2022 -0800

    small optimization

[33mcommit 88c07679305879ebf6a6a3b38412d4b565cc53cd[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Feb 10 01:54:55 2022 -0800

    CM developer out to kill plugins 🦀

[33mcommit 30dbe0d67dd51e8e97ffce54b10b38d2eb5ddbd9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 9 21:38:46 2022 -0800

    whoops i did another optimization

[33mcommit b7c083e65afc6c6bc368a167a4c1e316c208eb1b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 9 21:38:18 2022 -0800

    Add null check (fixes #371)

[33mcommit d776834e4f75c5600da62c5f89374de1e5b55f99[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 9 13:32:24 2022 -0800

    bring back bts

[33mcommit 4624e9c35a5734cf6c57d7038f023e0190b21585[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 8 13:10:49 2022 -0800

    obstacle hitboxes were doubled for some reason

[33mcommit 8f7a83501f379d6ecf5ba4d826664f135c17a947[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 6 16:53:24 2022 -0800

    Localization update

[33mcommit 795115393a1fb265ee6b77f7616941ce62b0e208[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 6 16:53:18 2022 -0800

    New bookmark dialog remade with CMUI

[33mcommit 80b7a656837e3e4ac3c1f2189596b540ea3d6ce8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 6 16:32:22 2022 -0800

    news flash! untested change fails test!

[33mcommit 180f6cba0b79c77f54c2945d05e3b12e177bca16[m
Merge: 3eef4c83 71018e5e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 6 14:30:57 2022 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 3eef4c832bdc574f3e7f49841ba76afb605ee900[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 6 14:10:55 2022 -0800

    Add NotoSans CJK SC font for Simplified Chinese

[33mcommit e8996a202a73760f21a50e4012c2bf406ce0f097[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 4 22:07:25 2022 -0800

    Add additional APIs to text box component

[33mcommit 7b75fe189a6a5280c194d04027883228a1e2f421[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Feb 3 20:49:43 2022 -0800

    haha! get around needing new localization

[33mcommit c0aedf6c87abce4209ad83cb28f532eac01c55fb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Feb 3 20:40:43 2022 -0800

    forgor to revert map directory too

[33mcommit f95f6b8cee44621f0367db056c4f605657bfe616[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Feb 3 10:14:14 2022 -0800

    force component store to always load

[33mcommit 2e952857a49c7544182e78d25f6c51aef9df3d8c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Feb 3 10:13:56 2022 -0800

    try/catch autosaving

[33mcommit 437625d2ec9ce447689b612693466a8accbd0545[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 2 18:50:41 2022 -0800

    Once more, with feeling

[33mcommit d7a2cb0e88d251812f01ac86f4394dcfd21b643e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 2 18:50:30 2022 -0800

    Update localizations, add chinese

[33mcommit a8cc9a248b35b578df91b3f34d71ba37e4f0fa91[m[33m ([m[1;31morigin/new-dialog-boxes[m[33m)[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 2 16:27:03 2022 -0800

    whoops i broke caching in nested color picker

[33mcommit a0788ba71d4c68fc3fae17057a6bfb4e8f4c567c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 2 16:14:30 2022 -0800

    Begrudgingly write docs in PLUGINS.md

[33mcommit 89948dea13a96bee9fb204b39ddc7216c55860a2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 2 15:28:03 2022 -0800

    Final API tweaks and documentation updates

[33mcommit c0810ad1546f41335b6a11e546a5403a03d71445[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 1 23:39:12 2022 -0800

    Nested color picker component

[33mcommit adba4c586fabdada81a614a60ea88fb7909715e5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 1 22:44:43 2022 -0800

    QOL change

[33mcommit 2e774c9dd5027800591289d8a59218682f6e24c6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 1 19:52:57 2022 -0800

    Bring DialogBox to CMUI folder

[33mcommit 3636d777a66cc51d4a9f7c520d02482caa9d1c89[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 1 19:48:03 2022 -0800

    Add non-generic API methods

[33mcommit b59f044052b01bd8421d2587d27ba0135c0ca2a2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 1 18:27:50 2022 -0800

    Small QOL change to CMUI

[33mcommit 5b771eb695381020cf1039de7f73e3509c1cd195[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 31 13:45:41 2022 -0800

    holy shit i fixed that nasty bug in CMUI

[33mcommit 098484b0528b123bc9ebdc8318f106b93afaff61[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 19 14:13:27 2022 -0800

    ...because of color picker

[33mcommit dc0b4e9e28e7c45deff2dee7849b017e3486d3ce[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 20:20:00 2022 -0800

    UNFORTUNATELY I CANT HAVE A FUCKING ASSEMBLY DEFINITION

[33mcommit 643002473077c6a2386335cba54eefa2259450b8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 14:27:02 2022 -0800

    desaturate imported colors, retrofit input box

[33mcommit f84e13a7cc83f9df3c46f347430d47924e7f91df[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 12:46:22 2022 -0800

    fix layout problems

[33mcommit 3f2ce45b6f9d3ba4897acde388f6f494037d6505[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 12:24:31 2022 -0800

    yeah i forgot to explicitly open the box

[33mcommit 41eabca94089ecdbaa6f6dd7b4bf0a7b0dd056ea[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 12:23:52 2022 -0800

    HOLY SHIT retrofitted onto old dialog box API

[33mcommit 09136abb183991c0338f1f55ba7e7ede68616c9d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 11:47:59 2022 -0800

    Dialog Box prefab

[33mcommit 65662530a6d980d36f044e7fe515404b0b7b5139[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 11:47:53 2022 -0800

    Restructure to prevent cyclic dependencies

[33mcommit 8f5ab03c3cc86df7ccc0fd6cb6b99d8861a00f59[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 11:32:57 2022 -0800

    first iteration of new Dialog Box

[33mcommit 1a64f36741b856c4ed1b25ae2862e1eb5979f8ab[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 18 11:32:46 2022 -0800

    Expose Refresh as a public API

[33mcommit 54798a1861b5a7b28a027d5d897cf5d4a8d48282[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 17 19:17:15 2022 -0800

    enbiggen dropdown

[33mcommit defccd2f755c0240bddae568322ab107b6073bf8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 17 13:50:21 2022 -0800

    Increase size of Progress Bar and Text Box

[33mcommit 2edf3032dbdc81868b427f9be091ef26337b87f0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 17 13:44:11 2022 -0800

    fuck

[33mcommit 3f76cac7873c02379ca2baf35324f8f2a34f8ea4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 17 13:39:45 2022 -0800

    idk why but the scene didnt save lmao

[33mcommit 6dfdcc8d60ed28af4c4a2e2176eb83b48800af3c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jan 16 22:59:28 2022 -0800

    add localized text method as extension

[33mcommit bb4cd094c26c3e49f063113decc8414172911300[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jan 16 22:55:23 2022 -0800

    Add button component

[33mcommit b441e57dabf36109327c4bffdd3223f541894570[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jan 16 22:34:55 2022 -0800

    simplify progress bar/text

[33mcommit 74d2bf97595ff48bd24f2125d0ebe57b4b446a0c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jan 16 22:34:48 2022 -0800

    add dropdown component

[33mcommit 0f3cb41e089e6b41dd211125067d0088c389f9fc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jan 16 15:27:25 2022 -0800

    add 5 CMUI components

[33mcommit 6167dd31f7cb2ce1b417e0a8e9b71d88bae5c2ee[m
Merge: b55239c5 8ebadbcc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 12 18:19:12 2022 -0800

    merge dev (i hope nothing fucked up)

[33mcommit b55239c5179b188e54da4594c226552b77079db2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 12 18:17:01 2022 -0800

    rename to CMUI

[33mcommit 71018e5e77150ac969c2980221095c5638dcd615[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Tue Dec 21 01:06:55 2021 +0100

    fix difficulty label not showing immediately on copy (#366)
    
    * fix difficulty label not showing immediately on copy
    
    * append "(Copy)" to difficulty label when copied

[33mcommit edef06ea6513c1b24e652cfeb975fc6f3dfa9fff[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Dec 21 00:06:47 2021 +0000

    Fix song cache issues (#367)

[33mcommit a142c8f71be16ccdc3c5d441750a2c2c9757f8e1[m
Author: Bullet <68104413+XAce1337manX@users.noreply.github.com>
Date:   Mon Dec 20 11:48:30 2021 +1000

    Prevent 2.0 gradients on lightID (#368)
    
    oops

[33mcommit 8ebadbcc8b068536a62314ef0ae1d980c5314882[m
Merge: 273dd366 678ce2ce
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Dec 12 15:34:15 2021 -0800

    Merge branch 'song-offset-things' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit 273dd366b484d6194f4c87d729b3921fa1fbe8f6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Dec 12 15:31:08 2021 -0800

    Managed to compress Hangul like a gamer

[33mcommit d4151942958db617bf24a3c3c1dee44c437af5c1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Dec 12 14:27:38 2021 -0800

    Add Korean support, Hangul font asset

[33mcommit 8f5e10c3df9ad2843858719c791cab6847d16e05[m
Merge: ec3cb145 6e49284f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Dec 12 12:54:22 2021 -0800

    Merge branch 'playback-speed-latency-comp-fix' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit ec3cb145c8fde6b73d05d2c9074141cf7fd800cd[m
Merge: 18b56ec4 8a0795e7
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Dec 12 12:54:14 2021 -0800

    Merge branch 'lightID-fades' of https://github.com/XAce1337manX/ChroMapper into dev

[33mcommit 18b56ec4d61a9ce08332a0e0e2899c0fd71d0f85[m
Merge: f9842484 fc624f0e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Dec 12 12:54:02 2021 -0800

    Merge branch 'playing-mode-improvements' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit 6e49284f5694e397957594b554f2b9cedfc43a9a[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Dec 11 17:24:16 2021 +0100

    account for playback speed when using audio latency compensation
    
    This still isn't ideal behavior since the grid visibly jumps around in time, but it doesn't seem to actually cause things to break.
    Ideally you would apply playback speed changes only after some time has passed (equal to the amount of compensation) but that would be a lot more difficult to implement.

[33mcommit 678ce2ce6dc42019a9b6e3949cf01ac974bc2dff[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Dec 10 18:44:52 2021 +0100

    remove unused song time offset warning strings

[33mcommit d0168aa05bd37336c139a728391404ad258b19ee[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Dec 10 18:34:08 2021 +0100

    fix exception on save when song time offset input field is empty

[33mcommit 80df5f1c5c8cf65470d3f795c14d3bd6831a9a8d[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Dec 10 18:19:59 2021 +0100

    fix exception when setting song time offset larger than song length

[33mcommit 438a1752cb23dcaf97051ca43d2c377dc5638895[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Dec 10 18:09:19 2021 +0100

    create a new AudioClip when applying song time offset
    
    Apparently there is no way to change the length of an AudioClip once it's been created, even when setting the sample data.
    As a workaround simply create a new clip with the correct length.

[33mcommit c9c38c79c6dc10d66ab88c9da1448a115a8aa317[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Dec 10 17:27:51 2021 +0100

    only apply song time offset when loading the mapper scene
    
    In game, the song preview is not affected by the song time offset value.
    Since the only use of having it apply in the song info screen is for the song preview, and because the song is always reloaded when switching scenes anyway, this seemed like the simplest solution.

[33mcommit 37312d5bcaf28f1405631a425476cbf62756ca38[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Dec 10 16:59:43 2021 +0100

    don't reload song when finished editing song time offset
    
    The song is already reloaded when the map info gets saved, and this really doesn't serve any purpose.

[33mcommit fada797e6141739b74ea24fcb43a96eeb2ff65d8[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Dec 10 16:54:09 2021 +0100

    remove song time offset warnings since it's no longer broken in game

[33mcommit 8a0795e7d49ff80254cb873b7fa62da2098bc2ad[m
Author: Bullet <vincentn@live.com.au>
Date:   Wed Dec 8 21:40:15 2021 +1000

    Allow flash/fade/2.0 gradients on light id again
    
    Functions in game as of Chroma v2.5.7

[33mcommit fc624f0e344d1a5e262ad374680f2066f1769bbe[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Tue Nov 30 17:17:59 2021 +0100

    save and restore cursor position when locked/unlocked
    
    disabled on macOS due to Unity bugs

[33mcommit f9842484702a789e1a14a9f591763c966e0431e5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Dec 2 22:03:35 2021 -0800

    disable light renderer when invisible

[33mcommit 17a110b2259941fc7093b794085597e2656fe519[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Dec 2 04:09:31 2021 +0100

    fix input fields in dialog boxes sometimes not being selected properly (#360)
    
    This issue occurs when repeatedly placing bookmarks. The input box doesn't get selected even though the method is called.
    I'm assuming this happens because the same game object is selected twice in a row, resulting in the events not being sent properly.
    A working solution seems to be to set the selected game object to null first.

[33mcommit 851ec8bbbc9e319fb667704b66d70fce30f8d1b3[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Tue Nov 30 20:34:11 2021 +0100

    don't try to lock cursor in preview ui mode

[33mcommit 08ac448d26ea58ec3e6615634845413fe9d9b65a[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sun Nov 28 21:53:29 2021 +0100

    also hide bpm change blocks in playing/preview modes

[33mcommit 0d5f971b0cb11ff64d39b5d0f491e3dbc96b8978[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sun Nov 28 21:08:50 2021 +0100

    shorten ui mode selector transition times
    
    also reworks the code to make them more consistent

[33mcommit 09f061a0e3a6fffd6b4813ad981f1ff7f34b5336[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 23:33:28 2021 +0100

    change SpawnParameterHelper into static class

[33mcommit e48de9e5cd081e3a69c8cc01d1d1feb1a567e723[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 23:05:32 2021 +0100

    also show song timeline when paused in preview ui mode

[33mcommit 68d6151b91f9bc5479d9501a44ecabc64b2a7ba9[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 23:01:49 2021 +0100

    tweak camera z position in playing ui mode from -7 to -6
    
    -7 felt just a little too far away imo

[33mcommit 43511aefd711e99c3791fb71b978e1ab4d5e16dc[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 22:45:13 2021 +0100

    scale obstacle fade radius during playback with spawn offset

[33mcommit c2ab08f0fc05fff7dbfb041b2cf4ab45ec18eaef[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 21:48:09 2021 +0100

    disable note surface grid line when the placement grid is hidden

[33mcommit 7ccea8a077f831b86af18fc8f075ab96d874ff40[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 19:01:36 2021 +0100

    greatly speed up ui fade transitions
    
    changes the default transition time from 1 second to 0.2 seconds

[33mcommit 6c4e87207c236fe02d72b0dd1d1424ecc71b1ef6[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 18:40:58 2021 +0100

    show song timeline and cursor when paused in playing ui mode

[33mcommit de42baa73415dc3c6e065aba47ba5ea0c5059855[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Nov 27 17:29:21 2021 +0100

    move half jump duration calculation to helper class, and fix spawn offset in playing ui mode

[33mcommit 519b50acb6bd92782c736f8d99ac1821f1a188ab[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Nov 26 23:03:37 2021 +0100

    save and restore camera position when toggling playing ui mode

[33mcommit 8bba24cde02b6d7c1c27dea0619ab8960f3312ea[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Nov 26 19:42:22 2021 +0100

    fix crouch button in playing ui mode
    
    apparently this was broken since the playing mode was introduced

[33mcommit 9e53d673f4be617355278e2ec9f2e3b4d739fad5[m[33m ([m[1;33mtag: 0.8.454[m[33m)[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 21 16:35:16 2021 -0800

    fix _step jank

[33mcommit b97dd5edf790fc0a904505533835e71fdb46e3a9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 21 13:31:47 2021 -0800

    take prop mode into account for last step gradient event

[33mcommit c94d895bfd6bf8450e273b94a3d872ba4c5839e4[m
Merge: 54942a69 2fa1ddd4
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 19 16:48:04 2021 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 54942a69ed7df2a95eaf7857ff594ee55c569306[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 19 16:47:44 2021 -0800

    Misc. fixes from livestream

[33mcommit 2fa1ddd4d1452651bcdac8642b4785ff7931b47c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 13 20:20:16 2021 +0000

    Fix options search (#358)
    
    smh

[33mcommit a2e6c375ed8ab3668b088b346c3ef1c9e2c22540[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 11 14:12:06 2021 -0800

    Wall duration tweaking uses BPM changes (fixes #357)

[33mcommit 4c2e5a2606efa6f9186147c3a29dae8df4489cde[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 11 11:43:01 2021 -0800

    add discord assets for new environments

[33mcommit 439fc2a9836baeaf9fa4b553709c7959713e3bdf[m
Merge: 9cbf356f 8ea9992f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 5 12:25:27 2021 -0700

    Merge branch 'linux-fixes' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit 9cbf356f73a398dd140803185fd820760a894bc5[m
Merge: d294ba75 94a99f26
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 5 12:25:18 2021 -0700

    Merge branch 'spooky' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 94a99f26548327f13cb8242b28c609d1f9b807aa[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Nov 2 23:38:15 2021 +0000

    Billie and Spooky Environments

[33mcommit 8ea9992f518d3bda48adb77c62f8e2f236547b96[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 28 16:49:21 2021 +0200

    add moving to trash capability on linux
    
    uses the 'gio trash' command, provided by glib2
    falls back on regular delete if the command is not available

[33mcommit d294ba75f8273be04927ada4a715634ba3e83f8c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Oct 26 22:02:19 2021 -0700

    Pull Crowdin localization; remove obstacle outline entries

[33mcommit 1f972931d4ca021ea7f1ba970c1b0e6ca9ae996f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Oct 26 21:58:47 2021 -0700

    Optimize obstacle outlines; related setting removed

[33mcommit d25a062c3d1a34f34f868a804028119bb03fe6a4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Oct 26 21:57:49 2021 -0700

    Disabled editorconfig style that does not compile with Unity Mono

[33mcommit 7830a4e236f7077bf3f75bdf5515281a7b9fd8a9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Oct 26 12:23:43 2021 -0700

    fix unsupported environments breaking CM discord RPC

[33mcommit e11e2e0d5db2d5e6da748198084bbaa0718b5ecc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 24 20:10:26 2021 -0700

    idk why notosans keeps updating

[33mcommit 0fc88b1b2735017af17ee077dc7bf7e32e06da45[m
Merge: e4d47748 a1ac3382
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 24 20:09:54 2021 -0700

    Merge branch 'fix-playback-sync' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit e4d47748ee64b0fc0fad1e48eaf1fe5e47cf4534[m
Merge: 753e35f5 fed835ec
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 24 20:09:49 2021 -0700

    Merge branch 'linux-fixes' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit a1ac3382d79ac06860610650d7d6e52b7e4e7d25[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sun Oct 24 21:37:30 2021 +0200

    fix playback sync when using audio latency compensation setting
    
    this eliminates constant stutters that would previously occur due to a wrong comparison in the sync logic

[33mcommit fed835ec1f97231c6d5bf3dcd2284ff86bcd621b[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 14 18:03:53 2021 +0200

    slightly rework opening song folder and add support for linux

[33mcommit b944f3a7d3cf4d429cda30de1ec50bee32c8c12d[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 14 16:52:44 2021 +0200

    update gitignore for linux-specific unity files
    
    similar entries already exist for windows

[33mcommit 0aeb6370c48b6ad05bdbffa6483be6682a7751af[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 14 16:43:18 2021 +0200

    fix deleting of directories for non-Windows or macOS systems
    
    still doesn't send things to trash on linux but at least deleting maps works now

[33mcommit a5ad944d6a839cfa22c750b37db79537d0c70c3b[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 14 16:19:05 2021 +0200

    fix temp loader directory path on linux

[33mcommit d239a63b2fe9eabaf876a55ea1a0ec1eb4574f4e[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 14 16:01:56 2021 +0200

    rename info.dat to Info.dat when loaded

[33mcommit 78cf56257af0896608045562a474c38c5261c043[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 14 15:40:37 2021 +0200

    fix create zip for case sensitive file systems

[33mcommit 738915fa87a000957b7ae56f6ea76c26d8dbe283[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 14 15:35:52 2021 +0200

    fix editorconfig warnings hidden by preprocessor directives

[33mcommit 753e35f501a09d0dc57ed7108c5fa4d4f9eb7482[m
Author: Bullet <vincentn@live.com.au>
Date:   Wed Oct 13 21:06:55 2021 +1000

    RGB Lighting (ChromaLite) option affects blocks on grid

[33mcommit 6699ca4501d95bad9bbabb3db3243896a922506e[m
Merge: c26b7d7d 0632d09d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 9 14:24:29 2021 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit c26b7d7d706f5d931974c8584850934376a1472c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 9 14:24:08 2021 -0700

    Update crowdin languages (especially JA, FR, and RU)

[33mcommit c23fb38dea864def312185bdf350696e1f029353[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 9 14:18:50 2021 -0700

    start dialog box system

[33mcommit 0632d09d1a681da8622986e4f3199505747061d3[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Oct 7 17:56:52 2021 +0200

    change Half Jump Duration limit from 1 to 0.25 (#348)
    
    This reflects the recent change in Beat Saber 1.18.1

[33mcommit 2f526783ac38b8c0e5b9d124595ffa55a9de62bb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Oct 4 00:21:32 2021 -0700

    clarified README regarding x64 official builds

[33mcommit 292b45195271823c97e0198512fbdf6c24f9b96a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 1 15:27:04 2021 -0700

    enable vsync by default

[33mcommit 69e1a940165fc28370f025c4e30fa8411800c512[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 1 12:22:23 2021 -0700

    slight tweaks

[33mcommit bb6838848382c7d725031f0f93a08570734c5593[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 1 12:16:21 2021 -0700

    README update

[33mcommit b4b743f830fa028dc60c655bf7972496454e7add[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 1 12:16:18 2021 -0700

    zyxi cat update

[33mcommit 07d64c127f485fef01349cc79e84f41d6668ede3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 1 12:12:29 2021 -0700

    patreon update

[33mcommit 2dd66a3759d2c73aed9c112fe2721efb49a3df01[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 1 12:12:21 2021 -0700

    crowdin localization update

[33mcommit 33a9ffb62e9d1b6dd5c1b4aaf1c14ede830f117e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 1 00:16:43 2021 -0700

    bongo cat 2: electric boogaloo

[33mcommit 3d146cdfd810f5a897ca7e9d74dc63ddca4974a7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 30 18:57:47 2021 -0700

    enable node editor by default

[33mcommit 05bb03295dd6faf24e71ee6569b79d9d409e5301[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 30 11:49:54 2021 -0700

    change default settings

[33mcommit 974382056d71179fbb2f8bf933a153ac70b1e89b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 30 11:49:48 2021 -0700

    strobe generator passes default to disabled

[33mcommit e299db71ec72712abb5666a0e96cb6c719cc8130[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 30 11:49:37 2021 -0700

    fix wrong smart variable name

[33mcommit 4a1425216678543f559d517cea87efb5310c7123[m
Merge: c97d388b 8172c3ce
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 29 13:15:40 2021 -0700

    Merge branch 'open-beta' into dev

[33mcommit 8172c3cee579290169880f846f092562e1020d9a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 29 13:15:01 2021 -0700

    update/add .md files

[33mcommit c97d388b71abbcd95132d0c6b895d70420d22879[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 29 10:55:35 2021 -0700

    fix jank with spawning/deleting objects while playing

[33mcommit adeff323dffd5a8dc6a2248dd0cc1edac3e1243a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 29 09:27:35 2021 -0700

    lmao you couldnt exit cm

[33mcommit b8d2444d9042486e9ce785130d44b75ad0eb6fbb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 28 14:29:58 2021 -0700

    Bump minor version

[33mcommit 0a0770ecf29ee02f5b1174298a2294e4beffea00[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 28 14:29:22 2021 -0700

    Simplify / make discord controller not garbage

[33mcommit 7beacf7318b227e5598f9465fd713de4b954b1b3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 28 09:55:01 2021 -0700

    fix njs not being applied

[33mcommit a2383fbdae0d52c97f0f739b9691a7809a05ebd0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 26 23:28:46 2021 -0700

    fix bpm tapper

[33mcommit 35931c1e36fd800326263b1adc5306129a7c5ca2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 26 23:19:32 2021 -0700

    fix broken counters+ smart variable names

[33mcommit ac1605d270c59a758519f0583d313d6d6b038878[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 25 15:39:12 2021 -0700

    haha fix tests

[33mcommit 75d9a1942d7e92a34ba372d33995bc3710bf5bba[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 25 14:53:21 2021 -0700

    Fix mirror with NE positions, now mirrors cut direction

[33mcommit 7ede349d49f1c531a37663f076e88d95350ae684[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 25 14:36:28 2021 -0700

    Fix more monoscript/class name mismatches

[33mcommit 8f634360efc3a59944569deff3f362bc6215b12b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 25 13:01:16 2021 -0700

    used wrong method to set bloom

[33mcommit 8ee97afe9f2a7925758a1b8436dfe5f6d248c0b7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 25 11:32:18 2021 -0700

    fix brokey bpm change placement

[33mcommit 2956479e403f8b1b3c9e78ec849e18e536e2ed80[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 24 22:49:59 2021 -0700

    Update VS/VSC Editor packages

[33mcommit 371acea621b682806fb7de9e77f35362ddb1b916[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 24 21:21:25 2021 -0700

    Add .editorconfig, and project refactor (#333)

[33mcommit 0c0ab7a54e67cf1bbd8632baf8d30c03d883f752[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 24 19:17:00 2021 -0700

    Enabled High Quality Filtering for High Quality Bloom

[33mcommit 05e153cd2afd0d88e52e4b20330a8e4d70a31c11[m
Merge: cc31eb7a 4bd0d28d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 24 19:12:53 2021 -0700

    Merge branch 'dev-console' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit cc31eb7ad4b37d2b625aa44d7b54c905f389de34[m
Merge: c74f3c31 c9ee6e7a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 24 19:12:47 2021 -0700

    Merge branch 'bookmark-rand' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit c74f3c3107dac3d2579cc7913f0bae25355bdf1e[m
Merge: 4af1ca7d a4091b74
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 24 19:12:39 2021 -0700

    Merge branch 'yeet-jaggies' of https://github.com/XAce1337manX/ChroMapper into dev

[33mcommit 4bd0d28d562d812500a7bf071591c5ed279c5d59[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Sep 23 14:13:47 2021 +0100

    Fix windows not opening to log folder

[33mcommit c9ee6e7a9317d344985841205aadf940f13ec9d1[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Sep 23 12:51:22 2021 +0100

    Fix bookmark colour random

[33mcommit 4af1ca7da1097e072bba8b1cd6292cc3f16114fa[m
Merge: 84afa700 89853fa2
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 22 16:30:13 2021 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 84afa700d834800793c1e2fdbb0ba3632d0f51f8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 22 16:30:04 2021 -0700

    whoops forgot to assign this in the inspector

[33mcommit a4091b74f07a51bd908bbddf115cc0a9156b2802[m
Author: Bullet <vincentn@live.com.au>
Date:   Tue Sep 21 04:08:10 2021 +1000

    Not update AA every frame lmao

[33mcommit dba049cd46b9aadb85329edc65b0c3c967da8dbf[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 20 17:13:25 2021 -0700

    update crowdin

[33mcommit 095bcc2e73be974443cbc3dcb7cfd04fb7fb9ec0[m
Author: Bullet <vincentn@live.com.au>
Date:   Tue Sep 21 00:26:38 2021 +1000

    Anti-Aliasing Options

[33mcommit 89853fa2588c72bf69f96a968f1efe03384fb515[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Sep 20 17:38:50 2021 +0100

    Fix NullReferenceException when searching options (#342)

[33mcommit 534c17075dc0d1b6c3ceb47a518b31212cc6a99c[m
Author: SuperSmay <74476397+SuperSmay@users.noreply.github.com>
Date:   Sun Sep 19 12:48:47 2021 -0500

    Fix bookmark color error on beatmap save (#341)

[33mcommit 0fb6f8007cac461cd14594fd05341d381f2c09d7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 19 10:47:57 2021 -0700

    (Hopefully) fix CMJS menu being beeg

[33mcommit e30f185c77d66f1144375873563c3828e1bd107e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 21:29:50 2021 -0700

    Tweak event models to use enums more

[33mcommit a3e21ebf71d168a6acf0c6fb4f8a897d243642e3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 21:29:41 2021 -0700

    Settings supports Enums

[33mcommit f3cf71e07d7c1089caf4449bed3f3bc8817384b8[m
Merge: 934600c7 a57a7506
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 21:17:04 2021 -0700

    holy shit unityyamlmerge can actually be useful

[33mcommit 934600c7d11d954091fa7386439cfa5ce063b2dd[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 17:40:54 2021 -0700

    Force default console to commit die

[33mcommit 0b10040642cb99de4648b717e2f55dc7bc0c8621[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 17:40:43 2021 -0700

    Re-enable Development builds (for clean stack traces)

[33mcommit ee3b899c38ed4b8a4c5501aa2deb38f7cd1d2818[m
Merge: 81f7c84a c280def5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 17:17:04 2021 -0700

    ooo wee i sure do love merging Unity files

[33mcommit 81f7c84ad71adec5500cff825cf53bd64c3f1165[m
Merge: c5a101c1 6fc15194
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 14:48:27 2021 -0700

    Merge branch 'dev-console' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit c5a101c12ea5e734b04065b488d9d302ae4d6d3c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 18 14:46:27 2021 -0700

    casually do a whole editor ui refresh in one commit

[33mcommit 6fc15194c8d7f018a687135d978f2d601b2ea72e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Sep 14 13:33:25 2021 +0100

    PR comments

[33mcommit c280def5e854f7ab1615548d688da2b8057934b5[m
Author: Smay <samaskling@gmail.com>
Date:   Mon Sep 13 21:00:43 2021 -0500

    Remove more unused stuff I forgot about

[33mcommit 3b130e867c56ea7ec00af5caa3f5330126489f76[m
Merge: 3b75e89a 516b9c93
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 13 18:42:41 2021 -0700

    Merge branch 'osx-plugins' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 3b75e89ad855fcb3815248325b6989455610396c[m
Merge: 0c3d6b85 2b33df4d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 13 11:54:54 2021 -0700

    Merge branch 'preview-camera-changes' of https://github.com/SuperSmay/ChroMapper into dev

[33mcommit 0c3d6b85e3e7c83ba169b3c253e88a37e816894d[m
Merge: 9f6ca1f2 b84f0753
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 13 11:52:34 2021 -0700

    Merge remote-tracking branch 'origin/issue-templates' into dev

[33mcommit c25b089578702f399b30ac60a09054d154c09605[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Sep 11 19:05:10 2021 +0100

    Rewrite dev console

[33mcommit 516b9c932308352b1eef85e0d9e72f269969dd6f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Sep 11 19:18:55 2021 +0100

    Set native libraries to Any CPU on OSX

[33mcommit 11bb29549faee8ed14c57ef7f7fbe7ee90e1f93f[m
Author: Smay <samaskling@gmail.com>
Date:   Mon Sep 6 20:05:31 2021 -0500

    Create new keybind for bookmark color modifier

[33mcommit 5da62763b298fb59d15bdfa43c2affbf5faebf60[m
Author: Smay <samaskling@gmail.com>
Date:   Mon Sep 6 19:55:37 2021 -0500

    Use CMInput.IUtilsActions for shift keybind

[33mcommit a57a75069a9f8f1a903fa3dd69b4ced34a416d78[m
Author: Smay <samaskling@gmail.com>
Date:   Mon Sep 6 17:00:26 2021 -0500

    Change system for getting material properties

[33mcommit 72706da7ce43ddf5708f50d24a7fcb6967f14364[m
Author: Smay <samaskling@gmail.com>
Date:   Mon Sep 6 15:56:16 2021 -0500

    Use Color? for ColorInputBox and use JSON color helpers

[33mcommit 1dd0f3bfedd28ef2342ac5f3462fcb946a488f9f[m
Author: Smay <samaskling@gmail.com>
Date:   Mon Sep 6 03:01:59 2021 -0500

    Fix new event model, add dropdown

[33mcommit b84f0753fa68d96466631361f3acb6f358b5856b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 5 18:17:27 2021 -0700

    Improve bug report template

[33mcommit 783af3f672314c730f259fed947f8e767d92266c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 5 12:18:08 2021 -0700

    Add pull request template

[33mcommit a07ace2e3edde8f53f0b2b74b5bbdbe7ba8ba805[m
Author: Smay <samaskling@gmail.com>
Date:   Sun Sep 5 01:27:48 2021 -0500

    Clean up color dialog and fix shift click to edit color

[33mcommit 9f6ca1f24b7c40ea256a01c211beb0aca66fb6d3[m
Merge: 0d008c70 6f00e997
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 4 18:45:02 2021 -0700

    Merge branch 'Top-Cat-dev' into dev

[33mcommit 6f00e997bf139194505c696b164bab7e844fa2d3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 4 18:44:38 2021 -0700

    Add missing moving light components on right side

[33mcommit 4d2f395c4821796c5a93f4e614f03cd304cb5fe5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 4 18:34:33 2021 -0700

    add skrillex discord RPC asset

[33mcommit c7200352fad7a08b0d90bfc1c9914e3f7c721a58[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Sep 5 00:19:36 2021 +0100

    Skrillex platform

[33mcommit da2dc6bd68e131e507ca51768ffb8b6a7b3f0674[m
Author: Smay <samaskling@gmail.com>
Date:   Thu Sep 2 22:13:59 2021 -0500

    Added basic color selection popup

[33mcommit 14055787ed6597b3bf94816e672fd0ed8524734a[m
Author: Smay <samaskling@gmail.com>
Date:   Thu Sep 2 18:34:26 2021 -0500

    Colors based on chroma selected color

[33mcommit 04c3c9ed07c20d55f5fbcbca0e0f73f228d338f0[m
Author: Smay <samaskling@gmail.com>
Date:   Wed Sep 1 23:59:27 2021 -0500

    make bookmarks save and load color

[33mcommit c087b08a025ebc9d1077358eadaa83335b3afb6d[m
Author: Smay <samaskling@gmail.com>
Date:   Wed Sep 1 21:49:57 2021 -0500

    Add visualization of event alpha

[33mcommit 2b33df4db7d60a8abba8acf69ccc4294989a5f70[m
Author: Smay <samaskling@gmail.com>
Date:   Tue Aug 31 02:45:58 2021 -0500

    Improve "preview" and "playing" camera modes

[33mcommit 721efd17742f0c3f1baf6cb48fdfd665969d5c6f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 26 20:54:59 2021 -0700

    Issue Templates
    
    Nothing fancy here, just issue templates for Bug Reports and Feature Requests in preparation for a public release.

[33mcommit 0d008c70571442752ce59b00d653ad9142787170[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 26 20:32:10 2021 -0700

    Update DiscordGameSDK

[33mcommit 35fa7588289bc37ee325c8115f2ab1f6a5cb9092[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 26 12:25:40 2021 -0700

    simplify UpdateZPosition usage (bullet dont kill me)

[33mcommit 925a5363b288e2b3fafd217bb69d751c2e815855[m
Merge: f7854126 deafd8ee
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 26 12:17:21 2021 -0700

    Merge branch 'z-laser' of https://github.com/XAce1337manX/ChroMapper into dev

[33mcommit f7854126906dd3ca373c23f27567aaca853a94d5[m
Merge: e4d3ed68 dd850d87
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 26 12:17:17 2021 -0700

    small merge conflict

[33mcommit e4d3ed6804c21f73deec552d7786940e970a8017[m
Merge: 63adc3c0 78b42bc5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 26 12:16:05 2021 -0700

    Merge branch 'editor-scale-keybinds' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit 63adc3c08fc7d149849187b3a1c264da9a915f61[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 25 23:10:39 2021 -0700

    Rename and optimize old method

[33mcommit 2c131aaa4dd0b364172adaf4afd5a5e3f5ad14b2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 25 23:06:04 2021 -0700

    Turns out I cant skip this thing I thought I could skip

[33mcommit c2c70392a8438dc60e83a86ac3c3d652c1a843f6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 25 23:03:17 2021 -0700

    Tweak values to work with OpenGL/Vulkan/Metal

[33mcommit d1d35b57b4a43fa8e6638847b914fa7e24569399[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 25 23:02:57 2021 -0700

    Be efficient with BPM Changes and grid shader

[33mcommit 78b42bc5d3115106f190898c0e36616fae0dc9ec[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Aug 20 15:21:40 2021 +0200

    add editor scale keybinds

[33mcommit dd850d87542a598b87045b559b297e666abfb552[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Aug 16 20:49:39 2021 +0200

    make accurate editor scale setting apply without reloading map

[33mcommit a59950e3424c915d5985b666fe01cf235851a5bb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 16 11:40:02 2021 -0700

    *sigh*

[33mcommit 0557534914321741e606dbd7927d9d8efde6f71e[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Aug 16 20:23:49 2021 +0200

    add setting for bpm independent editor scale

[33mcommit 137b424f5bdc1da8cb81aef4014bc866f1fd4a2c[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Aug 16 19:01:51 2021 +0200

    make beatmap object translucent culling use proper editor scale

[33mcommit deafd8ee511e4d2ffe2fdf3a72b66c311d7b5926[m
Author: Bullet <vincentn@live.com.au>
Date:   Mon Aug 16 22:15:42 2021 +1000

    zPosition Magic Numbers
    
    Very magic. Makes lasers align after a certain number of laser speed events.

[33mcommit 067412f62ded4241659cd9cdbe2616582950ab08[m
Author: Bullet <vincentn@live.com.au>
Date:   Mon Aug 16 22:14:43 2021 +1000

    Lights more accurate for environments that offset using zPosition
    
    (Affects Timbaland FitBeat and BTS)
    I'm sure this can be improved

[33mcommit a053f9da2378ff0ad1baa003988ca5c3491e28d9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 15 15:35:51 2021 -0700

    make that shit work with laser speed interpolation too

[33mcommit c58be7a7b0418ede8595e006519ed5cd2fc9fc75[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 15 15:33:53 2021 -0700

    add _speed alias for precise laser speeds

[33mcommit baadb8a2868362e09c8222cd324147f66a7af3d2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 15 14:40:26 2021 -0700

    add chroma precision ring zoom step

[33mcommit 9dc95e418628d3ae4576ae5731af3946f46085a4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 15 14:37:30 2021 -0700

    fix map customData not refreshing with full reload

[33mcommit 9022b1fe87420fa46f9bedd5f019359338d2ed07[m
Merge: 5764604a c444e659
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 15 12:58:03 2021 -0700

    enable various forms of timeline input during playback (#326)

[33mcommit 5764604ae71bc1b08366f811a5b0fcd99ff34201[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sun Aug 15 21:57:49 2021 +0200

    remove old song offset code (#327)

[33mcommit b5ec175da2ac57189dcf78d977bf26a2db08af77[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sun Aug 15 21:57:37 2021 +0200

    fix accumulating floating point errors on track z-position (#328)

[33mcommit 914670ab15326ff523385077073dfaf7546c3b4e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Aug 14 14:23:00 2021 -0700

    forgot to apply vsync/max FPS on startup

[33mcommit c444e659aad261dbd635ebbe9283b04732ca9ecd[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Tue Aug 10 23:03:26 2021 +0200

    enable bookmark navigation during playback

[33mcommit 88fe32fcdaadffb888799ba2be7cc5af16135683[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Tue Aug 10 22:45:15 2021 +0200

    enable bookmark clicking during playback

[33mcommit 3032ecde7b3af2d4b1106f61653e49f5491cf651[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Tue Aug 10 22:11:20 2021 +0200

    enable timeline dragging during playback

[33mcommit fd14610ccaa6b0efecd36ae33c9ad9c1ca0067d4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 10 10:04:07 2021 -0700

    you're welcome, galaxymaster

[33mcommit aa00ebbaeded5cefe862041fdfe6af419358a861[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 20:14:42 2021 -0700

    Disable VSync, add Maximum FPS setting

[33mcommit 622db2564477170895f65f201f4ab8c0ccaa70d5[m
Merge: a08d75aa aa95ba42
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 18:09:37 2021 -0700

    Merge branch 'SuperSmay-transparent-grid' into dev

[33mcommit aa95ba42d539c6175099b57b369fee7c99625e80[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 18:09:21 2021 -0700

    Add new option to SearchableSection, new keyword

[33mcommit 82932daca3422c950b97dc8f6fad345cb5f9419a[m
Merge: a08d75aa 2fec3e13
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 18:07:54 2021 -0700

    painful merging

[33mcommit a08d75aa7152ffbc84b09f5f2812ed0bb866cca7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 17:53:16 2021 -0700

    hey song time offset is now not half assed

[33mcommit e278f735529ad702857e64fdb336deeea6b58633[m
Merge: dd67d22a c22b1712
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 15:10:35 2021 -0700

    Merge branch 'GalaxyMaster2-audio-latency-compensation' into dev

[33mcommit c22b17123f492c7e82e8826c7ba76f8124b3659a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 15:07:23 2021 -0700

    be not dumb with Counters+

[33mcommit 274ecc5929b166ad1116e3aa7b3819be927c6fa1[m
Merge: dd67d22a af1b985f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 15:06:39 2021 -0700

    fuck unityyamlmerge

[33mcommit dd67d22a059a9ed4cbb628083e42375f47e69555[m
Merge: e7e14c7d e580bce6
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 14:10:47 2021 -0700

    Merge branch 'GalaxyMaster2-countersplus-expand' into dev

[33mcommit e580bce677cdd16880e7880c14af8c48478f989b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 14:10:22 2021 -0700

    Light Counters+ rewrite

[33mcommit e4b8b91b3ace4b3b17578041a6886dda55937828[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 14:10:08 2021 -0700

    Fix Options menu, add more search terms

[33mcommit 11d83acbad188e998d4f2c8429dc232f077fc94f[m
Merge: e7e14c7d cadde31c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 9 12:59:07 2021 -0700

    Merge branch 'countersplus-expand' of https://github.com/GalaxyMaster2/ChroMapper into GalaxyMaster2-countersplus-expand

[33mcommit cadde31c64dd1e4e03ac84b452a2fef7e4f8cba5[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Aug 9 21:05:54 2021 +0200

    add bomb counter to Counters+

[33mcommit 7d529d3a1158b8879695cd87a681ebb1d851bc43[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Aug 9 20:38:30 2021 +0200

    don't count bombs as notes in Counters+

[33mcommit e7e14c7db4950a400ace6b029820364d4a3fa671[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 8 22:06:03 2021 -0700

    Android build settings, use Dx12 & Graphics Jobs on Windows

[33mcommit da542460cfd9e0919bf64d9a157dac2375b6162f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 8 22:05:13 2021 -0700

    Fix low-RGB, high-alpha lights being bright as fuck

[33mcommit 72169016912316113a7d5e89b52dc70c55be28e7[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Aug 5 14:38:01 2021 +0200

    make counters+ counters individually togglable

[33mcommit 96004e2a912662fbe55575ad445d9bc5ee513270[m
Merge: 970a40d6 17bfad75
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 4 11:49:57 2021 -0700

    Merge branch 'panels-fix' of https://github.com/SuperSmay/ChroMapper into dev

[33mcommit 970a40d6a52950a095e3999c81a5303add919f3c[m
Merge: dea91b21 f9fce5e6
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 4 11:49:40 2021 -0700

    Merge branch 'rocket-changes' of https://github.com/SuperSmay/ChroMapper into dev

[33mcommit dea91b2112523ff1fea2ca1fba0aba5a37da4aca[m
Merge: e235cac9 33d9fc61
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 4 11:48:58 2021 -0700

    Merge branch 'dev' of https://github.com/XAce1337manX/ChroMapper into dev

[33mcommit e235cac9498da17bf0e5f5a9104556a9426d17d7[m
Merge: e79730f5 9f38baec
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 4 11:48:45 2021 -0700

    Merge branch 'hitsound-mute-keybind' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit e79730f54a764a141db710869917ac3687dfa43e[m
Merge: 538c9740 787c9f3d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 4 11:48:23 2021 -0700

    Merge branch 'ring' of https://github.com/XAce1337manX/ChroMapper into dev

[33mcommit 538c9740afb3584d0f054223988459a81150beae[m
Merge: b09962ba 13aadac7
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 4 11:47:50 2021 -0700

    Merge branch 'placement-bug-fix' of https://github.com/Atlas-Rhythm/ChroMapper into dev

[33mcommit b09962ba0e4c43e64009f01e6adaba5394dabe13[m
Author: Bullet <vincentn@live.com.au>
Date:   Wed Aug 4 11:19:08 2021 +1000

    Rotation display no longer blocks clicks

[33mcommit 13aadac7fa6de175e3905c5b8c37436b2824a98b[m
Author: dcljr <dcljr@me.com>
Date:   Tue Aug 3 17:15:26 2021 -0700

    Check if instantiatedContainer is active before placing

[33mcommit 581fa26f5fb365f286998fd9eb3d04ddec7ef668[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 2 11:38:15 2021 -0700

    prevent SendMessage on initial value set

[33mcommit 2eba01e70b20b1eb45def48ccd43245b76fc3756[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 2 11:23:25 2021 -0700

    forgot about volume slider

[33mcommit d59f8c57a0e25b3e8372a4b5be5052ee6d9039a3[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Aug 2 02:13:11 2021 +0200

    move counters+ canvas up slightly

[33mcommit 14faecdf5ac02660729edb75c70ceade43f23917[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 31 16:43:58 2021 -0700

    Set initial value without notifying

[33mcommit 7f8f538aa86a6b6f9f57172f74d7c15e7e799b04[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sun Aug 1 01:19:21 2021 +0200

    add red/blue ratio and current bpm to counters+

[33mcommit 9f38baec9d2838d2ba689b24cef1a7b44e6760ee[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Jul 30 14:37:52 2021 +0200

    fix unwanted hitsound muting

[33mcommit 787c9f3dd8698821cdf18ce8409ef5b8e7a18d86[m
Author: Bullet <vincentn@live.com.au>
Date:   Fri Jul 30 19:44:57 2021 +1000

    Shrink Ring Lights

[33mcommit 2fec3e130345a14e1acfe787112a785b166cbe26[m
Author: Smay <samaskling@gmail.com>
Date:   Thu Jul 29 02:16:27 2021 -0600

    Add transparent track slider

[33mcommit f9fce5e61db59bc5ff1c7dad3d13f675be47f252[m
Author: Smay <samaskling@gmail.com>
Date:   Tue Jul 27 23:37:04 2021 -0700

    Update Rocket Env

[33mcommit af1b985fa01a9941780ac699258a2bc99590f713[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Jul 29 21:13:01 2021 +0200

    avoid unnecessary StopCoroutine call

[33mcommit 33d9fc61e337502fc17ac2f02a0f404be8ce2619[m
Author: Bullet <vincentn@live.com.au>
Date:   Fri Jul 30 02:42:13 2021 +1000

    Song Time Offset warning works for negatives
    
    (Also tooltip fix)

[33mcommit fa9c2d80bebf371db8d0157534187580f407d0c1[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Thu Jul 29 11:56:07 2021 +0200

    add audio latency compensation slider

[33mcommit 2e5b011095865f287da8a9b8762765acb85e57f0[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Wed Jul 28 19:51:38 2021 +0200

    implement audio latency compensation

[33mcommit 17bfad753527e3687e5c9b9ac76160c60070e85c[m
Author: Smay <samaskling@gmail.com>
Date:   Tue Jul 27 16:57:51 2021 -0700

    Make panel and triangle rings work in _nameFilter

[33mcommit 9a96f4ce4d424339b5070b378de2fc16de65b748[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 26 15:44:26 2021 -0700

    forgot to actually commit the merged file lmao

[33mcommit 1e64b8cf46fbb55cdbda860913b8b18eb299498b[m
Merge: 889bf2a3 6c787261
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 26 15:44:08 2021 -0700

    Merge branch 'songlist-remember-sort' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit 889bf2a3c282c4d9d7f770262d65ad749e688e11[m
Merge: 5d999bc5 212fbfaa
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 26 15:43:58 2021 -0700

    Merge branch 'bpmchange-improvements' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit 5d999bc513f386453f826bc847c3f940f4cf3b99[m
Merge: cbf32f1c eecf852e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 26 15:43:23 2021 -0700

    (hopefully) cleaned up merge conflicts

[33mcommit cbf32f1c63e1b3e1487e9a3030b52392d378926f[m
Merge: 614286fd b9975d6b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 26 15:40:48 2021 -0700

    Merge branch 'options-search' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 614286fd2fa9cb5b0265c277b23c552e1834e7ee[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 26 15:38:35 2021 -0700

    i guess theres a dashboard url now

[33mcommit 6c787261b5dfb2068ac7c789080084cb0e873717[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Jul 26 22:03:38 2021 +0200

    remember song list sorting type

[33mcommit 212fbfaa8bdae48c85487d148c3473fb9317de7e[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Jul 26 19:58:22 2021 +0200

    improve scrolling behavior around bpm changes

[33mcommit 1175cfa5cb70a8509c058dad9ce8717b5e8196f5[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Jul 26 14:24:30 2021 +0200

    placing bpm change defaults to previous bpm value

[33mcommit b9975d6b9e2146d8aa9796de68cddb10f34a70c7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 26 13:34:48 2021 +0100

    Search on section headings in keybinds too

[33mcommit bd54102431ee80b038683c81b264de2cca19c153[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Mon Jul 26 13:45:24 2021 +0200

    make cursor follow grid on bpm change tweak

[33mcommit 279876e678ae6fbef6e87c8e59fd36ef93551105[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 24 12:38:38 2021 -0700

    why do unity now store lighting files separately, cm doesnt even use it

[33mcommit b26b3a714d6cbcc583196f992ca65c175eb09e20[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 24 12:38:20 2021 -0700

    Disable grids with disabled object types

[33mcommit eecf852e34ab05a1ad26143494ac22ecc28d2f83[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Jul 24 13:57:00 2021 +0200

    update saved volume when hitsound volume is changed

[33mcommit 271ccd3c33ee0d509ac322e03a2ceca1c90f5a07[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Jul 24 12:50:51 2021 +0200

    restore muted hitsound volume when exiting map

[33mcommit fb44ece83f7cf5e38555385cb1ad0bc0402bec46[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Sat Jul 24 12:33:41 2021 +0200

    remove namespace

[33mcommit 2a4cdeec80466b67fc1b667d51278810c20e980f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jul 24 11:01:00 2021 +0100

    Load keybind options in coroutine

[33mcommit 64d0a8f864f66b4215b86e2c4b90beffe2ccfbfa[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Jul 23 22:59:34 2021 +0200

    add keybind to toggle-mute hitsounds

[33mcommit 8a05514cd0c646266b12c6781f76a65dda5daf41[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 23 22:35:44 2021 +0100

    Make options searchable

[33mcommit a32b9c145a79d89e00cf7f7063e99d8691990a49[m
Merge: f6f9e0a0 63aad3af
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 23 13:55:17 2021 -0700

    Merge branch 'spectro-freq-comp' of https://github.com/GalaxyMaster2/ChroMapper into dev

[33mcommit 63aad3af23e578577272e6170672126709a1b9d0[m
Author: GalaxyMaster2 <10052298+GalaxyMaster2@users.noreply.github.com>
Date:   Fri Jul 23 17:50:38 2021 +0200

    compensate for frequency bin in spectrogram

[33mcommit f6f9e0a068fde02250145b51595ec76b9447028f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 23 11:55:37 2021 +0100

    Upgrade inputsystem to 1.0.2 again again

[33mcommit ecde9039fd0202bfa7ea85ae072aa44359371cac[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 23 11:53:19 2021 +0100

    Make raycastall match raycast

[33mcommit 18d5d2a8eaa5c890da7b9f81e177614ade74a2ae[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 23 11:52:05 2021 +0100

    Remove files that shouldn't have been commited

[33mcommit 4ed765f36b6d7439b590d4ce9728f18e27c79ca5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 23 01:12:47 2021 +0100

    RTFM (#305)

[33mcommit 506630324be67bab77249d3e8a173a4d3f98417d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 14:52:05 2021 -0700

    extra shit from unity 2021.1 upgrade

[33mcommit 25deaabe2f06e9bab4d148e83850554bfc45bd6b[m
Merge: 6c4ee00d 4f1f5fd8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 13:43:32 2021 -0700

    Merge branch '2021.1' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 4f1f5fd8a37d8e99049b1de7bd74b34b40fab3d0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 22 21:39:11 2021 +0100

    Fix input system patch

[33mcommit 6c4ee00d296a529c4e0f6f0cd41a2d38ac9d21f9[m
Merge: 61b46ed7 3e8b6d86
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 13:02:42 2021 -0700

    Merge branch '2021.1' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 3e8b6d8667f5b70469b78b9799c83cc1a9ab89a0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 22 21:01:33 2021 +0100

    Stop using async localisation methods as they were becoming invalid

[33mcommit 61b46ed7d7bdd562ac1b9d37f74c6248b0d94e7c[m
Merge: c1127018 8dbda56e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 12:16:38 2021 -0700

    merging unity files makes me wanna commit uncode

[33mcommit c11270184abeeb59cdbf40d70a56f62305a48a22[m
Merge: 23425762 75cadf0b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 12:13:08 2021 -0700

    Merge branch 'bug-fix' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 234257625a8f7be338b08b71747f125bd14ebe1c[m
Merge: 31f0a75d c8382edd
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 12:12:51 2021 -0700

    Merge branch 'translation' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 31f0a75dc3786af12ee2c66b667ce792b1949905[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 12:12:41 2021 -0700

    leftovers from Unity 2021 upgrade

[33mcommit b2b413db0385a522e1963b758cbef77a947e3791[m
Merge: 5da81d64 421953bd
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 12:07:58 2021 -0700

    Merge branch 'raycast' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 5da81d644f122eda116f06a5cf71a1104b90be88[m
Merge: 3587db76 b0dca1b6
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 22 12:07:46 2021 -0700

    Merge branch 'toon-tweak' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 3587db767173ac92691072db2d83e57b3c47db95[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jul 21 14:03:31 2021 +0100

    Linux build method

[33mcommit 26b8b13500c4bf02bff0fbfebe76142465728eac[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jul 21 21:31:15 2021 +0100

    Update version in build guide

[33mcommit d496080026f810d842257add14bcc824c7f5643c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jul 21 12:42:43 2021 +0100

    Upgrade to 2021.1

[33mcommit 421953bd7d80d5396129ddd301137e1a5a53cf7f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 18 18:36:26 2021 +0100

    More raycast refactoring

[33mcommit 75cadf0b91df410ad7d82bff6fb76a530e8e4e83[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 18 17:14:49 2021 +0100

    Null check TOCTOU

[33mcommit aa31522a480502a9d1e7b598cdc15b490bee7084[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 18 17:04:01 2021 +0100

    Bop unity analytics

[33mcommit 0b8014b3b673754cb2d2645189d291cc51041493[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jul 14 14:38:42 2021 +0100

    Raycasting fixes

[33mcommit b0dca1b6a18b39c0767718051cd178fee72f0044[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jul 14 13:12:30 2021 +0100

    Add option to outline shader to offset mesh scaling

[33mcommit 0712ab359bdca7d9629019a1b924eba244405569[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 16:45:45 2021 +0100

    REEEEEEEEEEEEE brackets

[33mcommit c8382edd8e4f76e26a0a8d7b0fe65d42a0c5a74d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 15:42:01 2021 +0100

    Fix swedish too

[33mcommit ff4e5ae03f480f6ea0847e6ae3c80d841c54bda3[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 15:30:10 2021 +0100

    Fix spanish locale code

[33mcommit bacbdbfec3ed4df64ae379540203e48c5849bb66[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 14:21:38 2021 +0100

    Update translations

[33mcommit 3862d39050ebfaa57d8a9ab7c33b13183a8df02d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 14:19:24 2021 +0100

    Minor changes to strings

[33mcommit efefca49a807a44e217a046bbad92987ed8452cb[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 14:19:24 2021 +0100

    Minor changes to strings

[33mcommit a9f0851f2db63354744d652948afaa25fdbf008e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 11:12:49 2021 +0100

    Wait for previous spectro generation to finish before generating another in song edit ui

[33mcommit 6bf6a47d5f05466003531b5a47d72a2e13d65083[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 10:58:40 2021 +0100

    Force placement note to not show arc

[33mcommit 1c701f82edc1eab55904e15be41637058d4392b7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 10:48:39 2021 +0100

    Improve options menu resilience

[33mcommit d561e80ee0f72ef1a53139106d64826367060cff[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 10:42:23 2021 +0100

    Fix selected tab when returning to song list

[33mcommit 00ad6f092df4aecc32f5a5541a712392d08aea13[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 10:29:11 2021 +0100

    Song Info UI difficulty fixes

[33mcommit d3d8cb84b64b876f37e2283cfa88518fc4f508ed[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 12 10:28:23 2021 +0100

    Stop keybind titles getting updated when changing language

[33mcommit ac05033f7be8cfb14a130d6ecd10409e110dc58e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 11 15:53:16 2021 -0700

    Force disable Unity Analytics

[33mcommit 8dbda56e7ca6f7b42fd877e88c453ee51502d2c2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 8 22:29:28 2021 +0100

    Add some ui prefabs to make plugin ui slightly easier

[33mcommit dfc94ce086b7badae247afc0867642169aece3a4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 8 22:29:48 2021 +0100

    Add extra condition to strobe generator filter (#297)

[33mcommit ef360ffc880d9386d083ba69daecd8221a75e15c[m
Merge: 89084813 e422c936
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 7 10:19:49 2021 -0700

    Bug fixes (#294)
    
    Bug fixes

[33mcommit 89084813161f275df3bf8ed5c7dce2a6ba1934fb[m
Merge: b966fb2e 81b8e19d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 7 10:19:27 2021 -0700

    Be lazier loading diffs (#296)
    
    Be lazier loading diffs

[33mcommit e422c9364a4a738c4b1563575c4dc1fee11f0da4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 5 17:39:22 2021 +0100

    Fix clicking UI not swapping light colours

[33mcommit 81b8e19df91ac55f774b0afd34bb549a9d31cf01[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jul 5 03:46:26 2021 +0100

    Add equality members to EnvEnhancement

[33mcommit dc5a0cd00b0b531bda6a19bd1b39bd86b10f4dec[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 4 18:10:41 2021 +0100

    Handle null customData in ShiftSelection

[33mcommit b7a9ff8ebbb334272ded5ecc852caeafa25ecdbb[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 4 00:36:10 2021 +0100

    Be lazier loading diffs

[33mcommit 24d59e49c2a7ba7219d2facaa6bb650fa3081073[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 4 01:02:04 2021 +0100

    Fix issues when spamming space on song load

[33mcommit 5a262c2b5dc1f8be81022e95db82e92c2218610d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 30 14:39:30 2021 +0100

    Cancel previous song loads when triggering a new one

[33mcommit 41e3fc6727e6145c6d1a053570e3390c934c57b2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 30 14:37:54 2021 +0100

    Change lightid filtering in gradient generator, add tests

[33mcommit 7af5123583297ce9f48a186235b33e586a62ec20[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 30 13:18:19 2021 +0100

    Avoid error when writing duration cache if it doesn't exist yet

[33mcommit 7561b5274913031874becc07a27db8c546c23e5e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 30 13:17:51 2021 +0100

    Fix boost event materials not updating

[33mcommit 94e94f34751308c4cc7bbdd6a1899ce8ba92f7ef[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 30 13:17:28 2021 +0100

    Don't remove _active if it's false, contrary to the docs this actually does something

[33mcommit b966fb2e58c2405940b7f257a595971380326073[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 20 14:01:56 2021 -0700

    only apply isLeftEvent bool on chroma directions

[33mcommit 0cda931fab918f3328f1df560c67502b609c78be[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 19 13:08:17 2021 -0700

    Reset shader rotation

[33mcommit 1f1cd7b96dfe2bb7ab94de7d9defd745379cc09a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 19 13:07:26 2021 -0700

    Add Discord assets for new environments

[33mcommit 92932deb9b8d403e3d5956e28bdff203826b6b62[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 18 14:05:48 2021 -0700

    ok fine i guess we want physics

[33mcommit 83f19dc04988c903ed6c50ae01154e47631b13b1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 18 14:05:41 2021 -0700

    forgot to GPU Instance arrow material

[33mcommit ce6a80a7adfb048c3bb500bd3c8116d36e313394[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 18 13:18:49 2021 -0700

    duration was already multiplied by editor scale

[33mcommit 9a5d1171e4790d50a64b86e4623a6e227ef133b9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 18 11:42:04 2021 -0700

    Update PLUGINS.md documentation

[33mcommit 2740dddf2cb4bee0223c956973948c860b3519e1[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jun 18 18:51:52 2021 +0100

    Ignore missing diffs when refreshing requirements (#292)

[33mcommit eb4a12657d9f52c25e5b2225d110d26ed9472c68[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 18 10:50:24 2021 -0700

    Brightened note dot and arrow

[33mcommit 8001a4cc9207852746215dcd3d2d03664f2d6380[m
Merge: 722b8ab4 c08c610f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 17 15:29:56 2021 -0700

    merged fast (non-GPU) raycasting

[33mcommit c08c610fac92c50f393936a696de6afdcdc962cf[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 16 20:55:31 2021 -0700

    move from gpu raycasting to chunk system

[33mcommit 722b8ab4267eb0a3b927913232a7437cef57c6d8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 16 00:54:14 2021 -0700

    add reddek

[33mcommit 0b1335419d646e80d69095f402129c4f461052fb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 22:35:39 2021 -0700

    tooooop caaaaaaaaaaaaaaaaat

[33mcommit 0c6f2f8cbca0c7e8e29f4b4564121fde84b98c82[m
Merge: fbd7e2d3 8d850769
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 22:32:25 2021 -0700

    Merge gpu-instancing into dev

[33mcommit fbd7e2d303c0dda1e85613423caa902fdf61408c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 22:24:46 2021 -0700

    fix interscope lights

[33mcommit a7b95ed2da9b1a9a1b0d31c425e35811ff0170f3[m
Merge: 15e53547 16239e87
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 22:21:04 2021 -0700

    Merge branch 'path-fix' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 15e53547f6a2d15e15b89c040963526049e1ab68[m
Merge: 71592119 0444eccd
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 22:20:07 2021 -0700

    Merge branch 'reqs' of https://github.com/Top-Cat/ChroMapper into Top-Cat-reqs

[33mcommit 7159211913daa32def353a4ca1ea0bcb0b3b0273[m
Merge: bc87cf11 ef210880
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 22:18:11 2021 -0700

    Merge branch 'bug-fix2' of https://github.com/Top-Cat/ChroMapper into Top-Cat-bug-fix2

[33mcommit bc87cf110f2b6411d0b5899c018d2e960a42342c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 22:17:07 2021 -0700

    added some haha funnies to interscope

[33mcommit 507d7c26ec02b70dd5ed8271a33d6528a3c3dcf3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 14:55:14 2021 -0700

    event handler boilerplate, improve interscope

[33mcommit b23af86d0df9f7e88abc7864278cc839a9ece866[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 15 14:54:47 2021 -0700

    Fix NREs with env. enh. on new maps

[33mcommit 173d7bbbd2ab9995689bb18920fa1d3d7012b577[m
Merge: 9a036e97 6f050cd0
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 19:20:56 2021 -0700

    Merge branch 'interscope' of https://github.com/Top-Cat/ChroMapper into Top-Cat-interscope

[33mcommit 9a036e97aedef595146f2323e46547ec26259b10[m
Merge: d6694ef8 16cd0525
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 19:20:15 2021 -0700

    Merge branch 'env-enhancement' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit d6694ef8ebea7c1c0aeceb162038332b807de9d3[m
Merge: 53ba32c9 a1a8717a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 19:19:36 2021 -0700

    Merge branch 'bug-fix' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 53ba32c9a4d458bee2b8bf34522ae6613a828807[m
Merge: f69d9728 f5dbca98
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 19:17:47 2021 -0700

    Merge branch 'dev' of https://github.com/XAce1337manX/ChroMapper into dev

[33mcommit f69d97288d0c087c6dc31d3ff0148684edd6141e[m
Merge: 5a28ecc7 9ccf7a10
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 19:17:09 2021 -0700

    Merge branch 'release-song' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 8d85076926e536976dfbd6dbb161821e8ec0b3fc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 15:19:31 2021 -0700

    Tweak preset options

[33mcommit 6d325cbca69a50303f711a9841c505c26422d4f5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 15:19:12 2021 -0700

    turns out gradients werent working either, whoopsie

[33mcommit d8ca71442fd6cb34d575ec5e5675376904325490[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 15:17:27 2021 -0700

    Add Obstacle Outlines option

[33mcommit c79b99775fb83a7a778653db7f443778a6e7a4fe[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 14:28:25 2021 -0700

    be not dumb with spacing

[33mcommit 34220915a04a13b78b5f27fa23ec604a8cc25a1b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 14 13:02:16 2021 -0700

    Fix sprite lights not applying texture

[33mcommit 5104ac3a059cca16c2ee345ac6d878e725b981a2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 13 14:06:17 2021 -0700

    misc. optimizations

[33mcommit 0172f4e047929ce94cfb75d2e063bc6b79697737[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 13 13:57:45 2021 -0700

    Enforce positive scale on walls for better batching

[33mcommit 22fdd9ae4b972ab6d781864ef6aebaae8f5ee565[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 13 13:57:29 2021 -0700

    GPU Instance-ify notes and bombs

[33mcommit 0064a12b416e1bb01ea1f2b0bc02cd421070eed8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 12 18:12:33 2021 -0700

    Optimize selection rendering

[33mcommit a384b1e9058f3ebaf2a3b37b7b5002485206bfa0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 12 18:12:18 2021 -0700

    Enable GPU Instancing on more materials

[33mcommit 5497a9e0ae4a91f84127f6580fbecb8b891bba9f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 12 11:43:18 2021 -0700

    (attempt to) GPU Instance-ify notes

[33mcommit 9ee689d82e76f1aab708351608c811c10b0668d1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 12 11:42:55 2021 -0700

    GPU Instance-ify lights

[33mcommit 71f681deba724d7ccff8091935a303befbc985f0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 11 21:49:22 2021 -0700

    GPU Instance-ify obstacles (outline setting pending)

[33mcommit 4de43c72cd96876706c029b1f83e5a2b85681144[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 11 20:54:22 2021 -0700

    forgot to enable instancing on other grid materials

[33mcommit 23721adb0fc7da10c2b6f47b5048b02037479a94[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 10 16:10:47 2021 -0700

    GPU Instance-ify grids

[33mcommit f9a5185044bd8373bbd47aabaa4f5a4a0081b7bc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 6 00:42:16 2021 -0700

    Raycasting optimizations coutesy of danike

[33mcommit 1283b1be5ae4c406a36b3dff88d55abd03a5d824[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 2 10:29:12 2021 -0700

    fixes

[33mcommit c79a4320b8b8d4032090350a12880a96cee3df59[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 2 10:29:08 2021 -0700

    Remove all physics colliders from mapping scene

[33mcommit 8caf467fe491b94e374e09118b979b806f388ece[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 1 19:30:17 2021 -0700

    Y E E T       P H Y S I C S

[33mcommit d33f6d9c2c82d925882df0d9fb12d0026d69211b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 1 19:30:09 2021 -0700

    convert placement code to use intersections

[33mcommit 5940128f6f1fa1cccf35adea39a33aed89ac0479[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 1 19:29:40 2021 -0700

    Replaced core prefabs with intersections collider

[33mcommit a68c0cdbd1f774d4090e5769b4456be26ffb3a8b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 1 19:29:15 2021 -0700

    Import helper meshes, modify readability settings

[33mcommit ffd7fcaf3e3be97fa26fa66efed3cf506fae6c17[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 1 19:28:51 2021 -0700

    Necessary changes to accomodate CM implementation

[33mcommit 13c90afc5304a003ca68cb7ea66cb04a68249766[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 1 16:48:30 2021 -0700

    Add fast intersection files (developed separately)

[33mcommit 626b38149e52805b8b82cb398078f7ce92a3115e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 1 16:06:27 2021 -0700

    Set up framework, GPU Instance-ify events

[33mcommit 16239e8780e708ce072d49febdbcd962e4979ea7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 30 19:13:53 2021 +0100

    Use C# apis to normalise user-suplied paths rather than find/replace

[33mcommit 0444eccd7c5616c88e019e6dccb97e3cb58cfd53[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 30 18:42:48 2021 +0100

    Don't require chroma for coloured notes

[33mcommit ec0722518cefb3495246a53606a12919a149fff8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 30 18:40:24 2021 +0100

    Refactor requirement and suggestion checking

[33mcommit ef210880f976829c181922fddf425021f3792242[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 30 14:49:19 2021 +0100

    Save song before diff if needed

[33mcommit 15793e845f4a30deeab491c9da08f8c207f1d591[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 30 14:41:39 2021 +0100

    Fix chroma direction rotating lasers in opposite directions

[33mcommit 8ec268378bfba6213af4862f48f0abfb268676f6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 30 14:08:44 2021 +0100

    Middle click to invert boost lights

[33mcommit 6f050cd0ea5843f5c3096da185e7af56f07c1347[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat May 29 14:05:12 2021 +0100

    Interscope Environment

[33mcommit 16cd0525e64311643dae030f642606b4ffaba957[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 30 16:07:25 2021 +0100

    Upgrade env removal to env enhancement

[33mcommit a1a8717a8963aa79370ac8743b9a2aba7eeeb149[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat May 29 15:04:33 2021 +0100

    Prevent song list items being merged if they share the sorted attribute

[33mcommit 5dfa3bf50cb96b64339343dc2e788d9e2e5c03b9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat May 29 15:04:01 2021 +0100

    Allow box selection to deselect unloaded objects

[33mcommit 5053977773bdac661a6360434369127a3571b0d2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat May 29 14:51:28 2021 +0100

    Rewrite conflict check to handle stacked objects better

[33mcommit 5a28ecc71f311f7b51c0ee6066770b72f6900dc2[m
Merge: 7225131c 19808ddc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 28 18:20:52 2021 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 7225131cb3ed8b150a3c9f1be44aaa1262cc71e6[m
Merge: 2a522913 591bfadb
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 28 18:20:45 2021 -0700

    new song list pogU

[33mcommit 591bfadb0213e220369b041a6caa4738e387b1ac[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 28 18:20:12 2021 -0700

    my own changes (big commit lmao)

[33mcommit 19808ddc196f36636a2fc015608752c68deb33a1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed May 26 23:51:40 2021 -0700

    i forgot i also have to explicitly copy custom colors

[33mcommit f5dbca9889042b745b01087c62536e3271fef6a6[m
Author: Bullet <vincentn@live.com.au>
Date:   Thu May 27 13:58:30 2021 +1000

    Selection count no longer blocks clicks

[33mcommit 2a5229134e25c9f1f67443ff3e811132e385bb46[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat May 22 17:01:11 2021 -0700

    Forgot to add new Chroma gradient to customData

[33mcommit bcff1d1cf9fb2881ebd200340de75e85abacb45b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 21 11:15:05 2021 -0700

    fix song duration bugs on newly created maps

[33mcommit 52862816a970dc47d7e1b1909b2ad74ce7a6b9aa[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 21 11:14:49 2021 -0700

    Properly assign references (this may have been a Unity moment)

[33mcommit 19e45cf0282b0b9d8ed77cdf7274e12a160e0daf[m
Merge: 7dacb11a 30759da5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu May 20 14:02:01 2021 -0700

    Merge branch 'songlistui' of https://github.com/Top-Cat/ChroMapper into Top-Cat-songlistui

[33mcommit 7dacb11abbfcabb1a615743436c15728482d12e5[m
Merge: aa30ffea 7c487319
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu May 20 14:01:17 2021 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit aa30ffea03b263b32b30af73deaed111ed32f277[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu May 20 14:00:50 2021 -0700

    Enforce naming guidelines

[33mcommit 7c487319f3289516c01d945d25bad06e6e86d0a5[m
Author: Bullet <68104413+XAce1337manX@users.noreply.github.com>
Date:   Fri May 21 06:58:16 2021 +1000

    Song timer and speed text no longer blocks clicks (#275)

[33mcommit 9ccf7a10508453d4196dec83b5493d4913c5cbea[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue May 18 19:40:51 2021 +0100

    Allow audio clip to be released when ATSC is destroyed

[33mcommit 30759da5600ae13a424da02ec81a3af878e72ed6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri May 14 17:57:59 2021 +0100

    New song list UI

[33mcommit eb6c6fe9a283189b76558581c653ed0c4636e8ce[m
Merge: ff0a6326 7977c06c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun May 9 14:57:04 2021 -0700

    Just write good code the first time 5head (#272)

[33mcommit ff0a63267a295b417d0a644c07aa4d4d48f3c04d[m
Merge: e01bab1e 28efd167
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun May 9 14:47:16 2021 -0700

    Fix light ids in default env (#274)
    
    Fix default env right laser ids

[33mcommit 28efd1678b18a1041aef96cafed0f737bccc2c56[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 9 21:59:16 2021 +0100

    Fix default env right laser ids

[33mcommit 468f93bcc25d293124d070cfafd828c821d8f7ce[m
Author: Bullet <vincentn@live.com.au>
Date:   Sun May 9 18:09:47 2021 +1000

    Check leftSelected.enabled instead

[33mcommit 8253ef33c3d451dba7f350136151b17430c1ed8b[m
Author: Bullet <vincentn@live.com.au>
Date:   Sun May 9 18:03:02 2021 +1000

    Revert rotate in place keybinds
    
    Toggle on Alt + Q

[33mcommit 7977c06cfb36f2b0f4e394e23cd1b808b1af292a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat May 8 20:53:06 2021 +0100

    Handle null in UpdateQueuedValue

[33mcommit e01bab1ea2d0cb5013300f50755089b5ffcd9d0c[m
Merge: 693e536b 4c111376
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat May 8 12:03:14 2021 -0700

    Aalto is now happy (#271)
    
    Make aalto happy

[33mcommit 4c111376560d8d0f3865ecc642502b389ed99d9d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat May 8 19:24:32 2021 +0100

    Remember if camera is locked when toggling lightshow mode

[33mcommit cb471ec2e040c148d59edc6e5effda847ab5cb25[m
Author: Bullet <vincentn@live.com.au>
Date:   Sun May 9 00:25:36 2021 +1000

    Change rotate in place keybinds to Alt + Q/E

[33mcommit 8a1b665634c03ac55c33248df433edf748798612[m
Author: Bullet <vincentn@live.com.au>
Date:   Sun May 9 00:03:08 2021 +1000

    Added keybind colour toggle on Q

[33mcommit cfc2f8304b533da1493778ae293cb872017f89a7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri May 7 12:47:45 2021 +0100

    Allow fade/flash in all lights lane

[33mcommit 01c17d25b03457a39f05914adaaa2dbd2b5d4a75[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri May 7 12:41:17 2021 +0100

    Ensure we don't disable the camera when entering lightshow mode

[33mcommit 693e536ba8f3e35e8339f96f11d0d86da26bd5f1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed May 5 12:53:36 2021 -0700

    Improve track assignment on irrational beats

[33mcommit 41a64f50212de47b252a7d33881cfab8f78aea32[m
Merge: eb62b8d7 715683b8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:08:57 2021 -0700

    Oh hi, it's been a while. (#268)
    
    Development (4/22/2021)

[33mcommit 715683b8c3ded749c132f2b976aec3358c0228f3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:07:46 2021 -0700

    ok seriously why is NotoSans updating

[33mcommit ac64a4f5e362fd8e33697a13291f9127b20f4c4c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:03:53 2021 -0700

    Camera now locks to the track by default

[33mcommit ea0aea497ca9a6375cda4dad22a81f2938a63b13[m
Merge: 507e318d ad3176b1
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:02:49 2021 -0700

    Merge branch 'default-stable' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 507e318d1cfc58cf67249bc0e5157e1524e48418[m
Merge: d22ab1d9 066faffb
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:02:29 2021 -0700

    Merge branch 'bug-fix' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit d22ab1d988291a57fb611370c2564b5afde391d1[m
Merge: 13b34e37 835c5532
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:00:42 2021 -0700

    Merge branch 'plugin-unicode' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 13b34e370f9e842dff40fbe92ed43e7a644e1a63[m
Merge: 91f511b4 690ef415
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:00:32 2021 -0700

    Merge branch 'drag-guard' of https://github.com/Top-Cat/ChroMapper into dev

[33mcommit 91f511b43bf1ea53f4c72169aba0fb9fb7461125[m
Merge: dae68a2e 350a5ad7
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon May 3 00:00:14 2021 -0700

    Merge branch 'dev' of https://github.com/XAce1337manX/ChroMapper into dev

[33mcommit dae68a2eb2dcb51397b968cb545b274d92051009[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun May 2 23:59:35 2021 -0700

    Improvements to #232

[33mcommit f91f2a6625d5c320762a035a3a340b62b98620e8[m
Merge: 673af131 9d31476d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun May 2 23:55:05 2021 -0700

    Merge branch 'dev' of https://github.com/Pixelguymm/ChroMapper into dev

[33mcommit 066faffb04aa2692f0cc2a58152088498ee3cbc5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 12:25:47 2021 +0100

    Update translations

[33mcommit 906fe5c730c91069f1ba54eefa9bd5063a030e38[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 12:21:43 2021 +0100

    Fix Color preset editing when multiple presets are the same colour

[33mcommit f123befed115bc201b3a9f3fd04a11acd76e7c43[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 12:04:32 2021 +0100

    Fix contributors needing two clicks to open

[33mcommit 86885d2e17c0b773f69fcd680cf7b23e79330dd5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 11:52:56 2021 +0100

    Fix lightid being removed when shifting if not an array

[33mcommit 24547ff6c0466f00862edcdf9c62c9b94ea6de81[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 11:43:00 2021 +0100

    Bring back towers in Triangle (which stops them toggling incorrectly)

[33mcommit 7ec386566d74d6219a0b533ba7521d3aa223935d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 10:30:58 2021 +0100

    Update mainNode when doing full refresh

[33mcommit a836fbb6ce83589ed46841a42262ae2eeabd38c5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 10:17:04 2021 +0100

    Fix bomb precision placement placing to the right

[33mcommit 6089dac8e80ca3cfedf54ac1b9847a8af79c2b78[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 30 10:08:55 2021 +0100

    Fix saturation and value bars not rendering correctly

[33mcommit 673af13151883e295b557bee65229cb6d4c104a8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 29 23:39:11 2021 -0700

    fix non-NE _customData fucking up note mirroring

[33mcommit 835c553238c680fff1d2344a5b914e55791cb39e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Apr 27 11:32:26 2021 +0100

    Fix plugin loading with unicode paths

[33mcommit 264d73a32bf2374e5714cd0b76bba362572798c7[m
Merge: 88d5d4f7 fb24cdc6
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 22 22:37:59 2021 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 88d5d4f7297b905383ee7937754165afff66e61e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 22 22:37:39 2021 -0700

    Update Patreon list in credits

[33mcommit fb24cdc65647bcf18aa43cfc63554b0636bdf7bb[m
Merge: b55e6f89 e7d14e10
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Apr 20 23:06:38 2021 -0700

    Easy way for plugins to add buttons to right side panel (#267)
    
    Extension buttons

[33mcommit e7d14e106644c1559182cdf6494a67cb6882f2f6[m
Author: dcljr <dcljr@me.com>
Date:   Tue Apr 20 15:46:19 2021 -0700

    Added ExtensionButton.AddButton(ExtensionButton)

[33mcommit f97409f446526f779cf9b3f021163a8d69463396[m
Author: dcljr <dcljr@me.com>
Date:   Sun Apr 18 13:13:01 2021 -0700

    Added ExtensionButtons section to PLUGINS.md

[33mcommit 5fe8a74c3904ecfd1072c2154eff47891b6d6064[m
Author: dcljr <dcljr@me.com>
Date:   Sun Apr 18 13:04:29 2021 -0700

    Added ExtensionButtons for use in plugins

[33mcommit 690ef415e976a52ae418c69b046af45eb8044104[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Apr 17 14:49:51 2021 +0100

    Defend against modifying objects while being dragged

[33mcommit b55e6f8934aa534bc308ab4fde7665be7351ad8c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Apr 17 02:01:06 2021 -0700

    (Hopefully) fix step gradient not generating the very last event

[33mcommit 694b98dfc163b6bb5478f7889a34b62ddc620f5b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Apr 17 01:49:03 2021 -0700

    Improved copy/pasting across multiple BPM Changes

[33mcommit 6982bcb423d12885cf557fdd0e1a0d3ff0490c34[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Apr 17 00:07:21 2021 -0700

    Add Alt-Scroll to tweak BPM Change value

[33mcommit 30555fdc5d35504abad7d99c332b1d886050ec0d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Apr 16 23:55:26 2021 -0700

    Stop flash/fade/2.0 gradients from generating on light id events

[33mcommit 29c884c20969c5f87b8cd5b34d9b9da38d535a49[m
Merge: efd04728 569cbea0
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Apr 16 08:59:54 2021 -0700

    Resolve enumerables early mmmk (#265)
    
    Enumerable needs to resolve now

[33mcommit 569cbea00d1759e4a72ef60183763d9199056b57[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 16 16:25:32 2021 +0100

    Enumerable needs to resolve now

[33mcommit 350a5ad7f314fea1ccc13a97039a4405db0b9d2f[m
Author: Bullet <vincentn@live.com.au>
Date:   Wed Apr 14 19:25:51 2021 +1000

    Fade in is back
    
    (and faster)

[33mcommit b75f7bacbb73d2c19fd4c5fc9b782d919afba901[m
Author: Bullet <vincentn@live.com.au>
Date:   Tue Apr 13 16:51:57 2021 +1000

    More accurate audio preview

[33mcommit efd0472813b8672bf4a7b1d7c24afa179b918295[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Apr 11 23:07:04 2021 -0700

    Replace constants with color alphas

[33mcommit 5fe25eb50bda463ac24f49f1765b323ca618c050[m
Merge: b892d613 15a0d28f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Apr 11 23:05:37 2021 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit b892d61315e961b048c40f926093a9f081a9b6c0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Apr 11 23:05:27 2021 -0700

    Force multiply alpha reset on other event types

[33mcommit 15a0d28f2f1a797f96d11e6c6c894caf9329b656[m
Merge: 13b92eb3 415aef09
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Apr 11 17:41:14 2021 -0700

    Fix null BPMChange _customData making it into saves, Node Editor fixes (#262)
    
    BPM Fixes

[33mcommit 415aef09402c1eae57b793105b3b26a9a92c284d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Apr 11 15:15:23 2021 +0100

    Apply node editor actions using action code. Don't include custom data if it's null

[33mcommit 13b92eb3d818cd54e3292513e4ed7ea361475be8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Apr 9 00:13:09 2021 -0700

    Some more optimizations

[33mcommit f6caad5c90910a42ceaac8fe65f8d727bb2e486f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 8 18:50:06 2021 -0700

    Forgot to apply HDR Intensity to On events

[33mcommit ae668d85f65bb54272da6a0937e7376241436c04[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 8 18:49:57 2021 -0700

    prevent snapback when playback actions are disabled

[33mcommit b74ce9b171ac38c3c11b200d310b81e558dded46[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 8 00:12:04 2021 -0700

    We do a little bit of code optimizing

[33mcommit b7ac1eb6f6bb38d52887dd9ef2a3a4c6f2c7ec78[m
Merge: 7214bbd4 9f60064d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Apr 6 14:01:59 2021 -0700

    imagine merging faulty code smh my head (#261)
    
    Don't apply quick edits while dragging

[33mcommit 9f60064d1687b97f9a3760d61afa708a45f967b4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Apr 6 21:47:45 2021 +0100

    Don't apply quick edits while dragging

[33mcommit 7214bbd44d066a60080b1a5be8ec42d9d65e9a1a[m
Merge: ba0a1e63 ea66ba45
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Apr 6 10:17:32 2021 -0700

    Who could've merged faulty code smh my head (#260)
    
    Fix painting objects with no customdata

[33mcommit ea66ba45e8d1d234591749a66cd92fa45d4e4b6d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Apr 6 10:12:41 2021 +0100

    Fix painting objects with no customdata

[33mcommit ba0a1e63375b06271d55fbe9dfe994c9a57f238b[m
Merge: 86fff880 fd9fe42c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:53:19 2021 -0700

    Merge branch 'Top-Cat-resync-fix' into dev

[33mcommit fd9fe42c7db6a3801e047e6def240c80adbbfd39[m
Merge: 86fff880 3da64f54
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:53:11 2021 -0700

    Merge branch 'resync-fix' of https://github.com/Top-Cat/ChroMapper into Top-Cat-resync-fix

[33mcommit 86fff880e8e1d66f9bd3017de3090d095d2087f8[m
Merge: 0150120c 0d0420bc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:50:32 2021 -0700

    Merge branch 'Top-Cat-quickedit' into dev

[33mcommit 0d0420bc306104629ecd8f3a3e20bd50bfa2acf3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:48:56 2021 -0700

    i'm going to be that guy (again)

[33mcommit 2605515134e7905033c5b0bd1272bb3b41c2647d[m
Merge: 0150120c 96ea185d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:42:40 2021 -0700

    Merge branch 'quickedit' of https://github.com/Top-Cat/ChroMapper into Top-Cat-quickedit

[33mcommit 0150120cc46264ff086947a2f5eef8280599f5f0[m
Merge: dbf0752d a341dab5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:41:46 2021 -0700

    Merge branch 'Top-Cat-save-precision' into dev

[33mcommit a341dab56dd89ee80933e82a19403f39eedc5df5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:41:24 2021 -0700

    im totally going to be that guy

[33mcommit 2d6d7f3e9e578aec95e34a3e0b74e50194600b86[m
Merge: dbf0752d cd459a3a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:38:45 2021 -0700

    Merge branch 'save-precision' of https://github.com/Top-Cat/ChroMapper into Top-Cat-save-precision

[33mcommit 96ea185d8ba44b9250b89ca32ef82381896f1cee[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Apr 4 19:57:26 2021 +0100

    Remove static property, prepare for conflicts

[33mcommit dbf0752de70f5cc6d1cbe31fb5b550cf72bc0d46[m
Merge: 630c2518 aea34154
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:30:55 2021 -0700

    Merge branch 'Top-Cat-bug-fix' into dev

[33mcommit aea34154e00eb4cac35bd4a4042b5d0bdb50b5cc[m
Merge: 630c2518 e84779bb
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:27:08 2021 -0700

    Merge branch 'bug-fix' of https://github.com/Top-Cat/ChroMapper into Top-Cat-bug-fix

[33mcommit 630c2518ce20e5812636a55d86ccfd60a4d19ebc[m
Merge: 7a0aff55 16e4f3b5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:22:29 2021 -0700

    Merge branch 'TheRandomosity-dev' into dev

[33mcommit 16e4f3b55b4ee71eb292b5aedf5f2aab12809f67[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:22:03 2021 -0700

    Simplify CancelPlaying()

[33mcommit a7ca8b84a596e0f107e9f7e3664f7e82c558e42d[m
Merge: 7a0aff55 348c0921
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:17:03 2021 -0700

    Merge branch 'dev' of https://github.com/TheRandomosity/ChroMapper into TheRandomosity-dev

[33mcommit 7a0aff55204f1f03c678eb8d6d08318413128a47[m
Merge: 62d488b1 42a3f4d8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:15:54 2021 -0700

    Merge branch 'Atlas-Rhythm-rotate-in-place' into dev

[33mcommit 42a3f4d8d037a4ada55e1e0fd97e7192f1442190[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:15:32 2021 -0700

    Prevent rotate in place from creating invalid events

[33mcommit 79a9e4cbc651853edf3403c62266e0ce8c20107e[m
Merge: 62d488b1 950bca31
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 17:06:21 2021 -0700

    Merge branch 'rotate-in-place' of https://github.com/Atlas-Rhythm/ChroMapper into Atlas-Rhythm-rotate-in-place

[33mcommit 62d488b1f94a71f4e4dd3aefdda06fc060a69450[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Apr 5 16:23:57 2021 -0700

    Add .vsconfig to .gitignore (from recent VS2019 versions)

[33mcommit 3da64f541c2d1b971bf503334d516f4514f05a2a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Apr 4 17:08:20 2021 +0100

    Add lower bound to resync trigger to prevent lots of syncs happening at high framerates

[33mcommit ad7fb8e57202de89c69cdb5f8daedc129910a60b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Apr 4 16:05:43 2021 +0100

    Add quick note editing option

[33mcommit cd459a3a7e7c4cdc78f820e8791624710114e72d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Apr 4 15:41:52 2021 +0100

    Fix not being able to click on mirror button

[33mcommit 294ecf5108bd8985a6330bf097e7ae4eef9a9bb6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Apr 4 14:48:39 2021 +0100

    Save precision between maps/restarts

[33mcommit e84779bbf825c61eea887ac2c41e694c43e85d1d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 2 18:53:47 2021 +0100

    Try and fix errors from spectro generation again

[33mcommit c0831566d1f5b3531ca36d37a2a103f59c18c1c8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 2 18:19:23 2021 +0100

    Allow most inputs when mouse is outside window, add helper for safely accessing object customdata, more defensive coding around lightid editing

[33mcommit e757169a526bc909d04a270ee209928d3af43147[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 2 18:16:15 2021 +0100

    Add another digit to stop stetching between spectro chunks

[33mcommit 3da74e86fed677992473e16c9a5442ba2ac0f070[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Apr 2 18:15:21 2021 +0100

    Fix inconsistent grid visibility in hide grid mode

[33mcommit 32b4e0ec0f1aad459b063558f998cf5de983eecf[m
Merge: 5991e3b1 7828b76d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Mar 28 21:32:48 2021 -0700

    More fixes (#353)

[33mcommit 348c09218c159297266b861dd0223c0ed7541c6a[m
Author: TheRandomosity <burns.james@outlook.com>
Date:   Sun Mar 28 12:20:09 2021 -0400

    Move constant definition up out of method
    
    Move cancelInputDuration out of method, renamed to be more explicit as to what input is being canceled now that it's in a bigger scope.

[33mcommit 9255d8b23f5c052565c3e326833ddaf5d30fb89b[m
Author: TheRandomosity <burns.james@outlook.com>
Date:   Sat Mar 27 22:31:02 2021 -0400

    Holding Play and releasing after X time rewinds time
    
    Bringing in behavior from MMA allowing for releasing the play button to rewind back to the time playing began IF play was held for a length of time.

[33mcommit 950bca31e4abb0cb6e308c0a9e25a7e57e8a9967[m
Author: dcljr <dcljr@me.com>
Date:   Sat Mar 27 14:46:45 2021 -0700

    Added rotate in place keybinds

[33mcommit 7828b76df48481e3ba2f0352be1ea1609891f11b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Mar 26 16:55:26 2021 +0000

    Improved edge case error handling for event shifting

[33mcommit dcaa94514c5da32021ffdcf33bbf189ab7a0b9f4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Mar 26 16:54:20 2021 +0000

    Fix unloaded prop events not shifting properly. Playback at end of time is weird again?

[33mcommit 5991e3b14b22720009a583226783f05969cf8d66[m
Merge: d90c9d7a 97fbde2a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Mar 25 12:30:53 2021 -0700

    Bug fixes (#252)
    
    Bug fixes

[33mcommit d90c9d7ae03a86a6b95cb003ff719c00ca52b097[m
Merge: 82282a11 0fd35adf
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Mar 25 12:29:07 2021 -0700

    Fix rocket league lightids (#250)
    
    Rocket league lightid

[33mcommit 97fbde2a302e339ffad4fa693ef574611a256b8a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Mar 25 16:17:07 2021 +0000

    Apply uv clamp to both axes of spectro

[33mcommit 5b78362ce9cdc58c162df480acf891c49ef2be28[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Mar 24 12:14:52 2021 +0000

    Excuse me green day?

[33mcommit 63a13a9ea9d4237d0851c175fdfb3f4252bedef8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Mar 24 00:00:35 2021 +0000

    Fix interval text alignment

[33mcommit 3c9b850569f02896257dbf1cefd80f28a29ef1ea[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Mar 24 00:00:08 2021 +0000

    Fix exception generating gradients on events without customdata

[33mcommit 456b1ac01a4e70f8e9244921a4263f7d48612324[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Mar 23 23:58:49 2021 +0000

    Catch exception when cancelling spectrogram generation

[33mcommit 0fd35adf896146c40dc48ce7faf21e2e813d87c6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Mar 21 12:29:51 2021 +0000

    Group for reverse to fix duplicated lights in rl

[33mcommit 82282a1119adbc146709d4ca977f2d5585e2fd80[m
Merge: f51ea53f 17d89634
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Mar 20 19:33:36 2021 -0700

    Bug fixes (#249)
    
    Bug fixes

[33mcommit f51ea53ff6332efdd89bcea543b55c5d14886dce[m
Merge: 0a77e387 5ed008dd
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Mar 20 19:33:09 2021 -0700

    Add .wav support (#248)
    
    Include wav support

[33mcommit 0a77e387e95c486ea3b6b61af502b58fbccdaefd[m
Merge: 49e0999f 7fe625a0
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Mar 20 19:32:44 2021 -0700

    Kaleidoscope + new light ID system (#247)

[33mcommit 17d896343b51945bd35663bc001cfa488cfc6d8d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Mar 19 16:28:07 2021 +0000

    Fix bongocat disabling in lightshow mode. Correct metronome calculation with bpm changes

[33mcommit 74fb94ec3331caf98e5da063752cee4a39385791[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Mar 19 16:08:48 2021 +0000

    Fix off-center play view, update tmp library

[33mcommit 7fe625a013c24952f675cc516399c9caa0b04022[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Mar 18 20:31:15 2021 +0000

    Swap laser ids

[33mcommit 1d89e82360e271584832ea5737ffb102242034bc[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Mar 18 20:17:02 2021 +0000

    Move lasers to seperate ringmanager to avoid seperate offset value

[33mcommit 11da2fd63bd56ca82458b58f9689e2be94a07066[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Mar 18 19:11:45 2021 +0000

    Kone

[33mcommit f042d9924117ec74ff686ac92d08e0a4d2acf327[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Mar 17 18:13:28 2021 +0000

    Chroma light id change

[33mcommit 5ed008dd91b3acbe1568eb6b6f074405f318bcde[m
Author: FernTheDev <15272073+Fernthedev@users.noreply.github.com>
Date:   Sat Mar 13 19:22:44 2021 -0400

    Only support explicit audio types instead of defaulting to UNKNOWN

[33mcommit 49e0999ffa389c55187b7c114e22fab8b0efca42[m
Merge: d8f7c41f 08c3d089
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Mar 13 15:06:14 2021 -0800

    bug fixes (#246)
    
    Bug fixes

[33mcommit dfbf6216107f44617a3b57afb5bb0f6e3255f8e3[m
Author: FernTheDev <15272073+Fernthedev@users.noreply.github.com>
Date:   Sat Mar 13 13:44:58 2021 -0400

    Explicitly define extensions and default to UNKNOWN

[33mcommit a927fe482716446093b26d40784082d641cc415f[m
Author: FernTheDev <15272073+Fernthedev@users.noreply.github.com>
Date:   Fri Mar 12 12:36:12 2021 -0400

    Include wav support

[33mcommit 9bb181ba1355663567b5309fb12d7ccd85d5e382[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Mar 10 22:30:54 2021 +0000

    1.13.4 prop updates

[33mcommit e729f40082db0c154bf39d404304e03580d9fbdb[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Mar 10 19:13:17 2021 +0000

    Update environment colours

[33mcommit 08c3d08974d4cdb819fde6250c6b6145a5e1dfeb[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Mar 6 16:49:11 2021 +0000

    Fix renaming bug, undo button not working, moving after playing view

[33mcommit ae15987e58e66dea4b4acba4b466c4332ce2c49b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Mar 10 18:24:56 2021 +0000

    Fix tab after closing options

[33mcommit d8f7c41f2d13e7f5888fdc96b46db1abd8e49bdf[m
Merge: 51056de5 d1e2f1e0
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Mar 3 18:26:03 2021 -0800

    haha bug fix go brr (#245)
    
    Bug fixes

[33mcommit d1e2f1e08a6635c75018e5bd15f7d4655ba7e638[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 28 15:29:19 2021 +0000

    Fix song overwriting, bpm changes not updating grid, undo redo actionmap split, osx folderbrowser fix

[33mcommit 51056de50bea50fabe20f86a763dae4c30ccc12b[m
Author: Thomas Cheyney <github@thomasc.co.uk>
Date:   Sat Feb 27 22:57:28 2021 +0000

    My attempt at fixing audio sync late into songs (#244)

[33mcommit 13a8f914703206587cf31c63a0de13801f112439[m
Author: Thomas Cheyney <github@thomasc.co.uk>
Date:   Sat Feb 27 01:29:50 2021 +0000

    Bounds shouldn't be applied for obstacle precision placement (#243)

[33mcommit c6c712c7d17b1fc02c49016eeb61cd97de64be04[m
Author: Thomas Cheyney <github@thomasc.co.uk>
Date:   Sat Feb 20 23:05:01 2021 +0000

    Apply big offset only if validation is likely (#242)

[33mcommit f6d3e8b8842790709e1a54e5d562cb844bd392af[m
Merge: 760e5a6f 83ad5e1e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 16 20:53:11 2021 -0800

    Bugs fix (#240)
    
    Bug fix

[33mcommit 83ad5e1ed1f53b387249fa1bbb13e1e34f1ef99d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 22:42:30 2021 +0000

    Use get invalid file name chars instead of hardcoded list

[33mcommit f4002aaf8efe2d12a485a1f8e5fdbd51e2c0063b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 17:28:52 2021 +0000

    Fix routine is null errors

[33mcommit 760e5a6f114843dd4c0446c9e348fc996589fc97[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Feb 13 21:36:11 2021 -0800

    Only write alpha values when writing to array (Chroma colors)

[33mcommit 9888f5af77ceb587f4599536034a200ab2a2773a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 01:06:45 2021 +0000

    Use box select constraint code to constrain wall placement

[33mcommit 57f0e699736abd078b11f95c92c19739b0ecc42b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:39:41 2021 +0000

    Fix objects being deleted behind notes

[33mcommit 2390f9df2efe383fde2e1a103c0f026f3c1d3b9c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:34:59 2021 +0000

    Clear input box resultAction after call

[33mcommit 59b0d5fcf9384648a46d411aa39603b42f4c6146[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:33:49 2021 +0000

    Fix song info tab order

[33mcommit bc498765c14d75681437fc832a396f3126600270[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:12:03 2021 +0000

    Apply local scale to extents

[33mcommit a27b5837e32d7d322ac94e17a8c42439379369f9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:11:41 2021 +0000

    Force save message to display for at least a frame

[33mcommit d9877350a3d99747415e98a024d855c682fcad67[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:10:43 2021 +0000

    Fix exception when shifting prop lights off the end

[33mcommit ff6e2fc5b74bbb2532122854c2f0af7c87311608[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:10:21 2021 +0000

    Fix info screen textbox sizing for non-validated fields

[33mcommit 3cafb7b48bfac163f9d564eb1f502d85fa1f5d89[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:05:11 2021 +0000

    Remove more special characters from clean song name

[33mcommit 4740bf336ff3fa4210eb639b6d5e68efa1b09e96[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Feb 14 00:04:57 2021 +0000

    Fix timbaland ring offset

[33mcommit ddb6a66c3e30c89a86212f976b17bbd1cc32099b[m
Merge: 43acc44d 547e9a4a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Feb 13 10:29:17 2021 -0800

    HEY! _reset IS FIXED! (#238)
    
    Bug fixes

[33mcommit 547e9a4aa485bd8e0d44f7d57b0f11b45e9d9c9e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Feb 13 18:06:36 2021 +0000

    This isn't how reset has worked for 6 months

[33mcommit 004d6cb9566bf8fae18b6646442df6669c6be51d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Feb 13 17:36:17 2021 +0000

    Properly remove pyramid collider for hover event

[33mcommit 43acc44de44260aaf2139f5ca6865cefef1d8b4e[m
Merge: 2b6133d5 c553675f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 12 14:45:37 2021 -0800

    Apply WD-40 to tests (#237)

[33mcommit c553675f5ba8742877b02b9b66956d123983cc87[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Feb 12 21:12:00 2021 +0000

    Fix failing test, hide 360 warning in test

[33mcommit 2b6133d504ba284325eeb9b6ade99d8eb57a3d89[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 12 11:47:29 2021 -0800

    fix incorrect left/right rotating laser IDs in big mirror

[33mcommit 6362c7171c842b38442b713751883ca85baf34e6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 12 11:35:02 2021 -0800

    ensure a final event is created at stop time

[33mcommit c4128fe8b78b71c9ae610f179515ee676da11d9d[m
Merge: 35d8e939 d036ec0d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 12 11:30:33 2021 -0800

    undid some jank with actions (#236)
    
    Objects actions refactor

[33mcommit d036ec0d215177ea65d11414385b549ab54572d5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Feb 10 22:59:30 2021 +0000

    PR comments

[33mcommit 6ee9a6375998cc95a7af4a4b396689e867865802[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Feb 6 22:44:16 2021 +0000

    Don't search for objects when deleting anyone, refactor actions to not copy objects 100s of times

[33mcommit dccef4a85fc24a553008a434266c5cfa54924acd[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jan 31 05:23:00 2021 +0000

    Fix blinding gradients

[33mcommit 35d8e939fab3a4ad5d37692f1452fc5f27cd7c9d[m
Merge: 0d3d6624 5cd3e01b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 29 00:58:31 2021 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 0d3d6624b3d3d5a2799d458380bb2f9f5305ddcf[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 29 00:55:10 2021 -0800

    Clear settings notifications on ATSC destroy

[33mcommit 0b1eab97f8aa3006a097ad5f705520f5dcf60240[m
Merge: 7cb0ab6a ef4f23cc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 29 00:44:02 2021 -0800

    Merge branch 'song-volume' of https://github.com/Top-Cat/ChroMapper into Top-Cat-song-volume

[33mcommit ef4f23cc9ddeab216d434632413ef6b06f16727b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 27 18:05:10 2021 +0000

    Song volume updates while playing

[33mcommit 7cb0ab6a73999f80f355cb401107d7aecaf8c33d[m
Merge: 897ac969 9804306a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 25 13:39:08 2021 -0800

    Merge branch 'song-volume' of https://github.com/Top-Cat/ChroMapper into Top-Cat-song-volume

[33mcommit 5cd3e01b613508e822707d8fb654ae32e8c84e56[m
Merge: bdbcb649 71247892
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 25 13:37:39 2021 -0800

    Changes to save notification (#233)

[33mcommit bdbcb6498afc71b978e08866820f24ab20060ad5[m
Merge: 897ac969 d0677747
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 25 13:25:23 2021 -0800

    Ignore chroma 1.x values for boost
    
    Ignore chroma 1.0 values as previous values for boost

[33mcommit d067774702c81d766ddb6d92d838a6fb2eedaea5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jan 24 16:44:11 2021 +0000

    Ignore chroma 1.0 values as previous values for boost

[33mcommit 71247892fd432613a6a25f7f6fd4b8746ee2d6de[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jan 24 13:59:53 2021 +0000

    Save notification doesn't fade, disappears early when save is complete. Options available for other notifications

[33mcommit 897ac969f7dd8a97ae5e2de472719b439283c75b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jan 24 00:19:59 2021 -0800

    prevent placement of obstalces with type 2+

[33mcommit 9d31476d6300f212dff4611b533effaa561f08a6[m
Author: Pixelguymm <73407947+Pixelguymm@users.noreply.github.com>
Date:   Sat Jan 23 21:01:21 2021 +0100

    Only use _preciseSpeed when necessary
    
    Will not use _preciseSpeed if decimalPreciseSpeed is a whole number. Will also change _value to the whole number closest to decimalPreciseSpeed (unless that number is 0, in which case 1 will get used).

[33mcommit 9804306a82a12e0b9a57ab05bd2e908bc3eccb35[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jan 23 19:57:08 2021 +0000

    Add song audio control

[33mcommit ca9d310796551ab3b045f75a0b527af6f34c2e0e[m
Merge: 20ab8b1f 612c0837
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 22 18:40:37 2021 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 20ab8b1f9f7210df6a609229aa4a1ae35865c3a7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 22 18:38:59 2021 -0800

    null check song and directory when validating file

[33mcommit 612c083792803854d32b2504afd273a909c08586[m
Merge: d05534ad f2d655e3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jan 21 12:54:50 2021 -0800

    Experimental DSP settings (#230)
    
    Add experimental dsp buffer size option to allow reducing audio latency

[33mcommit f2d655e318fb9757d07d99eed7da2969f77cc835[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jan 21 08:56:44 2021 +0000

    Remove unnecessary conversion

[33mcommit d05534adbd999745a07db57bc7a8f83bddb445c7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 20 20:17:05 2021 -0800

    fix incorrectly positioned NE notes in 2d preview

[33mcommit 9eb01ad814e26a2174144e0b52022155b523de51[m
Merge: 3fde2f05 8828348f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 20 20:02:41 2021 -0800

    Keybind rejig (#229)
    
    Keybind rejig + assorted other changes

[33mcommit 5eb114d91f6022f6c4d2d151e609071920787287[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 20 14:15:25 2021 +0000

    Add dsp buffer size option to allow reducing audio latency

[33mcommit 8828348f206f39bcb4bd86be66f5bd8b38ad03b7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 20 10:21:37 2021 +0000

    Don't activate diagonal notes or ring rotation when laser speed is being edited

[33mcommit 430f392a4a7a4799db1f246e80ac9662cc95090f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jan 17 13:11:59 2021 +0000

    Keybinds and assorted

[33mcommit 3fde2f056bd2eebae7ff2d96ef6ed362670d54e9[m
Merge: 9d2fdf2f 59fde00f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jan 16 16:17:21 2021 -0800

    Strobe Gen improvements (#227)
    
    Add chroma step gradient easings including two custom easings

[33mcommit 9d2fdf2fce213707561890051cb79a54e88866c5[m
Merge: d46c84af 570b6185
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jan 16 16:13:42 2021 -0800

    360 selection fixes (#228)
    
    Fix selection in 360

[33mcommit d46c84afa03453a17df8e41c6136082f0732c2b5[m
Merge: cd9c54cc bba21a93
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jan 16 16:06:02 2021 -0800

    _rotation support (#226)
    
    Add new rotation field

[33mcommit 570b61857c76f606c97acdb891e0d6a98be7666b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 13 12:50:04 2021 +0000

    Fix selection in 360

[33mcommit 59fde00f4e8c9889df382973bad27096ad7c0c7f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 13 00:21:32 2021 +0000

    Add chroma step gradient easings including two custom easings. Made it way easier to scroll in the strobe generator UI

[33mcommit bba21a9367464cad6f6d7e2539c64ee0ee254603[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 12 23:32:36 2021 +0000

    Add new rotation field

[33mcommit cd9c54cc9797c5250540d20f8fcc6c0b44b71423[m
Merge: 7e2e5254 dc5d9cad
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jan 9 17:19:39 2021 -0800

    More small additions/QOL (#225)
    
    Various features

[33mcommit dc5d9cad62b6a5b1fcec4197843053b8191119e5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jan 9 05:26:29 2021 +0000

    All the mirroring

[33mcommit 2ddded19e43ecbf6578f2e19ef54afbd407ad9d8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jan 9 05:24:30 2021 +0000

    Allow hiding KDA tentacles. Bookmark renaming

[33mcommit 7e2e5254335ff8f05ab67bebe3ca0999e04fb7a8[m
Merge: 60745be1 dba4c42c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 8 12:49:42 2021 -0800

    Fix light id stuff in KDA (#224)
    
    Light id changes / fixes

[33mcommit dba4c42c901a7da56bc20654f76446fb024a867d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jan 7 03:10:27 2021 +0000

    Missed this for light id strobe gen
    
    Add light id offset

[33mcommit 60745be18faa2cf1610412f49efe7323f0218c60[m
Merge: 7578b76b a42159d1
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 6 16:33:40 2021 -0800

    Merge pull request #223 from Top-Cat/bugfix
    
    Bet you didn't see that one coming, did ya

[33mcommit 7578b76bcb2f182812ce7e9b31589b3730f1fc51[m
Merge: 7987a99f efc1d9ad
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 6 16:33:04 2021 -0800

    Add ground spikes to KDA (#219)

[33mcommit efc1d9ad85d18ef9dd58dcf0f271428cb74b0d2a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 6 17:56:28 2021 +0000

    Sort out KDA props

[33mcommit a42159d10c9db037104ef849631902978b4ea9cb[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 6 15:52:05 2021 +0000

    Remove bind helper when leaving scene

[33mcommit 65ed8caca401cd64e6cb2caa5da62d5971edeb36[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 6 15:36:49 2021 +0000

    Revert change to alt+scroll that skips laser speed 4, works for me without this

[33mcommit 7bf07213b608b3bf25c4b3ff6126cd3aa607cc09[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 6 15:35:54 2021 +0000

    Fix escape returning to main menu from mapper view

[33mcommit 000f120129680b684740ff4cc779fd79e5a15c04[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jan 6 15:35:11 2021 +0000

    Ignore null in custom data when copying diff

[33mcommit 7987a99f14c7e58416a635f39252e914fdb79307[m
Merge: 3105090a 3c1f9a79
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 5 22:36:50 2021 -0800

    Add support for NE note scaling (#221)
    
    Add NE note scaling

[33mcommit 3105090a1b337de27dfe6cd8a4bb6f168ef1d66e[m
Merge: c82354f9 8f3a7b89
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 5 22:35:12 2021 -0800

    Gettin' some bug fixing action (#220)
    
    Bugfixes

[33mcommit 3c1f9a7998b1260886f476b573dcb3cbd784dc7d[m
Author: Futuremappermydud <54294576+Futuremappermydud@users.noreply.github.com>
Date:   Tue Jan 5 20:47:09 2021 -0500

    Add NE note scaling

[33mcommit 8f3a7b8911b39b8ee1acd383ee581c1ffc24f084[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 5 22:44:02 2021 +0000

    Allow keys to be released even when input map is disabled

[33mcommit 0dc0adb1fb7fbf45d9d4ed689cad8339adb94b48[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 5 20:49:03 2021 +0000

    Spawn discord hit objects at correct time

[33mcommit 3687ed7f9d6a2f468253d2466676020723b984e0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 5 20:30:14 2021 +0000

    Fix crab rave and monstercat colours

[33mcommit 32f6b4d6991faedd043b3261998f919e37e43a50[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 5 20:18:16 2021 +0000

    Fix monstercat prop ordering

[33mcommit 6a921f165c30b3c43a525e0218d9c28cbab946ef[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 5 20:07:31 2021 +0000

    Fix selection box getting notes to the left

[33mcommit f36c8a14814ea32273a527f1815bff5bc7daa5c6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 5 19:55:20 2021 +0000

    Update current bookmark display when adding or removing bookmarks

[33mcommit fc012f7b35b8dbbf45ecbe061932ff5f9885262a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jan 5 19:51:53 2021 +0000

    Break zoom on monstercat as it doesn't work in game

[33mcommit 1f1b2ab82671988aace5e89d422a7c63d2786f54[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jan 4 10:22:41 2021 +0000

    PR comments

[33mcommit 1efd906649d1999ee37b407cc22df5ca4a290939[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jan 4 00:53:09 2021 +0000

    Add lightid mappings for KDA

[33mcommit 458517c989bead303a265355279174800fe719f5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Dec 31 11:48:33 2020 -0800

    Just pushing this for top cat to take over

[33mcommit c82354f91b2ed98e6ba65fcef3c512a63a5e5ec4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 30 18:58:45 2020 -0800

    Fix NE note positions being shifted to the left

[33mcommit dd558221724cb53f4f1de0ef7c6cc8cac86aebe5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Dec 22 18:21:32 2020 -0800

    Copy difficulty customData as well

[33mcommit a1d1f1cc2023fd6d5f46100bd225a8376bb94b3f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Dec 19 23:21:57 2020 -0800

    Fix alt-scroll not working for laser speed events

[33mcommit 9aeafd8761146d6f167a2d71eacf528878c41e99[m
Merge: 0653ecd5 3a38f7a7
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 18 15:59:10 2020 -0800

    More bug fixes (#217)
    
    Small fixes

[33mcommit 3a38f7a72b39ea394ea6b7499ddfea603e98e63b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 18 16:29:55 2020 +0000

    Fix logo light grouping in timbaland

[33mcommit b981eed836188c9e4dad21812400202b2d7a7828[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 18 14:09:51 2020 +0000

    Fix colours on map with no custom data, hide big bad error

[33mcommit 0653ecd575dad3b8519fe3410fe595a721b55357[m
Merge: b3f4dffa be29bc5e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 16 16:52:26 2020 -0800

    Bug fixes (#216)
    
    Bugfixes

[33mcommit be29bc5e40603dae265d2d1bae88b8c383a2d173[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Dec 16 01:04:51 2020 +0000

    Don't put slashes in zip or folder of songs

[33mcommit 9a3dbe20768d47d31629e2eefa46f8db1b1e026e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Dec 16 00:53:08 2020 +0000

    Null checks in DifficultySettings

[33mcommit b3f4dffa77c4671499f844f9a638d2fcb3d3d7fe[m
Merge: 0e863225 1266fcc1
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Dec 14 13:51:44 2020 -0800

    Switch from TMPro to Text Mesh for event prefabs (#215)

[33mcommit 991824197c1292c7bb23b629a48e6c4eb3339c71[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Dec 13 19:29:49 2020 +0000

    Don't get whole list of materials

[33mcommit 1266fcc1056d9aee3d2a03ed1d521c7999f9c3a6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Dec 12 13:08:32 2020 +0000

    Replace TMP in events with TM for performance

[33mcommit 0e863225b0d80718f8b19f0a220cacf0c13e6e8e[m
Merge: 50e9751d 49051c60
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 11 12:13:25 2020 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 50e9751d247490de4be32e5725db87c673a89e00[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 11 12:11:09 2020 -0800

    Fix problems in timbaland environment

[33mcommit 49051c608074f7638e610f0b2033debbac988b23[m
Merge: c2407b64 241ec16f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 11 11:45:52 2020 -0800

    Oopsy whoopsy topcat did a fucky wucky (#214)

[33mcommit 241ec16f7e3c36808af0c96b4afd94cb809c8bc5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 11 19:40:08 2020 +0000

    Fix event placement npe

[33mcommit c2407b64df668baa5daf6a694124843cf55ce118[m
Merge: 678ca4d0 2e70602d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Dec 10 21:50:31 2020 -0800

    Bugfixes (#213)
    
    Bugfix

[33mcommit 2e70602d23020fd66c65de5903c9b43591ce8be7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Dec 10 22:23:50 2020 +0000

    Better centering when changing UI scale

[33mcommit 03fe0bac97bd9dab8d4dda79aac9b47b09b9be54[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Dec 10 14:27:47 2020 +0000

    Fix UI elements spreading out at high scale

[33mcommit fdac76141be38ec58bf7430f7761aa2b78265d5b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Dec 10 13:22:46 2020 +0000

    Translations update

[33mcommit 409db81ac01761abbedf84d95dcb935b2f076a66[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Dec 10 13:16:48 2020 +0000

    Store rotation event values in side channel to prevent conflicts with normal event value hotkeys

[33mcommit ae3b377454ac43f02db3810ca570137081ffcf0e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Dec 10 12:42:00 2020 +0000

    Apply song speed to metronome. Fix laser speed controller killing other binds. Disable other binds when new text fields are focused

[33mcommit 678ca4d024d0605f95f9abbc0cca3b8f6968afa2[m
Merge: 65c55963 41f68861
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 9 14:25:48 2020 -0800

    Merge branch 'Top-Cat-envremoval' into dev

[33mcommit 41f6886119a0762441df3dc615a9f3f14e1c3769[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 9 14:25:29 2020 -0800

    Font update

[33mcommit dcd39c1b7c772a8db06ca95b95b9fc166eb3f655[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 9 14:25:17 2020 -0800

    Have custom colors update new UI

[33mcommit e2a1598f3ffc7763fee1e478d2aca5f97f87c39b[m
Merge: 65c55963 35571e6e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 9 12:17:17 2020 -0800

    Merge branch 'envremoval' of https://github.com/Top-Cat/ChroMapper into Top-Cat-envremoval

[33mcommit 65c55963f59aaf6ad8e1d6b3e2d3ca31d4e18645[m
Merge: 086f0137 e2a5bc70
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 9 11:57:46 2020 -0800

    Fix merge conflict (added bug fixes)

[33mcommit 35571e6eac70559327b36ec2a053b370347d4b3f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Dec 7 10:07:08 2020 +0000

    Add border when selected

[33mcommit e2a5bc70d4e30c2b6eafb2d7a779073841f02c4e[m
Author: dcljr <dcljr@me.com>
Date:   Sun Dec 6 19:58:06 2020 -0800

    Added swap spectrogram side button

[33mcommit e75e1d48421aeb54d405f0fba7f2ccbc44789292[m
Author: dcljr <dcljr@me.com>
Date:   Sun Dec 6 17:01:21 2020 -0800

    apply english translation for all new string references

[33mcommit 4af952c14dc4601ed8dca4838074e5203aa28a94[m
Author: dcljr <dcljr@me.com>
Date:   Sun Dec 6 16:50:51 2020 -0800

    switch .LastOrDefault(predicate) to .Where(predicate).LastOrDefault()

[33mcommit 9be33e93e17ac601bf4c84b7b95c505c1f943668[m
Author: dcljr <dcljr@me.com>
Date:   Sun Dec 6 16:48:53 2020 -0800

    use LoadInitialMap.PlatformLoadedEvent

[33mcommit fd948008ddf39d6eecac854c593dcc870cc23a63[m
Author: dcljr <dcljr@me.com>
Date:   Sun Dec 6 16:44:14 2020 -0800

    Fixed past notes grid sort order

[33mcommit 348ef70e4134623366a3c61a05716658eb92eebc[m
Author: dcljr <dcljr@me.com>
Date:   Sun Dec 6 16:40:05 2020 -0800

    Moved cursor interval tooltip to just the label

[33mcommit 88d011d8c7a23f0b164b4111cd0c9b1434a6dc16[m
Author: dcljr <dcljr@me.com>
Date:   Sun Dec 6 16:38:34 2020 -0800

    Added color hover highlight to mirror icon

[33mcommit 5ccd1eadf388814bfa19b0ba40c28799b417abf6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Dec 7 00:07:07 2020 +0000

    Add environment removal window in song edit menu

[33mcommit 086f01373e4a26012fe40066f75e40f2b1904688[m
Merge: c2068709 05c4f8de
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Dec 6 12:26:03 2020 -0800

    Fix bongo cat going early (#211)

[33mcommit 05c4f8de8097127ffa8045b949707507fbeef728[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Dec 6 19:01:47 2020 +0000

    Fix bongo boi, don't factor in time correction when scheduling sounds

[33mcommit c2068709eb2b810192a1a4bdc4f26b210c68fda5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Dec 5 23:09:02 2020 -0800

    Remove code that I should've removed 10 seconds ago

[33mcommit 7f399eb7aaf5e00cdff1bedfc190b7156ebdb2a1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Dec 5 23:07:49 2020 -0800

    Use smooth delta time rather than constant

[33mcommit c1aecb5049903d0b539e90a8b73413413f31234e[m
Merge: 9e2a5f6f 320375b3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Dec 5 20:49:35 2020 -0800

    Allow deleting last bpm change, bookmark, custom event (#209)

[33mcommit 9e2a5f6f5dff1c9f8a34a5383af6d0989bd98a52[m
Merge: 0ee0665d 8a0d0164
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Dec 5 20:48:30 2020 -0800

    Schedule hit sounds in advance with dspTime (#208)

[33mcommit 0ee0665dde858cff348461120f9f245e2a5c2fae[m
Merge: 0f1239e7 d7353ed8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Dec 5 20:44:43 2020 -0800

    Autosave bug fix (#207)
    
    Fix errors during autosave clean

[33mcommit 96c0a6da9971d825081d905c41eff97a1cfc151d[m
Author: dcljr <dcljr@me.com>
Date:   Sat Dec 5 20:35:29 2020 -0800

    in editor ui redesign

[33mcommit 320375b363392cd67bf1410cad6d3fbe83aaa547[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Dec 5 16:04:28 2020 +0000

    Allow deleting last bpm change, bookmark, custom event

[33mcommit 8a0d0164fc09fb8934db60f21197a3b42f92f747[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Dec 5 15:55:21 2020 +0000

    Schedule hit sounds instead of playing during frames

[33mcommit d7353ed816c1cefe9a7cc0085b09e38ae454bf53[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Dec 5 13:13:13 2020 +0000

    Fix errors during autosave clean

[33mcommit 0f1239e7bb71d3963a455d248c800cf67d638bc6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 4 16:37:24 2020 -0800

    Forgot to stage this change

[33mcommit 4faf639adf4749dd8edefa422a05b74217d484d4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 4 16:37:12 2020 -0800

    Various fixes and changes

[33mcommit c1d02a73d75d4ff1bab5e70c88ef4f200d40c251[m
Merge: feacd3cb c2875370
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 4 16:30:50 2020 -0800

    Manually fix prop id maps (#204)

[33mcommit feacd3cbadce86fe4fb297f79d7d6d9eca2bdf4f[m
Merge: 5132dbf8 f999aff6
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 4 16:28:53 2020 -0800

    Update first boot menu (#205)
    
    Spruce up the first boot menu, add language selection

[33mcommit f999aff63ea23866fc861fc34661ee5aa6bcf44a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 4 21:30:59 2020 +0000

    Show graphics dropdown on firstboot

[33mcommit 5f5f10d6e9897d30d554de429d1f2a7b0182041f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 4 18:32:38 2020 +0000

    Spruce up the first boot menu, add language selection
    Auto-downgrade some visuals if low specs

[33mcommit c2875370f97c5833db74dd3c1ccba0dd9be96cec[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 4 15:31:59 2020 +0000

    Correctly resume prop lights

[33mcommit 5132dbf88b7159046c894444ef67ecb3c691a1ce[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Dec 4 00:31:26 2020 -0800

    Revert "Create file if it doesn't exist"
    
    This reverts commit ab241ea323d38a57e744548fa583ac7174dcf768.

[33mcommit 29e8378afbce580ac4b5def86a471116b0d2cad4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 4 04:59:23 2020 +0000

    Add back rounding offset

[33mcommit ce3470d2cc7a044ce1f6e812b822e674b00302c8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 4 03:45:35 2020 +0000

    Move Grouping to LightingManager

[33mcommit 485c9aed1ef46dfa211a5026d617e0d18465ab47[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Dec 4 00:40:03 2020 +0000

    Fix mirroring propids

[33mcommit d5e5066ac7793b1abe7b08b7b456273e66711a94[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Dec 3 23:51:26 2020 +0000

    Manually fix prop id mappings

[33mcommit f30db20c913084fec2f65e705a70e92239d3be45[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Dec 3 15:28:02 2020 -0800

    Whoops, fixes to Rocket environments are already covered by Top Cat. None of you saw anything!
    
    This reverts commit 92382b4a1e3c39a71bb9e13aa88dd21020bc0ec4.

[33mcommit b8a93522fbe5dbcf7b97b5ce1b545eb01352d930[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Dec 3 15:25:34 2020 -0800

    Prevent Strobe Gen from removing objects if none were generated

[33mcommit 92382b4a1e3c39a71bb9e13aa88dd21020bc0ec4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Dec 3 15:24:36 2020 -0800

    Fix incorrect light groups on Rocket

[33mcommit ab241ea323d38a57e744548fa583ac7174dcf768[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Dec 3 12:43:48 2020 -0800

    Create file if it doesn't exist

[33mcommit 0fee1b78b2dd55e11fa417755a9e61cab5b70148[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Dec 3 00:59:15 2020 +0000

    Prop id fixes

[33mcommit 19a07eb161469e6354fa81bb717dd0bb293a17eb[m
Merge: 180e4b9d d04ef11e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 2 11:59:55 2020 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 180e4b9d3ff1e4f0346a99fa180d7db9a9938e8a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 2 11:59:41 2020 -0800

    Maximum autosave cap

[33mcommit 9948ba5c070e24d0d1667d58dfe601aee14259b1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 2 11:52:20 2020 -0800

    Add BTS and Linkin Park images

[33mcommit b609e49d24eec6a9982906722ae4c209cf16817a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Dec 2 11:52:06 2020 -0800

    Use different approach to playing hitsounds

[33mcommit d04ef11e429cbef25df94f06d6d8fad022d97616[m
Merge: 8e34bc9e 7f423b28
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Dec 1 12:04:30 2020 -0800

    Put simple block model on a diet (#203)

[33mcommit 7f423b28d170ecdf931386193b004b9055c071b2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Dec 1 14:34:46 2020 +0000

    Slightly squashed model to reduce z-fighting

[33mcommit 8e34bc9e60e3fd80a139f90a9584bd90c6b23e1c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 30 16:34:54 2020 -0800

    Maintain fixed prop ID when shifting between lanes

[33mcommit 4d1b537db449df76d0959548a797b55990ec73cf[m
Merge: f4e16ed8 15e4281c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 30 16:29:20 2020 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 15e4281c0ef68180620e363b97af9e06ae270291[m
Merge: bdefcc9c 2a010645
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 30 16:29:05 2020 -0800

    Prop fixes (#201)

[33mcommit bdefcc9c94b128f1b7a086c7316b3c031a8a48a2[m
Merge: f37c1f5f be358d9f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 30 16:27:49 2020 -0800

    BPM Change fixes (#202)

[33mcommit be358d9f6a911481a324623493fc4cbce5b1e13b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 30 17:50:09 2020 +0000

    Fix metronome not changing on bpm change

[33mcommit 923cdb26689b6e95a2fc970ef72e13e9e40c0e47[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 30 17:49:01 2020 +0000

    Allow interacting with bpm change through its text

[33mcommit ad3176b1863039abdac1a2fa037218a98f5ebf15[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 30 17:28:28 2020 +0000

    Change default release channel to stable

[33mcommit 2a010645aa74d4cf29510bcea42cb4c0e7d00bdb[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 30 17:27:11 2020 +0000

    Fix shifting prop events

[33mcommit 1e96a5f032e87407d7815fe64db63b046d137769[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 30 17:02:17 2020 +0000

    Fix greenday ring prop groups

[33mcommit f4e16ed8cef149cee5029519c276718ef95f8cad[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 30 00:24:54 2020 -0800

    Remove usused array from ye olden days

[33mcommit f37c1f5f457f3463a7f5387bccb11267f505d195[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 30 00:14:24 2020 -0800

    Fix incorrect prop ID assignment (you're welcome)

[33mcommit 249d766a8180d62cd732709f79499e48656e5b4b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 30 00:07:51 2020 -0800

    Remove unused custom platforms (you can load them now)

[33mcommit 372dd582d8acea2a499cbd811cacec022dfcf9d9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 29 23:37:48 2020 -0800

    Improve Big Mirror environment

[33mcommit ec2833e3f32b592b344cb71aea4483bccee8b1d6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 29 20:07:25 2020 -0800

    Update solution file to match changes in #199

[33mcommit 706b51a526039b199448d7739e017f7247aec03b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 29 20:07:08 2020 -0800

    Fix some actions not selecting all affected objects

[33mcommit 20a3a159bc8e9e7040fb7f8bb690e816eba2de15[m
Merge: 65961be3 d88234e1
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 29 19:54:43 2020 -0800

    Project restructure, add tests, break plugins (#199)

[33mcommit d88234e111c6116e69cb6cc98027a17741846a02[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 28 18:45:54 2020 +0000

    Fix build

[33mcommit 0fd65f87e7d1dd7c3eff2e6b5dc0680338860004[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 28 18:24:10 2020 +0000

    Add undo/redo test

[33mcommit fc2b9958551d99d7ef3ab0601bc54461f51d860d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 28 16:59:40 2020 +0000

    Add multi node editor tests

[33mcommit 77d7c7838c03bc410a7c5aa6c92035fd39d5da1f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 28 15:22:01 2020 +0000

    Add shift note test, load actual mapping scene

[33mcommit 3b8e43ce437914a0c873f16bd1d66943e5b04f3b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 19 21:07:42 2020 +0000

    Unit test

[33mcommit 262834e1243a33f49ee7d9135a3f39de53b60f9f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 19 17:03:43 2020 +0000

    Move scripts to main assembly, add assembly for plugins folder

[33mcommit 65961be334a21b627f1db350ea1c70898e2b4465[m
Merge: c34f9a87 002db1fb
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 27 14:49:31 2020 -0800

    Strobe gen performance improvements (#198)

[33mcommit c34f9a87405429b3dc301cb679ce6a5d2aaef780[m
Merge: d1d88ed0 326d8411
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 27 14:48:04 2020 -0800

    Potentially fixed ghost objects? Needs testing (#197)

[33mcommit 326d841129b876f690bdc41740a045a120804dae[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 27 20:30:46 2020 +0000

    Move Epsilon + TranslucentCull cache to BeatmapObjectContainerCollection

[33mcommit 002db1fbffbf1411bcfebd022db961683eb48703[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 27 19:09:09 2020 +0000

    Strobe gen go brrrr

[33mcommit ab495cd80d35ab017c3b0e8f0591369bd1945d51[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 27 11:15:31 2020 +0000

    Fix some notes becoming ghosts when moving in time

[33mcommit 49a4a55489d4e49b07fe6e74cb67ae0c4b26a76f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 27 02:34:28 2020 +0000

    Fix drag causing ghost objects

[33mcommit d1d88ed0193a2de5b8b67e999c31dbc9eded2df3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 26 18:20:02 2020 -0800

    Fix grid being shifted down via Ctrl-H

[33mcommit da0f96faa69a0556c77db6758fb75edcf95fd147[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 26 13:04:20 2020 -0800

    Fix interface grids not being aligned to measure lines

[33mcommit 92df528916f1bab5bf915fcccca47796a8760356[m
Merge: 835b7d7d 7d8f2f47
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 26 11:23:42 2020 -0800

    ...node editor customization (#196)

[33mcommit 835b7d7dc74105e7323112df005bd97029770e70[m
Merge: 46aea8d7 4bb1712e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 26 11:21:48 2020 -0800

    i still havent completely woken up but heres some bug fixes (#195)

[33mcommit 4bb1712e1e43ddfa64608076ee4ba9c94d7689e5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 26 16:58:21 2020 +0000

    Chroma 2.0 for presets

[33mcommit 9c85c404a38a98776bf795ded5d5e81af8cf6d6c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 26 16:18:58 2020 +0000

    Fix event soloing and boost colours before first boost

[33mcommit 6ffb94dee1e5fd0bb20c7b2c4441ebf7a79b2b22[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 26 16:02:53 2020 +0000

    Move gradient error to after the user clicks generate

[33mcommit 69e39091f0e801becb06fe90881d0da7d924fff7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 26 16:01:05 2020 +0000

    Fix colour picker display being incorrect on start, cancel box select when changing prop views

[33mcommit e1506900826d5dfc4b0d5b4db6ee671a4b964371[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 26 15:59:24 2020 +0000

    Fix keybinds activating when typing in colour menu

[33mcommit 7d8f2f47bb677b04f26d605590ecfb4b345639a1[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 26 15:27:17 2020 +0000

    Add text/ui size node editor settings, clean up ui, fix editing off-screen objects

[33mcommit 46aea8d72c23ee3c0c96f6f220450dbcd7aa841d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 24 18:52:17 2020 -0800

    whoopsies forgot to remove reference

[33mcommit 141422a8a80457252dd85e0d99a2f31fbf3db2d4[m
Merge: 5183e48c e26fe834
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 24 18:51:49 2020 -0800

    Multi-node editor (#193)

[33mcommit e26fe834da2d230a9ecc284e57a25c868559203d[m
Merge: 45d559f9 5183e48c
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 25 02:46:31 2020 +0000

    Merge remote-tracking branch 'origin/dev' into multinode-editor

[33mcommit 5183e48c9d6cddd497fa336bd415ada61b238c34[m
Merge: c9713629 3301722e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 24 18:42:22 2020 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 3301722e24b2a16283c9bd1e0b369bdf7e4717d8[m
Merge: 569dc3e5 497c6e8a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 24 18:42:08 2020 -0800

    Remove OSC from CM core (and add more plugin attributes i guess) (#194)

[33mcommit 497c6e8a738bccb2c1d81b18ae81e571b2d766a6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 25 01:53:06 2020 +0000

    Add note event too

[33mcommit c9713629c8e59faa0f543cb20ea01cb473c71f44[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 24 17:52:57 2020 -0800

    Only copy _propID while alt dragging events

[33mcommit cac17517844fec1ac31f2d6ee4c1a77ba5a95b52[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 24 17:52:43 2020 -0800

    Removed the list while im at it

[33mcommit 761341e6a6452ff4126333cee193e0e801d2193e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 24 17:52:32 2020 -0800

    Remove "ignored paths" list since its all calculated at the start

[33mcommit 9c1ccf38eb6df8dc125d7f32d70694c1f4f60896[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 25 01:02:49 2020 +0000

    Remove OSC code from core

[33mcommit 45d559f952e09afbe1b2df36eb05c4bc22b5c6c4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 23 14:57:11 2020 +0000

    Make differing values show as dash

[33mcommit 569dc3e5805856ec541a61eb33860f685e6b6002[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 22 19:10:38 2020 -0800

    more null checks because apparently we need it

[33mcommit 0aec0e8262130c78016770847595c67a4195a6cb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 22 17:57:34 2020 -0800

    Dictionary -> ConcurrentDictionary, BPM Change fixing(??)

[33mcommit 77350d021d88e56adcbf400c1c82c0afb714fba4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 22 23:38:50 2020 +0000

    Fix missmatched array length check

[33mcommit b208865957d079ceedb78313221520c6955d17a4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 22 19:54:16 2020 +0000

    Fix booleans appearing as null

[33mcommit cb3c54c8faac32d1074f4dbdad67e1f08d9f923d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 22 19:40:53 2020 +0000

    Multi-node editor

[33mcommit bc2ee03b553e4df52670204e33ba55b07b220f11[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 21 22:26:09 2020 -0800

    I had a once in a lifetime big brain moment

[33mcommit 559309684a62b30ccf4c6872d89a5e5192055dea[m
Merge: a65d584a 7fda48d8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 21 19:21:57 2020 -0800

    We're gonna go well into the 200s with bug fix PRs, aren't we (#192)
    
    Make transparency cull dependent on user precision and editor scale

[33mcommit 7fda48d833e58b2e46adf7c28285781988e0a0a0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 22 03:10:33 2020 +0000

    Casing

[33mcommit 0394e3f30e88c65468da21a1f18c9f91c7233f31[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 22 02:43:53 2020 +0000

    Fix normal gradients

[33mcommit 501519fbac34f80788558aadef92e362a8912774[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 22 02:12:35 2020 +0000

    Make transparency cull dependent on user precision and editor scale

[33mcommit a65d584a5b1e49941131af9d110c952ddfacd4c1[m
Merge: d59b9727 b8ec9c20
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 21 12:53:24 2020 -0800

    strobe gen fixes (#191)

[33mcommit b8ec9c2014198a96c3cc9a461c1a51b866526357[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 20:37:38 2020 +0000

    Pass propID into StrobePassForLane

[33mcommit d59b9727c71748de9a23455b96c899408cf9a305[m
Merge: 35aafafc 39fdd605
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 21 12:18:00 2020 -0800

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 39fdd605c2ec0b47822b8897a58764764d77be2b[m
Merge: f1480825 7c51461c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 21 12:17:48 2020 -0800

    choo choo (mapper) #190)

[33mcommit a9016a35d9f895e590281bf6017ffdab32025f50[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 20:13:16 2020 +0000

    Split prop ids when running strobe gen

[33mcommit 35aafafc3dd565337b1b40c81263c56a5c8a7a62[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 21 11:50:56 2020 -0800

    hi aalto please test this commit

[33mcommit 7c51461c235d1d79a5216a15307f6824322c7351[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 16:25:13 2020 +0000

    If it's not loaded maybe selecting it is a bad idea

[33mcommit d8de9cf2bcd67ee3c740c116f2a54da6f221872c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 16:10:50 2020 +0000

    Fix random blocks changing direction

[33mcommit f7fa5c4659165378093a53f076c18efdb3c38fc9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 15:34:57 2020 +0000

    Re-enable escape on dialogs

[33mcommit edc8089ebc972f2797688a799ed1e70ee0a051f2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 14:31:19 2020 +0000

    Only allow each class to register a single block on an actionmap

[33mcommit f292feb49e0ab783816f7e04bcaa440c0dd7f816[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 14:15:24 2020 +0000

    Keep bookmarks sorted

[33mcommit f1480825a0696361761e8058083659c17d47a30c[m
Merge: 75ee4825 5095405d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 22:55:38 2020 -0800

    im running out of funny commit messages (#189)
    
    Prop shift optimisations

[33mcommit 5095405df62cdfbe3c55dfa6787a851bee0efc88[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 06:30:20 2020 +0000

    Prop shift optimisations

[33mcommit 75ee48257393595f7311a7ee871cec197852f330[m
Merge: 0983c1b3 e0f389fe
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 20:55:05 2020 -0800

    wait holy shit we're almost at 200 pull requests/issues

[33mcommit e0f389fe7d8a3fecb848a2f4c49244a3f796970e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 04:48:08 2020 +0000

    Fix song refresh

[33mcommit 0983c1b3c488efe432a7cb19a86aa7afaf8cc6f9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 20:18:01 2020 -0800

    change hue2 to velocity™

[33mcommit 66935651140f82accf936424f45abfc98937ad72[m
Merge: dca8e8b1 cee031ec
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 20:07:39 2020 -0800

    i can write performant code (#186)

[33mcommit dca8e8b115d99d8a7dcfd0902def2db6efb02da6[m
Merge: 1a730eaf d80a0f84
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 20:03:52 2020 -0800

    denied his pull request, get played (#187)

[33mcommit cee031ecf116e47d7b524ec70ac341834582dee0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 03:17:41 2020 +0000

    Fix creating past notes forever if no new ones pass the cursor

[33mcommit d80a0f8495da8bc60ec9ea7675ede487c820ef56[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 21 02:10:06 2020 +0000

    Add chroma counterspin

[33mcommit 1a730eaf6229dde2eefc8d991385a5e3c0cd67b4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 17:38:37 2020 -0800

    step gradient gen + localization + fixes

[33mcommit 0127e165542fa0b190f55356084732f375c7add0[m
Merge: 4370211c 9185e4ea
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 16:27:36 2020 -0800

    Contributors UI rework, file browser support (#184)

[33mcommit 9185e4eaee897ba857ff6d303399e92286b1af0a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 23:42:50 2020 +0000

    Move folder icon down, disable escape button while browser open

[33mcommit af6a6f4edf411fb37afa9e8e9c742ab95d6a64c0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 16:02:42 2020 +0000

    Disable validator by default as weird stuff is appearing

[33mcommit daf43f30c5fe22e759301469ed2e76d923255546[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 14:54:50 2020 +0000

    Add extra dupe file check

[33mcommit b89cf5ce4c8006656e6b754516d76923c58b5b18[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 14:48:04 2020 +0000

    Hide random folder icon

[33mcommit 686def424a52dedf2dee129d2e5a7bcadd8da577[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 14:40:41 2020 +0000

    There's a reason those validators were on the text fields

[33mcommit 9108ab8e76dc5a8f358b8d0b63398b7911b5c4a5[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 14:31:48 2020 +0000

    Move contributor editing to SongEditUI, add file browser support

[33mcommit 4370211c730fad961c189262d821c9bef57e026b[m
Merge: e60401c5 47930c9a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 14:19:28 2020 -0800

    top cat fixes my dumbass errors for me part 2: electric boogaloo (#185)

[33mcommit 47930c9a05f1b27acb17e86aec288fa3da060df9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 18:25:14 2020 +0000

    Fix paint selection for events

[33mcommit 71581b6ab7911014f49fc24a31ae469bb1217b51[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 18:08:40 2020 +0000

    Fix null issues

[33mcommit e7b613699a20c51bf23eb690316e0f529b015a01[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 16:34:34 2020 +0000

    Try to protect against floating point precision issues

[33mcommit 7b265c0677156502d8167bcb8240292fdd25437f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 16:28:03 2020 +0000

    Fix grids again?

[33mcommit f636a3707d5d482a6ef1331db28082eb1ac49946[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 20 16:19:58 2020 +0000

    Fix loading wrong note colours

[33mcommit e60401c58a5f8949f07c6592684addd64458a4ca[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 20 00:27:41 2020 -0800

    Forgot my null checks

[33mcommit c337cba972022404d124cce5f003a1b2656ee46f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 19 21:00:12 2020 -0800

    Clear suggestions/requirements if none are present

[33mcommit 60c6bb743ecaf34647d60dfc350abcede6450fac[m
Merge: 0a177c1c 75bb8014
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 18 20:23:48 2020 -0800

    another bullshit unity merge conflict

[33mcommit 0a177c1c3abb1045efdc783be3d648f1fa8774e1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 18 20:23:23 2020 -0800

    I *think* i fixed some ghost object problems

[33mcommit 75bb801464003ed32146860a57cfbb51c53904c5[m
Merge: 45baead2 03094550
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 18 20:21:55 2020 -0800

    top cat fixes my dumbass errors for me (#183)

[33mcommit 45baead2e4bcb361ad2fad7d3aa726f22a56bd8b[m
Merge: 24234071 a8b1bd6b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 18 20:21:10 2020 -0800

    Better Box Belect (#182)

[33mcommit e967402b05497a5ff7706fbb0566ce358383ec17[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 18 14:16:20 2020 -0800

    remove unneeded logging

[33mcommit 0309455064c1e500a29d780e1533cf47c55df6fd[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 19:38:22 2020 +0000

    Show rotation lanes in any diff if events are already there

[33mcommit 02d3a333ec4a865f764d177ab44eb69291e575d8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 16:23:57 2020 +0000

    Sorry linq

[33mcommit a8b1bd6bb0f64935eb4d2c5b05d195806d044721[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 10:18:32 2020 +0000

    Remove unused code

[33mcommit 6e9ffe8dc212ab58e3f14a93853ea8d8ea9024e8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 10:11:49 2020 +0000

    Missing file

[33mcommit 5f53a1c8a8ecfd8bd5c5b8e6c9fb300a22590316[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 09:52:15 2020 +0000

    Move constants to class they're used in

[33mcommit 315a7f95d7661702c835f26840978e78fcf09fc7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 09:50:03 2020 +0000

    Spelling

[33mcommit af9d2fa8356f405e1635993a590fccb3a0ab506b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 09:48:27 2020 +0000

    Selection box PR comments

[33mcommit 0b8d452a62e68f94284227771195a865526bb4f4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 18 02:00:27 2020 +0000

    Make box select great again

[33mcommit 242340713be6093e422c7e19c1faefed5428fb04[m
Merge: e55b6652 3a2946b0
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 17 14:59:36 2020 -0800

    SongCore override fixes (#181)

[33mcommit 3a2946b0c18f54428e5126389b635274503487ac[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Nov 17 13:29:18 2020 +0000

    Move platform colors to new class

[33mcommit ea7aa0435ca848e035e1381523a0253a9e152df6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Nov 17 13:14:31 2020 +0000

    SongCore overrides don't support alpha so make sure these are all forced to 1

[33mcommit 80a4564febf55b013446f13f4a2ab81ccfc8386c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Oct 26 21:40:57 2020 +0000

    Fix resetting colours

[33mcommit e55b6652e117f291636244a8b3c3f4990f9fea6c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 21:04:41 2020 -0800

    Fix cuboid model not applying properly

[33mcommit 3d9160078302885f226c2bb1bdb1af571a32d56d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 19:02:05 2020 -0800

    node editor machine unbroke

[33mcommit cf80dee11f6c5b0828071f660c755c6d4821e563[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 18:17:52 2020 -0800

    Add missing alt-key for top row laser speed input

[33mcommit 772cc626e785d62b6bcde1bfc1789436788ea211[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 18:13:50 2020 -0800

    Collection of fixes

[33mcommit 9f3a010bc0225fe6602b9e2c902c64fdee701d43[m
Merge: 5346bdc0 c3dd3f0b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 17:49:18 2020 -0800

    Surprise merge conflict!

[33mcommit 5346bdc0cac445fcf7afb343321a1876b599d030[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 17:48:18 2020 -0800

    Tweaks and fixes to color menu

[33mcommit c3dd3f0b08335b8720c0a273b7779c525931684a[m
Merge: a52caf9e 26719a14
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 17:40:46 2020 -0800

    Update languages (#180)

[33mcommit a52caf9e07db18acc5f51e3996ec01ffabd59366[m
Merge: 82cc5765 34669a54
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 17:39:06 2020 -0800

    Some bug fixes (#178)

[33mcommit 82cc5765e914766ea5fd14690ade2bb4e9f514b6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 17:35:38 2020 -0800

    upgraded color menu

[33mcommit 26719a144b05087df9993c873aa18fae2d332fb9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Nov 17 00:25:04 2020 +0000

    Update language files

[33mcommit 34669a5467dbe7a4e04ce3aad31737b4c0abb495[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 16 20:50:56 2020 +0000

    Don't create a new list each time

[33mcommit 25ecd711632b4feab17c8ed631d7eccbf5f580b0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 16 16:56:26 2020 +0000

    Half the UpdateMovables calls thanks

[33mcommit 2073dcb48fdadd7fc5c09115b3dd22d808b1932e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 16 16:37:05 2020 +0000

    Optimisations

[33mcommit 617192807142b51c0b2f727d8476ba47789c5497[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Nov 16 08:43:27 2020 -0800

    actually fix bindings this time

[33mcommit d6f225babcefe2aa39bc598cb42c210e09a7a9f8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 16 09:04:03 2020 +0000

    Re-enable tick layer on notes grid

[33mcommit 63775b4636909a94b81d7838a0367e8127154c07[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 16 09:03:21 2020 +0000

    Move BTS logo to back lasers

[33mcommit 41a0818feb7517f442b8e1eb81b405aa88d13d39[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 23:28:29 2020 -0800

    Collection of fixes

[33mcommit 07e68f8b614b2af0a28b3260d7327bc0ae219088[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 22:54:53 2020 -0800

    Stengthened sync correction

[33mcommit 8410b3ff383b83f7d395181d9fe960d098dc007f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 21:25:43 2020 -0800

    Applied same fix to changing obstacle duration

[33mcommit e02648b62c99351d37321786b48f2fbb130b5601[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 21:24:17 2020 -0800

    Fix missing alt key for event tweaking

[33mcommit 914d93fda8ab66d4fe6af38e583005bb6d382c2c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 21:01:23 2020 -0800

    Add some sync correction

[33mcommit a6dcfe8d2e1d760b9d84666a9aeedcb26b52ad53[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 17:38:06 2020 -0800

    Smooth playback by using deltaTime

[33mcommit 1b7ec2d206ada885e764513f068ab67a3b8385b9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 17:18:11 2020 -0800

    Forgot to assign something for Lightshow mode

[33mcommit e3792b2142bf6ca5cf1f1e34e0e3c0cdf805e98a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 17:07:07 2020 -0800

    Grid tweaks and fixes

[33mcommit 732d811e4b3a093cef9a56607319b298e27bda01[m
Merge: 35716a9c db8fc180
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 16:48:13 2020 -0800

    Refactored lanes (#177)

[33mcommit 35716a9ca8641d62cc3db60c38c160b771a5494c[m
Merge: 2a62f86a 0657d5b8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 16:40:09 2020 -0800

    Make UI Scale not a pain in the ass to change (#176)

[33mcommit 0657d5b8cdeeb77e22874e51877aebdb7f26882a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 16 00:38:23 2020 +0000

    a higher number, like 10

[33mcommit 2a62f86aaa1ddd8a7967cb598a159b631f6e064d[m
Merge: 6939b64c 840dde5c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 15:58:40 2020 -0800

    Use dithering shader for notes (#175)

[33mcommit 6939b64ce907e4bd17d50b18162c293886a26868[m
Merge: 1e7ba8e5 4d4e1029
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 15:38:27 2020 -0800

    Add new keybinds for diagonal notes (#174)

[33mcommit 1e7ba8e5ca3d3482e0e969d619ceff3c6267cf30[m
Merge: 42993e51 759de59a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 15:36:49 2020 -0800

    Remove lasers from Dragons to match base game (#173)

[33mcommit 42993e51c32d5c68bf5ccd82d147ac4f5cc1666a[m
Merge: 255492b4 23586ffc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 15:31:40 2020 -0800

    Release channels (#171)

[33mcommit 255492b49e9bb6b21dfe8cf76ad28d7cd6caf91b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 15 15:24:55 2020 -0800

    Fix laser speed keybinds stacking over direct input

[33mcommit db8fc1805f75ec0ac2f3b0e0edb481a314bff3f0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 15 23:05:08 2020 +0000

    Move class to own file

[33mcommit 1091362f8109585450f5c54f57f6231053fb08a2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 15 23:00:06 2020 +0000

    Refactor lanes, hide 360/90 when not in a 360 map, fix shifting events off the side

[33mcommit 52851d0b65c3987c05a9992742000a59782ea102[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 15 17:19:10 2020 +0000

    Don't apply ui scale until sliding is complete

[33mcommit 23586ffc791ab0f14c9921c77896776dda0acc53[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 15 16:18:13 2020 +0000

    Left wrong panel enabled woops

[33mcommit 9e666f6e0c80af3735c459641792d86713813946[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 15 16:09:57 2020 +0000

    Add validation to release server option

[33mcommit 4d4e1029a149487a66885153007a5876e8210428[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 15 15:16:54 2020 +0000

    Add additional diagonal note keybinds on numpad

[33mcommit 840dde5c00ce50971ce5a9ef51805d55f4d1480d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 15 14:53:44 2020 +0000

    Add note transparancy via dithering

[33mcommit 759de59a9d2853dfe976386cf15ecff64ba2eeec[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 14 00:46:36 2020 +0000

    Deleted lasers from dragons environment

[33mcommit 3b3b7511a2853ec5509c48b42f3980243d05a730[m
Merge: 2fbe2265 afd868b5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 13 16:21:07 2020 -0800

    BTS Environment (#172)

[33mcommit afd868b52e20712491473ac18047bc13ee498241[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 12 21:15:14 2020 +0000

    BTS Environment

[33mcommit 2fbe2265d4b5d2b9fe9d646d3372a41723bb8b0b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 12 10:39:54 2020 -0800

    Add "support" for new _waypoints object

[33mcommit b3a188f915769d62185eae2575ecca58539cd7e7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 12 13:02:00 2020 +0000

    Add localisation for release server

[33mcommit eff75729002cd7d420d8c165de873172a032700d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 11 19:03:30 2020 -0800

    Stop rapid Ctrl-S spam from causing access violation errors

[33mcommit 31e8f6eae064283c868dc852154ff4afa29a4b0e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 11 17:16:38 2020 -0800

    Node editor machine 🅱roke

[33mcommit 1c6bcdc69d2157cd8fe3ff5c17ffdcdf701b1fa5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 11 16:53:51 2020 -0800

    Small optimization

[33mcommit 71d58c37262195a7fc337db1da9670fd29e54d2f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 11 16:52:29 2020 -0800

    Touched on input action disabling/enabling

[33mcommit b583bc15f4134de48dbf12d39a828c2088e03825[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 11 16:40:17 2020 -0800

    Small cleanup, still probably unstable

[33mcommit aeb370930554b3ebe5d5b1706b8db875fac0e3bc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 11 16:37:54 2020 -0800

    Input overhaul (should fix a lot of keybind conflict issues)

[33mcommit 7be8f58ddd7ea54f5bebc67c381b052d817b10c3[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Nov 12 00:14:34 2020 +0000

    Add release server url

[33mcommit 2ef267ac18fdc783757e031691b34c193244cce7[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Nov 9 20:49:07 2020 +0000

    Allow launcher to specify it's location instead of relying on hardcoded location

[33mcommit b0dee0e9bb18c91a1d84a61afa246e04c8c950ff[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 8 12:01:58 2020 -0800

    Properly read _beatsPerBar and _metronomeOffset from bpm change

[33mcommit 4125f7c32d1b6ce64328118ff4d5c755ea538229[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 8 02:34:04 2020 +0000

    Make it a button

[33mcommit 1cc7ee50c87caa7c9a0658f690dc4425abd6bfee[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Nov 8 02:04:24 2020 +0000

    Add update available text to song select screen

[33mcommit b8218c2d25bdd3337b11faaee79c25eed6af5d75[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Nov 7 23:59:21 2020 +0000

    Can rounded corners chill out?

[33mcommit a078a25cbf2db452f4cbe2056c0e4530191f3c27[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Nov 6 16:34:04 2020 +0000

    Add release channel dropdown in experimental

[33mcommit b168df76f81b5f4c76352965f83b090616a8abc2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 7 14:25:30 2020 -0800

    the other pyramid fixes i forgot to push yesterday

[33mcommit be27fbdd214fc4379fac48d3b593669a7eb4effb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Nov 6 12:08:11 2020 -0800

    i for one welcome our new pyramid overlords

[33mcommit e366ef47b81bbd36b49335138f22ea2cbc5c9c98[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 5 11:34:35 2020 -0800

    Attempt to forcefully recycle containers if object is not loaded

[33mcommit 7d5f89dad4167d390a8e83ceea93bb468eca122e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Nov 5 11:34:18 2020 -0800

    Big brain: Round ALL numbers when writing to JSON

[33mcommit 22991788071f10d147fdf7026488f5d564e90404[m
Merge: 7ac36b7a 214a080a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Nov 4 11:26:13 2020 -0800

    Normalize rounding with Chroma code (#170)

[33mcommit 214a080ae8eda214974fb2c8a95edfe310d73b7b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Nov 4 16:28:38 2020 +0000

    Change ring update loop code to match chroma

[33mcommit 7ac36b7a6a0433056ed46f166849fd71c82ab86e[m
Merge: 050a0e96 c2b83e67
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Nov 3 08:50:19 2020 -0800

    Poked text alignment until it worked (#169)

[33mcommit c2b83e6729ee15f253b64fa7cfc513b4306385e8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Nov 3 16:35:54 2020 +0000

    Fix text alignment in song edit menu

[33mcommit 050a0e96f43de08aa719f06739dbd3451a4b895f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Nov 1 09:28:51 2020 -0800

    always calculate ME before NE

[33mcommit 1ff8b95f8de99ad90ee8111fddacf8ff3ce11445[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 29 12:08:46 2020 -0700

    Add support for "importing" MM editor offset

[33mcommit f6d8c808bdb76b3a2372411ff5dc41e7d5395005[m
Merge: 223b9faa 4baf40ad
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Oct 26 11:45:50 2020 -0700

    Add _speed property, apply new properties before multipliers (#167)

[33mcommit 4baf40ad15fbb1412a968c8e2497c849bbbe3f00[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Oct 26 17:36:32 2020 +0000

    Add speed property, apply new properties before multipliers

[33mcommit 223b9faa1da3ce990532b31cc09bffb1d3383700[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 25 22:46:52 2020 -0700

    Have camera load in on first user-defined location

[33mcommit db97fe1d27168f10caa6fcef2f9b59961f75618f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 25 22:43:20 2020 -0700

    Set light material to render only front face

[33mcommit c4acff0c1d09238c326311c874553926944ce5ab[m
Merge: 09866b08 388a8cfc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 25 14:31:26 2020 -0700

    @Rabbit location hotkeys (#164)
    
    Camera location hotkeys

[33mcommit 09866b08db3049b69025a029490badd8101dc5dc[m
Merge: c96305fb c7a4a717
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 25 14:28:17 2020 -0700

    Even more bugfixes (#166)
    
    More bugfixes

[33mcommit c7a4a7170d35b26e803cce7433408a5733417e56[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Oct 25 15:13:32 2020 +0000

    Maintain previous selection status when using alt+click to move notes

[33mcommit 93a8489e24ca6434b6095eae85cbebd2a2feb484[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Oct 25 15:05:50 2020 +0000

    Refresh window angles when using keyboard to move notes in time

[33mcommit 4cda04ee56edfca3b6e3d54264fb098016ac3ad8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Oct 25 15:03:17 2020 +0000

    Use IsConflictingWith to delete LoadedObjects too. Fixes some undo issues

[33mcommit a251b301323a8b51ce0ec22aa232798b66fd91be[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Oct 25 14:30:05 2020 +0000

    Don't start a drag if shift is held. Alt+Shift+Click moves the timeline to a block

[33mcommit c96305fb8c947392fcc554edf4971ceb895ed401[m
Merge: 6fe0bdc1 a160b809
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 24 22:57:59 2020 -0700

    Various bugfixes (#165)

[33mcommit a160b809c917fbb50b31faaf0c5a7475813276a6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Oct 24 14:28:53 2020 +0100

    Reset last note time for tick and visual feedback when playing/pausing

[33mcommit 9b928fd539e305fe02befeae7fbd22574ba22e52[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Oct 24 14:14:21 2020 +0100

    Fix waveform moving on pause

[33mcommit 388a8cfcbe9f2ed24ea7c0949f5c1b71288e0e45[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Oct 24 13:05:53 2020 +0100

    Camera location hotkeys

[33mcommit 6fe0bdc169d6f273a9bce8ac4f44bbb960855b47[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Oct 12 15:59:45 2020 -0700

    Fix some visual errors with fade/flash and boost events

[33mcommit 6d98d48e9aedc0aabdfeb1d8bceeca63811d6624[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 10 12:57:27 2020 -0700

    Fix slight offset in bookmark position

[33mcommit 639a9e4f53541443034005f6c5a9d654e4ab0491[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 9 23:23:55 2020 -0700

    Fix text fields going wack (unity wtf)

[33mcommit 52bd51f91151759a401dbfa1c2764e1d78e90ccb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 9 23:16:19 2020 -0700

    spectrogram preview in song edit screen

[33mcommit e39d0fe499f539763dfdb87533e53cfe69852d5f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 9 19:34:53 2020 -0700

    various fixes

[33mcommit e5a0a38f3e1a6bdf7dfcf09676155503708eb2b1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 8 15:49:58 2020 -0700

    Update languages, add UI Scale option

[33mcommit 3ea3151dad33bcdc74b894ac07e409c53f7d7ed6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 8 14:51:51 2020 -0700

    Subdue obstacle color intensity when values are above 1

[33mcommit 24bd177c480f228e9e66477f24b834e57c692239[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 8 12:21:18 2020 -0700

    Improve UI scaling on 21:9 monitors

[33mcommit 5359b6d6977b89d186ad9c7a26691aa40147e44d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Oct 7 15:59:37 2020 -0700

    Swap GreenDay colors, add Linkin Park boost colors

[33mcommit 01bc6d16bd299c31aa68fb627aa16cd702a6311c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Oct 5 23:43:05 2020 -0700

    Do more null checking on event customdata

[33mcommit b6b963fdce95cbe1a86de4b8515d6074df47e665[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 4 11:39:31 2020 -0700

    This Bug Has Driven Lots Of Coders Completely Mad. You Won't Believe How It Ended Up Being Fixed

[33mcommit 4c7106b00f05633dc6073ccb25671f93e4f52866[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 4 11:35:54 2020 -0700

    wait fuck

[33mcommit e9c955ab910e6fee81f4aa01e950715d77c11bf7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 4 11:34:58 2020 -0700

    Never Before Had A Small Typo Like This One Caused So Much Damage

[33mcommit 1fa592a79324d4be959fbd6b54a96d6c3e03c067[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 4 10:12:46 2020 -0700

    This Was The Most Stupid Bug In The World, Fixed In The Smartest Way Ever

[33mcommit d7a955e6cb1855fef18e89179a5fe466708d995c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 3 13:29:13 2020 -0700

    Fix spacing issues with UIMode

[33mcommit 15ab699ea3b030045f9e9d42955be7a569bc8560[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 3 13:06:22 2020 -0700

    Tweak Strobe Gen UI to better fit localized text

[33mcommit 1684183c11c99bc3e2e7d8ff70d3803be0521e00[m
Merge: 8ef3a43c 335af3b3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 3 12:41:39 2020 -0700

    Swings per second (#162)

[33mcommit 335af3b3fee290f3c4007c77c4ccaf87069135ec[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Oct 3 15:05:01 2020 +0100

    Add SPS calculator

[33mcommit 219c71b1cbe33aca9904937fe78b99ae1940d188[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Oct 3 14:57:32 2020 +0100

    Fix NPE if saving diff before song

[33mcommit 8ef3a43cf5bb7d5d04e42a39d5aa18c63b22e4c0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 2 11:56:52 2020 -0700

    custom events can now be deleted agane

[33mcommit 9f951a8b9d96296fa5475df1d0afc8751c7c0a32[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 1 23:54:05 2020 -0700

    Fix Chromatic Abberation setting not applying when entering editor

[33mcommit 8ba48a588475a987b5452aae4091be7eedec5099[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 1 22:08:09 2020 -0700

    Temporarily swap precision placement to Shift + Alt + Click

[33mcommit 6d313ba50b7634495bfb6a53904495dbf62e9396[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 1 21:01:17 2020 -0700

    Optimized song list searching

[33mcommit 8444b00adad7cc958b4ac52301eec474e0ee4e23[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 1 20:45:30 2020 -0700

    misc. changes

[33mcommit b7e3909b634f6eeede15bd8ddea66f98ebb0a6b5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 1 20:45:09 2020 -0700

    add _step and _prop support (aalto just creamed)

[33mcommit 8175a11739387eda8898ec879f5ecec1a4d94442[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 1 15:25:03 2020 -0700

    Fix events triggering early when using Slice sounds

[33mcommit 2e5a79187cb49614f5b4f9e1fae79d45c4b79517[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 1 14:30:48 2020 -0700

    Move pause menu options to Options scene

[33mcommit 95cf8d7f7c441266e81db8c2ac639eca25f7d8b2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 30 15:20:31 2020 -0700

    Remove useless wall of text

[33mcommit 7b3a1679a9d3161ab132fddb71892b8c260b4697[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 30 15:20:02 2020 -0700

    optimize rotation callback

[33mcommit 86bd653934e7bc55c1d6502142b4718b88453931[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 30 15:12:11 2020 -0700

    Fix jitter w/two rotation events at same time

[33mcommit 864665be81e6f9547e94a9215aba098366fef958[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 30 14:37:48 2020 -0700

    Update languages from CrowdIn

[33mcommit 02c5d0837ab5443d2c2f753d7685d07c3506b1d4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 30 14:35:00 2020 -0700

    Automated process of retrieving Patreon supporters

[33mcommit 3fd1a285dc9cadb241b359bbdca7defecaf85ee6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 30 12:07:45 2020 -0700

    add Editor Coroutines package

[33mcommit fb825017f8513779c51e56042383de9d9fad4c58[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 29 16:34:35 2020 -0700

    add rabbit viewer tick sound

[33mcommit 4c1770fe4cd7442a5106371c552f9df5458721a2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 28 17:42:57 2020 -0700

    More fixes

[33mcommit c8f8bcbfe9f5386d21c6d2e595223db3894cb2d8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 28 17:37:33 2020 -0700

    Disable ISelectingActions in node editor

[33mcommit 4e6174ad518128873a5d83156e7a31d320af0f92[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 27 18:43:48 2020 -0700

    unlocked decimals for NJS and Snap fields

[33mcommit b446c10e48555c4a80447263823ceb336ab3b068[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 26 11:56:51 2020 -0700

    Fix map scrolling while mouse scrolls over UI

[33mcommit 90e6b376fa9789d7458a02b6ee509aaf4a823431[m
Merge: b6c05ce6 299848aa
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 23 22:46:14 2020 -0700

    Fix metronome drift (#161)

[33mcommit 299848aaecd129970c3df6b1074679b4a46af0b4[m
Author: Splamy <splamyn@gmail.com>
Date:   Thu Sep 24 02:07:28 2020 +0200

    Fixes metronome drift

[33mcommit b6c05ce652c3e2b0a879cd655cf6e95b409ec198[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 22 18:33:24 2020 -0700

    Fix incorrect laser speed based on non-linear easing

[33mcommit c176db2ce903bd25a79e88a711513a06ebaf40c3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 21 23:00:37 2020 -0700

    cherry on top

[33mcommit b3451ecfcf5792345aa5c9f7957931ef784ca9e4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 21 22:55:42 2020 -0700

    Update languages with new stuff

[33mcommit c416a303ec54080da91e08804bb08d232a534fda[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 21 22:41:47 2020 -0700

    Fix issue w/laser interpolation

[33mcommit 379f4f9f3bfb60b7c1c53f34778eee866aa96441[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 21 22:27:17 2020 -0700

    Update languages from CrowdIn

[33mcommit 14b0d39d90cb65ca5ce2fd63b734c39cb06d5402[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 21 22:21:26 2020 -0700

    i think im gonna make aalto cream with this

[33mcommit 57e72df69dbdbfe4941c2c098213dc0367529210[m
Merge: 3c81149d 309aafca
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 19 23:52:28 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 3c81149d66e9b8ec72fd997d283a7ff63d2a463a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 19 23:51:23 2020 -0700

    Perhaps some node editor fixes

[33mcommit 55d7b1e56b4c7e27e8a0da2f1823171a22b6d177[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 19 23:51:15 2020 -0700

    Do not regularly clear map _customData; preserves "_pointDefinitions"

[33mcommit 309aafca09eb06c34af379e30ab10631c9ddc247[m
Merge: a439c241 0b5d4582
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 13 22:41:28 2020 -0700

    Fix select-between bug (#160)

[33mcommit 0b5d4582e9747ca2ac4f0199766aa0b73c2e33f6[m
Author: dcljr <dcljr@me.com>
Date:   Sun Sep 13 17:01:10 2020 -0700

    fixed selection bug

[33mcommit a439c241c8d57b96c521607d796ec29d945cee47[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 8 22:31:58 2020 -0700

    Filter out NaN time values

[33mcommit 69b219a214e421dc8a4d988022e395b1aa439246[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 8 17:39:15 2020 -0700

    Improved visuals with Boost Colors

[33mcommit 5f0f3cf8b22fcedd4a522c88398e27f0f921d56c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 7 09:55:18 2020 -0700

    ree

[33mcommit dbffa36c62fe9f46b2c6d40ed081d5b42bb5d56a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 6 12:35:27 2020 -0700

    third time is the charm

[33mcommit 5c989a8862bf6f9325ed634888547bca72b6d957[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 6 11:54:28 2020 -0700

    nope, actually fix it this time

[33mcommit c8bf72fe46dbcf5e8bededcc16c339e18d21b32a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 6 11:42:18 2020 -0700

    Fix wonky gradient behavior

[33mcommit 047d63bd1a6bf5d28deaf995290f068bb01dc23b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 6 00:34:57 2020 -0700

    implicit null checks can sometimes go fuck themselves

[33mcommit e3c1383edcdb8c9a0c82eee050f5b89f9c02fff3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 5 22:40:42 2020 -0700

    Forgot context.performed check yet again

[33mcommit 58f25f99e34cec75cae6c79646f1982b970f91c1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 5 22:36:32 2020 -0700

    Add new localization keys for chroma easing

[33mcommit 6a437d38a1a15594396102508cfa2cbeff9aaed8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 5 22:26:03 2020 -0700

    fix en-PT, add basic UI for separate chroma easing

[33mcommit 25b152b018bcfd421a685dec82981117b4150d02[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 5 22:24:48 2020 -0700

    Rewrote Strobe Generator internally

[33mcommit 598ea9d555cfbd44de226fb686c3d274b1207bab[m
Merge: ad156479 0877cd06
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 5 22:22:21 2020 -0700

    More bug fixes (#159)

[33mcommit 0877cd06d2f7ebb5dfb6a42986faabe3d091755c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Sep 6 02:08:12 2020 +0100

    Improve scrolling with BPM changes

[33mcommit 5bc25b70075debcead38371e782a4e43ce390e5f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Sep 6 01:40:29 2020 +0100

    Change how inverted lights handle boost colours to match game logic

[33mcommit d6d273ce3f37afc76fe88c0e88d43c66cf88cb92[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Sep 6 01:40:03 2020 +0100

    Make gridlines better match with numbers that have repeated factors

[33mcommit ad15647908fe607f4a4161b41bd63d3df7597113[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 4 18:30:15 2020 -0700

    im pretty sure this caused CM to sort notes incorrectly

[33mcommit 4b9672fbebd1e6895a1eef680552c283e517ff78[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 4 18:29:55 2020 -0700

    add more detection for NE requirement

[33mcommit 139bc141435ecbe5cddd039ad7674d1dc38d29d8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Sep 4 11:33:39 2020 -0700

    Remove Custom Events warning as they no longer break BeatSaver schema

[33mcommit f4049faa1da064bcae0d21a3e9cc8be2c6895ae8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 3 20:53:38 2020 -0700

    found out that FirstOrDefault() gets absolutely creamed by Where().FirstOrDefault() and Find()

[33mcommit 36479c1e10c0e98ed5af9bd6acd6d5d92cfdc747[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 3 15:12:06 2020 -0700

    You are a pirate!

[33mcommit df250370330e4148d7647a242d25fafaf0f19f3a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 3 15:06:29 2020 -0700

    did the impossible: added owospeak as a language

[33mcommit 7bea7e1b5771efe9bacd2cbd0187b9fdc687637d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 3 14:40:09 2020 -0700

    fix orphaned tables

[33mcommit 7f40accda39c5296950565cc08633ee61fe2ba1c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 3 14:15:24 2020 -0700

    AAAAAAAAAAA FIXED NOTE DIRECTIONS

[33mcommit e2f9ae02d88922fb13f0daebaf9d93a3ca3d90ad[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Sep 3 12:56:39 2020 -0700

    Added RU, DA, DE, ET, and FI as languages

[33mcommit 0cf77b90e696dc9ac297d4418566af1000514a2b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 31 12:23:05 2020 -0700

    Small changes

[33mcommit 9859f52b5e10dc2d6be2814e2630c7206f52dcea[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 31 12:22:55 2020 -0700

    Song time display goes to milliseconds

[33mcommit f14d1969a2ec473060fdd8747cb334e6616ff4e0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 31 12:22:35 2020 -0700

    fix track name in JSON

[33mcommit affc229ddf899ce99f949209f43269dadf1bcd8c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 18:53:32 2020 -0700

    Update langauges

[33mcommit ab5c8ff232ea6ad1317debb547388814eb995023[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 18:47:45 2020 -0700

    slightly unfucked custom event placement

[33mcommit c52e5cc2c1b79f964d6387dfcc38fe6c6b2608bd[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 15:39:38 2020 -0700

    Fix wonky custom event placement

[33mcommit 4049d2549b08cac131faac4f4ee2d9b723b42d5f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 11:16:56 2020 -0700

    Hide node editor TargetInvocationException errors

[33mcommit 876a66e4af1f575e34adf3ccdcd322e0fd144cca[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 11:09:18 2020 -0700

    update localizations

[33mcommit 11df1fe97b5583d7ee2e9b9240929efd80429dc4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 10:54:26 2020 -0700

    Add Custom Events setting

[33mcommit 7d65ee9ab0f9ad8a282509cf6b972315add256ab[m
Merge: fd26b77f 1fc7d64a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 09:51:20 2020 -0700

    Add Linkin Park (#157)

[33mcommit 1fc7d64a8ffb098e527be8a2bb9c9fe54998a6c4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 25 13:17:32 2020 +0100

    Store rotation speed with multiplier

[33mcommit 3c8e0026f39089ab8715670537eecc381c59026c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 25 12:30:52 2020 +0100

    Use left laser speed for ring rotation updates

[33mcommit 01edde403224c9463d66dbbf45b0899f09185021[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 25 12:03:37 2020 +0100

    Rename abstract classes, fix cases where lasers would get misaligned

[33mcommit fd26b77fdd4339f257e4aea2a212c6e6a26f9ddb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 00:17:01 2020 -0700

    Make AudioManager.IsAlive thread-safe

[33mcommit e1fab1e872ae4365f5fafeb5d8ab74b1d899cfd6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 25 00:14:50 2020 -0700

    fix box select unselecting despawned objects

[33mcommit 3c4d87d76dab1183b8fe3d66ddee4f39b3089928[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 25 00:34:02 2020 +0100

    Linkin Park Environment

[33mcommit be9040a7401287e3605fe85cd78a1d718db673a2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 24 15:54:48 2020 -0700

    fix note cut keybinds getting stuck

[33mcommit 7b3d6e852d921e18b15946d5aff7c20282534cd8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 24 15:23:30 2020 -0700

    round colors to 3 digits when writing to JSON

[33mcommit bd7964d70af16265ada29c7851648050b58e0d5d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 24 15:18:28 2020 -0700

    Misc. changes

[33mcommit 4b264267d483c1752bfe63e1726033989765f7e9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 24 15:18:22 2020 -0700

    Move Beat Saber shaders to _Graphics folder

[33mcommit f0c2ec25084e47a24344e2573d8bcc83a8ac411b[m
Merge: 6c204e0b fe944603
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 21 15:16:15 2020 -0700

    Fix 2d spectrogram shader (#156)

[33mcommit fe944603b7d50440fee2af9253bd5675b443e45e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Aug 21 23:07:56 2020 +0100

    Force 2d spectogram shader to be included

[33mcommit 6c204e0bbe0a7845a2435bbb56dfe5ee607f407c[m
Merge: a2b112b9 cae2cd8d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 21 15:09:53 2020 -0700

    Merge branch 'Top-Cat-boost' into dev

[33mcommit cae2cd8d0e746594c356ed6ceeabe8937acf5357[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 21 15:09:32 2020 -0700

    fix inputs not loading

[33mcommit 84a3c076843f39cbb89eb30084fa3212f1f99d87[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 21 15:01:07 2020 -0700

    fix playback breaking when playing w/boost

[33mcommit 5319a8d4189d3faba57e468233abf852f38b2105[m
Merge: a2b112b9 b6027c33
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 21 13:47:58 2020 -0700

    Merge branch 'boost' of https://github.com/Top-Cat/ChroMapper into Top-Cat-boost

[33mcommit a2b112b9131be9181e0ecded45d860ac3484d5bf[m
Merge: 11c4ab65 512ef414
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 21 13:45:59 2020 -0700

    Use note position for conflict check (#152)

[33mcommit 512ef414cf3fb4396b13972662f0457bf153841a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Aug 21 17:36:44 2020 +0100

    Reduce conflicts to 0.1

[33mcommit b6027c332b7847668011924e1c4bb3a4176482c0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Aug 21 17:17:04 2020 +0100

    Add support for boost colour event

[33mcommit 11c4ab653f817b741449334351b312844a95ede9[m
Merge: efa5b7a6 6c7c8a2b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 20 21:49:54 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit efa5b7a603096433943358c5b20e7552d0175b76[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 20 21:49:49 2020 -0700

    BPM Change related fixes

[33mcommit 6c7c8a2b8917203e43371f8ce3abe8b78429835f[m
Merge: 0e2e7397 79f45a50
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 20 21:48:38 2020 -0700

    Various fixes (#154)

[33mcommit 0e2e73974bbaa39087b1019fdc9f44e9880b224a[m
Merge: e7218a4a 387982b8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 20 21:47:25 2020 -0700

    Cursor highlight for 2D spectrogram (#153)

[33mcommit 79f45a507c2eeceaf3386a32e52c84830c83734b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Aug 20 12:30:25 2020 +0100

    Fix grid rendering being reset when pausing or change ui modes

[33mcommit 503d6121ac90e041f277e6745084aa0a1ba1ee81[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Aug 20 12:11:42 2020 +0100

    Reduce legacy long scrollbox in pause menu, don't allow scrolling when paused

[33mcommit cc8d5c3c90068c69458540359b604ced5e56ed2b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Aug 20 12:09:55 2020 +0100

    Improve input box ux, fix clicking on timeline

[33mcommit 387982b877f80ab3d1d300697914e68006be8338[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Aug 20 09:10:36 2020 +0100

    Add cursor highlight to 2d spectogram

[33mcommit f85489d29ee0a3936828cbb4ce6eb07345136b27[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Aug 19 15:23:11 2020 +0100

    Use note final position to determine if they conflict

[33mcommit e7218a4a9c0ae8c53f012e6d58df730e1c2e519e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 16 20:56:31 2020 -0700

    Improve High Contrast Grids

[33mcommit b6359283ea9064b20454cf996079458453c6d6aa[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 16 16:49:19 2020 -0700

    Update languages

[33mcommit 814e5552da8147d2fb0721954a738fe51f1aa951[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 16 16:47:24 2020 -0700

    Improve English descriptions

[33mcommit 31d1cdba3ab2db33377ba9d0e16e21ac86562d29[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 16 16:37:10 2020 -0700

    Add High Contrast Grids option

[33mcommit 6054eff29b8146a0fb90c240b447b1ca92392db1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 16 15:37:25 2020 -0700

    Add fallback for null/empty bookmark names

[33mcommit ebced4a1874287f5572042e34d6479736ca5a063[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 16 15:37:09 2020 -0700

    Add epsilon for BPM Changes

[33mcommit 1803e270d8e944ffd3f59caa1a5e3c26cd6bffc2[m
Merge: 1d6c1ec5 7769c593
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 14 14:51:07 2020 -0700

    Add compression to some images (#149)
    
    Add image compression to backgrounds to reduce filesize

[33mcommit 1d6c1ec50325682f495f68da6118f48c6d8d587c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 14 14:48:23 2020 -0700

    Update languages

[33mcommit 4cc5b3598a6b44886de32395e32d70d4bdabc176[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 14 14:36:50 2020 -0700

    Fix strobe generator unable to be undone

[33mcommit a4ac2c855504f5865270cce4453176fda9006b0e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 14 14:30:54 2020 -0700

    Fix Ctrl-X not being undoable

[33mcommit db2b6c4f68109ccac9220224db7354b56cceb19f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 14 14:18:49 2020 -0700

    fix gradients ignoring event soloing

[33mcommit 7769c5931b5a6949cd07dddd277cd758bc56df3b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Aug 13 23:39:51 2020 +0100

    Add image compression to backgrounds to reduce filesize

[33mcommit f97d4a9a9b033a68b2cbb88d798cbb4233d74d90[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 13 14:55:34 2020 -0700

    Add precise grid snap control while scrolling

[33mcommit 7b1e86030b46c588c6929b640bd83df8a1c9be7a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 12 12:59:14 2020 -0700

    Add alpha slider (need to improve event shader)

[33mcommit 340f6452ae9002fa151ef2a8be72e3515dda9b86[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 12 12:50:53 2020 -0700

    other changes

[33mcommit ae6dbfc9e1e6e79dcdbdf07469246b4f49c25f64[m
Merge: 0549e390 f7411020
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 12 12:50:26 2020 -0700

    Merge branch 'AsoVR-dev' into dev

[33mcommit f74110209258d1f6eea3f14cf4324260f6a321d8[m
Merge: a25d2592 0549e390
Author: AsoVR <AsoVR@users.noreply.github.com>
Date:   Wed Aug 12 17:07:00 2020 +0100

    Merge remote-tracking branch 'upstream/dev' into dev

[33mcommit a25d25925cc7f1e90178c15bfabf34813dbacd95[m
Author: AsoVR <AsoVR@users.noreply.github.com>
Date:   Wed Aug 12 17:06:15 2020 +0100

    renamed ChromaToggle -> Chroma Color

[33mcommit 0549e3906d31ea75523e81d3364b350569890919[m
Merge: 75545a2a 4ac8f62b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 11 21:31:28 2020 -0700

    Various fixes (#148)

[33mcommit 4ac8f62bbb1a3c5f8c5a1a4f6203210273832597[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 11 23:20:55 2020 +0100

    Try and remember the last map a user loaded and pre-select it

[33mcommit 848a0b60d59f558bbb999568bdb14c29e00403b3[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 11 22:34:38 2020 +0100

    Stop both note and bomb placment controllers creating undo actions, don't add action if we didn't move the object

[33mcommit 02b95ec349fdc8a7e05315d79c3c40d9cc27d87d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 11 22:24:19 2020 +0100

    Fix note special angles flipping the wrong half of some combinations and double dots rotating infinitely

[33mcommit 0d7b0d81f3533b78461e1ee9c3792a119e7878ce[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 11 22:23:01 2020 +0100

    Fix auto save translation key

[33mcommit d937de5f5e9710a04351406c03558da10d26b1ea[m
Author: AsoVR <AsoVR@users.noreply.github.com>
Date:   Tue Aug 11 21:17:07 2020 +0100

    added chromatoggle objects button

[33mcommit 75545a2ae5b2035aa9b79cd3195d4a0f42850554[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 21:32:21 2020 -0700

    Make more shit queue BeatmapActions

[33mcommit 4e0240058a37ca79568cde7b5d48fda992cb25ec[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 20:55:10 2020 -0700

    Update languages

[33mcommit a2dc9250169af7ca830741e63811702ce80fe07b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 20:48:55 2020 -0700

    something idk

[33mcommit 620478bb9868dea1677fc025a72acb3292a0ad45[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 20:17:06 2020 -0700

    one line worth all of the optimizations

[33mcommit 3996238ac5094e5c1e8016509cb2232d1f0ae43c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 20:16:56 2020 -0700

    improved measure line behavior

[33mcommit 64ca2fd1c983973e7972c8c82b6bfa938ac01c1b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 12:00:06 2020 -0700

    Laser speed now affected by song speed

[33mcommit 20e76265e94f1b1d3e9eb12dfd0473636c2566e7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 11:46:29 2020 -0700

    add display text for song speed

[33mcommit d378a78f5bb1811df1594ad3c4a451cff2642706[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 9 11:38:19 2020 -0700

    misc. fixes

[33mcommit b7cecd485d4b1a473d39d978a1fa9300696889d2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Aug 8 11:54:32 2020 -0700

    bunch o' bug fixes

[33mcommit c5a31162499e3c6cce1c9566b97bd1f13a9c2f86[m
Merge: 2a04351b c2252814
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 7 11:34:16 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 2a04351b1c33088bde84c0bcb44f3f5de1563e4d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Aug 7 11:34:04 2020 -0700

    fix some special angles issues

[33mcommit c225281415ead9c8d40e64097091eb82a467d55e[m
Merge: 18a6d2fc 08f142ae
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 6 14:00:34 2020 -0700

    Node editor fixes (#144)

[33mcommit 18a6d2fc3cfb83751d2849554449575ffbb67a83[m
Merge: da46ad49 c99cf6ba
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Aug 6 13:59:57 2020 -0700

    fix typo (#143)

[33mcommit 08f142aec143f2e2e80c42eab4b8acbe48aa5a0f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Aug 6 16:38:45 2020 +0100

    Fix node editor disable/enable keybinds, hide close button when not using keybind as it didn't close it anyway

[33mcommit c99cf6ba01c773ececa3758af414e2b128eec45d[m
Author: Steve Dougherty <steve@asksteved.com>
Date:   Wed Aug 5 20:20:59 2020 -0400

    Fix spelling of "aforementioned"

[33mcommit da46ad496fbc4ce7197dca4ac16de6f8b59e2f19[m
Merge: 6bca952a d7184c79
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 5 16:50:36 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit d7184c7994266e4336e2c09aad69405e6014f4e2[m
Merge: b08ca8f0 ce766ae4
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 5 16:51:29 2020 -0700

    Localise inputboxes, dialogs and messages (#142)

[33mcommit 6bca952a7bc6e3ff090ef2da99347a2a0f1360c5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Aug 5 16:50:33 2020 -0700

    Add default to editors object

[33mcommit ce766ae4c1491edc33703c1a2912e3376d0104f4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Aug 5 17:14:03 2020 +0100

    Accidentally left pause canvas visible

[33mcommit 53a6b707aedbdf783933e778ef5da068ef2a55b2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Aug 5 03:02:02 2020 +0100

    Even more auto-sizing

[33mcommit b34297edbc0dd88b43f3f45c536d7492e62f203f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 4 18:16:06 2020 -0700

    misc. fixes and cleanup

[33mcommit 794f6f294631abf9986c648760e341efdf603530[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 4 21:35:23 2020 +0100

    Fix NPE when packaging zip for unsaved map

[33mcommit dd73a4a0c8180c6cd55f20a2a6f8a7ea1353298f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 4 21:00:54 2020 +0100

    Add messages

[33mcommit b08ca8f0d4071876a1c9bd866cdd51cd33365a52[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 4 12:35:34 2020 -0700

    excellent lack of null checking there, caeden

[33mcommit 3d879e22021196e63b21d8828aef1c28dbc18f56[m
Merge: 3568d6d2 ded362f3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 4 12:31:52 2020 -0700

    Add remaining localization keys (#140)

[33mcommit fd7a0c2b7370e923c8392d293b2a6b3e104cd569[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 4 20:11:41 2020 +0100

    Add dialogs and input boxes to localization

[33mcommit e94429e7cf40417f5093da90a220d531a97886c9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Aug 4 02:12:39 2020 +0100

    Make atlas bigger in an attempt to actually include the whole font. Contibutor view auto sizing.

[33mcommit ded362f32145ee2d9b03ba7089eeb2b6c517a9cb[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Aug 3 22:16:15 2020 +0100

    Fix diff save, copy, paste and undo icon tooltips

[33mcommit 3568d6d2711cf08b4a2d070254d90c8469778a64[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 3 13:17:30 2020 -0700

    Update languages

[33mcommit 8df0fbccf181c7955ddd345928533ae0b52ef99c[m
Merge: 984a4929 cb1a4656
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 3 10:24:04 2020 -0700

    Japanese fallback font, new languages (#139)
    
    Add new translations and languages. Added fallback font for japanese …

[33mcommit cb1a46567d911f631e02313308e08d506c677469[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Aug 3 17:50:36 2020 +0100

    Add new translations and languages. Added fallback font for japanese characters. Fixed a few auto-size issues in options

[33mcommit 984a49298fb4e2591ec25ef5f1ecd5ff44c44eaa[m
Merge: 14a6965f d59db299
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 2 20:55:10 2020 -0700

    Permanately fix orphans (MonkaS) (#138)

[33mcommit d59db29901850fce0acbe2ebcf17685676e8929d[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Aug 3 04:29:32 2020 +0100

    Make fix orphans permenant by marking broken collections as dirty

[33mcommit 14a6965fb9b55e6f8edf1f6812e1818464967083[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 2 16:59:53 2020 -0700

    Fix legacy conversion error, fix bomb alt-drag

[33mcommit 613ea04eba5b042b7912c940c434354d222f2201[m
Merge: 39b6c099 c5cd5436
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 2 16:59:20 2020 -0700

    Merge branch 'Top-Cat-localization' into dev

[33mcommit 45aa2bf341abc7f14a8be80075c6bcdef3fb3ce9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Aug 3 00:47:04 2020 +0100

    Remove old translation key manually. Fix settings heading when changing language

[33mcommit c5cd54363b7adf1954363f84598b8701e808c67f[m
Merge: 49a949d5 45aa2bf3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Aug 2 16:46:31 2020 -0700

    Merge branch 'localization' of https://github.com/Top-Cat/ChroMapper into Top-Cat-localization

[33mcommit 49a949d57a24db0d0a255696e8ce4df836418621[m
Merge: 39b6c099 1e0b2a07
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Aug 1 18:27:51 2020 -0700

    Merge branch 'localization' of https://github.com/Top-Cat/ChroMapper into Top-Cat-localization

[33mcommit 39b6c099cd29e86fbedd40bce241dc64fb6fbb4b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Aug 1 18:20:13 2020 -0700

    misc. changes

[33mcommit faca586f52db31048404997bd239a8b601e041d5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Aug 1 09:34:54 2020 -0700

    Final touches

[33mcommit 807d32eb30c4666ef5f1ccdc676536b06104833d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Aug 1 09:34:35 2020 -0700

    Wrestled with the Input System for a day

[33mcommit 1e0b2a079637caf6f10ae5569f98c921432dbbe8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 31 18:56:26 2020 +0100

    Fix casing

[33mcommit d81ca081d4bcdd820c456fdf6ad57e4cfcac9335[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 31 04:40:23 2020 +0100

    Formating fixes

[33mcommit f5da6fd3e2d72e85fec9454fc2ca655ad1ea48fa[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 31 03:56:03 2020 +0100

    Switch timbaland ring material

[33mcommit 39e7da5c3c8ac6b2e5cfe8d2715a4ee95ce24616[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jul 31 00:09:29 2020 +0100

    Add windows addressableassetdata

[33mcommit 89efcf745995b9b3561958ed34e8fe30be119a1f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 30 23:52:11 2020 +0100

    Try building player content

[33mcommit adfb80f8618941482d27453d4ce98d1d8faaef2c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 30 23:35:28 2020 +0100

    Set dirty when loading

[33mcommit 22cc773df366efa826ab5ff890e334cf78e42094[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 30 23:23:57 2020 +0100

    Add AddressableAssetsData

[33mcommit ed03cc916d0bdcc61484a75bea837e64a0aea9de[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 30 22:52:40 2020 +0100

    Add cli build script

[33mcommit 116a19de6c961a3bf42b404d9b94b6ef1585665c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 30 22:11:16 2020 +0100

    Move language dropdown into existing menus

[33mcommit 7fbefd136de7028ddabe6c5dac407354952bc129[m
Merge: 33ce3f49 7200332a
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 30 21:14:18 2020 +0100

    Merge remote-tracking branch 'origin/dev' into localization

[33mcommit 33ce3f49843df09bcb13541af0429f6450c45085[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 30 20:48:14 2020 +0100

    First localization pass

[33mcommit 7200332a99d3b9e6b0b4d92081d936682c07e64d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 30 12:01:01 2020 -0700

    Fix buggy bomb placement

[33mcommit 9fb74c7be58a3fba43f0b0bd9eb9f91915854321[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 29 14:31:55 2020 -0700

    Use Content-Length for temp loader progress

[33mcommit 8444fc22734e85240700c572e8de8b752a397cdb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 29 14:00:46 2020 -0700

    Fixed stretched grid at negative beats

[33mcommit 9a5412d684b3c611246c67b139376ff4ab9a3b73[m
Merge: 2e9f84bd a148c640
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 28 23:44:53 2020 -0700

    Consistent object groups (#134)

[33mcommit 2e9f84bd1c8f3b12dd1ea89f53c60da083fe29e6[m
Merge: 564151ce ab177249
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 28 23:40:21 2020 -0700

    Fixes from Top Cat (#135)

[33mcommit 564151ce3f504eca7bb2de7a2106a007af896dad[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 28 23:38:08 2020 -0700

    Add clamping option for rotation display

[33mcommit 564737eff8411c7d5602afe7e39b5683e6b5ffcb[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 28 23:25:51 2020 -0700

    Add frame of delay for text to update

[33mcommit dd83cbe0b1fdfdbe6175f496c2f0b7f8536d82b7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 28 23:24:02 2020 -0700

    Trim whitespace from input location

[33mcommit ede6e63662234038141069db297be4c84048f094[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 28 23:20:44 2020 -0700

    Add CM temp loader

[33mcommit 4a657c38a4247284f95925268e1df4c1f43e2efe[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 28 23:17:20 2020 -0700

    Move extension classes to folder

[33mcommit ab17724929f6d55c53775a115fbd0ce95ab7441c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jul 28 19:29:53 2020 +0100

    Fix selection outline not showing. Allow deleting noodle walls

[33mcommit a612c8efe6bfbb0b3849a73426a1219fdde04d7a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 27 21:48:55 2020 -0700

    Misc. fixes

[33mcommit a148c64025a17889a7d7aa394983b55be03ec671[m
Author: dcljr <dcljr@me.com>
Date:   Mon Jul 27 17:35:29 2020 -0700

    made object groups consistent between overwrite paste and select between

[33mcommit d4a7271182e91fafc841889fd3f8152d649bea0e[m
Merge: 95870407 2494d2c3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 27 16:37:20 2020 -0700

    Set bomb material and switched shader to opaque (#133)

[33mcommit 2494d2c395a1e91757205d85eee8e27b89e715a3[m
Author: dcljr <dcljr@me.com>
Date:   Sun Jul 26 15:23:58 2020 -0700

    Set bomb material and switched shader to opaque

[33mcommit 9587040798d54c691f2906f2c0d396fbcd12ebfa[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 26 14:48:32 2020 -0700

    Fix typo when deleting difficulty

[33mcommit e7f7d93e92fe41b89725b8c4712c21b5d534991b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 26 14:34:17 2020 -0700

    Various fixes

[33mcommit 29920bceafd97c48f58d73a0eb37497349ecd220[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 25 20:03:56 2020 -0700

    absolutely brilliant coding by ya boi

[33mcommit 2caf78baa3e248f026d1d3c139104ee3511fcc71[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 25 13:14:32 2020 -0700

    Change Typical Values for Jump Distance

[33mcommit 7682a62b2def4b212169db55a896c012cdecf154[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 25 13:14:24 2020 -0700

    Simplify grid lines when possible

[33mcommit 867a0cb22ed2346969c02d4d4229bc3a6af8bab1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 24 23:26:57 2020 -0700

    Fix cam movement persisting when entering dialog

[33mcommit b52977079df2bf5f0ceaf6e848d50404f9c41087[m
Merge: b9d5c22a 9735b39f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 18:18:23 2020 -0700

    Added select between with ctrl+shift+left click (#132)

[33mcommit b9d5c22a31e5983556b7c8e14ecc45e8d39a6b43[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 17:42:06 2020 -0700

    More optimizations

[33mcommit cce833f36d4a7bcf60b6a4b55dc162bae1d0020e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 17:20:40 2020 -0700

    Make SongEditMenu clone header materials

[33mcommit 7984b7242ed604de47e268d2f6d9928de8794c4d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 17:20:24 2020 -0700

    More optimizations

[33mcommit d964b49c344ffac8e1fca8db5917e55f71634d2f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 17:08:42 2020 -0700

    Return Equals to JSON string check

[33mcommit 249fcd0b1d2ef97901916643e73bcd517afd42c8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 17:08:30 2020 -0700

    Simplified RemoveConflictingObjects

[33mcommit e2ac242800da0bee83792518ed07f3523b3041f4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 17:06:06 2020 -0700

    Move Conflict methods, performance gains

[33mcommit 76cda22957b5fb6b223b08dc165cb814541e37df[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 15:45:57 2020 -0700

    Fixes w/Custom Events

[33mcommit cc3a921f8d1b4498983befa765e1396a7f2eabc1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 14:11:53 2020 -0700

    Fixed errors when song preview extends past its length

[33mcommit ce764623cf66b0db4a967899e40ccd42c1918b97[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 23 14:11:26 2020 -0700

    Add null checks to EventsContainer

[33mcommit 9735b39fa1f45fa6da5e4bd38c1dd5096a981d03[m
Author: dcljr <dcljr@me.com>
Date:   Wed Jul 22 17:19:33 2020 -0700

    Added select between

[33mcommit 60da7aa5c49f6123dad43c266e96a4add806c063[m
Merge: 9db0bfab 3b3eb062
Author: dcljr <dcljr@me.com>
Date:   Wed Jul 22 16:27:22 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into overwrite-paste

[33mcommit c56060091c930165c07fe2e8cafb0531320bb5ab[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 22 00:14:36 2020 -0700

    Optimizations

[33mcommit 34e66a65bd55feda79bef865721ceeca504e21de[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 21 22:41:27 2020 -0700

    Apply rebind when clicking off

[33mcommit 3b3eb062e7562147a0d9ede21ac334f93cb7215b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 21 11:37:41 2020 -0700

    Fix conflict check failing with high decimal precisions

[33mcommit e577069c2729eb0e34d4ead8b00e51575e40566e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 21 00:41:14 2020 -0700

    Fix off-sync placement times with interface grids

[33mcommit 4c31a4c9867d1be8c72317cbfbdd6152d7bf4334[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 20 19:25:02 2020 -0700

    Fix BOCC dictionary error

[33mcommit 7e180cdce4f753fcf6128e23025448ef5237968d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 20 19:24:53 2020 -0700

    Fix Past Notes not detecting all blocks on last beat

[33mcommit 9db0bfab27ada1c2ae51ca2148b7192809b936c7[m
Merge: 916d3b57 507fecd4
Author: dcljr <dcljr@me.com>
Date:   Sun Jul 19 13:57:49 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into overwrite-paste

[33mcommit 507fecd4e5e6e032b6bc46aa5234ed81164b8c10[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 19 10:41:37 2020 -0700

    change methods to prevent error

[33mcommit fabb6637e00c0ab3d557075cec0c8bd171bcc2b3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 17 17:42:07 2020 -0700

    Turned out that yes, you need to refresh the pool

[33mcommit d3c0760e132b582c6590a46aabf2e0a31037f441[m
Merge: 1bb09639 298e3693
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 17 17:34:45 2020 -0700

    Merge branch 'Atlas-Rhythm-overwrite-paste' into dev

[33mcommit 298e36938a2b40bd7eb7ab0c72b63475c6a46450[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 17 17:34:18 2020 -0700

    Change order of operations on Paste method

[33mcommit ff09846db75ab0d94f6dc8575236bf374f1158d0[m
Merge: 16313838 df7a1f2c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 17 16:47:49 2020 -0700

    Merge branch 'overwrite-paste' of https://github.com/Atlas-Rhythm/ChroMapper into Atlas-Rhythm-overwrite-paste

[33mcommit 916d3b57afab7c9b4aea0e3cecac9dae0551dafb[m
Merge: df7a1f2c 1bb09639
Author: dcljr <dcljr@me.com>
Date:   Fri Jul 17 16:06:50 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into overwrite-paste

[33mcommit df7a1f2c06935bde04caabf19bb8c5a5ade5ebb3[m
Author: dcljr <dcljr@me.com>
Date:   Fri Jul 17 16:05:03 2020 -0700

    include epsilon when looking for objects in overwrite paste

[33mcommit 1bb09639fba8bb80b1ea6e56556b120f8d51b811[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 17 12:16:08 2020 -0700

    Add setting to toggle loading messages

[33mcommit 16313838e8a5171559a4c45adf0d2f9c5e63326d[m
Merge: 942de1c6 9eccb098
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 16 18:08:50 2020 -0700

    Merge branch 'overwrite-paste' of https://github.com/Atlas-Rhythm/ChroMapper into Atlas-Rhythm-overwrite-paste

[33mcommit 942de1c68f449ecbec84015477a6987120cee089[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 16 17:48:20 2020 -0700

    whoops forgot to disable overwrite for localization

[33mcommit 24cf3b11c079c432b7b57464f0a2b8a8d13e50a3[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 16 17:42:57 2020 -0700

    uhhh.... meow?

[33mcommit 3974b2c210190f09d065e347f37da40f381f8387[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 16 17:36:57 2020 -0700

    Fix conflicts between ring prop & non-ring prop events

[33mcommit a32ca9c35e22b045e8434c2e58bed52d7dc1617e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 15 22:01:03 2020 -0700

    tweaks to event and note prefab

[33mcommit 8637e7408abb56b70c2ffde572678878c1cac70a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 15 21:51:12 2020 -0700

    fixed obstacles being placed outside of 4x3 grid

[33mcommit dfc4fbd489196dcca9f8e87d4c017373b36a469a[m
Merge: 9eccb098 5bcf3cf8
Author: dcljr <dcljr@me.com>
Date:   Wed Jul 15 16:33:20 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into overwrite-paste

[33mcommit 9eccb09875cc014a4cd81590ceb6f05e5b287735[m
Author: dcljr <dcljr@me.com>
Date:   Wed Jul 15 16:23:40 2020 -0700

    overwrite objects in groups depending on what is being pasted

[33mcommit 5bcf3cf8954520539c50f44a07fe258f4af9e3bf[m
Merge: 074616f1 146d5915
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 15 13:24:06 2020 -0700

    Color picker color space conversion (#131)

[33mcommit 074616f194792f5c22ac923fa43525ba23954aa7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 15 13:21:02 2020 -0700

    Add note rotation snapping

[33mcommit 1f7fcc3402b9ddf11dfe37ee4eb54569634e0d65[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 15 13:20:05 2020 -0700

    Conflicting fix, add some virtual methods

[33mcommit 913a9ea605c3f790b54c1c4caa3a1f3347f00eb9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 15 12:40:29 2020 -0700

    fixed keybind cancellation (again)

[33mcommit 146d5915888b7dd7b7528de11f3578deea6e30ac[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jul 15 03:02:12 2020 +0100

    Apply color space conversion to color picker

[33mcommit 872bd835cabfd839ff3a5cde5a6945b9d1e57cdc[m
Author: dcljr <dcljr@me.com>
Date:   Tue Jul 14 17:39:13 2020 -0700

    added overwrite paste and fixed buggy paste action redo

[33mcommit 9725e64a8c95ffb178b58925dddeb9cab9503037[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 20:49:14 2020 -0700

    Fixed conflicting, click+drag, & placement jank

[33mcommit 50d9a55b87144de27b9cbee2d03a9efdeb3ade66[m
Merge: 100f0256 a3aee648
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 16:05:04 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit a3aee648cfbff753e04ee308053d405a2acf9c13[m
Merge: 43ce769a d60e46e7
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 16:05:42 2020 -0700

    Bug fixes with BeatmapObjects being passed by reference (#129)
    
    I could convert them to `struct`s but parts of CM do rely on the fact that they're passed by reference.

[33mcommit 100f0256e1b682d8ac6a6085a499a4c6fb8ed8e6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 16:01:34 2020 -0700

    add some sorting for events container

[33mcommit d60e46e79716df47a98ecd8a4f35544fb280f21e[m
Author: dcljr <dcljr@me.com>
Date:   Mon Jul 13 15:40:36 2020 -0700

    fixed epsilon calculation

[33mcommit 43ce769a96a9ca8afec49ac4e4c3e4392707d846[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 15:38:10 2020 -0700

    Add keybind cancelling by clicking off

[33mcommit a7660cc93e0b147b5bfca6de3678341c60845136[m
Author: dcljr <dcljr@me.com>
Date:   Mon Jul 13 15:30:06 2020 -0700

    fixed drag outside window and multiple drags in a row causing issues

[33mcommit 8881fec12e780eb521e42b911d292fbce9ce123c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 15:10:11 2020 -0700

    alt-middle click now also inverts note colors

[33mcommit 01667bf976b837541a239de6d3b93115643a6811[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 15:09:45 2020 -0700

    remove LOD from events, now use MM block mesh

[33mcommit 92ea4cb6d53ab3c621e48d6fd5a28309feb9739f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 13 00:13:53 2020 -0700

    Restructured actions, set keybind version to 1.0.0

[33mcommit e9c2645b9a28eacb1537a0531e41d24013860f75[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 12 23:47:07 2020 -0700

    instantly made keybinds 1000% better

[33mcommit 74939d7977d3701f30ef2aeba1623859bc809699[m
Author: dcljr <dcljr@me.com>
Date:   Sun Jul 12 18:11:39 2020 -0700

    fixed buggy undo and redo when dealing with moved notes

[33mcommit 4f20d60062ecb6e92a1d65adf53d9ff6da163e7f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 12 17:22:19 2020 -0700

    Create Editors object when creating new song

[33mcommit 61a88446bd0f073575ad25c0cfa46978a088c456[m
Merge: eb62b8d7 dca70faf
Author: dcljr <dcljr@me.com>
Date:   Sun Jul 12 16:42:02 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper

[33mcommit dca70fafbf9e40bd5567c78ca75dba5fe5b72c5c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 11 15:43:53 2020 -0700

    tweak LOD levels for event prefab

[33mcommit 3a3ba8ddb1f099170b39556e01a6de92b5a8c8d8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 9 22:44:04 2020 -0700

    Add Alt-Shift-LClick for jump to object time

[33mcommit 477ae18b802b4fe5d10fcf4ae14e80c22e6d789a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 9 20:47:40 2020 -0700

    ME detection checks for empty customData

[33mcommit dab3b0f57866f5b3df00d3e402eb64c73eab9b35[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 8 19:09:26 2020 -0700

    Remove unneeded code from Accurate Editor Scale

[33mcommit b5eb4a77e555472b1070f4a6db701a258d388d73[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 8 17:58:58 2020 -0700

    fix placement controller active with node editor

[33mcommit 3a114db689f296c22d00a499570361523fe4b25a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 8 17:58:45 2020 -0700

    tweaked jump duration/half jump typical values

[33mcommit 735a3b49130f939c39a8b582f4d1f1c3269d09f0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 8 17:58:23 2020 -0700

    Turn Node Editor keybind disabling from blacklist to whitelist

[33mcommit fa1806c347d5788fd4e2705ff94805867361e94a[m
Merge: feabdff1 67f211d5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 7 14:45:26 2020 -0700

    Alt + Right Click (Happy, Rabbit?) (#128)

[33mcommit 67f211d5fbf97eed3fcb52ae4c1add53bbbc8231[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jul 7 22:25:43 2020 +0100

    Get z from note time not position, fix getting stuck when mouse is released outside window

[33mcommit 23ed68e15de4136d3d0c4d59a0806fab9132e77b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jul 7 21:58:48 2020 +0100

    Use local position

[33mcommit cd6d28a318de3a591e23beea66d5870d853b24a8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jul 7 21:54:16 2020 +0100

    Retarget individual grids

[33mcommit ad3a0cca67f237031089e2e00f45b446c38568ba[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jul 7 17:23:36 2020 +0100

    Alt + Right click drags at block time

[33mcommit feabdff13c22fd6b67b02deb65ae31f56e62af94[m
Merge: 4556e746 21d3c103
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 7 13:52:18 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 21d3c103d8040486991fe8959c11dc07e13f7dfb[m
Merge: e136b490 753cc369
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 7 13:52:08 2020 -0700

    top cat did a derp (#127)

[33mcommit 4556e746d73f48f1f9849bc45c7d3a4a1bcc9712[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jul 7 13:45:45 2020 -0700

    shuffle comments around

[33mcommit 753cc36980ebc0a9983d4654f3ac5f73105f61e0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jul 7 17:47:32 2020 +0100

    Fix bombs

[33mcommit 4e9527bc0c3b105daccf49d1018c4b2c69b13b5e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jul 7 11:39:02 2020 +0100

    Forgot to add the extra property to the transparent shader

[33mcommit e136b490a4a9cfc056e629c8335813144c137cd4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 6 22:17:58 2020 -0700

    Make Unity not cry about editor name or version

[33mcommit 25fb532c935dbfad778a45de655327f9f7ca5e91[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 6 18:32:38 2020 -0700

    Support new "_editors" object

[33mcommit 8f3af0881547579519950bd4892190fd4df4fbe6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jul 6 10:49:04 2020 -0700

    Set spectrogram chunks to right render queue

[33mcommit 7a5d37b094159c88ca14d82b3758365b18a250fc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 21:35:54 2020 -0700

    Throw error if some fields aren't in map objects

[33mcommit c551d8bc2a516493053523062342242ee9b970a6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 21:16:53 2020 -0700

    Move Mirror code around in hopes that it fixes stuff

[33mcommit bf25e458f4e848b8ccf8b9a76100da2747d9e9ab[m
Merge: ba650ee3 8ef5f19a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 20:49:16 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 8ef5f19a6e9e0d0b58e91f67f433b0a0f470267e[m
Merge: 059d736b 44699dd0
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 20:49:07 2020 -0700

    MM block model option (#126)
    
    Add simpler block option

[33mcommit ba650ee35d8b82f99125753c6396c62ca1bd8bf1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 20:29:09 2020 -0700

    forgot WaveformWorkflow setting existed

[33mcommit 44699dd0e1ff320bed89b882fdfef30f4a434489[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 5 23:30:31 2020 +0100

    Add simpler block option

[33mcommit 059d736b9629182be2f8fdb98d0efd311e205191[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 14:22:34 2020 -0700

    apparently shader offsets were wack

[33mcommit 67f7b49948eb333862edcb628f30b3a50e1c1b26[m
Merge: 74b2cdcb 3b90df85
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 11:50:09 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 74b2cdcbe2c6fd02da39194576e8996db0ccf1fe[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 11:50:00 2020 -0700

    Added Shift-C to trigger Strobe Generator

[33mcommit 9395001cf995d0834409aa7393402c5e907949b4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 11:42:12 2020 -0700

    fix some SimpleJSON issues (reflected in fork)

[33mcommit 3b90df859a3d26e55df5deb81f612b777338d4b8[m
Merge: cc980b33 55555808
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jul 5 11:38:19 2020 -0700

    Fix explorer opening w/commas, increase waveform contrast (#125)

[33mcommit 5555580850cce7f04f136d8a4ecdbbfc4a272e38[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 5 14:38:14 2020 +0100

    Fix open explorer with commas in path. Increase waveform contrast

[33mcommit cc980b337840ac4a887e142bc0addc384f46e6e4[m
Merge: 587f81b6 e70ee110
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 20:40:05 2020 -0700

    Tweak placement snapping (#124)

[33mcommit e70ee11035cb310c435948b14d835e44e766aa18[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jul 5 03:36:13 2020 +0100

    Tweak placement snapping

[33mcommit 587f81b6676ec094052a641fba2ad10462145dd6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 13:26:32 2020 -0700

    Add JSONParseException location trace, fix grammar

[33mcommit 813c53930ff1a31b6db0287cef49f682f8039908[m
Merge: 1c207ea9 518ad555
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 13:02:36 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 1c207ea99319bbf35776c99075bc1938121e4a14[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 13:02:27 2020 -0700

    wtf is UnityEditor.PackageManager doing here

[33mcommit 518ad555f7794bdc848e640fa5c52b57d8286906[m
Merge: 01fc87e5 69ae34cc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 12:58:19 2020 -0700

    Tweaks to Plugin API (#123)

[33mcommit 01fc87e538913d864b5ca9654ce7f68142dc690b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 12:00:03 2020 -0700

    Edit/remove navigation from SongEditMenu elements

[33mcommit d4ffa0b700a11feb3ebc777dbd5f9b83709498fc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 11:59:32 2020 -0700

    Support CustomPlatform's SongEventHandler(?)

[33mcommit da55760583ec38650f606ab868fb36c7da1631ec[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 11:59:05 2020 -0700

    Added JSONParseException, added parse error

[33mcommit 6f6b19aed610da1d494f100dbf0f5080c676416b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jul 4 11:51:17 2020 -0700

    chroma requirement filters out bombs

[33mcommit 69ae34cc996e420f400d85c72b0ebab24b310fbd[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jul 4 16:30:49 2020 +0100

    Plugin API tweaks

[33mcommit 04ded540e08b89eb95fdb610528cd9f601515f03[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 3 15:30:27 2020 -0700

    fix copy/pasting and BPMs

[33mcommit ed4e6e097b338fdad20e7e3356e4abefc0dad22f[m
Merge: e402c28d 846d9a26
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 3 14:59:35 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit e402c28d659bd9ab5ddf8af0d0255f72ed59a088[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jul 3 14:59:17 2020 -0700

    last time I am touching note ordering

[33mcommit 846d9a262868c3a0bc66e35970a4b15e13f3dba8[m
Merge: 090d15fa 546b6d00
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jul 2 01:19:50 2020 -0700

    Bug fixes (#122)

[33mcommit 546b6d00032cdadd72959e6c80ae8d22f42f96d6[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 2 08:49:06 2020 +0100

    Fix selected NPS caclculation

[33mcommit fd993aa3d3ed551f08de102fcc290006cb3d4f05[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jul 2 08:35:52 2020 +0100

    Fix copying difficulties. Fix reference exception with selections.

[33mcommit 090d15fa8b8b14e926bb6a79e2e8dcc4edaaaadc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 1 14:49:49 2020 -0700

    Node Editor ignore more keybinds

[33mcommit 6f0874c97ddf2eaf8a9fb86bee6b5534552dca72[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jul 1 12:19:45 2020 -0700

    Fix NE pos persisting if precision placement is off

[33mcommit be5018ffc89bbf2e4506b0d23bd35bc0218f8e5b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 30 22:53:46 2020 -0700

    Do not create empty customData objects

[33mcommit e1f6d9c4265b4c8b080e8b7d17843459c6db89b6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 30 22:53:36 2020 -0700

    Mirror now handles light prop if in light prop mode

[33mcommit d26759d28c7c4c9a082808ba864044f98d643f3f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 30 22:31:44 2020 -0700

    Fix Light Prop issues on Timbaland

[33mcommit 77a03e33586dc16c5332fd22107eba77bd2f069e[m
Merge: 852f7ca7 fd758fd9
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 30 10:54:04 2020 -0700

    Fix exception when creating a new map (#121)

[33mcommit fd758fd984efd5a6725d3ca0e495d62340d047c3[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue Jun 30 18:34:53 2020 +0100

    Fix NPE when creating new map

[33mcommit 852f7ca7323f13f0a7bb7945cf516d898cdacdd8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 29 17:36:15 2020 -0700

    Small BeatmapObjectContainerCollection optimization

[33mcommit 06bac92176acddbcce1740375ae8b7d12f38cc4e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 29 17:35:45 2020 -0700

    Another approach to note direction keybinds

[33mcommit 2d1e9c739ff32fac3115f35f8a2fb13978aed677[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 29 15:31:46 2020 -0700

    Remember song page and location

[33mcommit cdbc05f25b0c9544c986608c9306c1f479dd26d6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 29 15:27:04 2020 -0700

    Box Select starts flat, can select from top down

[33mcommit 2cd9e2107446275524fde60c02a7826a21f01355[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 29 13:32:29 2020 -0700

    Support NE scaling on the Z axis

[33mcommit c79e99e26d838d075706768db07443a1282fd331[m
Merge: 7a641432 95fe5655
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 29 09:58:36 2020 -0700

    Saving CM from Aalto maps (#119)
    
    Add note block LODs

[33mcommit 95fe56554e390498cd73b863b8e05ca6dc87201a[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jun 29 07:58:54 2020 +0100

    Remove magic number

[33mcommit 7a6414326326598619dc418c4436858169407980[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 28 19:26:03 2020 -0700

    Fix incorrect NE positioning in Past Notes Grid

[33mcommit 3e7fbcff139d1b0cd2751ace41d2f8e0048a240d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 28 19:25:55 2020 -0700

    Forgot GetCollectionForType<T> existed

[33mcommit 5372c347e2db91edd37cfa8f66209f94363dcca8[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jun 27 09:37:31 2020 +0100

    Add note block LODs

[33mcommit a0c3b6093d65dbcde8e6c807c3f8152fa4ff16d2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 27 13:51:50 2020 -0700

    add auto detection for Chroma requirement

[33mcommit 6de7f71bfa5c5f548546a151b3fed3d291f5cf70[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 27 13:15:32 2020 -0700

    Color picker now picks start color from gradients

[33mcommit 8760885eae2bf6fc55881522925f9b612ecac689[m
Merge: feee738b 7e0bfeb7
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 27 13:13:06 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 7e0bfeb79828b441f1e87329e39a287df6ba913d[m
Merge: 80011fd6 5ed84f60
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 27 13:13:52 2020 -0700

    Group duplicated characteristics (#118)

[33mcommit feee738babc84004503ee46cb45b04a1987466dd[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 27 13:12:34 2020 -0700

    Strobe gen removes objects conflicting w/chroma

[33mcommit 5ed84f6008469577501d56c6ae64503c39cf5198[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jun 27 19:23:13 2020 +0100

    Group duplicated characteristic blocks

[33mcommit 80011fd66391616ea43e58a90577fa1bbfdbd4ac[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 26 13:13:15 2020 -0700

    fix metronome with bpm changes

[33mcommit 5533d6b98a83789cafa07ef1cc528ae38bd95c48[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 26 12:18:11 2020 -0700

    fix NE positioning for notes

[33mcommit 1c90e9443768cede238676b09d27bdbb0421403a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 26 11:39:27 2020 -0700

    Add handling for prop ids outside of bounds

[33mcommit be49f25a630bc0caa2b0757516f990ccbfd1b115[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 26 11:05:50 2020 -0700

    Add discord assets for new environments

[33mcommit e82c2d0392b17da79419250286205cc6f6410023[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 26 00:48:57 2020 -0700

    Improvements to paint tool

[33mcommit f58fd3f655aa88650c3555517aea0acd93eb414e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 21:41:13 2020 -0700

    fix static lights from not properly setting color

[33mcommit e17cb06b35dd0e44184e0e50c124d37636c141e6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 20:57:08 2020 -0700

    Log inner when Node Editor throws TargetInvocationException

[33mcommit 363417ca91a9b1316623b3705ceced051af174bd[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 20:52:58 2020 -0700

    Tweak alt-dragging events, fix gradients on off events

[33mcommit 60a7e3cfc712d818e0efccb7c9616df0d5a6fe39[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 19:57:12 2020 -0700

    forgot easing for visual gradient

[33mcommit 970ac021de4925d46761899d13ad9d336fc5e5e1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 13:38:05 2020 -0700

    fix some issues, add toggle setting

[33mcommit 64df04473959d40c4cf10200bd32c3a26986bddc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 13:23:03 2020 -0700

    Added gradient visual

[33mcommit 448efe0d658b4fe575a0185cf9966f7af13513df[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 11:12:00 2020 -0700

    make box select default on

[33mcommit 32e4e03d9bbfc0864fbb7758b730f1a4286e7a80[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 10:49:53 2020 -0700

    fix stuck directions with note placement keybinds

[33mcommit b77bf96d9239019809483300c727bc91ab754f94[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 25 00:40:33 2020 -0700

    fix gradient alpha and persistent canvas alpha

[33mcommit c5aadd812781e520e13b429ce3da760cacc47bc9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 24 21:13:38 2020 -0700

    fix jank rotating lights on Nice enviro

[33mcommit 098be1938ce757fe1f007ec0626e57cee0e9864a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 24 21:05:07 2020 -0700

    due to significant community uproar, i have converted the spinning Chroma logo from an 8k spritesheet to a svg + rotating rainbow

[33mcommit cbe50763e315359c75b8e779f105e65f79dec4ae[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 24 19:09:25 2020 -0700

    Make the entire background city disableable, since it conflicts with the event, bpm change, and custom event tracks

[33mcommit 7f929ea70d51c262ee0f20572dc399e770cbcad7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 24 19:04:27 2020 -0700

    Green Day logo and grenade cannot be turned off in game.

[33mcommit 8d13f2c24dbd7685448e6e7ce2db110ca41dbd8a[m
Merge: dc3d82d8 19397e80
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 24 18:58:45 2020 -0700

    Add Green Day environments (#117)

[33mcommit dc3d82d856cb5fadd1d4a12c9caa0ce7e3d83aa9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 24 16:09:36 2020 -0700

    Strobe Generator supports light prop

[33mcommit 6e58e5a574743a9405e349a1dc1296b644e45f25[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 24 15:23:44 2020 -0700

    dump a fat load of bug fixes in one commit

[33mcommit 19397e80eea91d902b3cba7635edcd55d77de924[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 24 07:55:49 2020 +0100

    Make green day rings a prefab variant. Add green day logo. Remove some materials

[33mcommit 205c09bb543d87b918ad93184ce20b5e00745406[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 24 03:52:23 2020 +0100

    Add green day environments

[33mcommit 4a759c36c7b542c6531e029de587179e20beedb4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 23 09:42:13 2020 -0700

    fix InvalidOperationException

[33mcommit 5d0ce810ed95a1c106a93c6cc6d86234fa4cc94a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 23 00:12:44 2020 -0700

    Better code to snap tooltips to screen edge

[33mcommit 6f98f1c7675d480d0fbfa310dd5a8695fa891735[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 22 22:42:01 2020 -0700

    Remaining grids now affected by editor scale

[33mcommit 90e584dfa983fd51f537d3f0ab84dfcc42a7cd52[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 22 21:25:48 2020 -0700

    Add action for modifying Node Editor text

[33mcommit 3a271c95481ed998aee26c778236bcacc69cfbe0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 22 17:41:55 2020 -0700

    fix rotating lights causingNullReferenceExceptions

[33mcommit 47a4fd8450a8e6abb68b6ab87f5448bd22a730ea[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 22 11:47:07 2020 -0700

    more fix

[33mcommit 7ce65d00bd7b3b0608cb7e61e63020c8241643ae[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 22 11:34:32 2020 -0700

    bug fixes aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa

[33mcommit 5fee4ba7a74790c1aebac554712b523f888c9024[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 21 23:39:51 2020 -0700

    Fix various bugs

[33mcommit b31167359a2637883f723df9733fb87f66d609dd[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 21 22:31:10 2020 -0700

    Fix ctrl+left/right limiting to a prop ID of 15

[33mcommit a11db4cee09e2f666f6fc3fd26c5f4c82a688d42[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 21 22:30:39 2020 -0700

    Fix prop mode throwing NullReferenceException

[33mcommit 0c1fd0c1d4a8853a1c3f65e28f7a09c2818ac8ce[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 21 22:30:17 2020 -0700

    Add Paintbrush

[33mcommit da0d3dd723f6377ddbf87961963250a6071e5ac7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 21 11:38:25 2020 -0700

    Add default easing to MapEvent, remove unneeded ( )

[33mcommit d7f1b2227b5eaccc393175a47081cac11a331360[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 20 12:41:39 2020 -0700

    fix LoadInitialMap not applying custom colors correctly

[33mcommit 2c7f418b1d70787f49a4252311f440aa7ffc709d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 20 12:20:04 2020 -0700

    Node editor action made generic; now used for alt-drag

[33mcommit 628ba47e2e1c94343e7d578c768f07672a78bf7f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 19 17:43:07 2020 -0700

    fix note cut vis., add alpha for chroma colros

[33mcommit 01d212b4f45ab339755bd6ef82cace5ed2ecf459[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 19 11:54:31 2020 -0700

    fix reported bugs

[33mcommit f7a4e20b03f03241cf39c4b48f2e7ebbc6cb01c2[m
Merge: 24106d15 7525bc57
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 19 08:03:37 2020 -0700

    Fix spectrogram sync (#116)

[33mcommit 7525bc57c0752f5c7c8b6b93f56e2322e07bc6a9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jun 19 07:26:00 2020 +0100

    Fix spectogram sync

[33mcommit 24106d1515e2247077c01f883a9559674b7cb022[m
Merge: d6eb08f6 8eb95fd8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 18 18:52:53 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit d6eb08f62371a1d5a0130b9378e9f935c80a23e5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 18 18:52:49 2020 -0700

    remove completed TODOs

[33mcommit 8eb95fd8a0e57cb0a81eb798479dcaaa00719989[m
Merge: 18829dc1 860a361f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 18 17:24:50 2020 -0700

    UI fixes (#115)

[33mcommit 860a361f4386d526b5de1ce7c356aa367274470c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Thu Jun 18 22:16:59 2020 +0100

    Fix floating point changes triggering dirty check and loading info.dat with duplicate characteristics

[33mcommit 18829dc1e211bd9e437e8426d11de7b1c49575d1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 17 22:21:09 2020 -0700

    Replace SortedObjects with optional GrabSortedObjects (untested)

[33mcommit de9f10800c6a0df357827c24d2ca810d28887f5f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 17 18:20:55 2020 -0700

    bug fix dump

[33mcommit 67b6ff1296e07b38a3e3d4e91f2b1b82158956da[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 17 16:26:26 2020 -0700

    made custom colors not pepega

[33mcommit 41ca5050bb804d0f62d57b9a14af47f8be8eade2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 17 11:35:30 2020 -0700

    check existence of all the other map files

[33mcommit 1318b977e64d9e2488bc961058810ef634c3401f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 17 11:24:10 2020 -0700

    Check cover and audio file when packing zip

[33mcommit e636ea2ce6514bd7e728412a4a495b6da0730b54[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 17 10:49:06 2020 -0700

    add Noodle Extensions support for Mirror tool

[33mcommit c74d1bc327bcd36142815be9f1d581fc0c219dc4[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 17 10:23:39 2020 -0700

    various bug fixes

[33mcommit eb62b8d732b5dc0e60002de940ec0b7d41f3f12c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 15 20:38:30 2020 -0700

    Create FUNDING.yml because i have no soul

[33mcommit ad8f08579bbefac84e39eea694ecfab4551b207a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 15 18:32:58 2020 -0700

    fix NodeEditorUpdatedNodeAction

[33mcommit 40f54bcfec8ecb944517f3fb0da3f4096c788d4b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 15 18:22:02 2020 -0700

    disable precision placement when using node editor

[33mcommit 0b7a873aca986243858898fd691cadd788597351[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 15 14:09:36 2020 -0700

    node editor node edits again

[33mcommit 9a977aeef7ebaca64376f0d6504e7c7c6137726e[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 14 17:42:29 2020 -0700

    Move stuff out of Experimental settings, add setting

[33mcommit b2a4e340206f0f9a6bc7ce07d9e44449895df5ae[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 14 14:26:39 2020 -0700

    make editor scale a float, remove 2^n scaling

[33mcommit ec64348fe98b48beb6beea67c336eb7e5d3d5397[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 14 12:15:00 2020 -0700

    add more precision error checking

[33mcommit 8c0fd6154d71b14323645e5cf89f4db3955b2589[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 14 12:02:12 2020 -0700

    cleaned up rotate lights code

[33mcommit a9453fa8eb61822441b5644e9660826bc8064508[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 14 01:23:59 2020 -0700

    actually fix noodle extensions local rotation

[33mcommit 62b301cb4f3c0fe7a06ff44e1bac8ebdec7629e8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 13 18:20:46 2020 -0700

    Precision Placement support for Bombs and Notes

[33mcommit 999af420f4556a982fbc668d1b4e06249a9a43a9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 13 18:12:02 2020 -0700

    Remove InitialBatchLoadSize, add PrecisionPlacementGrid setting

[33mcommit 522563e4c546c4687aec0ddd108342d5e412cb90[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 13 17:29:02 2020 -0700

    rendering, UI mode, bomb color fixes

[33mcommit 654bb9c07e920cd857e5ca3790122bb6676134e1[m
Merge: d4da73fa 4a37fb04
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 12 21:56:41 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit d4da73fa71a3b469f69cb74dbb89182669cb6177[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 12 21:56:34 2020 -0700

    Precision placement grid, obstacle support

[33mcommit c85510a3c657af30b9d8a0e15c65bb7a5c55e048[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 12 21:56:18 2020 -0700

    Restore SelectObjectOnGrid, fix spectrogram rendering issue

[33mcommit 4a37fb045f06d28c8504cc08aea117419a2aad91[m
Merge: 3d96f775 d17d8f04
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jun 12 18:02:25 2020 -0700

    Song Edit Menu Upgrades (#114)

[33mcommit d17d8f044a25d086c2c44d51f11ae1eeb74cdf8e[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jun 12 22:15:01 2020 +0100

    Add atribution to FileOperationAPIWrapper

[33mcommit f1ff97e45a8b3ae3622a484f255440bea4810e42[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jun 12 21:45:44 2020 +0100

    Review changes

[33mcommit 5ba31de16ec91a5af04fadce2cfb1c559a2e82f8[m
Merge: e3753949 3d96f775
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jun 12 20:50:55 2020 +0100

    Merge remote-tracking branch 'origin/dev' into songeditmenu-rebase

[33mcommit e37539494ece6cd5bb19be43e0bdb288d4afe47c[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri Jun 12 20:39:57 2020 +0100

    Minor tweaks

[33mcommit 3d96f77566c1ba9a650f6cd04c1e4d5088c2613a[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 11 22:30:27 2020 -0700

    placement fixes

[33mcommit 733b92a131296e5b8eaee7d286d4261db28737b9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 11 22:02:10 2020 -0700

    fix notescontainer

[33mcommit 616c4eb3fde14b0cefd929d8dd3b24f649842463[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 11 22:01:43 2020 -0700

    forgot to stage the prefab changes, whoops

[33mcommit a400954968cb681a0c7dca2536d7dd651ca98700[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 11 22:01:25 2020 -0700

    Fix rendering issues with notes on the grid

[33mcommit 56eb54eff8806b486020ecafbfab3110904090e8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 10 21:20:46 2020 -0700

    optimizations

[33mcommit 1a1043731fa5d9496b12e4dae492862198ff2c91[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 10 19:52:52 2020 -0700

    NE precision placement for walls

[33mcommit ae7f60673e3b7cd84a6970dd0a738a80f1ed720c[m
Merge: 6a205ac3 1d0caeaf
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 9 23:06:39 2020 -0700

    ChroMapper Refactor (#113)
    
    "Use your administrator privileges to merge this pull request."
    
    Aren't we forgetting one teensy-weensy, but ever so crucial little, tiny detail?
    
    ...I OWN YOU!

[33mcommit 1d0caeafe16415b3870773b65a3be558b6e68a04[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 9 22:28:50 2020 -0700

    fix more delete tool jankness

[33mcommit ea6e834112e57b8484c10e8c4837aea4752fe1ba[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 9 15:49:05 2020 -0700

    Added more togglable stuff to environments

[33mcommit 0c04bbe4fc6f7a6af2c0438f048840ce6e5bb02c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 9 15:28:46 2020 -0700

    refactored some placement code

[33mcommit 030cc29dd8844e4852b53243b12169491780a1cf[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 9 14:51:11 2020 -0700

    Remove unnecessary subfolder support

[33mcommit 359b055f5c006d0735c5b9f2b65718d6ef5c6a4b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 9 13:30:06 2020 -0700

    fix rings w/empty custom data not spinning

[33mcommit c5b0a7d478f892c7b056656b0ac966befe8c072d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 9 00:44:36 2020 -0700

    Delete SoftAttachToNoteGrid, bug fixes

[33mcommit 818667906330f5de9eac854490ac6213db3306b0[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 8 23:47:20 2020 -0700

    created a dynamic grid ordering system

[33mcommit 059c09f2135877189e6b5821fc1ced252c8ea09b[m
Merge: 1c5298be 6a205ac3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 8 20:41:52 2020 -0700

    Merge dev changes to refactor

[33mcommit 1c5298be8fad2443b934e2bcc9768217e79471d2[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 8 20:41:16 2020 -0700

    Upgrade Unity to 2019.3.15f1 for Mac compatibility

[33mcommit 30f9d94da9d7779b490583e8822c385861e06087[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 8 17:36:41 2020 -0700

    fix multiple bugs

[33mcommit 6a205ac3bcd9581d2c223e05ce6f44debf759a00[m
Merge: bee97d7c b4571c9c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 8 13:20:35 2020 -0700

    Environment updates (#112)
    
    Fix background lasers following different pattern than in the game

[33mcommit b4571c9cf2cc10473d09d8dd850c87f9f7012de2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jun 8 18:59:39 2020 +0100

    Change default laser angle

[33mcommit f2d5a6b8341d1f2bbf6979863a6821ac1deda6ac[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jun 8 18:55:10 2020 +0100

    Change default lasers too

[33mcommit 041f41d7ce5d94c705df8ab0e8e6df75ae22dd51[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 8 10:25:31 2020 -0700

    fix delete tool pepeganess

[33mcommit fb8d8d0c8245c3fa6d79a9a36a628db73cac0b73[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jun 8 15:24:29 2020 +0100

    Fix background lasers

[33mcommit a863768fbd2b32ab98400900f3b6f4167d6f12f7[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 7 23:52:37 2020 -0700

    Update README
    
    regarding a certain tutorial that has popped up recently
    
    also committing directly to master pepepoint

[33mcommit c9a688346493cee361375526c7ca27e4898c372c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 7 12:23:32 2020 -0700

    More simpler method of commit 92c73d4

[33mcommit 2bf8915cc49220712c8a8dec479671ae00752cc0[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jun 7 15:21:03 2020 +0100

    User feedback, refactoring, documentation

[33mcommit 92c7ed4afb9ede36ebe783f3d6a154a99c21fd25[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jun 7 01:32:19 2020 -0700

    fix saving custom colors in incorrect format

[33mcommit 64c869aef9951ac38e8734d809cba9af767a31ee[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 6 18:54:01 2020 -0700

    Update version to 0.7.0

[33mcommit 6d8839cc8995779e550911274de95b7da646034d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 6 18:53:43 2020 -0700

    Update Input System to 1.0.0

[33mcommit 5b89712ac732d13312331c717ee24f15a3d806c2[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jun 7 02:40:33 2020 +0100

    Refactoring

[33mcommit b3681d544e9a4606c1d5075e8230eb53b8a8ccde[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun Jun 7 01:41:14 2020 +0100

    Copy paste

[33mcommit a58ec9c5acc453b163ac32e3a8a4992b49019c7f[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jun 6 23:41:13 2020 +0100

    Fixes for OSX and contributor view

[33mcommit 84b9de73d0585acafe763d910482a5e8b6dae545[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat Jun 6 23:23:07 2020 +0100

    Redesign song edit menu

[33mcommit bcc42306e6e808933a3cb34a38f9bba832c0fa1c[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 6 13:14:41 2020 -0700

    comment the FUCK out of BeatmapObjectContainerCollection

[33mcommit 9d6c0469ef9549d797c18fdbf22a1cbe319a307f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 6 13:02:00 2020 -0700

    Cleaned up Chroma converter

[33mcommit 78a51d74f9f8c1ab2775d00e4632b66cda0d42c1[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 6 13:01:49 2020 -0700

    Fixed events that aren't being propagated spawning anyways

[33mcommit 0a862cfcc1172179e50389a0a6e3dfc21fbf1402[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 6 12:33:36 2020 -0700

    Past Notes grid works when scrolling (again)

[33mcommit 5d43a2cd4bc6b27380b61832addf381bf79bb28d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jun 6 12:10:46 2020 -0700

    fix shader not refreshing when deleting BPM change

[33mcommit a7d84040184fdabfac6b9b4f0e0b8b90669bd9f5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 4 23:56:31 2020 -0700

    Comments, shadergraphs for 3d rotation but broken

[33mcommit 0a7dbd55273a037bd3e89edc8627a684d40ae8ce[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 4 19:01:38 2020 -0700

    Add extreme angle warning for 90Degree maps

[33mcommit 22bc7b1ab600a83cb076691012b7614bf572d0b9[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 4 19:00:28 2020 -0700

    bug fixes

[33mcommit 407dc5c2a210d106f438580c6643914c07414419[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 4 18:59:52 2020 -0700

    NE vector3 for rotation support

[33mcommit b3478c8dce015054f3804ecfd84ea7bb5fdfe7d6[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 4 09:46:05 2020 -0700

    Optimizations and bug fixes

[33mcommit 3202bdefc3591ba651ae11efcb7b4abfbfffa9ee[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jun 4 09:45:59 2020 -0700

    "Convert From Legacy" now converts Chroma events

[33mcommit d464ca677a603d61429c9fcaa8dbe82c85051dcc[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 3 20:45:08 2020 -0700

    bug fixes

[33mcommit bee97d7caeafc9be2208aa7fee20482e63ea0c9e[m
Merge: 8816656b d30eec8f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 3 17:26:55 2020 -0700

    Contributors screen update (#110)

[33mcommit 07f694e8452dac2feddfdea676acf7b61e7a0bb8[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jun 3 10:18:13 2020 -0700

    bug fixes with chroma placement

[33mcommit d30eec8f78a6f8bc4b7216e1e5f9bdf0ad4fc527[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Wed Jun 3 17:52:50 2020 +0100

    Upgrade view to use new input system. Use input action to trigger tab event

[33mcommit da3920c6f1c7810ccdd08220f5bdd18775d6747f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 2 18:34:06 2020 -0700

    Add UI-friendly display names for easings

[33mcommit a02941664b20233f1398153612eed8933041118f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 2 17:18:03 2020 -0700

    Add easing support for Strobe Gen

[33mcommit d7ae3d8e6e8b713c04e9299d4009c982df0ace68[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jun 2 17:02:36 2020 -0700

    fixing Strobe Generator

[33mcommit 8816656b7bbbf55f692780ec21267a30809572bb[m
Merge: d1688c72 6616ae47
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jun 1 22:13:14 2020 -0700

    Package Zip button (#111)

[33mcommit 6616ae47f04b9fc193ea24f328aa7dfe6b036ba9[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon Jun 1 01:10:08 2020 +0100

    Add button to song info to create a zip to upload

[33mcommit bf0d66669a18517e4b72dd31677061b7f87bc38b[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun May 31 15:05:29 2020 -0700

    more bug fixes

[33mcommit 8320cf68adc9d68aebb066710af4a4a986480da3[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sat May 30 23:33:01 2020 +0100

    Update contributor view

[33mcommit 36128b6ebcdfd5820a36701bf1979fe36f1d9f4e[m
Merge: 56a934fd d1688c72
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 29 15:24:58 2020 -0700

    pull dev changes into refactor

[33mcommit 56a934fd77c690dc10c39d3b79d6efa303a33a7f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 29 14:55:44 2020 -0700

    i mean actually remove it this time

[33mcommit 5963def922b1fc3a5dd8452091c41cc0b2dca674[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 29 14:55:05 2020 -0700

    Remove (1) from solution name

[33mcommit 5b47b811471ed5d346a9b23693cd63f870b39223[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri May 29 14:53:32 2020 -0700

    more bug fixing from refactor

[33mcommit 3d5cf7321969be0c2fff4729f43d2302a4eaffa1[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 28 00:51:21 2020 -0700

    oh yeah things go vroom vroom now

[33mcommit 0c07ce1b6305239ef6f748c466f5715b25ab8fda[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed May 27 15:05:38 2020 -0700

    deleting is mega broke, checkpoint saved

[33mcommit d1688c72a6c212783ac5b31b659279ef4cf704c7[m
Merge: 574c0feb 9aa25321
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue May 26 18:23:29 2020 -0700

    Big spectrogram upgrade (#109)

[33mcommit c1fa7833bc40fe44bd7eb67e55831eec3300aee4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue May 26 15:49:00 2020 -0700

    Fixes that belong in 'dev' but im lazy to switch

[33mcommit 9aa25321afa1e4621bd02a470733453337aad9aa[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue May 26 23:07:09 2020 +0100

    Add 2d waveform, optimise 3d and 2d FFTs, remove cache

[33mcommit bb0056e72d725bc2eb76fc1594e2a640322183de[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 25 12:44:10 2020 -0700

    now we start fixing shit that broke due to refactor

[33mcommit a7e5039541b7b18373b3f03133bfdadc23a89c74[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 24 17:19:54 2020 +0100

    Fix save triggering on read, remove some logs

[33mcommit a09b7597e3b35dfde31703e37c4a92472de556f4[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Sun May 24 16:06:39 2020 +0100

    Add threaded DSP to generate spectogram

[33mcommit bf426e3d250f76a58a8d913f47136c42f006b832[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Fri May 22 13:38:31 2020 +0100

    Fix spectogram alignment

[33mcommit 466b351c0c94056b409684b0803cdcdec8db3a46[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri May 22 23:52:53 2020 -0700

    with this refactor ive thrown all hope of committing small, frequent changes out the window

[33mcommit 1f14f2a161b7fc671b9ec908946546c78811d2fa[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 21 14:45:05 2020 -0700

    haha loading times go NYOOOM

[33mcommit 574c0feb11fc9edb714e33972942925a233cf9d1[m
Merge: 18e3799e 3b10ef68
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed May 20 10:01:05 2020 -0700

    Fix Box Select when dragging right to left (#107)

[33mcommit 512a1095bbbf0f6acb4dae42132be047b1940bbd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed May 20 00:17:27 2020 -0700

    whoops ive been so focused ive forgotten to commit

[33mcommit 18e3799eb1b96ca351ab2fdc689c49154c2a63d5[m
Merge: fa0ee9de a6dcfa1c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed May 20 00:11:47 2020 -0700

    Merge pull request #108 from Top-Cat/steam_discover
    
    Steam discovery doesn't work on fresh install

[33mcommit fa0ee9de3be02cea9883df95e78f81cd55890637[m
Merge: c1fbc481 22d7d94b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed May 20 00:11:00 2020 -0700

    Fix Grid ZDir shader on MacOS (#106)

[33mcommit a6dcfa1cd289d1a1e28d11491bc6eb041b495c66[m
Author: Thomas Cheyney <github@thomasc.co.uk>
Date:   Mon May 18 21:19:50 2020 +0100

    Fix steam folder discovery on fresh windows install

[33mcommit 3b10ef68ff238d8a457d3fd2d4a17d39a675cc7b[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue May 19 16:31:35 2020 +0100

    Fix placing blocks when finalising selection

[33mcommit 28c972d917b74d904af4b2497973f0bc228f6d65[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Tue May 19 16:05:16 2020 +0100

    Fix weird note selection box behaviour

[33mcommit 22d7d94beb39f952a99ef28f75b501556d68b165[m
Author: Thomas Cheyney <top_cat@thomasc.co.uk>
Date:   Mon May 18 14:26:06 2020 +0100

    Shader won't compile on mac with huge bpm change arrays

[33mcommit c1fbc48166008344edb430077e76f00ec9ce52d7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed May 13 21:07:43 2020 -0700

    ignore cache folder

[33mcommit 3acab40481bf008fcbf6badce6fc895b68fbd2cb[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed May 13 12:25:40 2020 -0700

    bug fixes

[33mcommit e16dec90727ebc7134c4a0872f4759497411a241[m
Merge: 2bc46a6e 2c0d1393
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue May 12 14:43:43 2020 -0700

    Update master to dev branch (#105)

[33mcommit 2c0d13931c712863df4173e7e4842afaffaefafd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue May 12 14:40:35 2020 -0700

    be sure to sanitize user input, yo

[33mcommit 4864e5dfb1c2f5b1ff6cee19fc3f6b1014a2af29[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue May 12 14:39:10 2020 -0700

    commit not dum code

[33mcommit d8ddafcbdaa99a7b5b8159a30aa540ecb6bb2476[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 22:13:49 2020 -0700

    Ring Prop -> Light Prop, supports every light group

[33mcommit cd84fefe69c1582d3654b482236f74421832c631[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 18:57:16 2020 -0700

    CM Info revamp

[33mcommit e07f8a71ef02c18246a8d6cd48c5024bb1b57341[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 18:39:53 2020 -0700

    Add plugin section + restructure

[33mcommit 4218c2b6f6ecc5e0f61179c17c4596ef47c8dcbd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 17:26:52 2020 -0700

    "Save" button now also saves Info file

[33mcommit 8a328d46a4bb780bbd3689c16c40ac13beed690d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 17:17:55 2020 -0700

    Box Select rounds to nearest grid space

[33mcommit 5c76b44b610c037c5da3ca4391c0c4e3f5f3e476[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 13:59:21 2020 -0700

    indy has big gay ✔️✔️✔️

[33mcommit b147608b9223f1cfdb5b177e87a713f94909c556[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 13:50:24 2020 -0700

    misc. fixes

[33mcommit 645decb2433dd1445a9b0545bd5a1c7c476305e9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 13:50:07 2020 -0700

    fix Timbaland and Panic environments

[33mcommit b53af851167ccfeb8917065c702b0167b17d6601[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon May 11 00:25:15 2020 -0700

    even more fixes

[33mcommit 96048089b2fa4ddbff9f8d5e082412e29333f7e1[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 10 22:53:04 2020 -0700

    More fixes! Get your fixes here!

[33mcommit b2d097007adac5439e6c3afe25f8977bf35da70a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 10 21:59:39 2020 -0700

    lolPants aboutta slap us with Info.dat

[33mcommit 8ca2faa29a7d3b59f1954a73b6f6669576dbda78[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 10 21:09:28 2020 -0700

    disable action maps in options, node editor

[33mcommit 6d21d27f16984c4b5d27995106ce897ff8021f81[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 10 17:31:38 2020 -0700

    fixes

[33mcommit 73ee1071ca967b5e08488d6ff7766b2b4c2d59ab[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat May 9 23:53:42 2020 -0700

    final waveform update + ring prop fix(?)

[33mcommit 74201c5a3940bbb907c94ec210c3c053e92dab1c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat May 9 11:16:12 2020 -0700

    take that sample count and crank it wayyyyyy up, morty

[33mcommit e4dea7f6acbffd67ce94c2d5b265f886cf79f401[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri May 8 17:58:19 2020 -0700

    NE support for Past Notes grid

[33mcommit 9f6080dd6cf82b3eb59f8a9e921871fb214f89b7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri May 8 10:02:18 2020 -0700

    made more fields decimal numbers

[33mcommit 758512e4b3073380553b7e97117569bcc89e1fe9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 22:44:37 2020 -0700

    more fixes

[33mcommit 2d8a60ceb147d88649961a86b970c9f43c09c40c[m
Merge: 81cda639 a10baf11
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu May 7 22:06:23 2020 -0700

    BPM Changes PogU (#103)

[33mcommit a10baf1145ce677846e762b70211c735c024e943[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 21:28:13 2020 -0700

    reduce artificial delay for dialog/input box

[33mcommit 33aee63bab08434050c8985d3b106623d14e0917[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 21:27:52 2020 -0700

    fix first frame accidental click

[33mcommit 83903639372cbaafa5cc40a5969162ce04e41dde[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 20:58:38 2020 -0700

    simplified name for keybind

[33mcommit d2e350bf2f02849c6a66fe9ce9c90e4042e1a25c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 20:58:26 2020 -0700

    add artificial delay to dialog/input boxes (reduces accidental first-frame clicks)

[33mcommit d27c2a2d2151a5524ec784e6054f9098f3aeb02a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 20:42:43 2020 -0700

    make measure lines play more nicely with bpm changes

[33mcommit 70b01ac717862bd172df587a258f401639506400[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 19:46:49 2020 -0700

    inputs, fixes

[33mcommit a9de05b75f31e89d011668604f08f2433d19b31a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 19:09:36 2020 -0700

    woops did math wrong

[33mcommit 097ff35f5834e13cd45d1af5286494513e44beef[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 19:08:16 2020 -0700

    added some fool proofing

[33mcommit d830f811fd6ea0e3a0ec38fa4fcf01b3d6115e3c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 18:09:39 2020 -0700

    Placement + bug fixes

[33mcommit 09ef3d3619dde7c218fd469b6ea30c8030a3a3c7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu May 7 14:30:45 2020 -0700

    Update block + grid for BPM changes

[33mcommit 63c592e4d21232832e309341edaa9cecba9d8357[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue May 5 15:43:10 2020 -0700

    fix scrolling issues between BPM Changes

[33mcommit c186e0da7a0b79f7a51709aa0f3978a8e69f3c11[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue May 5 14:36:31 2020 -0700

    grid shader resets after a bpm change

[33mcommit d775e3bef2982374ae297ac32f281b8ca1d12295[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue May 5 14:18:32 2020 -0700

    placement now snaps to bpm beat

[33mcommit 62dbeba63e5fee22b7426f8ea368fadd70e3a111[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue May 5 13:32:34 2020 -0700

    fixed scrolling

[33mcommit ee65d2c64396e9821b7743df7810a1edeaf3b881[m
Merge: b083244f 81cda639
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 3 21:34:04 2020 -0700

    update BPM Change branch to dev

[33mcommit 81cda6397cafd1d352392fc1c30ae39f1ea1a267[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 3 21:27:54 2020 -0700

    fixes

[33mcommit b083244fc24f76ad2c5e7de1e9b42506aa683a71[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 3 17:48:56 2020 -0700

    scrolling and beat text now snap to bpm beat

[33mcommit dfd984e37c01b5fc35f9e50a0b38719124348fdd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun May 3 16:40:40 2020 -0700

    fixes

[33mcommit 29daf8d5cee7d31344eb6982c4677c57e99d346d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat May 2 17:38:04 2020 -0700

    I FUCKING DID IT

[33mcommit 62f1e23c0aa99da336a90fe9295dfd62c0302b12[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat May 2 17:10:04 2020 -0700

    made BPM changes align mostly correctly

[33mcommit 552c722877e6823f60de86d90c2b186243606153[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat May 2 15:53:28 2020 -0700

    misc bug fixes that should be on Dev branch but i dont git stash too good

[33mcommit bae28cca557d3c373fb288edac6605f60aa44da6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri May 1 20:35:40 2020 -0700

    more fixes

[33mcommit 57a3d1aabbb58e7e1e74a4c3d7234f7b69b916af[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 30 21:20:20 2020 -0700

    Well, they say third times a charm...

[33mcommit fb71c3a4c255a0d392a3cf0fc9cbe6a6ed822c71[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 30 21:19:40 2020 -0700

    PLUGINS update

[33mcommit 070ce17d486f5b94371d73ebe7409fa5b562045d[m
Merge: 240d595a 92321e1f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 30 20:51:12 2020 -0700

    Plugin loading PogU (#102)

[33mcommit 92321e1f9170b987130fd6fd8cde7dcfdb607531[m
Author: dcljr <dcljr@me.com>
Date:   Thu Apr 30 18:34:55 2020 -0700

    Added very basic plugin interface and loader

[33mcommit 240d595a6ece06c7ab92625de25b4d16e9281818[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 27 20:43:06 2020 -0700

    Make it an error so people can see it LULW

[33mcommit 392eedaffcb14b636a2826629476b2b16915c54c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 27 20:42:51 2020 -0700

    Add file name check to custom platform loading

[33mcommit 62d71b27d5b3eca4a61cfa8b9ab3584e05a38139[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 27 20:33:07 2020 -0700

    add prop id to chroma suggestion check

[33mcommit b655ad830c81c7b0e6848798a5fb81171da3fb91[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 26 12:17:03 2020 -0700

    forgot performed checks for Undo/Redo

[33mcommit 399b8ae180b1df47e07342c20507d83f311638c5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 25 23:36:40 2020 -0700

    Update Patron list

[33mcommit 034a26bab741cd3dc2337e125147dcee8a23e3c3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 25 18:56:52 2020 -0700

    fix custom colors

[33mcommit bd2bafe7939ab446b1dddd447183774aa627495e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 25 17:12:47 2020 -0700

    Add basic contributors screen

[33mcommit 15209a6e3f88278668bacab167a894333432d99e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 25 12:49:26 2020 -0700

    Precision snap persists between songs, fixes #100

[33mcommit 61ab42f70d469dce32c5db5e531db31ac85d6909[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 23 21:20:53 2020 -0700

    bug FEEEEEX

[33mcommit 9fb233d06863edb9534eb6fd6341a586d1434207[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 22 16:45:58 2020 -0700

    More keybinds, bug fixes, and tweaks

[33mcommit 8711f31c51abaa91fac9438d31351cb561f9f7d5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Apr 21 19:30:30 2020 -0700

    More bug fixing

[33mcommit db537098c241951984aac9196598c8e000e9b478[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Apr 21 19:14:00 2020 -0700

    More bug fixes, add InvertScrollTime

[33mcommit 3a20a6d733b4695ce44597c48bc58405fd20a669[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Apr 21 18:54:12 2020 -0700

    Bug fixes! Bug fixes galore!

[33mcommit 2bc46a6e287639daefd9b2db5e28ff3810be4a50[m
Merge: f059156e f54d9282
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Apr 21 17:03:16 2020 -0700

    Merge pull request #98 from Caeden117/dev
    
    typo in README

[33mcommit f54d9282348cda80f8672c3c3f3511a7598fd7af[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Apr 21 17:02:46 2020 -0700

    typo in README

[33mcommit f059156eca8787cab5f4a8ef48d501d66751bed1[m
Merge: 8a7b3eae 9a0ab842
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Apr 21 17:00:44 2020 -0700

    Merge pull request #97 from Caeden117/dev
    
    Development (4-21-2020)

[33mcommit 9a0ab84290167288446d0b700e913f7db2be4b3e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Apr 21 16:59:40 2020 -0700

    new Chroma ring/laser speed events

[33mcommit f0fb8d7138bbb21ac4faae8ee9b526aea7c718c5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Apr 21 15:13:22 2020 -0700

    version bump

[33mcommit 3f28bf4c2cca3a6b42f8bf5f5149117c37d911a8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Apr 21 13:20:28 2020 -0700

    Editor field now uses Name/Version

[33mcommit b26b4d5cc5a0cdbf2d4738b0c19709750262b93b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 20 14:50:20 2020 -0700

    more input issues fixed

[33mcommit 0ec3d575b77ce9e712335318b82a54c9db02540c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 19 15:31:07 2020 -0700

    remove unneeded TODOs, bug fixes

[33mcommit fc3423463b91f5d0b00330e9196468f7a97caa01[m
Merge: 9e78bfeb fbdf5ab1
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 19 14:56:56 2020 -0700

    Merge branch 'udoheld-more_oculus_store_locations' into dev

[33mcommit fbdf5ab139ad18e3504d5b3d07a6ae2f8f983c5e[m
Merge: 9e78bfeb 5fbeda11
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 19 14:45:35 2020 -0700

    Merge branch 'more_oculus_store_locations' of https://github.com/udoheld/ChroMapper into udoheld-more_oculus_store_locations

[33mcommit 9e78bfeb55a7c9b5800fd289ab5ba7ec74026dae[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 19 14:44:43 2020 -0700

    add line layers to ME detection

[33mcommit 9e3043982bb80bcc13aef3dd322edf5861a343d2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 18 21:47:14 2020 -0700

    fix more input issues

[33mcommit 33dc9a0bb4fd6d2836eea1c29663386f9edd8a18[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 18 15:41:08 2020 -0700

    more chroma colors support

[33mcommit 2f4788d5288c4f22c3861784b1134eec42379da5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 18 12:28:19 2020 -0700

    bug fixes, new option

[33mcommit 5fbeda1148f17e036d8b950cf5c1586aa6d2e54f[m
Author: Udo Held <github@udoheld.com>
Date:   Sat Apr 18 23:31:03 2020 +1000

    Added support for more oculus store beat saber installation locations.

[33mcommit d8011e391850df62707376dd8bcc1472399b5318[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 17 16:33:26 2020 -0700

    how many bug fix commits are we makin

[33mcommit fad5c47b7924de32a53445536a33adad237c5440[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 17 15:03:52 2020 -0700

    even MORE bug fixes!

[33mcommit fe16b7e1f2e4d4451066d93dac2025054f936957[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 17 14:45:48 2020 -0700

    more fixes

[33mcommit 4b8eb2b54906bab482b06b789c7fefc199483211[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 17 13:31:26 2020 -0700

    bug fixes

[33mcommit 5b1e13a6a2aefee96ee651f5f2a600ed21ed1a7d[m
Merge: 5e9f6bd4 91924192
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 17 11:45:16 2020 -0700

    Merge branch 'udoheld-guess_installation' into dev

[33mcommit 919241923d20fd93084b449bb4d53543b6570faa[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 17 11:44:57 2020 -0700

    fix Oculus registry location

[33mcommit b5a834ad457e640dc8a3bf2bcff02d8eea9a3cb3[m
Merge: 5e9f6bd4 64182796
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 17 10:30:07 2020 -0700

    Merge branch 'guess_installation' of https://github.com/udoheld/ChroMapper into udoheld-guess_installation

[33mcommit 6418279684cb70aa75a159f978ccf50059b2cd3c[m
Author: Udo Held <github@udoheld.com>
Date:   Sat Apr 18 00:01:51 2020 +1000

    Guess Beat Saber Oculus Store installation folder.

[33mcommit 5e9f6bd4d503e398e019ac9bdfe266479a368351[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 16 21:53:10 2020 -0700

    Added DeselectAll w/predicate overload

[33mcommit 1ec065024915b957d972971c62265fbbc976eeec[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 16 21:48:08 2020 -0700

    bug fixes, tweak event labels

[33mcommit 1c03e23913746721eefcd8a86f4a7d09c5b4ee9f[m
Merge: c2510a1c 1031a00a
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 16 20:03:13 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit c2510a1c6816db2c59d782f5a762a14f07e9810d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 16 20:02:59 2020 -0700

    Add Fitbeat, refactor lighting

[33mcommit 1031a00aa60440b45a9ce0f284c531af56209fcd[m
Merge: dcc51dfb e4008e6e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Apr 16 10:17:54 2020 -0700

    Dev setup guide (#92)

[33mcommit 15ba35a75a328fcce3039375f5b16e90601681e0[m
Author: Udo Held <git@udoheld.com>
Date:   Thu Apr 16 19:51:30 2020 +1000

    Guesses Beat Saber installation directory for Steam installations.

[33mcommit e4008e6e50c875d5c425073fda2245a5ebeb120e[m
Author: Udo Held <git@udoheld.com>
Date:   Wed Apr 15 21:40:06 2020 +1000

    Added build guide

[33mcommit dcc51dfb4bd1ef4649f41d43ab0ae3ddee6082f0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 16 00:18:06 2020 -0700

    i forgot Ctrl-H for UI modes

[33mcommit aff2eb93f94fc1e4f7cdb6f7fc0f1a0285debac9[m
Merge: e23cfc30 5c0a46f5
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 23:50:12 2020 -0700

    merge dev and new-input-system

[33mcommit e23cfc306e7b3d656e36c2a283a5394a3277ab3a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 23:47:35 2020 -0700

    misc. stuff before merge

[33mcommit d37ba2330309b0bc760eb28953c13042366b31ef[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 23:24:40 2020 -0700

    Change Keybinds settings icon

[33mcommit 8d8953a7e09b40524a5a6803e805757404088627[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 23:19:23 2020 -0700

    Filter keybinds that dont rebind properly

[33mcommit 3b3fa10e429a04a1b5a320547ff08e0d63b1445e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 21:25:08 2020 -0700

    final touches

[33mcommit 88729c7e29c2fd21752ce156456b14196eafc13a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 21:22:34 2020 -0700

    Keybind overrides saving and loading

[33mcommit 672a5f81ea845f04acc42edffe7281921df5320f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 17:52:08 2020 -0700

    Filter actions/keybinds from showing in UI

[33mcommit b34a9f76915023caecc9b40cb90a961558ea141e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 17:26:19 2020 -0700

    More work on keybinds UI

[33mcommit 59c85f7e88c6fb69731b15c659bf80962af35f1a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 15:34:25 2020 -0700

    Rebindable Hotkey UI

[33mcommit 7e747314645809561d2cb81486d1484e7c509108[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 15 14:19:58 2020 -0700

    modified CleanObject function

[33mcommit 1d49cec2e37f5dd6f7648b38484914e2d9090f70[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 19:17:07 2020 -0700

    fix NE local rotation

[33mcommit 0290b06d6f6f00fadb3fd10bb7a343c12af5f959[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 18:10:17 2020 -0700

    Add CM Defaults control scheme

[33mcommit 971d4c757620e2dead0859d040d8233c700434db[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 18:06:20 2020 -0700

    NE requirement + fix dum bug

[33mcommit dc59a15b5e378d5be58e1f0d5a8a7661a712bbe0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 15:00:51 2020 -0700

    Node Editor and camera disables keybinds

[33mcommit 8a3e6ae08fd930a0610765cea4cfe1e90ebf3b47[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 15:00:37 2020 -0700

    Fix Timbaland

[33mcommit db5dc3156937683bed22393f3ecb0a0c3a8e95f6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 14:33:50 2020 -0700

    Begin action map disabling

[33mcommit 0e0d3d6860b12b49babe262c92d11ab2a31341a9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 14:01:36 2020 -0700

    Finish off CM keybinds

[33mcommit 962c46e3df538295c45a38b9d531e872407f85a0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 13 13:55:17 2020 -0700

    more keybinds + tweaks

[33mcommit 529c1cf1535145ce6a64dcf0cab4e7837a10069b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 12 22:46:43 2020 -0700

    Hovering keybinds for objects

[33mcommit a3978c24f13cd49bb7eb361ed2dd610a4fd43d86[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 12 13:58:20 2020 -0700

    Begin keybinds for map objectse

[33mcommit 6c66eb0082f819be78c1dba9722213409caeb1a5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 23:37:46 2020 -0700

    Credit where credit is due

[33mcommit a57938d58f3a6582ec6fbb2bad8461f9d1c3b717[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 23:26:34 2020 -0700

    (unrelated) reflections are now PogU

[33mcommit 46651c9c91352bc6e24a9644fbf61050eef1547e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 23:26:16 2020 -0700

    ATSC Keybinds

[33mcommit dd809d91b0d5732736a32c5c4fc67a50002d3c0e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 18:27:07 2020 -0700

    Add rest of keybinds from KeybindsController

[33mcommit 5c6111f78921b2ba8ec0ed2df0239f4a4f11eb06[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 17:32:42 2020 -0700

    Saving and laser speed keybinds

[33mcommit a6d2b8930f47b4613c2a716d7310815a1393d12a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 16:08:18 2020 -0700

    More UI keybinds, fixes

[33mcommit 906b12bb2569dbd039bf62a87d882ec6917348c3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 15:24:40 2020 -0700

    UI keybinds, bug fixes

[33mcommit 869726b74b46f7064a58c84fb19e9ca5bd694a26[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 13:19:16 2020 -0700

    note and event specific keybinds

[33mcommit 529e4c512a586d3f4e591e4f7e3250a9c3687a41[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 11 12:17:22 2020 -0700

    Added cancel keybind for placement

[33mcommit 398620b890441fa9f2feba1bf3b0132b2e2b4acf[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 10 20:31:25 2020 -0700

    Placement keybinds + bug fixes

[33mcommit f45f4f93d809c3bd47aab00ff02299e7e6b9ecc4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 10 19:36:34 2020 -0700

    Now supports multiple of the same interface

[33mcommit 5c0a46f5bdedded16018ead663b28032b659290d[m
Merge: d836a538 aa9bd94b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Apr 10 12:38:34 2020 -0700

    Time displays in monospace (#91)
    
    Make time display in monospace

[33mcommit aa9bd94b7e30b725910074e7bb452f8e8af2e9b1[m
Author: Zhaey <me@zhaey.pw>
Date:   Thu Apr 9 23:40:08 2020 +0200

    Make time display in monospace
    
    Now on this branch.

[33mcommit 85154ec154c778947c9828fb6626a3ca6015dd3a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 8 22:47:47 2020 -0700

    more keybinds, simplify internal save/load

[33mcommit 12753afa47bdadfe62a683e73a1cce9cdffcc3c3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Apr 8 13:18:26 2020 -0700

    Selection keybinds mostly implemented

[33mcommit 8b1d943974f3a799e9e47e6c46a3d9c92ab2445b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 6 22:46:20 2020 -0700

    Add Utils + Selection keybinds

[33mcommit b1efeeb3fde5f15b888976bcc32a366e31c5b041[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Apr 6 19:44:40 2020 -0700

    Remappable hotkeys, finish camera controls

[33mcommit 65e379717e44dca1f5257ea06c6837b3960df385[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Apr 5 18:44:55 2020 -0700

    Basic Callback Installer + Actions set up

[33mcommit 68a788b74530192c024d8b5b81e1a19b6582cb75[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 4 16:47:08 2020 -0700

    I caved in. Let's do this.

[33mcommit d836a5383faa783897d90eadf9a14242a4753891[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 4 16:42:29 2020 -0700

    Save Chroma suggestion

[33mcommit 012102366385a9634842610270b7caee8405dd9c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Apr 4 12:45:54 2020 -0700

    fix Timbaland refusing to load

[33mcommit 254cc8d70bb8506aa8664621e572e24010f89254[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 3 14:52:18 2020 -0700

    add Chroma gradient support

[33mcommit 30fa314d952ce202ade6d9ff3028245630ddc66b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 3 14:52:13 2020 -0700

    comment out green day env.

[33mcommit a45c6514a4d05470a3c2fb9ae7bf60fd71e2dcb0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Apr 3 14:51:54 2020 -0700

    made platforms more accurate

[33mcommit a0038c2877aca21df97646329a08a76788d79459[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 2 14:00:11 2020 -0700

    bug fix

[33mcommit 29c61eedc6028cd1ab6d63d9f69f5179b31def13[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 2 13:29:37 2020 -0700

    fixed noodle extensions global rotation

[33mcommit be3c542e32a5842bb642e834d349bfb8657fb79e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Apr 2 13:12:29 2020 -0700

    god i hope im done with this

[33mcommit 6a037f18b1ee1754a5f15c299f35adc280478337[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 31 16:17:25 2020 -0700

    Remove navigation from UI objects

[33mcommit 193397e511df279674cee74b77b57d78b5c109d7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 31 16:06:22 2020 -0700

    tweak timbaland (need to get more info)

[33mcommit 207315e955f605afdc65a0612da188d810cd188d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 31 16:06:14 2020 -0700

    Fix custom data shit

[33mcommit 1c817f8a2f8038a61a13d3d25e0a10dfdc96ba8c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 31 16:06:04 2020 -0700

    Update CM ring code

[33mcommit 9d4d4dc5d2e826c257b1ace63eeaaad2784ce78e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 31 13:31:06 2020 -0700

    Keybinds ignored if Node Editor is open + TimeHelper

[33mcommit e9accb44f022260ea3dd5da5968461d518847985[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 31 13:30:38 2020 -0700

    Fix towers in Timbaland

[33mcommit e260eeb756f1831a28b03d8bee66cc0b42d8be41[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 30 13:06:41 2020 -0700

    Unpatch Harmony w/ID instead of everything

[33mcommit 8d8ed796fdb73f11f0b915bd59ac8141ab08a3cb[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 23:39:58 2020 -0700

    fix hovering over note grids before map loads

[33mcommit 24983272c36c7f7a60f07b466a12f2826f346343[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 23:34:46 2020 -0700

    Add toggl for better bloom

[33mcommit 0e3d6ed35faa2b5b2c5e085601f6a3bc779f0455[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 23:29:22 2020 -0700

    whoops wording

[33mcommit 59e0691ebb6c4726774ae11b94c9bb1d256e3d37[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 23:24:03 2020 -0700

    Use Harmony to achieve better bloom

[33mcommit 74baac2cc5b22c6f867e68e43913b315ea826e7f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 18:28:06 2020 -0700

    fix note rotation not being visually applied

[33mcommit ca3170d4291e1b899d1629d7ddec24665ee8f2a8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 17:09:31 2020 -0700

    Add disableable objects (L key) to environments

[33mcommit fa2200d5b3345a3fd99210decc112c902252a6c9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 17:09:12 2020 -0700

    fix notes not turning transparent again

[33mcommit beb24ce4ab5aa671669991cd37148ecfb5664ce9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 16:51:50 2020 -0700

    finish timbaland, fix rotating lights

[33mcommit 6547a082a0dc254444835c4ce1942d64c596f412[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 16:32:14 2020 -0700

    Add Timbaland

[33mcommit a83d31f1d1b4750e816f27536e7e96f6f1317973[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 16:32:09 2020 -0700

    Add overrides for light IDs

[33mcommit d47de2df535b440368892051cbd530173cd5896b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 16:31:53 2020 -0700

    Fix Custom Colors, add more platform colors

[33mcommit a5f353e515d3d1352c2de6bc47580e15e2e10ff7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 29 14:54:01 2020 -0700

    say that to MY face!

[33mcommit 5ae708893d9938e59f3ef9cfdc636e65132238cb[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 26 12:35:02 2020 -0700

    bug fixes

[33mcommit 0edea1164bc41c5d99262751ab7642a05375b08e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 26 12:34:58 2020 -0700

    Implement SimpleJSONUnity Color extension

[33mcommit dce312347f0ab15d0a4960428d1a31896a2bd459[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 26 12:33:58 2020 -0700

    Expand Color in SimpleJSONUnity, PR soon

[33mcommit d854575d48e6841293efc7cbf0ec7b1d3c0fec45[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 23:31:48 2020 -0700

    Update SimpleJSON, fix CleanObject method w/Clone

[33mcommit 273eb06f24dd491d0e9d5d531be17e0202cbbb40[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 23:31:34 2020 -0700

    Better performant code w/events

[33mcommit cf85b6fa9f8b51a8621c7b43819e8cbba41ed647[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 21:00:06 2020 -0700

    Big performance gains when pausing/playing songs

[33mcommit 2c39502f573d4b2545c9e1481480773644df182d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 20:59:35 2020 -0700

    add IsChromaEvent, loading new Chroma colors

[33mcommit 8500b2e08512521ed13eab14c87d0a30f264aaca[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 20:58:39 2020 -0700

    more null checks

[33mcommit f6342e6e338d9e4cd1a47aa612ef1256a560568c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 17:52:17 2020 -0700

    add more null checks

[33mcommit 66eeaceeff69015cd12c895cf68cf622962d367d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 16:53:02 2020 -0700

    improved customdata object cleaning

[33mcommit 5b98a46a98f9eed860d2cb74481859db0acc0882[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 25 09:34:51 2020 -0700

    change warnings to suggestions

[33mcommit 4cf9293b520f3b8b8b901a61c377b50697d7ce6a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 24 17:06:35 2020 -0700

    swap waveform time from fixed update to regular update

[33mcommit 635fab1ea53f2a3d13286046221b6c3d7f7dc29f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 23 22:45:13 2020 -0700

    Add custom field if it doesn't exist

[33mcommit 35d016e34825834ec284e19451a667a0a0ffadd7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 23 22:45:05 2020 -0700

    Optimizations

[33mcommit b04ca8a4006aedb20315402ff12f3dabed62f942[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 23 16:51:59 2020 -0700

    fix box select + dynamically selects/deselects

[33mcommit ec74242e983ed703dc956efd730a6cb34737cd0e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 23 16:51:47 2020 -0700

    more null checks

[33mcommit e32b18b39052d76cf947ed09a755945aa83c0db8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 23 16:51:39 2020 -0700

    fix chroma UI not affected by ctrl-h

[33mcommit b2a91ca205f72b32b5ac9b249fb97755fa40417b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 23 15:34:29 2020 -0700

    Add null check, remove unneeded property

[33mcommit 3587c4782731279bb906fbe976f633067de000ca[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 22 15:19:06 2020 -0700

    fix being able to place events on top of one another

[33mcommit ecb2fa87c5d8342cf6120c8b6c94e99f3e1d22cf[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 22 15:18:56 2020 -0700

    placing improvements

[33mcommit ebcb59fc13f17746fb839359f41a542810fcbe6b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 22 15:14:17 2020 -0700

    replace GLOW_RED/BLUE materials with light

[33mcommit 5b4ccf0a6eec8246ef289220e94d1f65a39b244f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 21 18:10:58 2020 -0700

    Remove Strobe Gen's req., selection performance bump

[33mcommit a6353f5a3fc12c95c10371ebbf84aca42c4a0a7d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 21 10:13:30 2020 -0700

    fix song time text cock blocking raycasts

[33mcommit b7491795e9b4f7200134ad6a157ff7d9105769ff[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 21 10:13:20 2020 -0700

    optimizations and bug fixes with collections

[33mcommit 1a8f5a1ba34138ca8efbb40dcb3c3a085e9bd9cb[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Mar 20 11:36:00 2020 -0700

    Change selection from List to HashSet for boost

[33mcommit 4be090cd94cb6ebfb20233cde690ee5a7c5147b9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Mar 20 11:23:17 2020 -0700

    bug fixes

[33mcommit 724cfe1d06b13d2c2b1b3f8e6aa560a40b46da35[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Mar 20 11:23:01 2020 -0700

    begin noodle extensions support

[33mcommit 8b1934b8bee995dff8f343831eac79cf7c5c44a0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 19 11:42:55 2020 -0700

    first custom platform fixes

[33mcommit 068caf88cc6248bc2ba575fdbf5c8f4cf4de4d7a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 19 10:21:27 2020 -0700

    Fix bugs, remove unneeded JSON fields

[33mcommit 9101d4a26351c0715f5f212d8a2eb82a43a3bc8e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 18 17:57:30 2020 -0700

    Node Editor uses N keybind by default

[33mcommit bbf0faafa90e20028f11b6c17e6a237fab43da78[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 18 17:57:17 2020 -0700

    Place Chroma events toggle is remembered

[33mcommit 6f13da84f4a60a28928ed33171e077425a1b4ac4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 17 17:12:19 2020 -0700

    fix all notes/walls past last rotation not assigned to track

[33mcommit 6b2b963a81553d979de5f01c6dbc25b612e741ac[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 17 16:18:47 2020 -0700

    Add setting to toggle reflections

[33mcommit 734cacba4428197627bc159e76be957d0bd15acd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 17 16:10:00 2020 -0700

    Visual bug and box select bug fixes

[33mcommit 8a7b3eae0be479dc99d1d967803b73f25f0c0e4d[m
Merge: 74f9eff2 5aa33726
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Mar 16 19:27:48 2020 -0700

    Update master to dev branch (#89)
    
    Development: 3/16/2020

[33mcommit 5aa337261c1815be99f9be08553f347f787f2d50[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 16 19:18:40 2020 -0700

    Add auto sizing to environment dropdowns

[33mcommit 1849f58fe585d64f2c9608aeed9c8e38651e2958[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 16 19:05:24 2020 -0700

    Past Notes now updates when setting changes

[33mcommit f43d385cce683cd3eaa607717a78deee59004d2a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 16 18:55:08 2020 -0700

    Fix KeyNotFound and NullReference exceptions

[33mcommit 0ee27d0c51f0353356f2e2c6c22ab948bc0b91e7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 15 22:40:15 2020 -0700

    Add dankness

[33mcommit 0277aafb0a4edf353a188153c70c16ffafe8b31f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 15 17:38:34 2020 -0700

    Apply auto sizing to slider value text

[33mcommit b02587355c68a947bbc51b2534b8870e759a8378[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 15 17:35:32 2020 -0700

    Measure Lines have black outline, can toggle render order

[33mcommit b6a02f58ef8b17bdfe335be9641fee4b560113af[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 15 10:33:29 2020 -0700

    Update Patreon list in Creditrs

[33mcommit 1660474cdf854067aa9401e78517d69be2aa4915[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 15 09:44:12 2020 -0700

    Bug fix with box select and ring prop

[33mcommit 3ecfc21855b2f32b3f81da53bd87b719bbf39fd0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 23:38:08 2020 -0700

    Huge performance improvements in 360 levels

[33mcommit d6a2667925b5cbb965b653055a86a94ad1c22199[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 21:01:07 2020 -0700

    Lightened basic material, bug fixes

[33mcommit fd4f4a20a48bd01dbad5b2f309d913c3dfae84d3[m
Merge: d4ec0167 68008cf5
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 18:08:59 2020 -0700

    Merge branch 'dev' of https://github.com/Caeden117/ChroMapper into dev

[33mcommit 68008cf5d35ef59f9f1e1a6b039e84b4daea983f[m
Merge: 25065170 19e9093c
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Mar 14 18:08:48 2020 -0700

    UIMode improvements (#87)
    
    Made UIMode not change when Node or BPM Tapper is open.

[33mcommit d4ec0167921c0c1bdd03d03e9e8c9f4281a95eec[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 18:03:45 2020 -0700

    fix monstercat platform, fix bug

[33mcommit 250651705552adf9c90d767a2b1a607c14b6a660[m
Merge: b2d854b6 1d9c8771
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 17:52:35 2020 -0700

    Merge branch 'Xavjey-dynamic-event-tracks' into dev

[33mcommit 1d9c877132b745e055019c0707803c19348d2dc3[m
Merge: b2d854b6 457376a9
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 17:52:20 2020 -0700

    fix merge conflict

[33mcommit b2d854b6a8ffb45ed4244ed44ae40f7d3e7f9fd2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 17:48:41 2020 -0700

    fix issue with pause toggling lights

[33mcommit 246d7637abbf45498f81d65e296bc5690ea149eb[m
Merge: 33a9d5f7 f305bd55
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Mar 14 17:47:58 2020 -0700

    Internal options refactor (#88)
    
    33a9d5f7a973769a01760dd9d62a1a3a56b4696d is the commit to revert to if we want to revert for any reason.

[33mcommit f305bd555cadfebd85c8b1752c7538366aa984d2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 16:20:43 2020 -0700

    Fix slider text not updating on initial load

[33mcommit 334af5707d20ae933b4ea2ce70e61d8dbe1336c6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 13:18:52 2020 -0700

    fix past notes grid scale setting

[33mcommit ab49167bc9f4f09d2ae1d0bace98808fda955e8d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 13:12:11 2020 -0700

    bug fixes + remove Find<T>()

[33mcommit 1ff0029bf36603a4c98b015003857e25e896a735[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 12:41:05 2020 -0700

    Add validate directory for beat saber install setting

[33mcommit 3967dcea2e3a98dc5e9be32d123e723f2c525c81[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 14 12:27:49 2020 -0700

    Add action callbacks for everything that needs them

[33mcommit c65d345245e2a1544ed50c16d57f0c9a4c473093[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Mar 13 22:44:30 2020 -0700

    Convert all the options to settings binders, start work on action-based callbacks

[33mcommit 457376a9dc1a93610a3a9ef2422a72b84a1cb271[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Mar 12 22:41:49 2020 +0100

    Remove unnecessary UnityEditor.Experimental.SceneManagement

[33mcommit b0f619daadf77149fe98e041af24bb90be9bebed[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Mar 12 22:41:21 2020 +0100

    Hide BigRings in KD

[33mcommit e9edfbf4d6b7a833d49a22adad42cf0f05f8fe25[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Mar 12 20:30:27 2020 +0100

    If no rings only 'All Rings' visible

[33mcommit 38c9476833fcf1f0c649874feac0389f51fc17be[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Mar 12 20:26:29 2020 +0100

    Store reference of platformDescriptor instead of GetComponentInParent/FindObjectOfType

[33mcommit 4806ba341dd1d308954e351bc00c100d3600ece7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 11 21:29:53 2020 -0700

    Continue work, about to replace OptionsSettings

[33mcommit 0efda495b3a9f32b239aece3da2e70e206e2e2a0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Mar 11 20:42:30 2020 -0700

    start work

[33mcommit 19e9093c8f57f93786d95da2eacab9d33ad49056[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Mar 11 19:53:31 2020 -0400

    Made UIMode not change when Node or BPM Tapper is open.
    
    Also made it so if Node Editor or BPM Tapper is open you can't switch UIMode

[33mcommit d6c7a4138ab6054611e662d92e5b6e6fa16f7987[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 10 19:24:40 2020 -0700

    Made header not wrap

[33mcommit 41e2fe349a065930116c957aeb6cb9b98b5f8170[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 10 19:16:23 2020 -0700

    Add TimeValueDecimalPrecision as setting

[33mcommit 3fad7f0532823c194319efae28dcae85ddc9a597[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 10 18:43:50 2020 -0700

    code improvements + new options section

[33mcommit 33a9d5f7a973769a01760dd9d62a1a3a56b4696d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Mar 9 06:10:33 2020 -0700

    fixed timing issue with box select

[33mcommit c88e5a099d081afdfa9f282a1757390261e51baf[m
Merge: eb0c52b2 d85dc026
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 8 23:28:43 2020 +0100

    Merge branch 'dev' of github.com:Caeden117/ChroMapper into dynamic-event-tracks
    
    # Conflicts:
    #       Assets/__Scenes/03_Mapper.unity

[33mcommit eb0c52b2b11ff75f91b48a4355484840c70036e1[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 8 22:35:14 2020 +0100

    Use PlatformLoadedEvent instead of wait

[33mcommit 89ab839ce4e1eeb311a294cccaeda052db48efee[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 8 12:31:14 2020 -0700

    Misc. fixings

[33mcommit 8325e93b5b6ea7d2c993326eb851b3c4740150ba[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 8 11:58:25 2020 +0100

    Use Start() function

[33mcommit b42521d7657d889dab5e69efc439081dd5da227a[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 8 11:47:10 2020 +0100

    Make a CreateEventTypeLabels reference

[33mcommit 5f006130fad011c49ff066179dc983e17d9ef92e[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 8 11:44:18 2020 +0100

    You should be able to get the same name effect by using customLight.name

[33mcommit b27dcb53cb6079808c7e16a079d5ab0b4e212c15[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 8 11:43:37 2020 +0100

    Cases 0-4 shouldn't be necessary

[33mcommit d85dc02614f4924dfd5e7ab3c6446815d408aad0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 7 22:22:02 2020 -0800

    Add null checks to action container

[33mcommit 39a975e1ee52eaf99acbc94cfc783533d8fb0b24[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 7 22:19:39 2020 -0800

    Fix and update shiny material

[33mcommit d5919a9ba8a500390b0f69a3bb99c60dbf1360b5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 7 22:19:23 2020 -0800

    Bug fix with node editor actions

[33mcommit c531c27150dc31ee496f96c8188537620d78d55d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 7 12:36:51 2020 -0800

    Chunk distance decreased when playing (more performant)

[33mcommit 45d0bb06a50e6de04338dd9b27e94d72a335bbdd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Mar 7 12:23:02 2020 -0800

    Normal map to shiny material (POG)

[33mcommit 0697847a3cd0563c6fee959443b881a08d4339da[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Mar 6 16:03:01 2020 -0800

    change default post processing intensity

[33mcommit 4ff5df32f14bb7d7876f6a05ce1c377c6dac4e10[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Mar 6 16:00:47 2020 -0800

    i swear of note order is still wrong here i will lose my shit

[33mcommit 05f4611fc79f4dd8a1ac7c8d8a753e3c80de35c6[m
Merge: 76e0f963 9facfdee
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Fri Mar 6 16:15:39 2020 +0100

    Merge branch 'dev' of github.com:Caeden117/ChroMapper into dynamic-event-tracks
    
    # Conflicts resolved:
    #       Assets/__Scenes/03_Mapper.unity
    #       Assets/__Scripts/Settings/CustomPlatformSettings.cs

[33mcommit 76e0f9638ca0a4786c49f5d580a4369b54f45664[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Fri Mar 6 12:37:21 2020 +0100

    StartUp routine that the correct grid is reflected after loading a song

[33mcommit 29ffd4155356fcb9e67d599f7d54c409e4a2e943[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Fri Mar 6 12:36:42 2020 +0100

    Create Labels for CustomEvents

[33mcommit 65707a93afdbaeffd4cfa3284e1c32286a863c92[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Fri Mar 6 12:36:26 2020 +0100

    New Events for Custom Lights

[33mcommit 39ff8f0afe243a106e51a9c34a86cab217369092[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Fri Mar 6 12:09:36 2020 +0100

    Correct customPlatform hash

[33mcommit b787b34c0b5e579f5c0fee6af2f70ca7d18b7869[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 5 20:10:01 2020 -0800

    Remove unneeded things

[33mcommit 9facfdee6467238f4969c39b20fed120126d87ca[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 5 19:33:48 2020 -0800

    Fixed a lot of jank with custom platforms

[33mcommit 473c8f8ba53919aeb2a31093dcef48df83756f8a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Mar 5 17:59:54 2020 -0800

    Fixes

[33mcommit 38c812d843694e3d910db30bc744e0ec94f00341[m
Merge: 518adcc4 d734d7f5
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Mar 5 20:57:10 2020 +0100

    Merge branch 'dev' into dynamic-event-tracks

[33mcommit 518adcc4534fbd4fcb6c72348302c81c6c4a1ba7[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Mar 5 20:45:06 2020 +0100

    Dynamic Count of existing LightManagers

[33mcommit ceccebc0c84c4ebb38b64d1623edebaae08ed58c[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Mar 5 20:44:43 2020 +0100

    Style EventTypeLabels for Prop

[33mcommit d734d7f5ebf74280224aed4aac8ba9d7338d29d0[m
Merge: f8ad7d2e e06921fa
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Mar 5 07:44:58 2020 -0800

    Got Platforms? (#80)
    
    Feature/native custom platforms

[33mcommit f8ad7d2eb6399d17357a63ee4b7286fbedc77ed6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 3 17:22:57 2020 -0800

    Improve Bongo Cat performance

[33mcommit f108c714cfe94b901c76316dd83b539779151a28[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 3 17:18:01 2020 -0800

    Get much better wall performance with this secret trick that ChroMapper developers do NOT want you to know!

[33mcommit a56548b50d82c83c99317f687a5a16b0656be677[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Mar 3 16:55:23 2020 -0800

    Add option for more accurate hyper walls

[33mcommit e06921fa2eeb419803f1c43a45c70144ba0ba84c[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Tue Mar 3 19:09:00 2020 +0100

    Remove CustomPlatform, only CustomEnvironment

[33mcommit ea9d0e31d3a921cbbd285a17908df43867dceeeb[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Tue Mar 3 18:37:47 2020 +0100

    Dynamic trackSize for propagation

[33mcommit 1fec22e6fb4bed522637a8c4b34169a3a357532f[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Mar 2 15:03:22 2020 +0100

    Smooth SongEditMenu

[33mcommit e1a02aaa3e212f100c178dfc5be07df84294a527[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Mar 2 14:59:59 2020 +0100

    Replace all existing materials on lights with black except white ones

[33mcommit 757de816119227b0a7643cea4c9ed04f98e5b2e8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Mar 1 18:45:42 2020 -0800

    Wall unloading now uses the chunk they end on.

[33mcommit 1cdebfc5af0ac7c25fd8d9078d9963b7b8ab1b70[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 1 21:50:24 2020 +0100

    Remove SerializeField from newly public variables

[33mcommit 1163d012e9f1725d1a4f9106056797d5aebbf56d[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 1 21:47:27 2020 +0100

    Loade ComponentInChildren

[33mcommit d9c291fd3dddc6d20e6355f2aae7ce5d33b88191[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Mar 1 14:22:24 2020 +0100

    Make function vor emission

[33mcommit c603394aafb0cbafac91c57a3401a74a7c715d48[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Feb 29 17:34:59 2020 -0800

    Made Obstacle shader ignore Y axis

[33mcommit f4ac4f1283ed72739cedbe891459ef3bc9d946b0[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sat Feb 29 19:30:38 2020 +0100

    Togge Emission of Lights

[33mcommit 48d69bf14acc835ec031578bc4c4a493425b8d17[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sat Feb 29 19:29:51 2020 +0100

    Fix shaders

[33mcommit 48e8655662e3f56d611565137287e6d274c0ad16[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sat Feb 29 13:32:11 2020 +0100

    Renderer Material/Shaders

[33mcommit 66b3af7b56c3c88b6ed82ba3d4bc19bf34ca0059[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Fri Feb 28 22:41:57 2020 +0100

    Load Platforms during startup

[33mcommit 96dfd96b1bb76f07f69a021714a8336c0a27635b[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Fri Feb 28 22:41:36 2020 +0100

    Working CustomPlatforms

[33mcommit 3380d405d14453d9b26bfdfe8a5fe75cb71e2ce4[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Tue Feb 25 19:46:50 2020 +0100

    All the CustomPlatforms

[33mcommit c6a5bbf3e8cd1a2170bd2a441b4d83117d359fac[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Feb 29 12:29:22 2020 -0800

    Fix bombs not applying to 360, perf. improvements

[33mcommit 4d73fe4474f671c73c28702cab823bf08bed0af1[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Feb 29 10:04:20 2020 -0800

    Performance gains with wall heavy maps

[33mcommit e0819eff9179860179e90057fb3bee49f64de209[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 28 20:01:50 2020 -0800

    Prevent lots of SongDatas by killing GameObject

[33mcommit 98c491a5104d4a7e0b9cd518e5beea285dec7931[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 28 20:00:12 2020 -0800

    Fix Text reference not being TMP_Text

[33mcommit 09fa1c0455bd78f04e8e930057bf039f8d5427d1[m
Merge: e03ca4e5 79c6bd99
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 28 19:41:27 2020 -0800

    we go agane agane agane (#79)

[33mcommit 79c6bd992be04305e46bbde6f502a0c5432cb5b6[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Fri Feb 28 22:40:36 2020 -0500

    Fixed Fonts. Fixed weird error.
    
    Replaced loading message with TextMeshPro. Fixed tab spacing for fonts.

[33mcommit bf76216da7d5c7e357379dc0cd107bb3210d0ac7[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Fri Feb 28 21:52:34 2020 -0500

    Hid UIMode by default

[33mcommit 95dd48bcb3936ab86837181696b64da3c6bf5806[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Fri Feb 28 21:50:20 2020 -0500

    Okay, fixed Rounded Corners
    
    You have to make a material and disable clone IF you are using Independent.

[33mcommit e03ca4e5992794ed5e8dcc9f61d000186ac3a3fc[m
Merge: 13e14c5a 562fc1bf
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 28 18:32:09 2020 -0800

    we go agane agane (#78)

[33mcommit 562fc1bf3b15e0fa73be1435b0d833c249a04ec1[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Fri Feb 28 21:29:08 2020 -0500

    This prob wont completely fix the errors with Options but still test it.

[33mcommit 13e14c5af0905d63a0e68b81f7211379a37f5be2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 28 18:00:04 2020 -0800

    Move requirement and warning saving from auto save controller to BeatSaberSong

[33mcommit dc835d9b86310052d152ad8644e2d64a3fb6148e[m
Merge: ea774d6d 136dff68
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Fri Feb 28 19:57:39 2020 -0500

    Merge remote-tracking branch 'Caeden117/dev'

[33mcommit 136dff6827a679bf50d8881571baad5f353b3bdf[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 28 16:19:09 2020 -0800

    Half jump calculates more frequently, fix paint selection issue

[33mcommit 1c4c7bc3eb1a1f0d547fe41ebd62e83a7f813962[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Feb 27 19:37:48 2020 -0800

    ignore beatmap sets with no difficulties

[33mcommit c154f76b7b8d3bd0e1dd4b7ac6a3257e08bb25c2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Feb 27 18:05:49 2020 -0800

    Add null check to tooltips

[33mcommit 76db9c9f171845e2f14ad0ee83f44b608df1bb21[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Feb 27 18:04:39 2020 -0800

    Fix Rotate Track option

[33mcommit 7275aa0052c94727a419fe4a42ab2d99a47c2918[m
Merge: 8b297624 ea774d6d
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Feb 27 15:31:28 2020 -0800

    lets try this agane (#77)
    
    Might have fixed the New Options Menu Materials

[33mcommit ea774d6da402d73a8343fe6ed787d4f9c654de9b[m
Author: Ryan <ryan@rivergroup.com>
Date:   Thu Feb 27 10:08:38 2020 -0500

    Hopefully fixed the problems with the options menu.
    
    Also fixed some problems with the Popout.

[33mcommit a968af6a42c2524073e6daa8e4c10cb4cd0f9a08[m
Merge: 16b4f739 8b297624
Author: Ryan <ryan@rivergroup.com>
Date:   Thu Feb 27 09:48:04 2020 -0500

    Merge remote-tracking branch 'Caeden117/dev'

[33mcommit 8b2976247475ad0a9702bbc7ba6e86b3a899b5f4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Feb 26 20:17:24 2020 -0800

    Revert "Bug fixes, gonna pull new options menu"
    
    This reverts commit ef4730ba86c02407484a9e7bc92315ab6c42cd8a.

[33mcommit 5f5dded76b139308ccf46a18cf47f9a5fc9df43f[m
Merge: ef4730ba cfe7bbca
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Feb 26 19:43:39 2020 -0800

    Merge branch 'RyanTheTechMan-master' into dev

[33mcommit cfe7bbca9370e83adb399fd488dd31cc5ee59006[m
Merge: ef4730ba 16b4f739
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Feb 26 19:43:18 2020 -0800

    Fixed merge conflict with ProjectSettings

[33mcommit ef4730ba86c02407484a9e7bc92315ab6c42cd8a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Feb 26 19:40:56 2020 -0800

    Bug fixes, gonna pull new options menu

[33mcommit 3fb63d7ca77b7ae9190e5c962b5b16cfc1c86c30[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Feb 23 12:31:28 2020 -0800

    Fix Physx "cleaning mesh failed" error

[33mcommit c1ae4946d7929badc735f10a126ab50557fddc36[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Feb 22 16:47:22 2020 -0800

    I am discovering just how bad younger me's code was

[33mcommit a8dafbc260c44a2f8a2a5aec6f6fbe88b8a8d4e7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Feb 22 16:19:58 2020 -0800

    Reduce load times

[33mcommit 21bf53ed1893e4fc52f188fad0b4e3a57ec4a6dc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Feb 22 15:14:18 2020 -0800

    Code tweaks and version bump

[33mcommit 74f9eff226ebf39198a9a4de4cace3cd68bdd232[m
Merge: 8ccf891a 3c6b2820
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Feb 21 22:15:55 2020 -0800

    Update master to dev branch (#72)
    
    Development (2-21-2020)

[33mcommit 3c6b2820567fc44df2add078602737db714f5582[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 22:13:54 2020 -0800

    Update Unity version in README

[33mcommit 74d85fcbfdbaff1ae4b17179f1f1d173b4bd9e0c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 22:02:32 2020 -0800

    Add more localization lines (We're now at 100)

[33mcommit c4709a68831a12913ecfeca83852bbc5844edcac[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 21:50:06 2020 -0800

    Make rotating lights more accurate

[33mcommit bd79e83c2680596da64e466cbfa75b247b07d8e5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 21:21:25 2020 -0800

    Fixes with placement detection

[33mcommit 86732a875b6d7b3cb48a67759ec559105bef2d1c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 20:38:02 2020 -0800

    Fix issue #57

[33mcommit 29234bd16cbc141b5905868f63d8a73770b7bd62[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 19:29:54 2020 -0800

    Rename `_bpmChanges` to `_BPMChanges`

[33mcommit a18b6ee0d80a34803afe9d3d210197a31784ab18[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 19:16:21 2020 -0800

    Finally fuckin fixed laser speed input not working

[33mcommit f54cd27561cb695194e916f7b7cda4330300e97b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Feb 21 19:14:40 2020 -0800

    BeatSaberMap JSON can now be beautified

[33mcommit 16b4f739756ac77a5be6f3b09bfa0c2a1255a0ad[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Feb 20 20:22:23 2020 -0500

    Fixed the issues.
    
    Related to https://github.com/Caeden117/ChroMapper/pull/49#issuecomment-589437228

[33mcommit 39bef827a71953dc3df7f46a4bd309757321e022[m
Merge: 63d27d3d cdbb35ea
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Feb 20 19:28:58 2020 -0500

    Merge remote-tracking branch 'Caeden117/dev'

[33mcommit cdbb35ea84b094a99f15490972864afd691da8e1[m
Merge: 60936c36 3bb83023
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Feb 20 15:36:50 2020 -0800

    More ring propagation fixes (#68)
    
    #fixed Dragging ring propagations #56

[33mcommit 3bb830233a27381d2a4fb9135b182f35641d860e[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Feb 20 19:47:10 2020 +0100

    Add EventsContainer reference directly to BeatmapEventContainer

[33mcommit 4594b4ea9a10145d282a1efcf1537b6f3b3c455b[m
Merge: 4c289376 60936c36
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Thu Feb 20 19:29:10 2020 +0100

    Merge branch 'dev' of github.com:Caeden117/ChroMapper into feature/fix-drag-propagation

[33mcommit 60936c368102fc7c0db125e66cf09a316d7af5b6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Feb 19 21:39:18 2020 -0800

    bug fixes

[33mcommit 3acd431e0b5635b253666fc0b77c3c1528f5e6a0[m
Merge: 14ad6eb1 7e9a7332
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 19 18:02:44 2020 -0800

    Fix actions still using old collections (#64)

[33mcommit 14ad6eb10853466a53955420d5bede0d8aa97169[m
Merge: a67396e2 7b963d19
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Feb 19 17:57:15 2020 -0800

    Fix issue #69 (NICE)
    
    #fixed After Ctrl+AlphaX stop KeybindsController execution #69

[33mcommit 7b963d1925c85155f71546cf6ca8b8dde1c5d4b9[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Wed Feb 19 18:02:08 2020 +0100

    #fixed After Ctrl+AlphaX stop KeybindsController execution #69

[33mcommit 4c28937671546082af8701a5d1a137b6ac6e8963[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Wed Feb 19 16:28:18 2020 +0100

    #fixed Dragging ring propagations #56

[33mcommit 7e9a7332b9d29d5594c348a8259764b146a1f2f3[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Wed Feb 19 10:13:32 2020 +0100

    Fix #63 - StrobeGeneratorGenerationAction still using old collections

[33mcommit a67396e20f7a5f9fd3be8782051a1773508a8914[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Feb 18 20:39:29 2020 -0800

    Apply new collection retrieval code

[33mcommit c857753aee132422149b30a388032d09d4a2db54[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Feb 18 20:30:39 2020 -0800

    Add easier way to get BeatmapObjectContainerCollections

[33mcommit 89e410ab8689504567c056967fa38ce4d46992c9[m
Merge: 1a3091f1 7a130ca5
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Feb 18 20:26:24 2020 -0800

    Merged origin with local branches

[33mcommit 7a130ca5e23b326a7433ca62a5b3004483fece75[m
Merge: ee96be6d 7fd3775f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Feb 18 20:25:52 2020 -0800

    Fix Ring Propagation bugs (#55)
    
    Feature/fix pastemove propagation

[33mcommit 7fd3775f580571291f6ae29f584929db2e17aaf2[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 20:49:27 2020 +0100

    Fix propagation without nulling _customData

[33mcommit ee96be6d30375d0d43cd7c25262aff8cbe234bc6[m
Merge: 596300c6 15355a39
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Feb 17 10:40:44 2020 -0800

    Fix #37 (Pull #54)
    
    Fix undo paste action

[33mcommit 596300c66a35b567836193af31fdf1fefc73ec4f[m
Merge: f615b5b1 0569c9e3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Feb 17 10:36:19 2020 -0800

    Merge #53, fix #36
    
    Increase maxPage only if songs count is actually larger (and not equal than items.length)

[33mcommit 857e88e8e9b5b7634d18e48a88a8fd33dc432da8[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 16:57:51 2020 +0100

    Fix place ringpropagation all rings

[33mcommit 35fde75633c1d8deada6b3c1eafa966622e3397b[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 15:44:58 2020 +0100

    Fix shift ring propagation left to All Rings

[33mcommit 79e4266bbca99b2b11de6cdc7dc92a6b2ced0f8f[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 15:19:15 2020 +0100

    Fix upDown Shift for obstacles and events

[33mcommit b3ee1acd5a350524c38548099813cd1fbaf4080c[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 15:03:35 2020 +0100

    Initial fix left/right shift ring propagation

[33mcommit 59649e5357fba1af9892ccc0a1df08f0b1915738[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 14:35:25 2020 +0100

    Fix Paste in Ring Propagation

[33mcommit 15355a397f007c1ff3b67f97b696cdfcfe1607e1[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 10:17:13 2020 +0100

    Fix undo paste action

[33mcommit 0569c9e3891c7484f841d7444ba7b5e290b2d641[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Mon Feb 17 08:57:10 2020 +0100

    Increase maxPage only if songs count is actually larger (and not equal) than items.length

[33mcommit 1a3091f179538789e83e7a9f278fc050ed2e37ca[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Feb 16 20:35:14 2020 -0800

    Fix README since we're no longer on 2019.3 branch

[33mcommit d355210628bdbdc727a48ab9884b796255c9c56d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Feb 16 20:32:03 2020 -0800

    Increase Start Beat Offset limit from 4 to 8

[33mcommit f615b5b1d17c6b4eb2b2b2cb5a560b123cf98a36[m
Merge: 233b9944 8ccf891a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 16 20:30:31 2020 -0800

    Merge pull request #51 from Caeden117/master
    
    Move Start Beat Offset Fix from Master to Dev

[33mcommit 233b9944ca4f451d2510e524c98d131d7defcbea[m
Merge: c6d6ee1b c2df10f2
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 16 20:20:42 2020 -0800

    Add some Keybindings to Info (#34)

[33mcommit 8ccf891a4e33b9c1c153c076c1fd508841a8343f[m
Merge: 3affea7f 0784f663
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 16 20:20:16 2020 -0800

    Start beat offset adjustment fix (#35)

[33mcommit c6d6ee1b5ce7495d2487b8360603e6cf4bc96695[m
Merge: 56623d20 e8f9c0c8
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Feb 16 20:19:16 2020 -0800

    Upgrade Unity to 2019.3.0f6 (#50)

[33mcommit e8f9c0c82118008a311b724d864c098775a0b2d2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Feb 16 20:16:28 2020 -0800

    Shave a bit of time off of loading

[33mcommit 63d27d3df2f0c03c60a8e1273bb3f5bf287489b6[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 16 17:41:05 2020 -0500

    Added Dark Theme for Unity :)

[33mcommit c1d8fcd9ecb0ebbff41ebbc59ad29203c0c6a116[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 16 17:38:36 2020 -0500

    Finished Merging and fixed some problems with it.
    
    Also some meta files wanted to be updated.

[33mcommit 749e1b956af61c1b3e6e4d37057cc7e39db69a9b[m
Merge: 6edbd9ea 9069978f
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 16 17:07:13 2020 -0500

    Merge remote-tracking branch 'Caeden117/unity-2019.3'

[33mcommit 9069978f354671275a2d89bf992a24ae9c8ba366[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Feb 16 13:43:08 2020 -0800

    Upgrade ChroMapper from Unity 2018.3.14f1 to Unity 2019.3.0f6 and fixed bugs that come with it

[33mcommit 0784f6634b08ba75f9912b3665a5270e934ff797[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Feb 16 18:46:41 2020 +0100

    StartBeatOffsetFix
    
    @Pixelguy 2020-02-15 4:07PM Increase limit to 4 characters, increase size of TextField, align numbers right and align Text left

[33mcommit c2df10f27c0196272437356f9bf231c04dc66a5a[m
Author: Xavjer <cedi.wyss@gmail.com>
Date:   Sun Feb 16 18:44:53 2020 +0100

    Add some Keybindings to Info

[33mcommit 6edbd9eaa63e6d6593e3255bdacddb6893bc0ad8[m
Author: Ryan <ryan@rivergroup.com>
Date:   Thu Feb 13 12:17:38 2020 -0500

    Removed Unused class

[33mcommit 89ed4e8d10ed65e68e3f727d523407e52312a403[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Feb 12 22:47:20 2020 -0500

    Added some 360 checks

[33mcommit 16a90fb090dcc2c34dcae7d6a8815e4bd2ea5c88[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Feb 12 17:30:53 2020 -0500

    Basically completed UIMode
    
    https://www.youtube.com/watch?v=UGbAqbHyqOQ

[33mcommit b1e3b2ac54cba53a2966c4c95f22a9abb25e59f1[m
Author: Ryan <ryan@rivergroup.com>
Date:   Wed Feb 12 11:07:15 2020 -0500

    Stuff

[33mcommit 059344343ef3cfd5eb3fdc01c448f48ba731740c[m
Author: Ryan <ryan@rivergroup.com>
Date:   Wed Feb 12 09:53:35 2020 -0500

    Added Playing view
    
    - Fixed some option tooltips
    
    I will greatly optimize everything soon.

[33mcommit 0e39fe5cc817db3e992fbba906a3b874ba8a0995[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Feb 11 22:32:25 2020 -0500

    For some reason i cant get a new preset to load?

[33mcommit 2b69220f9405a76b6856fc0281c3dc5a70749bd8[m
Merge: 073d4c02 77225076
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Feb 11 20:37:03 2020 -0500

    Merge remote-tracking branch 'origin/master'

[33mcommit 073d4c022e6307fe5e434214358e58f6c69a0419[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Feb 11 20:34:15 2020 -0500

    Added Preview Mode and a Working UIMode slider
    
    -  Made preview mode work correctly.
    - Made it so you can hold SHIFT to reverse direction of UIMode
    - Fixed bug that only occurred if using 360 mode toggle.

[33mcommit 772250767a56d0570451b31cbc5f9a4ff5a8b7b1[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Feb 11 20:34:15 2020 -0500

    - Made a public bool in CameraController so that you can see if 360 mode grid lock is on and change it easier.
    -  Made preview mode work correctly.
    - Made it so you can hold SHIFT to reverse direction of UIMode
    - Fixed bug that only occurred if using 360 mode toggle.

[33mcommit aaf0b11e90f11e955cb9db3fc1bc44b21a629be7[m
Author: Ryan <ryan@rivergroup.com>
Date:   Tue Feb 11 11:59:34 2020 -0500

    More UIMode work
    
    - Removed some unnecessary exception objects
    - Made UIMode slider work
    - Made UIMode slider work by pressing shift to go backwards

[33mcommit 7aadf202c4e05bc99a165ebe845ef0c6b90fc6fa[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 9 00:12:17 2020 -0500

    Working on a nice UI Mode Switcher

[33mcommit ae89bc34d855e46ad6197ad48977767042d53425[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sat Feb 8 23:13:43 2020 -0500

    Removed thing left over while testing from a few weeks ago.

[33mcommit 2083df6ec0da86551a7345f87ab312072322ce6a[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sat Feb 8 22:47:07 2020 -0500

    Click events in credits now work again

[33mcommit 29d6dc856a06dd6eb931a91430bc3255ccae5ad5[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Feb 5 23:32:09 2020 -0500

    Added BetterInput and BetterSlider CustomInspectorWindows

[33mcommit aedb3d297abe107e570bfeaad8ab47bafae73c56[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Feb 5 19:48:08 2020 -0500

    Got custom editor working for Tabs and Toggles!

[33mcommit 91235ff9e4353ae713ef679786f04d1b53e98a72[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Feb 4 20:57:28 2020 -0500

    Almost done with Options
    
    + Added options for changing what to load in maps.
    + Made it so options open to Mapping Options when Mapping.
    + Added popup warning if everything is disabled for load.

[33mcommit 61e71fe94fcb15241f772be83891db198d5c00b7[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Feb 4 17:35:15 2020 -0500

    Moved some things around

[33mcommit e23834a6d7a22689f8e6f604b1d165bde9b64106[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Feb 3 22:52:40 2020 -0500

    Optimized the Options Menu (Made it load 87% faster)

[33mcommit 5aae0566260877e5b161854fc4980faf653725ea[m
Merge: d23dfbd3 3affea7f
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 2 21:50:01 2020 -0500

    Merge remote-tracking branch 'Caeden117/master' into new-options

[33mcommit d23dfbd3d3aa560fcc72a73bb0f0829c57d87105[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 2 21:01:13 2020 -0500

    .

[33mcommit 6d093b956920afaa8e66d1839a12e4dafaf61614[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 2 20:25:54 2020 -0500

    Dropdowns! Volume Sliders! Settings Fully Completed.
    
    Made tool tips render on layer 20. Made TabPopouts render on layer 10.
    Completed credits. Completed Better Dropdowns. Completed Volume Sliders with decibel values.

[33mcommit 7715ce4e9da42d8aa7ab9505971189d6e9053666[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 2 15:16:43 2020 -0500

    Credits + Volume Sliders fix

[33mcommit c81b1e85d62dc2bc6c66df60ac0d68a65ed158f2[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 2 10:59:58 2020 -0500

    Made scrollbars match screen height.

[33mcommit 1f01fc47cfc2ec3e98b5d79c3faf6596a4919dce[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Feb 2 00:40:11 2020 -0500

    Added Scrolling to Options Menu.

[33mcommit b20d8e3565cf750052ed0950112cd50ab7c9dca1[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 30 21:55:46 2020 -0500

    Worked on Credits page.

[33mcommit b74082caff19929e216147c374db59dc6ebdd563[m
Merge: 0b3e25ce 56623d20
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Jan 30 17:04:37 2020 -0800

    Merge pull request #33 from Caeden117/dev
    
    merge dev to 2019.3 for last time

[33mcommit 56623d20c879865ca486308af12d1d9d50952a9c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 30 17:02:23 2020 -0800

    Add precision rotation UI support & action pepeganess fixed

[33mcommit 4aa3298962fd614a4cbef1c482441ac48950391d[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 30 18:48:26 2020 -0500

    Fully Implemented new Options in to CM

[33mcommit f2cb7fb48a18fd6c166bc85d7f7fafd81c35f86e[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 30 18:27:05 2020 -0500

    Added all settings except Dropdowns and Volumes

[33mcommit 64a431a13dc142573107358100fa52849b46f1b8[m
Author: Ryan <ryan@rivergroup.com>
Date:   Thu Jan 30 16:28:28 2020 -0500

    .

[33mcommit f12f3476e70cd069532a03c659d7a59d584f3594[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 30 00:27:01 2020 -0500

    Made better input fields.

[33mcommit 0b3e25ce7d78ae257c7cbc9de58b68789b63ea9f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Jan 29 18:42:43 2020 -0800

    Updated README with description/goals

[33mcommit 3affea7f6454ad54814945a0fabbb86db7132b8c[m
Merge: e08a732e 27b2d374
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 29 18:34:18 2020 -0800

    Merge pull request #32 from Caeden117/dev
    
    Development (1-29-20)

[33mcommit 41c8517678d9b0f3573dd772009eb19132f0e0f1[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 29 13:22:26 2020 -0500

    Added close button. (Not working yet)

[33mcommit b990510302710f4af871b58077a396a9d8154b9e[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 28 23:34:19 2020 -0500

    Worked on Better text fields
    
    - Also made lines in slider ring bigger.
    - Added a few more sliders.
    - Made slider ring show the right amount of ring.

[33mcommit 27b2d3748853de60ca78fc09b915bd3597e2ce46[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 28 20:25:14 2020 -0800

    Made Actions smarter with comments and optimizations

[33mcommit 7befcd4a6d89dcb47296ff9d26fb3544ca944a5a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 28 18:26:36 2020 -0800

    Add _editor field to help easily distinguish editors

[33mcommit e07f84aa9630e28f3feb4abc256130be32b349eb[m
Author: Ryan <ryan@rivergroup.com>
Date:   Tue Jan 28 15:44:32 2020 -0500

    Added more sliders to options

[33mcommit 5e132a932b11512e4e20fe20a8c3c171cada368c[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 27 22:30:03 2020 -0500

    Almost finished Better Sliders

[33mcommit 4155ee503a716bf20308fad2bdccf0e57a6f9843[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 27 20:33:47 2020 -0500

    Working sliders. That look good

[33mcommit d3461905d6996b4b60d9d26d81f700766ab7ec30[m
Author: Ryan <ryan@rivergroup.com>
Date:   Mon Jan 27 16:16:09 2020 -0500

    Made sliders people wanted
    
    Also worked a bit on the ring.

[33mcommit e08a732e9e65f3d34d72959573f622873f951bb7[m
Merge: 69bb31a5 df9461cc
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Jan 26 22:57:56 2020 -0800

    dont mind me i am huge dum bass

[33mcommit 688de31af0d7c2da9d4fc988a7b03737c0a19de3[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Jan 26 22:38:52 2020 -0500

    Worked for 10 min on better sliders

[33mcommit df9461cc642014fd87aff7fde1e67a5d353726ee[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 25 19:55:13 2020 -0800

    moved some stuff

[33mcommit 69bb31a58a1303cd8c93fbd24e80060379ddb939[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jan 25 19:26:01 2020 -0800

    Development stuff (#30)
    
    * add contributors, fix bugs
    
    * Fix errors with contributors & misaligned note + grid shaders
    
    * Various bug fixes
    
    * fix bugs + rotating lasers now symmetrical agane
    
    * Fix bug with rotation, and commented TracksManager
    
    * code optimizations and bug fixes
    
    * Internal Track refactor to support Mapping Extensions Precision Rotation
    
    * Fixed issue where obstacle go nyoom
    
    * fix render order issues and track jankiness
    
    * fix rotate track option fucking shit up
    
    * made node editor not select all on focus
    
    * Made notes not go transparent when song is not playing

[33mcommit 8892c3fb08fdb94abe22be1947d77fc3fa7dbb71[m
Merge: a865120d c7698008
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 25 19:25:12 2020 -0800

    turns out Visual Studio has an OK merge tool of its own

[33mcommit c769800827ca8bf81973f1d5df062a1d65d6dccf[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sat Jan 25 22:14:09 2020 -0500

    Welp, time to try Meld! (#29)

[33mcommit a865120d9276ea08e416d784acb7a0b09adde450[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 25 18:53:14 2020 -0800

    Made notes not go transparent when song is not playing

[33mcommit b717f1e29c911cd6342b70f14ed51d485280a9e2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 25 17:10:58 2020 -0800

    made node editor not select all on focus

[33mcommit 67d6f28e015a64c59da2f009c1b19061e7731631[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 25 17:07:58 2020 -0800

    fix rotate track option fucking shit up

[33mcommit 8a32df9f26091650458a5a3f73b3802fae57c66f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 25 15:52:52 2020 -0800

    fix render order issues and track jankiness

[33mcommit d793e922026e6bbfda09e4069126ae290c2ec3ba[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 23 22:15:24 2020 -0500

    Added buttons and data saving

[33mcommit e639fee8f9cfc7704aab38a39faea795295994c7[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 23 17:39:43 2020 -0500

    Even better

[33mcommit 472f7144e3d8f4a7c22067f8f61e12a88274c6ea[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 23 17:35:44 2020 -0500

    Shrank the TabManager class by A LOT.

[33mcommit 78fd25342f8aec269c91270ae359fff53f3e0d2b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 21 19:56:29 2020 -0800

    Fixed issue where obstacle go nyoom

[33mcommit 5feeced15884f3a91639aa055f3a84f046dd6de2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 21 19:44:00 2020 -0800

    Internal Track refactor to support Mapping Extensions Precision Rotation

[33mcommit a743f2ac57c5a003840aabb8e4f2a519e168c9b6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Jan 20 17:36:37 2020 -0800

    code optimizations and bug fixes

[33mcommit cbc5462db555dbfbe1fc96457c75c681eab96679[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Jan 19 13:11:05 2020 -0800

    Fix bug with rotation, and commented TracksManager

[33mcommit 8aa3f5443e2d50835854a3462f09bf4ca707de31[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 18 19:56:23 2020 -0800

    fix bugs + rotating lasers now symmetrical agane

[33mcommit 69362c0711438b63daa22117a4eda658ce0a6f1d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 18 19:13:53 2020 -0800

    Various bug fixes

[33mcommit 49836a392e69cb063968caafe235634786292bd4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 16 18:56:06 2020 -0800

    Fix errors with contributors & misaligned note + grid shaders

[33mcommit 1fa9d4b092d877c3246581906092cd98379c9eab[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 14 22:36:32 2020 -0500

    Done for the night

[33mcommit 44f2f1932c2860dcd5d60687253776bf9fc04979[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 14 19:37:14 2020 -0500

    Got toggle switches to work.
    
    They look nice.

[33mcommit e2c97500cf19e3366e9a5bcb63784524f510aaea[m
Author: Ryan <ryan@rivergroup.com>
Date:   Tue Jan 14 16:56:32 2020 -0500

    Working on More Options UI

[33mcommit fdb0a00a7e657073fb47bd29b0567b97d0f8fe6f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 14 11:16:23 2020 -0800

    add contributors, fix bugs

[33mcommit 7b6d6d0cf896fb87633b61b9633fea63eac5e848[m
Author: Ryan <ryan@rivergroup.com>
Date:   Tue Jan 14 11:32:24 2020 -0500

    Got discord Popout animation to work

[33mcommit 5e93cc0cc09ef5216f02076a8fe6bfd3798d2020[m
Author: Ryan <ryan@rivergroup.com>
Date:   Tue Jan 14 09:48:47 2020 -0500

    Imported Round UI Package instead of using package manager

[33mcommit 4e9e9b113a144351f1d70d3094d7b3397310f659[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 13 23:21:36 2020 -0500

    Working on New Options Menu
    
    Looks slick right now!

[33mcommit c5d38e2bd2aa48db40707be3d2c1fdeae3822bd4[m
Merge: 6315d6b3 ed2e8648
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Jan 13 18:05:14 2020 -0800

    Got a bit of stuff (#26)
    
    epic gamer time

[33mcommit ed2e8648a4b20dfeed604ee3f7b076a065b6fff2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Jan 13 17:37:50 2020 -0800

    ignore bpm changes and platforms now more accurate

[33mcommit 28595c4f1d76456c7c3b05102dbc505ebca30182[m
Author: Ryan <ryan@rivergroup.com>
Date:   Mon Jan 13 15:33:13 2020 -0500

    Ignore this again

[33mcommit 4410de3004712f47f904c73e93d0dd36ad310cc6[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 13 10:12:08 2020 -0500

    Ignore this.

[33mcommit f0d66a16f33359887900f90dad1fe2d6419fa2c4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Jan 12 16:43:26 2020 -0800

    Add loading toggles to objects so that i can test Cyberdemon without waiting a year

[33mcommit 3e4727f231a2968369adbb5eb0a0d1778650b4c6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Jan 12 16:33:15 2020 -0800

    made notes transparent after note interface + fixes

[33mcommit a3c51e290f062ec7dbb6c461267c86764dde05be[m
Merge: a6d8cd69 6315d6b3
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sat Jan 11 22:34:58 2020 -0500

    Merge remote-tracking branch 'Caeden117/master'

[33mcommit 5c8f5578a27e4da7b4e504eb4e3aef370e36cdf2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Jan 11 13:46:15 2020 -0800

    Made the last red/blue notes appear instead of notes on the last beat

[33mcommit 6315d6b39a8ccf43e629b3829451ec64dbe1a15d[m
Merge: 4c65a146 809b2c5f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 10 18:17:40 2020 -0800

    Merge from dev branch (#25)
    
    gamer time

[33mcommit 809b2c5fb87389886dcb478c364a2c645645efb5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Jan 10 18:11:59 2020 -0800

    fixed ryans tomfoolery

[33mcommit 7da31b88f1c15fda21ffaa423d58a3d6283034fd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Jan 10 17:22:26 2020 -0800

    Add some loading message ideas

[33mcommit 0b481387b321407a500fa1404d8f9d14546be263[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Jan 10 17:09:39 2020 -0800

    Reduce accidents that happen with alt-dragging objects over each other

[33mcommit c8ae08a566b6fd0bbe6ad5c7cbca72ff6ef688ad[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 9 21:29:03 2020 -0800

    Removed more unneeded type checks, fix alt + scroll on laser speed skipping 4

[33mcommit afccfcdb2c90cd06dd1def3926ca1fea63986122[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 9 21:27:19 2020 -0800

    Add disclaimer for BPM changes

[33mcommit bd537dc11fff3845db7cdd6dbc0a0dcbabda8e24[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 9 21:18:23 2020 -0800

    Converted and split utility event check into properties, removed some of probably many now uneeded checks

[33mcommit 34603c62f441a24310b5af6669f76422f68ef463[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 9 21:12:20 2020 -0800

    Fix fuckery from happening when trying to use ATSC when level is loading

[33mcommit 146ef33367664ca254024d0607ecd725ef060a69[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Jan 8 18:46:50 2020 -0800

    Made past notes into a pooled system, fixed a little bit of ui scaling

[33mcommit 926c3b68bd3868ad206d759738311efb08f5b795[m
Merge: cc5e6250 4c65a146
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 8 18:17:37 2020 -0800

    shhhhh

[33mcommit 4c65a146be428fe32724ad5b1965867709e20a44[m
Merge: cc5e6250 a6d8cd69
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 8 18:16:45 2020 -0800

    Make fading more accurate to base game (#23)
    
    Also HUD scaling

[33mcommit a6d8cd6921aee8c1b0b49fddc9d84a932f5feb67[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 8 15:23:28 2020 -0500

    Set Lighting Manager to hold lights a bit more.
    
    Also made lights update on FixedUpdate rather than per frame. Will be more accurate.

[33mcommit 295703d0fda99a08601f3e069e3a2c14067c602f[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 8 11:27:17 2020 -0500

    I have no idea what happened.

[33mcommit 5508581abc8abf802a0a1df1ed81a54419368943[m
Merge: ba339c19 cc5e6250
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 8 11:22:39 2020 -0500

    Merged Upstream #7

[33mcommit ba339c196b0c12f14c6f20b2b6f5ed436a26dc26[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 8 11:16:54 2020 -0500

    Worked on UI scaling.

[33mcommit cc5e62500400720d1dc2f7267f91eff6b5f3f6da[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 7 18:45:27 2020 -0800

    add Tokyo Machine

[33mcommit 46a330b651c8e09c9a784b8c8a26e66532cef53b[m
Merge: 838a4158 7187dcfc
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 7 18:45:00 2020 -0800

    have to re-add Tokyo Machine as a platform

[33mcommit 7187dcfc3457c648f0ea809724d1377cb6c3dac3[m
Merge: 3198b934 be9be0d3
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 7 18:45:06 2020 -0800

    Improvements I guess (#22)
    
    Welp time to fix some merge conflicts!

[33mcommit 838a415822db80df96a65d3379ff3ab431beba74[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 7 18:43:56 2020 -0800

    welp shit merge conflicts about to get gnarly

[33mcommit be9be0d32be7b5ce4010bfd4e05d0555194f3002[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 7 21:28:31 2020 -0500

    Made the Waveform be center on switch.

[33mcommit 7fabe1f82a564ab12d71bd8fc2f2da2d10ef5778[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 7 21:03:40 2020 -0500

    Made light workflow change waveform to opposite side. Can be toggled

[33mcommit e2183af42d008422147768d4208a43cb1e4b1aeb[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 7 20:16:05 2020 -0500

    Added a close button to BPM Tapper

[33mcommit 0ec98ee02fed58bf020455336b82c6afd58392cc[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 7 20:03:50 2020 -0500

    Added Camera FOV Slider

[33mcommit 3198b93435cf42c85446b8e10e1f7425cb088648[m
Merge: a7af364d ae363396
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 7 15:39:49 2020 -0800

    Merge branch 'master' of https://github.com/Caeden117/ChroMapper

[33mcommit ae36339617970e65028a4e6f4060f9a4b885be4d[m
Merge: 1ecfe421 bc78ddb7
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Jan 7 15:40:04 2020 -0800

    Past Notes Grid (#21)
    
    Added Fully Working "Past Notes Grid"

[33mcommit a7af364d3a3572bec4602a5d2cc1276df20ac415[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Jan 7 15:39:39 2020 -0800

    Start Tokyo Machine

[33mcommit bc78ddb7e86b6000077bd7a1d657d451a93538d7[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Jan 7 18:15:02 2020 -0500

    Past Notes Grid: Added Multi-lane and precision placement support
    
    Vertical precision placement does not fully work yet.

[33mcommit 287724fd7a6495cc56d042d626626706f5efd2e6[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 6 23:26:36 2020 -0500

    Added Fully Working "Past Notes Grid"
    
    https://cdn.discordapp.com/attachments/661811522523234335/663959552680787969/New_Past_Notes_Grid.mp4

[33mcommit a9b852e84d5c4a8c970c3a48a1bdf2dceb91e8d4[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 6 22:58:54 2020 -0500

    Made pressing quit, stop the editor.

[33mcommit aad101fedb3e274955b51bdc37dec6f342c4a6b9[m
Merge: 278a1584 1ecfe421
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 6 22:43:08 2020 -0500

    Merged Upstream #6

[33mcommit 278a1584d48c07dc33bf09fa49d64d6a031719f6[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Mon Jan 6 22:40:45 2020 -0500

    Added Past Notes Grid
    
    This is a push so I can pull the updated Options scene.

[33mcommit 1ecfe42114fe521051861731df312b62f493df00[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Jan 5 21:17:02 2020 -0800

    start work on buildables for pop up messages

[33mcommit 37796bb5a7502e85660aa77708334ae6c0c70d23[m
Merge: a256c6e8 a29cdd3d
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Jan 5 13:38:38 2020 -0800

    hopefully didnt fuck anything up

[33mcommit 8e935e9475f42faeddfb78c9bfa47ecf6ff808ce[m
Merge: 356fd3ee 4911f22f
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Jan 5 16:36:20 2020 -0500

    Merge remote-tracking branch 'origin/master'

[33mcommit 356fd3eeeb72b0e3501aeeeec2b41df9c75284d5[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Jan 5 16:32:15 2020 -0500

    Added Cowbell Easter Egg. Increased song playback speed slider.
    
    Can be triggered when a user holds LEFTCTRL and moves the Metronome Volume Slider.
    Why was the playback speed slider not always like this?

[33mcommit a29cdd3d90bd8912db3d595c20a2df2aaef4d693[m
Merge: 30146a13 4911f22f
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Jan 5 13:35:25 2020 -0800

    jesus christ this guy is going ham (#19)

[33mcommit a256c6e8ee61d0e80f3d4b3ff36a40674a0f93f1[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Jan 5 13:34:46 2020 -0800

    fixed note hit volume issues

[33mcommit 4911f22fa4907914110f897d4cec08c57b3c0f9a[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Jan 5 16:32:15 2020 -0500

    Added Cowbell Easter Egg.
    
    Can be triggered when a user holds LEFTCTRL and moves the Metronome Volume Slider.

[33mcommit 49d65bce239cc79203f5c7aaed5249ba59baa586[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Jan 5 14:30:05 2020 -0500

    Made the Metronome UI move!
    
    Works with any BPM and adjusts with Song Speed.

[33mcommit 0e596b42e2a0f8a52a625e21dafe3c742010b4b9[m
Merge: 52383892 30146a13
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Jan 5 11:28:37 2020 -0500

    Merged Upstream #5

[33mcommit 30146a13e00d6453df0f21ddeaf7f1f7dcef8bd9[m
Merge: dcc69ad8 52383892
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Jan 4 23:17:35 2020 -0800

    You hear that? You got new hit sounds. Are you happy? (#18)
    
    I will be working on the introduced CM Popup message tomorrow, with the hope of getting a more modular dialog system introduced

[33mcommit 5238389245646c1715f58687b096031b723a591a[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Sun Jan 5 01:33:51 2020 -0500

    Added some Note Hit Sounds
    
    Added new prefabs. I can't get NoteHitVolume to save. Help! Not all sounds are available.

[33mcommit 5e34d9f61fb694e28a3a9deca7c73d4f8961b0ae[m
Author: Ryan <ryan@rivergroup.com>
Date:   Sat Jan 4 00:35:57 2020 -0500

    Working on new Dialog Box Builder
    
    This is kinda hard. I might need some help!

[33mcommit a0828a3c2e29b61659d2fdfcf760bb909e1c6859[m
Merge: 37cc470b dcc69ad8
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Fri Jan 3 21:59:34 2020 -0500

    Merge Upstream #4

[33mcommit dcc69ad8d27da986c6e75ed0c93c15b64f8e1bbc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Jan 3 13:05:51 2020 -0800

    Combined both fixes and Ryan's optimizations

[33mcommit c73918312a044826e7f20ae8eb725a02dbfb3a08[m
Merge: 335c5637 06e8a036
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Jan 3 13:05:38 2020 -0800

    Fix SongInfoEditUI merge conflict

[33mcommit 06e8a0362f929b747d3138f199d27722388ea37e[m
Merge: 3da998c3 37cc470b
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Jan 3 13:02:59 2020 -0800

    CM code got more smarts (#17)
    
    lets hope this doesn't break anything

[33mcommit 335c56375e7339fc677d6a83e54e2cf2c69a2807[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Jan 3 13:02:57 2020 -0800

    some fixes

[33mcommit 37cc470b1a8ec40be14e76c0cb43ca27962c9aea[m
Author: Ryan <ryan@rivergroup.com>
Date:   Fri Jan 3 15:07:50 2020 -0500

    Did a bunch of optimization & removed non-needed code.

[33mcommit 830254f82d03e39e1f276818212dd732e903a3bb[m
Author: Ryan <ryan@rivergroup.com>
Date:   Fri Jan 3 13:23:49 2020 -0500

    Made quit button work while testing

[33mcommit 81cb951170d38a0ac3cfc7014269c9c63be683d5[m
Author: Ryan <ryan@rivergroup.com>
Date:   Fri Jan 3 13:22:10 2020 -0500

    Fixed a few locale problems with strings. Removed some non-needed code
    
    .ToString(CultureInfo.InvariantCulture) is used for working with numbers that could contain odd negative symbols. (Can happen)
    
    Switched to useing use expression-bodied property when available.

[33mcommit 02e6989e9b54ee813cfc1fc54fc666287cca250a[m
Merge: a2e828c0 3da998c3
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Fri Jan 3 12:33:24 2020 -0500

    Merge Upstream #3

[33mcommit 3da998c3926e210964f89c15a9b52d9921028a42[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 2 11:58:35 2020 -0800

    woops forgot to save README changes

[33mcommit 2748b71d92cbeb9760cda5457ea78d46ee2491a4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 2 11:37:52 2020 -0800

    Bug fixes and Metronome optimization

[33mcommit a2e828c001feb4d67d9919a1ac90056296ebb06b[m
Merge: b17a8836 86a78064
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Thu Jan 2 11:25:14 2020 -0500

    Merging Upstream #2

[33mcommit 86a78064b22108083218ecf04e206d8b63f57df5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Jan 2 00:23:26 2020 -0800

    Characteristics should no longer duplicate themselves

[33mcommit c66de0e8288615e2df3a7f36472f870168884cff[m
Merge: d44bc58e cf9553ce
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Jan 1 23:54:13 2020 -0800

    this wasnt actually a bad merge

[33mcommit cf9553ceee6489433485d1d5e319cb8f43f61bbd[m
Merge: 12c53564 b17a8836
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Jan 1 23:50:51 2020 -0800

    Click click click click (#16)
    
    Time to see how badly merging becomes

[33mcommit d44bc58e5d3c2c4507fb9401025eadc8dddb1629[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Jan 1 23:45:19 2020 -0800

    closed beta fixes

[33mcommit b17a8836b2266fb9b7f1e3264bc0fc63bfbcf1d7[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 1 23:26:29 2020 -0500

    Added Metronome to UI

[33mcommit 7108b7006ed6e326e5b811ef57f1139600e871d6[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 1 22:29:51 2020 -0500

    Added Metronome

[33mcommit 8e6c87acc9c521b5339309f626af58cffb674321[m
Merge: a873bf7c f9b036a5
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 1 12:24:31 2020 -0500

    Merge remote-tracking branch 'origin/master'

[33mcommit f9b036a50c74d93b3dc37f9c596c227085dec27e[m
Merge: 0a126edb 12c53564
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Wed Jan 1 12:22:12 2020 -0500

    Merging Upstream #1

[33mcommit 5a93cf9227b5d3caa8b658ad2a7158d129008008[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Dec 31 20:53:23 2019 -0800

    i lied box selection was being fucky so i fixed that too

[33mcommit 12c535648eeff4a3b1cc834f227682a48481b498[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Dec 31 20:31:32 2019 -0800

    last closed beta fixes

[33mcommit a873bf7c55c2a84c07379c70da571d9581917d79[m
Author: Ryan Ernest <ryan@rivergroup.com>
Date:   Tue Dec 31 23:28:24 2019 -0500

    Added option for metronome.
    
    Does not do anything yet.

[33mcommit 0a126edb61e76d210ebd236f69c34f896356d4e7[m
Merge: 4b300181 d8db86bd
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Dec 30 11:06:43 2019 -0800

    Blame Aeroluna for any bugs that stem from this (#15)

[33mcommit d8db86bdf4c7f687ac5f114fb175600d20a403d6[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Dec 30 05:03:28 2019 -0700

    Added toggle for chromatic aberration in pause menu

[33mcommit ef07beedf699768069d27cec15007babb6964710[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Dec 30 04:11:43 2019 -0700

    Color picker automatically enables the toggle

[33mcommit 0784d0da92511b6cc7f047b79d18d4b4eb347239[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Dec 30 03:29:09 2019 -0700

    Hover event smaller and transparent for more tactile feedback

[33mcommit ab39df85ca38802f0ee45d89cd82a6f1218d95af[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Dec 30 03:08:26 2019 -0700

    Placing event over another no longer overwrites

[33mcommit ab45ba366505c99c13ee119aba3118b623278e80[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Dec 30 01:43:06 2019 -0700

    Made scrolling in 04_Options not horrible
    NOTE: the speed might be WAAYYY to fast on anything but windows

[33mcommit 03f9e9560712749e925dda8b2e86370b5698cd95[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Dec 30 00:57:57 2019 -0700

    Added Invert Precision Scroll option to 04_Options

[33mcommit 4d85a392dbdfd5a2d75eb00979a175792ba66c42[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Dec 30 00:11:03 2019 -0700

    Fix mirror select to actually work on normal walls

[33mcommit 4b300181082b641d765dc48718c43c7de0dea337[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Dec 29 21:20:21 2019 -0800

    bug feeeeeex

[33mcommit a1c6f3d5a75216703cc4a117691ae80a766669c4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Dec 29 11:18:52 2019 -0800

    chromapper has gotten a lot more stable lately

[33mcommit 06827a536f0d3b4ae04785b7b247c7755533df31[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Dec 28 23:36:46 2019 -0800

    redid visuals controller

[33mcommit 1e9ef25f955c6a437a0bfbb448d540845ac72c11[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Dec 28 23:20:02 2019 -0800

    i ran out of BugsBeGone™

[33mcommit d4098d0b9320721a19f5247d71cd3075257db899[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Dec 28 17:00:32 2019 -0800

    more bug fix and stole counters+ code

[33mcommit da1e09b59f2b49affe2e78db65c3aaa8bf2a6dca[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Dec 28 13:14:49 2019 -0800

    bug fixes galore + modular map refresh

[33mcommit a1ec53e61fa2eff8c54438bf18f53ca18f6aa8ba[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Dec 27 20:32:52 2019 -0800

    i wanna say i fixed bugs with map creation but idk

[33mcommit ad84d5425c6d94332fa4ec3b3623474c8d7d3f95[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Dec 27 20:21:33 2019 -0800

    i should really call an exterminator for all of these bugs im finding

[33mcommit 56c65b2aa1c1631c3784b06a7780e08849ae681b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Dec 27 14:14:37 2019 -0800

    add localization messages, fix some bugs

[33mcommit aef604b211b145de13309241f3cf28c3d0b49003[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Dec 26 12:33:07 2019 -0800

    Quest? But I'm already on a quest!

[33mcommit 5bf1cc7a93087b72a27e69ff5ae4af23eb1d36a8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Dec 25 16:06:50 2019 -0800

    lots of bug fixes and implemented suggestions

[33mcommit f06f56495c19fdb0ef3c4189d5115a3c55492076[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Dec 25 12:51:17 2019 -0800

    christmas development is always fun

[33mcommit a0ab84670cade7b53e6200f6b65fff056f2fe664[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Dec 24 21:21:10 2019 -0800

    bugs fixed, merry christmas folks

[33mcommit 934a1682774e2b5ef4cde0c71c49cdc244dd1a71[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Dec 23 23:21:51 2019 -0800

    idk why i didnt branch this out to multiple commits but shit was done

[33mcommit cc6d2fa06a1c90318264f2a4fdaa196293141f76[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Dec 20 22:35:04 2019 -0800

    Glass Desert and README upgrades

[33mcommit fd628cfc39c5142be9b2c062f12bd1523d2093db[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Dec 19 21:43:49 2019 -0800

    A lot of changes, including swapping Obstacle material from shader code to Shader Graph for rotation support

[33mcommit 646b1154c1e023f14e94b2f61e93aa20455dee52[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Dec 16 22:03:40 2019 -0800

    Add Glass Desert environment, and tweaks/fixes to current 360 system

[33mcommit 86be780c422af6e802627fc556deb26b95df9abe[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Dec 15 21:28:18 2019 -0800

    Big refactor to split the main track into modular Tracks for 360 levels

[33mcommit b7d348a62df17191a969b0ff239a8f89057fcc5c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Dec 15 16:25:19 2019 -0800

    Various additions and changes for rotation

[33mcommit 40c96d02c392459e8b9d551b60c0f1a612df0014[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Dec 15 12:37:47 2019 -0800

    Update grid rotations in code (temp code for now)

[33mcommit 4a94ff0e43a92cf60157358aff163c6aef408482[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Dec 15 10:37:57 2019 -0800

    Switch grid-related shaders to Shader Graph, supports rotation (for 360 levels POG)

[33mcommit 8cc5148314d5bb2cac5d2123cbeea2927bfb32fe[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Dec 5 17:04:34 2019 -0800

    bug fixes by myself and others

[33mcommit 157f246a126171e6362fd42d718779beb6591021[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Dec 2 08:03:37 2019 -0800

    UI toggles for Chroma/Lite emulation

[33mcommit c8ccf1934a4e70ed116edb93325ed6350002a28e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Dec 2 07:56:36 2019 -0800

    ive been sick so i forgot what the fuck was in this commit

[33mcommit 1f7fc87c95ae6dfbff09763ec2b790c3d5c4c895[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Nov 29 12:45:03 2019 -0800

    Solo an event type (only have one group of lights play out as normal) to test out lighting

[33mcommit ab6abc3f1a4a5c568f6a261a631101601a1d879c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Nov 29 12:17:35 2019 -0800

    bug fix where difficulty label gets stuck

[33mcommit 26dcfb91b7f8dd009fba15980600774f6968c757[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Nov 28 10:07:14 2019 -0800

    Update Patrons list in Credits

[33mcommit 29b85b389646103fbc7beab568f564ec7622ecb7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 27 20:29:10 2019 -0800

    lots of misc. fixes and shit

[33mcommit 72cedef61df6291dbbd2d9960b728922373c9d59[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 27 15:45:26 2019 -0800

    Add options for movement speed & mouse sensitivity

[33mcommit da604d25e6f6b6fc6726aaba241b96f9dd228438[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 27 06:26:30 2019 -0800

    aero forgot critical key check

[33mcommit f6615ffe80e6a640478c24f7753d3d4e6bc14aa7[m
Merge: 3aa57c07 6fe74587
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Nov 25 19:35:30 2019 -0800

    Merge branch 'master' of https://github.com/Caeden117/ChroMapper

[33mcommit 3aa57c0757147860198dfabaca323a8df2bce2ca[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Nov 25 19:35:20 2019 -0800

    fill version text

[33mcommit 6fe745877be4cdb8d995138104b70a0fcc9e1013[m
Author: Aeroluna <40481393+Aeroluna@users.noreply.github.com>
Date:   Mon Nov 25 20:35:01 2019 -0700

    pepega code coming through (#14)
    
    * Quick fix to bongo cat
            modified:   Assets/__Scripts/MapEditor/Detection/BongoCat.cs
    
    * "Improved" a bunch of code surrounding the top bar ui
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scripts/MapEditor/Mapping/KeybindsController.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/PlacementControllers/EventPlacement.cs
            modified:   Assets/__Scripts/MapEditor/UI/Info Panel/ChroMapperInfo.txt
            modified:   Assets/__Scripts/MapEditor/UI/Placement Controller UI/EventPlacementUI.cs
            modified:   Assets/__Scripts/MapEditor/UI/Placement Controller UI/NotePlacementUI.cs

[33mcommit c245023f353dc0bb303a0ad2ccc885910c534015[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 24 18:37:07 2019 -0800

    P!ATD environment, finishing off the environments in Beat Saber

[33mcommit 39af3e048ba220be1183e8c3ac1af04f4693020a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 24 17:53:57 2019 -0800

    switch autosave folder to match MM/MMA2

[33mcommit a6fce00ad04e791888b77c673b123686e0e31e78[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 24 16:51:53 2019 -0800

    floating point precision error fix

[33mcommit 2311271a91246a2bbcc1c67340121fa46ba44327[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 24 15:50:05 2019 -0800

    Ctrl+Numpad to change laser speed, arc visualizer fixes, new setting soon

[33mcommit e27b7ecaa657716f36cfdba38f4c311dfed13192[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 23 17:26:50 2019 -0800

    fixed pepega mistakes

[33mcommit ca71764d945ff3ef787df872fd6313505d6e6f51[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 23 10:46:36 2019 -0800

    Selection performance boost, big brain stuff (hopefully)

[33mcommit fca883fdb6b4686f59a331d282dfb806d0a54c7f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Nov 21 18:23:37 2019 -0800

    more bug feex (hopefully)

[33mcommit 4400d75fd860b806f44fb2ba7078a2a3b2428ab4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 20 18:35:21 2019 -0800

    Placement Controller smarts + Discord RPC icons for more environments

[33mcommit 534b9249f5739c14acbd0ed2e2a6ee22e670afbf[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 17 19:59:50 2019 -0800

    Monstercat and Crab Rave environments, P!ATD coming soon

[33mcommit c6457647088f794737a59fec9afa30d90a031c41[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 17 12:38:15 2019 -0800

    fixed other wonkiness

[33mcommit 75d9e9c0e345478ebc90bd821f132286ff07ce6d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 17 11:50:48 2019 -0800

    bug fixes and quality adjustments (hopefully)

[33mcommit 9ddff8b465c3db2b63150fa041526bf33ca7f6cc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Nov 17 10:24:23 2019 -0800

    offset bug fixes

[33mcommit d4196a9101820aafca1ec01d2537afd2463122c9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 16 22:16:32 2019 -0800

    Alt and drag now moves highlighted objects

[33mcommit 0072fc29e1426f93856236d36a01940ac4f1f849[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 16 20:08:46 2019 -0800

    You can now create and delete bookmarks

[33mcommit 8d76d74e8730ab2fab6b9ff7349cfe8aa50f82d9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 16 15:45:34 2019 -0800

    misc. bug fixes

[33mcommit 7b9ff79e9d51c4cc5192cf564b819ee094ab152b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 16 15:07:21 2019 -0800

    fix layout issues and added new icons

[33mcommit 70f3d964bdc0f4f613c9bf1112a296f62446b280[m
Merge: a6c654c5 4da3aafc
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Nov 16 14:22:08 2019 -0800

    my little Aeroluna is all grown up now (#13)

[33mcommit 4da3aafcbc1b6429fcd865cad548b67c666f17ad[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Sat Nov 16 15:05:14 2019 -0700

    Added Mirror Selection and changed few other thingies
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scripts/Map/BeatmapObjectContainer.cs
            modified:   Assets/__Scripts/Map/Events/BeatmapEventContainer.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/KeybindsController.cs
            new file:   Assets/__Scripts/MapEditor/UI/MirrorSelection.cs
            new file:   Assets/__Scripts/MapEditor/UI/MirrorSelection.cs.meta

[33mcommit a6c654c5e3027e527a8c2888d881d0f60f1bfef7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 16 13:31:58 2019 -0800

    Fix animated loop, add Half Jump Duration + Jump Distance

[33mcommit 037828b5a921286b1677e79cb9cc6caff12beb5d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 16 11:29:11 2019 -0800

    Tweak UI scenes, bug fixes, add platforms to dropdown

[33mcommit a5f5b3c01714ea335d56cf1ad82d7cb2d45acc46[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Nov 15 19:21:05 2019 -0800

    select stuff under the grid

[33mcommit 83133540d9c3295f52df374d224fd6e83d6fc1a9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Nov 15 18:59:30 2019 -0800

    Fix sliders extending past their sliders

[33mcommit e32527acf931f413af8ff8d5eadf0214a9315077[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Fri Nov 15 15:52:57 2019 -0700

    Added delete tool, cleaned up KeybindsController.cs
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scripts/Map/BeatmapObjectContainer.cs
            modified:   Assets/__Scripts/Map/Events/EventAppearanceSO.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/KeybindsController.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/PlacementControllers/EventPlacement.cs
            modified:   Assets/__Scripts/MapEditor/UI/Placement Controller UI/EventPlacementUI.cs
            modified:   Assets/__Scripts/MapEditor/UI/Placement Controller UI/NotePlacementUI.cs

[33mcommit 94ddff06e8bc19939986e9971d508524f79da59e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 13 21:51:17 2019 -0800

    Screen Space Reflection simulation improvements

[33mcommit c174de90083db89dad080f95cf3558a417cc0373[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Wed Nov 13 21:59:59 2019 -0700

    Added override preset button on middle mouse
            modified:   Assets/HSVPicker/UI/ColorPickerMessageSender.cs
            modified:   Assets/HSVPicker/UI/ColorPresets.cs

[33mcommit fd28f052f11b18b5817f95363fa0366b904bb0d4[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Wed Nov 13 21:21:29 2019 -0700

    Quick fix to workflow button/measure line
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scripts/MapEditor/Grid/MeasureLinesController.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/KeybindsController.cs
            modified:   Assets/__Scripts/MapEditor/UI/UIWorkflowToggle.cs

[33mcommit f982438c850330fbd5f03c448ef0bc9360d52e24[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Wed Nov 13 20:33:09 2019 -0700

    BPM Tapper update & fix hitbox on 00_FirstBoot
            modified:   Assets/__Scenes/00_FirstBoot.unity
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scripts/MapEditor/UI/BPM Tapper/BPMTapperController.cs

[33mcommit 15a283bc7672a8e6b8c55d71841bcfbf157d8358[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 13 19:12:02 2019 -0800

    big braIN BOX BOYE

[33mcommit 5cfab1136e2a702eeca0cf944165a45de5c68aae[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Nov 12 22:01:16 2019 -0700

    Decreased size of counters+ and moved and made time only tick while focused
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scripts/MapEditor/UI/CountersPlusController.cs

[33mcommit a242a63cf0bf966efc62b572b3d2cd0769783c75[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Nov 12 19:42:22 2019 -0700

    MM does support ChromaToggle (in a half-assed way) and added ring prop to table
            modified:   README.md

[33mcommit 905224c99b9c3bcbc03341783067d141c5b1d591[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Nov 11 09:28:36 2019 -0800

    Rocket environment + other changes

[33mcommit 380a93fb24569b7269f4517964d1da769065b157[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 9 22:16:57 2019 -0800

    Time Spent Mapping (should convert to/from MM/MMA2)

[33mcommit 902dc93ea456279907dd36b25f605f62aa75c389[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 9 21:40:14 2019 -0800

    prevent notes triggering ding when they're too far behind

[33mcommit 8ad5572b7b6a6116dd7c3cc02965dc27c6a1ed8d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 9 21:38:22 2019 -0800

    Various platform fixes/changes, added Origins

[33mcommit 13e1fcb41cd83d1e242f55d8b7217e59a16a3864[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 9 21:02:35 2019 -0800

    Event lights on grid now match custom red/blue colors

[33mcommit 5d8c258ccec1cf0afafb5561f8a9571517629ace[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 9 20:46:38 2019 -0800

    Added setting and UI toggle, and box select now is ctrl click

[33mcommit f5d461ac280fdb53c2e7a81c76a36980a465012d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 9 20:13:51 2019 -0800

    box selection

[33mcommit 5cd0ff9a410e7a5c0f5cef1ad83c66c49d3c361d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 9 12:13:41 2019 -0800

    Bongo cat bongos, made it easier for modders, kiwi

[33mcommit 9072e45bc830bc70b98a9fdad19124d717b65d3e[m
Author: Aeroluna <40481393+Aeroluna@users.noreply.github.com>
Date:   Fri Nov 8 19:09:13 2019 -0700

    My job is about to be replaced by a birb (#12)

[33mcommit 364a245deb557075a75a8a5b15566665f1c1b5b1[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Nov 7 19:48:26 2019 -0800

    wasted two and a half hours of my life replacing the old spinning icon with the new chroma logo

[33mcommit c8ea55a42cdc55f8723e3d1e2709228bbdc78087[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 6 15:15:30 2019 -0800

    Strobe Generator rework final

[33mcommit b013a9d6c8cbab5622dd16410736f0d0d7d30477[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Nov 6 15:02:50 2019 -0800

    Strobe Generator rework #2

[33mcommit dd0e2ad5447bbb0665150e2097d039ccc290f63e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Nov 4 21:03:07 2019 -0800

    Strobe Generator rework #1

[33mcommit 5e45f491d6a32d6da228f8e4c5d4ac5720a4c6cf[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 2 17:29:38 2019 -0700

    New text logo, measure line tweaks, and bug fixes

[33mcommit 0cba2aba8d3677f534f6de90d6f72689b8992a40[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 2 13:32:11 2019 -0700

    waveform QoL and note sorting change

[33mcommit b88d85a48adffb548f8478fcaf14bbc1d58387bc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 2 12:44:42 2019 -0700

    CM Dark Theme and improved Waveform quality

[33mcommit c03c30ff669f8bd773c280ad8d3062a90c4eb74b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 2 11:32:41 2019 -0700

    Reminder add

[33mcommit d9513d02e86be380013dcf792e8a0558d04268c4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Nov 2 11:07:40 2019 -0700

    Bug fixes and more big brain refactoring

[33mcommit 25eeb173a8dd0276cffea798f74fa71322f550bc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Nov 1 22:11:38 2019 -0700

    big refactor because why the fuck not

[33mcommit b4af590b1828a85744c7e4e9f7d14b27b3514335[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Nov 1 18:20:05 2019 -0700

    Translucent note/bomb, song sorts sets and difficulties, other fixes

[33mcommit 091bcec1197c32d33d45c6fe6ee34d0b01e2adac[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Oct 31 20:36:50 2019 -0700

    helpful presets and bug fix

[33mcommit b9cee17353e8e3a1a3a853426fe7f4e1462149f7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Oct 29 18:03:39 2019 -0700

    Start Beat Offset and other tweaks

[33mcommit fc9e8d0afe721138dd082b861bebafde1d019c8c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 27 21:18:03 2019 -0700

    Fixes and BeatSaber schema updates

[33mcommit 0f72b3b0922d1c2a9d0f54c68aa8ceb9ffcf65a6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 26 21:08:44 2019 -0700

    Filtering by track ID

[33mcommit df586470e16697b3ae4ea6d8b88073a4b0e88805[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 26 14:22:52 2019 -0700

    Assign selected objects to a track id

[33mcommit c57acf9f2b1b26ffa16bda4e1f6c482f31bf7e10[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 26 14:02:57 2019 -0700

    basic custom events support

[33mcommit 9059c232f6f3d3359203651bd461f9f498237b14[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 26 11:33:20 2019 -0700

    add Input Box

[33mcommit c4bc133ac79f620014cbacc9cba5918927bb9864[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 26 10:50:50 2019 -0700

    the start of something epic

[33mcommit 404e8dd1bf05bd35c8fb70d3855b4e71e59197ae[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Oct 22 21:30:17 2019 -0700

    Single Saber > One Saber

[33mcommit e7a492d6c577eaf194472e5278839ac6b9e32fe3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Oct 21 18:06:14 2019 -0700

    Updated CM to new BeatSaver schema changes

[33mcommit 20f1325af8f4923c21c93b6d61a29cb2172de1c5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 20 16:56:54 2019 -0700

    final bug fixes for the day.

[33mcommit e8242c13fffde439998d2c395dc42295161d5afe[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 20 11:14:42 2019 -0700

    i dont engrish

[33mcommit ddb425f3429aa68d76370ecbad7c177d8ac6c4b5[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Oct 20 11:12:47 2019 -0700

    README changes regarding open beta

[33mcommit 16ff4c07177638b7524f3ed16142706e78677880[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 20 10:12:40 2019 -0700

    Set navigation mode for every UI element to none

[33mcommit bdfce471a01e321bf8d6995c606813d2d896a1a6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 19 23:01:16 2019 -0700

    more bug feex then i got sleep

[33mcommit a508df103827f1ead4eb6ad572804879e5833e1e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 19 22:00:26 2019 -0700

    Bug fixes and Credits updating

[33mcommit 9e5f9c4c5511cae1fd74db2395fd6a00c4da978d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 19 18:26:38 2019 -0700

    Fixes with KDA Environment, Mapper scene fixes

[33mcommit 10016fe31a7dcc952d316c65317bb1d8dafc14ba[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 19 15:21:27 2019 -0700

    Bug fix extravaganza!

[33mcommit d3aa7246569750c027589d45eefea34194402e68[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Oct 18 22:30:48 2019 -0700

    Performance improvements with object heavy maps, still some to go

[33mcommit c55e262eab5f370920ab187ed7b15251efb37a04[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Oct 18 19:34:37 2019 -0700

    offset bug continues

[33mcommit 5df9cec5b033fd4a2995d0d852cd4e5b2192ec84[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Oct 16 16:12:39 2019 -0700

    Placement w/offset fix, song info fixes and tweaks, Dragons platform more accurate to BS

[33mcommit e89168628e8dac8bda16ca729241c4f144c14f7d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Oct 16 13:45:40 2019 -0700

    Bug feex, you can delete preset Chroma colors with shift leftclick

[33mcommit 4033c31c5958bb6e7ea9eb7fc6a7e4559e9b9eba[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Oct 15 19:51:33 2019 -0700

    Tried (and undid) cone detection for vision blocks, finish arc visualizer, delete difficulty button

[33mcommit 07c4fb769a6164b9d6fd486ac4de49ec9499c5b7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Oct 14 17:59:33 2019 -0700

    Updated Swing Arc Visualizer, Saving maps now save requirements/suggestions in info.dat

[33mcommit 4d75bfb00058daef67d5d1947301eefd12a81bb9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 13 18:01:23 2019 -0700

    More offset fixing and start on Note Swing visualizer

[33mcommit 19c30dbfec98cad9a8fa928191a125ad90e25ae3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 13 00:32:37 2019 -0700

    fuck ton of bugs, moar tomorrow

[33mcommit 783b46c85ee5d37aa320fe358ea3b510966e588e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 12 16:08:17 2019 -0700

    Its bug fixing hours my dude

[33mcommit 92dd132baa47ba431afb4ff5fb0b3cf42a94505d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 12 13:14:30 2019 -0700

    Misc. bug fixes

[33mcommit 98ca26a3ce7e32d57ab062d6d728bf9b02177ce3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Oct 9 21:15:49 2019 -0700

    Fixes and such

[33mcommit bcc156c5233a586c5fa82d35b20859849a10c39c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Oct 8 16:01:57 2019 -0700

    node editor fix (git blames jackz)

[33mcommit 13b78a04e6271ab0279eac0e3dd579894fb1dc91[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 6 20:14:19 2019 -0700

    manually fixing git merge conflicts is fun

[33mcommit 23e237bb3017854b7277d8f468a6c86a01c41e4b[m
Merge: fef2a91a fb938f64
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 6 20:04:18 2019 -0700

    god i hope i didnt fuck up this merge conflict fix

[33mcommit fb938f6400299687d0f84c248df8b2002d3e8e8d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Oct 6 19:54:49 2019 -0700

    Bug fixes and tweaks

[33mcommit bfe6daec4432a3248b212bfb53c26a164ef06770[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 5 21:53:52 2019 -0700

    Custom Colors can now be set from UI!

[33mcommit fef2a91a5d4289cf4f27e03c906c8514dc798870[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Oct 5 17:39:29 2019 -0700

    Development: 10/5/19 (#10)
    
    * Bug fix with custom colors
    
    * Flash now gives off higher intensity before fading
    
    * new OSC message format
    
    * Flash brightness fix, remove unneeded FFT
    
    * Misc. fixes
    
    * Replace Post Processing toggles to an Intensity slider

[33mcommit af94cb30df6eded080f2998d387f68ed9b9641d7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 5 17:36:13 2019 -0700

    Replace Post Processing toggles to an Intensity slider

[33mcommit 171a72808656cc8d18bc7d7cf37b76c233d7a2ff[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 5 12:17:52 2019 -0700

    Misc. fixes

[33mcommit e0e3f97c31d04a490b7e589cfbe4d530308ad0b5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 5 09:53:17 2019 -0700

    Flash brightness fix, remove unneeded FFT

[33mcommit 437b4109ed8122af1f9cd32a50ee2a3b9e1b1d5a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 5 09:51:09 2019 -0700

    new OSC message format

[33mcommit c1f2a58deecd81b1d4d9ad0de2c6af0be4167212[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 5 09:44:05 2019 -0700

    Flash now gives off higher intensity before fading

[33mcommit 8fb68dd69ed39ab7a3d2a70dd583e06062481d2d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Oct 5 09:40:13 2019 -0700

    Bug fix with custom colors

[33mcommit 7ad54f44d2682e4e6e6402129ab71d2a4c481153[m
Merge: 5be5a298 f2e1b8cb
Author: Caeden117 <caeden.s@outlook.com>
Date:   Fri Oct 4 21:50:08 2019 -0700

    10/4/19 dev (Merge #9)

[33mcommit f2e1b8cb78adebb0c604b80b1a1d7759b77ebc95[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Oct 4 21:40:25 2019 -0700

    In one giant ass commit, allow you to undo placing shit and regenerating any previously conflicting objects

[33mcommit 836882364186ebed2a1232b1ec0c276e781957b9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Oct 4 20:35:20 2019 -0700

    Waveform final touches

[33mcommit 3f8388ca0f2e2d1d10dc3b6ea804e8e9821562e3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Oct 4 19:26:17 2019 -0700

    Waveform, better custom colors, bug fixes

[33mcommit 5be5a298cd4fe1d05a891c824c33cbb0ccc4287a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Oct 3 18:10:41 2019 -0700

    Editor Options rework, Node Editor fixes

[33mcommit bf467e03a9dab23dadac7f21a0577d2e768c3c65[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 3 17:03:03 2019 -0700

    remove "How to Apply" section from license

[33mcommit da67e4ce8183f4a3d2f282672a8bd79d66e8237d[m
Merge: 8718cb3c c6a59264
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Oct 3 16:59:03 2019 -0700

    Merge branch 'master' of https://github.com/Caeden117/ChroMapper

[33mcommit c6a5926454316b5c838fb8d6bf0ff59371ba1e24[m
Merge: d00885bc 1dea2534
Author: Caeden117 <caeden.s@outlook.com>
Date:   Thu Oct 3 13:05:40 2019 -0700

    Performance bump by at least 9999% (Merge 8)
    
    Replace checks with string.IsNullOrEmpty()

[33mcommit 1dea2534ef81db78e04cb552bea02d36b670093e[m
Author: Jackson Bixby <me@jackz.me>
Date:   Thu Oct 3 19:32:20 2019 +0000

    Replace checks with .IsNullOrEmpty()

[33mcommit 8718cb3ca36a2160d22ed91c479e691baa4e5d6e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Oct 2 17:18:12 2019 -0700

    Bug fix, N to toggle Node Editor (as a separate option)

[33mcommit d00885bcf36a6a189420c7651e4a6d9e997df994[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Oct 1 19:38:51 2019 -0700

    use big brain to get big brain stuff to happen

[33mcommit 82327c729a7f448cfe2045fdeceb16aca2d63060[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Oct 1 19:12:26 2019 -0700

    layer masks are wack

[33mcommit 58acba9b322a56202c179cde70efe508f179a684[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Oct 1 16:51:03 2019 -0700

    Vapor Frame and BMv2 backgrounds, some fixes i hope

[33mcommit 8c2fc638a7b443149a69cd6277879690f4886a7b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 30 17:45:31 2019 -0700

    Pixelguy complaints #2 (+ others)

[33mcommit 76824d4034b4538e6493dd432c2b2debce6662d0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 30 15:55:39 2019 -0700

    Fix Pixelguy complaints (#1)

[33mcommit fcd3bb02fee52cf8f301d6b522f021671181e6a3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 29 21:06:45 2019 -0700

    Batch Size UI setting and fixes

[33mcommit 9d1db41929aa78792fa847560a00bc55375e2cbc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 29 17:02:22 2019 -0700

    Huge ass settings rework (POG)

[33mcommit c4aad555c5aa049d3a595a8ad6d1248443f8a858[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 29 15:08:53 2019 -0700

    Batch Loading for initial map load; Settings rewrite now

[33mcommit c5a5b73cd0cfa0b5aa1805c4a8095ea25702d1f0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 29 14:35:08 2019 -0700

    Change how platforms are loaded

[33mcommit 64f214bd06448aa3fcdc112859d5fe4873d71629[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 29 13:38:48 2019 -0700

    Song Deletion now uses Dialog Box, code reduction

[33mcommit 64b2e2f80c2477323176f1bcca46a12f7febccad[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 29 13:27:26 2019 -0700

    Quit CM button added, Chroma Colors save/load fixed

[33mcommit 5206ca0927d5e7b2aa59f5a001148d97445f4904[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 29 13:10:29 2019 -0700

    Added a versatile Dialog Box system and a Save and Exit prompt

[33mcommit 3588f8753e25faa319aa86067861fce02030fb76[m
Merge: e0774e66 3d3723f5
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 28 20:27:53 2019 -0700

    yee haw gamer (Merge #7)
    
    Added BPM Tapper

[33mcommit e0774e665dacf8f129f0454e447244031e352157[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 28 20:22:38 2019 -0700

    Open Beta date

[33mcommit 3d3723f5d661610d57c9361f27ab74b7ccda9a98[m
Author: Auros <41306347+aurosnex@users.noreply.github.com>
Date:   Sat Sep 28 23:21:59 2019 -0400

    yee haw

[33mcommit 5738e078bef55d796fa66277bb6c14ed504e7566[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 28 19:18:23 2019 -0700

    Bongo Cat optimizations

[33mcommit 204d297ea812f11d9a152c6cc3336cf75bc6db0b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 28 15:35:03 2019 -0700

    Add bongo cat sounds when enabled

[33mcommit b5abc5f104990b2dd53a689a0f704ce2b1c68294[m
Merge: 2b120366 0d988e42
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 28 15:15:36 2019 -0700

    Merge branch 'Aeroluna-branch'

[33mcommit 0d988e42b2e4dc3737c7cb14148ea3ab01858c4c[m
Merge: 2b120366 ef94553a
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 28 15:15:04 2019 -0700

    merge conflicts taken care of

[33mcommit 2b120366f1e26f74aaf323ecad5ce49d5b224a79[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 28 15:02:57 2019 -0700

    Trying my best, in-editor is shit, standalone player is fine...?

[33mcommit 4a6277ba5c38f957f2ce332bc91475f145bf079a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 28 12:33:29 2019 -0700

    Split Editor UI into multiple canvases #1

[33mcommit f724f8b0428bb2cee739f9fb8a5af2c047fff49d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 28 10:47:11 2019 -0700

    You can now shift objects w/ME precision placement by user precision step

[33mcommit ef94553a079c5230eff08e3964b7b50807002cc3[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Fri Sep 27 14:50:17 2019 -0700

    Removed _editorOffset
            modified:   Assets/__Scenes/04_Options.unity
            modified:   Assets/__Scripts/BeatSaberSong.cs
            modified:   Assets/__Scripts/MapEditor/AudioTimeSyncController.cs
            modified:   Assets/__Scripts/MapEditor/LoadInitialMap.cs
            modified:   Assets/__Scripts/UI/SongInfoEditUI.cs

[33mcommit f6cba5d98c6eee8ed2580d7353a0657911ac950e[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Fri Sep 27 11:24:30 2019 -0700

    Tooltips revamped
            modified:   Assets/__Scenes/00_FirstBoot.unity
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scenes/04_Options.unity
            modified:   Assets/__Scripts/UI/Tooltip.cs

[33mcommit 91027d90631f09d42808d89d103b9747121324d1[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Thu Sep 26 23:57:35 2019 -0700

    Bongo Cat joins the fight!
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat.meta
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongodLdR.png
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongodLdR.png.meta
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongodLuR.png
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongodLuR.png.meta
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongouLdR.png
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongouLdR.png.meta
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongouLuR.png
            new file:   Assets/_Graphics/Textures And Sprites/Mapper/Bongo Cat/BongouLuR.png.meta
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scenes/04_Options.unity
            new file:   Assets/__Scripts/MapEditor/Detection/BongoCat.cs
            new file:   Assets/__Scripts/MapEditor/Detection/BongoCat.cs.meta
            modified:   Assets/__Scripts/MapEditor/Detection/DingOnNotePassingGrid.cs
            modified:   Assets/__Scripts/UI/Options/OptionsController.cs
            modified:   Assets/__Scripts/UI/Tooltip.cs

[33mcommit 5a735feb4adc5768b84116eb5022438dc013dcf2[m
Merge: cad69003 42ff3c63
Author: Caeden117 <caeden.s@outlook.com>
Date:   Wed Sep 25 08:05:20 2019 -0700

    I sense a theme here. (Merge #5)
    
    Merging PRs while at school, hmmmmmm

[33mcommit 42ff3c63ac649aff99303c01912b6f3f9fb78bc5[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Sep 24 22:29:12 2019 -0700

    Various bug fixes and changes
            modified:   Assets/__Scenes/03_Mapper.unity
            modified:   Assets/__Scripts/MapEditor/AudioTimeSyncController.cs
            modified:   Assets/__Scripts/MapEditor/CameraController.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/KeybindsController.cs
            modified:   Assets/__Scripts/MapEditor/UI/CountersPlusController.cs

[33mcommit f3b1b63bfd9acd54b436857461cc7a6c0d62bcc2[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Sep 24 20:29:35 2019 -0700

    Flatten event and note grids
            modified:   Assets/__Scenes/03_Mapper.unity

[33mcommit 371960558e780a0f684194267ff4c7dee3939935[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Sep 24 20:07:43 2019 -0700

    Patch auto-saves to always use the same folder
            modified:   Assets/__Scripts/MapEditor/AutoSaveController.cs

[33mcommit cad690032c214b59c52aadc9f75eff4590adfdf0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Sep 24 16:34:39 2019 -0700

    slightly not accurate Dragons platform

[33mcommit ab6dd468b595843d3fb46c66e444c44d1cf4855a[m
Merge: 51c69176 53356e5e
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Sep 24 12:16:16 2019 -0700

    It's like I'm coding at home (Merge #4)
    
    This is how much CM development speeds up with more contributors PogU

[33mcommit 53356e5e4974fa88fe0d42fbc8353a59190fab7c[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Sep 24 10:55:23 2019 -0700

    Fix tooltip not being visible on screen edges
            modified:   Assets/__Scripts/UI/PersistentUI.cs

[33mcommit 488e58c2ef3ec6c028f080db16790d2acc7d1688[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Sep 24 10:24:14 2019 -0700

    Bug fixes in EventPlacement.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/PlacementControllers/EventPlacement.cs

[33mcommit 7183fb7c89a36582744fae62566971f768512b75[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Tue Sep 24 10:07:35 2019 -0700

    Add Place Chroma Event Only Toggle
            modified:   Assets/__Scenes/04_Options.unity
            modified:   Assets/__Scripts/BeatSaberSong.cs
            modified:   Assets/__Scripts/Map/Events/EventAppearanceSO.cs
            modified:   Assets/__Scripts/MapEditor/Mapping/PlacementControllers/EventPlacement.cs
            modified:   Assets/__Scripts/UI/Options/OptionsEditorSettings.cs

[33mcommit 51c691760b5a80f6878673d5bb9b8c1e014b4fdd[m
Merge: 21dc85cc 4567676a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 23 22:31:10 2019 -0700

    At this point I could've done it myself but oh well (Merge #3)
    
    Fix middle-click events flipping to incorrect value

[33mcommit 4567676a9cb53ad1174ab2602a8690dbb7603fc2[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Sep 23 20:43:23 2019 -0700

    Fix middle-click events flipping to incorrect value
            modified:   Assets/__Scripts/Map/Events/BeatmapEventContainer.cs

[33mcommit 21dc85cccf36715dd7bf8478ed8ec4db55fa6e25[m
Merge: b209f60c b33d4552
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Sep 23 20:27:04 2019 -0700

    Discord Webhook sees all. (Merge #2)
    
    Fix pepega camera

[33mcommit b33d45526d6ee0e5b42448d71dd3585f85e83f0a[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Mon Sep 23 20:21:57 2019 -0700

    Fix pepega camera
            modified:   Assets/__Scripts/MapEditor/CameraController.cs

[33mcommit b209f60c4d12c3975614bb77d25b4ef8f536b1be[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 23 20:21:46 2019 -0700

    bug fixes for those bug fixes

[33mcommit 225cef732f98f82e4067a2261779c1b72ba5a851[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 23 20:12:10 2019 -0700

    More bug fixes

[33mcommit 1d56d4a24f4093b45e53ee121798a4e21fe85261[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 23 15:57:15 2019 -0700

    Tweak things in response to user feedback

[33mcommit 6b63c310b7a837c733439e3bb3cec38720bc37a8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 22 21:42:31 2019 -0700

    Alt and Alt+Scrollwheel modifiers for most objects

[33mcommit b6776e12218ae18480df4369e0a8b41134c9f772[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 22 12:20:35 2019 -0700

    Swap to Layout Groups, making adding Patreons/Alpha Testers a lot easier

[33mcommit 9c6eb587fe325a0a08cf302d69ab0bb607a84bbd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 22 11:52:10 2019 -0700

    Remove now unneeded Pixelguy Syndrome™

[33mcommit 978bf264678f7ed3d3d7e5416353e239f4c7abb9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 22 11:50:27 2019 -0700

    Modified PR code a wee bit

[33mcommit c0a6d562b42ea366123adae82b83bb0a6b00cbf0[m
Merge: ebc21127 5c3d009a
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sun Sep 22 11:47:39 2019 -0700

    TIL Thread.CurrentThread.CurrentCulture is a thing (Merge #1)
    
    Delete pepega toString and set culture to InvariantCulture before writing

[33mcommit 5c3d009a0925b58194e0d6e9e5293d32a7f4eaa8[m
Author: Aeroluna <aeroluna5@gmail.com>
Date:   Sun Sep 22 11:26:48 2019 -0700

    Delete pepega toString and set culture to InvariantCulture before writing
            modified:   Assets/__Scripts/BeatSaberSong.cs
            modified:   Assets/__Scripts/Map/BeatSaberMap.cs
            modified:   Assets/__Scripts/Map/Events/MapEvent.cs
            modified:   Assets/__Scripts/Map/Notes/BeatmapChromaNote.cs
            modified:   Assets/__Scripts/Map/Notes/BeatmapNote.cs
            modified:   Assets/__Scripts/Map/Obstacles/BeatmapObstacle.cs

[33mcommit ebc211273ade252952d4653a9ca5610cce87eb56[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 22 11:03:24 2019 -0700

    Fix more Pixelguy Syndrome™ instances, and Shift + Alt for basic mass selection

[33mcommit 9bdecfa2955e0eaf9a085f722280df1cf9c95608[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 22 09:34:01 2019 -0700

    get ourselves some obj

[33mcommit f47d0db821e9f1709017afc2b06fac1a103fc51a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 22 09:13:47 2019 -0700

    Heh, that's .meta!

[33mcommit 6a03c0436bb4f94c36d2e5f8a9d775f63442d656[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 21 22:10:11 2019 -0700

    i cant Markdown format

[33mcommit e434b2b108557e61a23f48ddd614d4f80a09385f[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Sat Sep 21 22:08:33 2019 -0700

    Explain potential format issues in README

[33mcommit e46b5eb328e68f1f80213d5385cb575dfd0a9f6e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 21 12:18:57 2019 -0700

    more JSON format fixing

[33mcommit 833bc6a3ea53d9abdc4c850e2ef240ff4d40ead4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 21 10:37:48 2019 -0700

    Ring zoom, fixed globalization issues

[33mcommit ea3279afa47ad2b8bd6b91ba090d06cb42643e53[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Sep 20 20:44:09 2019 -0700

    fixes and stuff for a sudden new alpha release

[33mcommit c4e144e0e220a1ef8fd478f881257a8adfe9f9ee[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Sep 19 21:45:08 2019 -0700

    Hopefully better beatmap characteristic support. RIP MM

[33mcommit 13c9d89430835a48f31b40eaf289f31fc58c0b2e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Sep 19 06:06:51 2019 -0700

    ring prop stuff

[33mcommit 364edeae271347630905998cc7bf2fd4c8876e59[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 15 19:47:02 2019 -0700

    fixes and propagation ID support

[33mcommit 4c9120fd4b92981b75d4fb766b2ede194c3cd91e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 15 14:53:41 2019 -0700

    Remove now unneeded classes

[33mcommit 75b87249c5f65b1c455a37ec637e5f6a865496cb[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 15 14:48:24 2019 -0700

    Final touches

[33mcommit d4d2f080a1eb90267f9ef03888f7e19d8e41026f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 15 11:45:11 2019 -0700

    Event placement and bug fixes

[33mcommit 09ce51858f77f662a2fb9735eaea46e691134ba8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 14 20:00:08 2019 -0700

    Obstacle placement & action fixes

[33mcommit 4db29a39bd3e9c9013a6ad063c0aa6fa16cfbaf2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Sep 14 18:06:56 2019 -0700

    Reflection Probe

[33mcommit c969c04f22d356013bdc02dbe051f3aab9cbbb87[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Sep 13 23:34:44 2019 -0700

    Swapped from crappy homemade RGB Slider to chad open-source RGB Slider

[33mcommit 68e9f060f2248a76bfff7895ef3b2eb43a258e97[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Sep 13 21:56:17 2019 -0700

    Bomb placement

[33mcommit 17eb0e46e7249b2a423d781ac6780b7dd20996d5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Sep 13 20:02:43 2019 -0700

    Rendering order fights and fixed outline shader

[33mcommit 65606b5aac2f20baf4c69ec7a0976c60b94e6334[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Sep 13 19:06:26 2019 -0700

    UI rework, placement rework going well

[33mcommit b4099af0a4f312b8d4820ab3ab4a80561a34acd9[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Sep 10 21:34:10 2019 -0700

    new logo + Discord RPC support for them

[33mcommit 7283d7b36785dd9b8a515b9ef39071b03b85bcd6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Sep 10 20:29:31 2019 -0700

    Reworking how shit gets placed. Redone red/blue notes

[33mcommit 608a4e1f655df1590aee51542b2c7f886138c699[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Sep 10 15:55:42 2019 -0700

    Autosave is now static (Persists between maps)

[33mcommit cd8327cc2686540d28659cdfd7d0fe22d1dc39ec[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 9 19:52:20 2019 -0700

    finish new light controllers

[33mcommit 6d0bd4e31bf1e60668546ef0411f3e55d3336746[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 9 19:19:55 2019 -0700

    Changed how lights are controlled (More performance)

[33mcommit f52c14329ea05fade388a3a460896f605e9285f4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 8 09:35:25 2019 -0700

    README update and obstacles feex

[33mcommit 6f2b3007818a25bb7399663799bb75f04b8ea593[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Sep 6 21:26:41 2019 -0700

    ChromaToggle converter & Obstacles bug fixes

[33mcommit d1c44756ff80bd93db66a317dd8af71a7789146a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Sep 3 21:38:51 2019 -0700

    Final bug fixes before closed alpha release

[33mcommit ca255a27b1b5d23bab58241c98c0a4b642087c38[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Sep 3 16:06:20 2019 -0700

    Bug fixes and Autosave moving to separate folders (Untested)

[33mcommit 39dabb07ef96162d090aff3697bb165162e35b2d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Sep 3 06:12:15 2019 -0700

    Bookmark support

[33mcommit 9aa96f95a7427d79ab2ff21cc3a4763b4eb7d366[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 2 16:59:43 2019 -0700

    Settings for OSC

[33mcommit 2620354ab2362059c9aac87b580e5443c5c91fa8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 2 14:48:11 2019 -0700

    Chunk-based loading, performance fixes

[33mcommit 9dd26ac771ac8b73dc71794b9a4b20e57f732cc1[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Sep 2 09:37:02 2019 -0700

    Remove unneeded materials

[33mcommit cb433ed803428a19dfa86955075dd43dc350b79e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 1 23:22:01 2019 -0700

    various tweaks and fixes

[33mcommit 6f1830e31b905fd4f87ee6f4939f7f7d7d4096dc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 1 22:19:01 2019 -0700

    Fixed flickering between lights/platforms

[33mcommit dbb70b00ec657337f6932d2877ee09d6926b623b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 1 22:16:41 2019 -0700

    Upgraded post processing (and controller)

[33mcommit c0366f778216ee90c6ca6e714a7e4d06017a72db[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 1 22:10:49 2019 -0700

    Added more closed alpha testers & Left laser fix & Laser tweaks

[33mcommit 0ef54f733f547635ef4fc47be6f3957325d1bfdc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 1 20:32:44 2019 -0700

    Fix broken UI from LWRP upgrade

[33mcommit 41bc5e34837315ef3a058d8d6265fd6bab1b30f4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Sep 1 00:56:34 2019 -0700

    Swap to LWRP for lighting, UI hella broke

[33mcommit 069389342f22ab09f95bb1ea61ce62d198ddc38d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 31 22:13:14 2019 -0700

    moved PPv2 into package

[33mcommit 3c04fcc48b997917f41b17463ceff8f5a6fd1cc3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 31 19:28:59 2019 -0700

    i fucked up badly

[33mcommit 632b9ec235df424760529efcf287d773b0ac6b7e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 31 18:15:19 2019 -0700

    fucked up

[33mcommit 458e95d83992a6e9b8e40e88c676042b40657b41[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 31 17:37:58 2019 -0700

    OSC Integration, bug fixes, glow shader start

[33mcommit f573349b59a803776dce24d1ddacacac7cf88a58[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 30 23:08:44 2019 -0700

    holy shit i finally fixed it

[33mcommit 7c334fc871919ab02bb3e1773160a4ccfb9934b7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 30 22:29:16 2019 -0700

    fixed it somewhat

[33mcommit cd72d8a037444b7acbe01bd0ce7a15d5fc7db5b0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 30 21:35:44 2019 -0700

    attempting to add StrobeGeneratorGenerationAction

[33mcommit d62dc084a63014c6a4bc5f6a7d09f37e1a634cc3[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 30 16:29:48 2019 -0700

    fix NodeEditorUpdatedNodeAction

[33mcommit 596d86a4bfd85b8cab15db6775d30255ef88d470[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 30 16:22:10 2019 -0700

    added NodeEditorUpdatedNodeAction

[33mcommit fcc8ce9b314db35de8c135fa1233cb92080399e8[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 30 15:58:48 2019 -0700

    fixed selection pasted action

[33mcommit e5c03df00b441f83a35ea258f30721243b28ec78[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 30 09:48:46 2019 -0700

    fixed flickering when hovering over grids

[33mcommit 202af0f1933c9e712bd4c7af472e6b559ab51717[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 29 22:40:00 2019 -0700

    More advanced actions, other tweaks

[33mcommit 8fb9395676c7129b347957a3663aefb2dc1befd5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 29 15:52:17 2019 -0700

    Added basic actions for placing/removing objects

[33mcommit 00bd2ee5f3c0a3f06e4e591e9663fe5a2eba10b7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 29 15:16:13 2019 -0700

    Basic Action Undo/Redo implemented! FUCK YEA

[33mcommit 02674dc8b01d0a3af208e3470b18ee9acdf87131[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 29 14:47:45 2019 -0700

    Huge refactor to make it easier for Beatmap Actions

[33mcommit b7a79945e5eb40bb1bc141c8d071a046cffd535d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 29 13:11:04 2019 -0700

    Basic Action refactoring

[33mcommit 32d41c61d30979ff3efdfdb9a5cbbb29e48ea700[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 28 13:18:48 2019 -0700

    Reversed Ctrl+Scroll for changing Precision Step

[33mcommit 681678b1e57d13a59ecc075df55ecbf79c5c64e2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 28 13:18:09 2019 -0700

    more action-packed Action stuff

[33mcommit d332903a7b119d6631646ac31531fcf4231c69e5[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 28 08:55:26 2019 -0700

    Basic Actions work started

[33mcommit a473b91313f75c6ec335b521c3e144151926f4b6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 26 19:52:29 2019 -0700

    add package dependencies

[33mcommit 4dd90f8771de122a129ca6aac0ee97e9bb8e8955[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 26 19:40:53 2019 -0700

    fix obstacle width bork

[33mcommit 7bc1c7ca728acf3c8ac526abf7db6027c16fd5c0[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 26 19:18:34 2019 -0700

    i should not have deleted the solution file

[33mcommit e00ba7b6308c2ba17c96429d2a9766e3f8063faf[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Mon Aug 26 19:16:38 2019 -0700

    finish up cleanup from github
    
    single file left lmao

[33mcommit 090d96c77dfb6bfeec340d7178deb2729363f426[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 26 19:14:40 2019 -0700

    Cleaned up repository

[33mcommit aebf5386fcc567458eb7dfdd2fd537a27b936569[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 26 19:10:24 2019 -0700

    Fix walls causing huge lane size

[33mcommit 5ff5b264861f775e6fb4b8a3dfeb62278a6a3737[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 26 19:04:06 2019 -0700

    Update .gitignore, hopefully will fix problems with opening in Unity

[33mcommit 745a487fd970b3e9de0d4abc012bd909408c812f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 26 08:59:03 2019 -0700

    Add walls detection of ME Requirement

[33mcommit 7260fdf001493f77fcbd23556fafd9394fe0ee53[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Aug 25 22:46:24 2019 -0700

    Fix message appearing several times

[33mcommit 147d8043018150c70fe077d79c0675e3f5f60f14[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Aug 25 22:27:18 2019 -0700

    Proper Mapping Extensions walls loading

[33mcommit c771e14e533365fa2ed656234913803394eeeca6[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Aug 25 21:23:29 2019 -0700

    Obstacle colors/outlines, ME walls support re-implemented (not yet 100%)

[33mcommit a91c89ff7e0544caeab4740431bc308549620f19[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Aug 25 12:30:00 2019 -0700

    Prefab changes, bug fixes, color picker, and deleting highlighted objects

[33mcommit a666dbc158f12e8d9cfb3dff0286f005fc39af1c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 21 10:41:00 2019 -0700

    Colliders for selecting & Measure Numbers text

[33mcommit 2b6a35ac38a7df6b8d0c066b15baadfdf0f34c03[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 19 19:19:19 2019 -0700

    i dont markdown

[33mcommit 064a8370f3fa17b99da4d012e1d95df49da60a16[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 19 19:18:00 2019 -0700

    Platform fixes and README update

[33mcommit 4f496edb31f6b6bc31770a713ae67c8f9887ad1a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Aug 18 09:52:40 2019 -0700

    Bug fixes and tweaks

[33mcommit 8bbb699b9f4132e82aa3841da1555ff35aa366ab[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 16 18:08:52 2019 -0700

    Switched default StringComparison to OrdinalIgnoreCase

[33mcommit f2784444f43463cc43535cae9dd6f4baf052d58e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 16 18:03:25 2019 -0700

    Improved Search and Ctrl-S to Save

[33mcommit b9308fc1eddaf4b66d526dcb967c380296e9729e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 16 17:22:05 2019 -0700

    Fixed Search Button and Options

[33mcommit 69ab93a395afa7e0a82847c5b6536016d587843f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 15 18:01:08 2019 -0700

    Options bug fixes

[33mcommit 67b9529a9ae9735506fbc5107e6a6eb230e6d383[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 15 17:56:21 2019 -0700

    PFPs for rest of alpha testers

[33mcommit 8ed9839daf301316143d57630aba00c98ba08e1a[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 15 17:46:55 2019 -0700

    Volume control and bug fix

[33mcommit b7f4867c311857c8bcb79c6cfb70390f057896bc[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 15 13:59:44 2019 -0700

    Options continuation 2

[33mcommit 5f0c39f8c5e279e97317122c68a0bf782a8f7aaf[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 15 11:36:30 2019 -0700

    Options scene continuation

[33mcommit 8bbf26265baf03d5258330c3949ba110f2f7c774[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Mon Aug 12 20:48:43 2019 -0700

    Fix Dueling Dragons platform

[33mcommit 65a1908b26d2bec61b9dbcc78ba7540c5a3db639[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Aug 11 15:49:07 2019 -0700

    Collider platform support

[33mcommit 2b2ab61ab1622aa243f9253c3a400f86b703587c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sun Aug 11 13:02:17 2019 -0700

    Replace old ring code with Beat Saber's ring code

[33mcommit f3cd747a6a9ad3bde9427ebe9e04fd115559bcba[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 10 20:23:54 2019 -0700

    Platform request from Skeelie #1

[33mcommit 426e501dfc88fc83ca6fb3469e99d77a5c21ca62[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 10 20:03:24 2019 -0700

    Colour Selector now has indicator

[33mcommit daed15337c4aeb803467469006b9d4a613377d8c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 10 19:56:52 2019 -0700

    Bug fixes and changes 1

[33mcommit 830b9c5f7d91a078e047bd9ad1eed4ed209afb29[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 10 15:14:39 2019 -0700

    Options scene started

[33mcommit 21d422d1d17c32b7a9334898f89e5052a1b26760[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 10 13:20:24 2019 -0700

    New delete keybind and help text change

[33mcommit 6da51d1d06a83a56c9ae88c0a9770c5df0be72de[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Sat Aug 10 12:19:30 2019 -0700

    Move selection w/ left/right/up/down arrows

[33mcommit 39d90ead52e86631d46a5b0edfca89dbacc10b85[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 9 21:07:44 2019 -0700

    Added Notes Per Second to map stats

[33mcommit 1d1cded8cf88f2210819edfc7d4a37691b7b3261[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 9 20:33:53 2019 -0700

    Removed debug scene swap code

[33mcommit 4904b92f2ec93816bcf5f1b8e6fe98069fd28ffd[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 9 16:44:51 2019 -0700

    slight change to discord

[33mcommit 5f5fc0248c1eaefd4f91f5a36cecad04665de68e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 9 16:44:02 2019 -0700

    Discord Rich Presence can now be toggled

[33mcommit 9f2b1d6c867bfb0bc35aca61b010eed102e60346[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Fri Aug 9 13:15:49 2019 -0700

    Discord Rich Presence support

[33mcommit 50229b6520ba741998ae27c8282a434167348206[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 8 22:40:50 2019 -0700

    Replaced Discord RPC with Discord Game SDK

[33mcommit 673bc8159b3456a79ae6be42c4d5ea43a2b0ebfb[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 8 17:03:59 2019 -0700

    Upgrade to Unity 2018.3.14f1!

[33mcommit 164f2ddea3cda831f26069fccd905e78f326db87[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 8 14:39:47 2019 -0700

    last minute changes before 2018.3 upgrade

[33mcommit a838dd9efecfcdf95f7c98fb1b3a6a6d9f8d8220[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Thu Aug 8 11:19:42 2019 -0700

    Bug fixes and stuff

[33mcommit 1d1450852ff7c24899344e5ef0e01da9b984b6cb[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 7 20:17:48 2019 -0700

    Note filters audio ding, bug fixes

[33mcommit eeb23fbbe6e04e7266dd9df2eafa4bc341e1d95f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 7 18:29:11 2019 -0700

    Show in File Explorer and Counters+

[33mcommit 4e1f92af693624689e85ee7ccaf2a9af672a605c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 7 16:53:43 2019 -0700

    Added manual precision step w/textbox

[33mcommit ea67be55d7061c5340f72fc170a2187d114633d7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 7 16:33:54 2019 -0700

    Timeline UI implemented

[33mcommit c9256a8ffc5a8c866f11a864abf647cef207116b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 7 14:35:25 2019 -0700

    various performance improvements and Waveform toggle

[33mcommit 0a1d3eb0a6609d8495a9743184d54a9d0053f128[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 7 12:58:45 2019 -0700

    Event material uses local space, big FPS boost

[33mcommit 1bcb0e5f82b12ac0b98ff3ee1c7ccfe6f60a77ea[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Wed Aug 7 12:39:10 2019 -0700

    Looking into ways to increase performance

[33mcommit ccb01285db477a4120a86bfe2d049d6ec24f323c[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 23:57:20 2019 -0700

    README update before sleep

[33mcommit ff6dc3a3c0b3cdf7c6d92b239d7d9c75ba67bc7b[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 23:45:23 2019 -0700

    Investigating a new way to check events

[33mcommit d57ed95338f87f412cddd48445f0192aec0bd205[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 21:19:06 2019 -0700

    slight change w/node editor

[33mcommit 6904da81069fcaf741433ec8621f08c94881b0a2[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 21:11:19 2019 -0700

    Node Editor finished

[33mcommit 5ee825f166732ce9f6b1bd9e174397f68045194d[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 20:11:14 2019 -0700

    Node Editor 2, and unplugged Undo/Redo from everything because you couldnt delete shit

[33mcommit 92d8605d531f0aa8eaad144bf44d2cff5a51829f[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 19:52:31 2019 -0700

    Node Editor part 1, and bug fixes

[33mcommit 32d383d1573fc05a0812529acfd9dada20ca8bcf[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 6 18:57:26 2019 -0700

    Update README.md

[33mcommit ba647fc07c48ea76ce6c12ff37682a9998efe842[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 18:26:15 2019 -0700

    more properly deleted github for unity? idk

[33mcommit 06d09e550943e2600f50ca23617eae73b74e3db4[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 17:12:21 2019 -0700

    README update from Visual Studio

[33mcommit de00f263895d90683417d512a5f449d74170894d[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 6 17:03:56 2019 -0700

    GNU GPL v2 license

[33mcommit 5f37ba5e9dc3c03c3ed2e5bac9ef21485d444083[m
Author: Caeden117 <caeden.s@outlook.com>
Date:   Tue Aug 6 17:03:32 2019 -0700

    readme, now!

[33mcommit d514e6f2c6096c0148f360791e667cf0062fbb19[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 16:56:17 2019 -0700

    removed GitHub for Unity

[33mcommit 2872f9065d0dcd94fa97dffa711923879a8f4c1e[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 15:50:48 2019 -0700

    Add project files.

[33mcommit dada6bb151e525c5d3332da8650d954393f33ed7[m
Author: Caeden Statia <caeden.s@outlook.com>
Date:   Tue Aug 6 15:46:01 2019 -0700

    Add .gitignore and .gitattributes.
