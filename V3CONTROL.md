# Additional Input for Beatmap Version 3.0.0
## Switch Map Version

In the map editor, press `alt` + `shift` + `.` to show the dialog box for switching version. Once confirmed, map version will be converted and then it will automatically exit editor. Next time you enter the same map and same difficulty, version is already converted.    
**Note 1**: Please manually backup your map before conversion.     
**Note 2**: Only compatible data(including notes, bombs, obstacles, basic events; excluding customData, slider, chains) will be converted, other may get lost. 
## Sliders
When exactly selecting 2 color notes(need direction), press `T` to create a slider between them.
The slider contains two notes, which indicate its 2 additional points in `Bezier Curves`. Those two notes can be color-inverted(`mouse mid`), tweaked(`alt` + `scroll` for start note, `shift` + `scroll` for end note)

## Chains
When exactly selecting 2 color notes(first one needs direction), press `G` to convert them into chains.
The header of chain(with arrow) can be color-inverted(`mouse mid`), tweaked note count(`alt` + `scroll`), tweaked squish amount(`shift` + `scroll`)

## Some Feature could be introduced
- ~~Undo for new introduced actions~~
- ~~Set transparency correctly~~(But not in consistent with previous notes' behaviour, I just set `TranslucentAlpha` to 1, which may not be the correct logic)
- ~~Select animation for slider/chains~~(similar as above, may not be the correct logic)
- Box selection for slider and chains
- Counters
- Updating while playing for slider and chain is activated by setting `UseChunkLoadingWhenPlaying`. I don't know if it is extremely harmful for performance.
- Support for customData(but plugins haven't been updated yet. So I don't need to do so...)
- Add support for angle offset
- Audio for chains
## Bugs
Not tested...