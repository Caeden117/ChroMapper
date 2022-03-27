# Additional Input for Beatmap Version 3.0.0
## Switch Map Version

In the map editor, press `alt` + `shift` + `.` to show the dialog box for switching version. Once confirmed, map version will be converted and then it will automatically exit editor. Next time you enter the same map and same difficulty, version is already converted.    
**Note 1**: Please manually backup your map before conversion.     
**Note 2**: Only compatible data(including notes, bombs, obstacles, basic events, customData; excluding arc, chains) will be converted, other may get lost. 
## Arcs
When exactly selecting 2 color notes(need direction), press `I` to create a arc between them.   
The arc contains two notes, which indicate its 2 additional points in `Bezier Curves`. Those two notes can be color-inverted(`mouse mid`), tweaked(`alt` + `scroll` for start note, `shift` + `scroll` for end note)   

## Chains
When exactly selecting 2 color notes(first one needs direction), press `C` to convert them into chains.  
Chain can be color-inverted(`mouse mid`), tweaked note count(`alt` + `scroll`), tweaked squish amount(`shift` + `scroll`)  
**Note 1**: Head note of a chain is still a note, but not chain. Therefore modification on chain doesn't go along with head note, and vice versa.  
**Note 2**: Placing chain in a weird anlge may not behave same in the real game.  

## Some Feature could be introduced
- ~~Undo for new introduced actions~~
- ~~Set transparency correctly~~(But not in consistent with previous notes' behaviour, I just set `TranslucentAlpha` to 1, which may not be the correct logic)
- ~~Select animation for arc/chains~~(similar as above, may not be the correct logic)
- ~~Box selection for arc and chains~~
- Counters
- Updating while playing for arc and chain is activated by setting `UseChunkLoadingWhenPlaying`. I don't know if it is extremely harmful for performance.
- ~~Support for customData~~(but plugins haven't been updated yet. So I don't need to do so...)
- Add support for angle offset
- ~~Audio for chains~~(now note is seperated from chain)
- Dragging for arc/slider.
## Bugs
- The note attached to a chain may sometimes rescale back to the original size. That is because I only check attachment when note/chain is spawned. Therefore, when other events(like change color, refresh pool) happen, attachement check won't get triggered, resulting in a wrong scale.
- Chain display seems not in consitent with game play when the chain is taking a U-turn.
- Order of selecting chains' two notes is imporatant, a note with direction could only be at **head**(not tail). Therefore if you happened to seem like making a note as a tail, it probably behaves not correct in the game.
- `Counters` is not precise. I haven't considered the case when a chain doesn't have head note. (So in fact it's using the old logic)
- Chain & arc don't fade when passing the threshold. That is because they are using `Chunkload`(same as obstacles, since you won't experience obstacle fading), not the same logic as note.
