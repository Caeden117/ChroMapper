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

## Transition Event Note & White Note
Transition event notes and white notes are introduced before map v3, but I will combine them together as v3 feature.   
To place a transition event note, press `C` and left click on the event grid.    
To place a white note, press `3` and left click on the event grid.  
### Half & Zero Modifier
After transition has been introduced, we will often place half alpha light and zero alpha light. To place a half alpha light, hold `X` and left click. To place a zero alpha light, hold `Z` and left click.

## V3 Light System
V3 Light system is implemented(roughly). Currently `The Second` plaform is supported. You will see the v3 light grids after entering this platform.
### Display UI Panel
The v3 light configuration panel is one of the items in the right panel. You can press `tab` and you will see a light aperture displaying `Add V3 Lights` on hover.  You can click the button to show v3 light UI panel. There is also a keybind for displaying the panel. The default keybind is `Shift + T`.  
Initially on the light grid, `lightColorEvent` is above the plane and `lightRotationEvent` is under the plane. The event above the plane could be placed. Clicking `Switch To Rotation/Color` will change the current panel and flip the light grid as well. There is also a keybind for this button. The default keybind is `Shift + R`.   
### Change parameters & Placement
You can change the parameter inside the UI panel to change the hovering note's data. Besides, the key bindings for old events may also affect the data. For example, changing to blue color will also change the color field in the UI.      
`Left click` will place the note on the grid. Placing multiple notes on the same position will stack them. This is possible because they may have different filters.  

### Interact with Selection
When there is exactly one light event selected, UI will display the data inside this note. During the selection, you can change the data inside the UI, and finally click `Apply to note` to store the data to the note. If you place a note after the selected note, the new placed note will be treated as a subnote of the selected note(in the json format, it will be the additonal EventData).   
After unselecting all, UI will display the hover note 's data again.    
For box selection, it will automatically select all the `lightColorEvent` and `lightRotationEvent` within the time and lanes, regardless of their heights(or Y-axis).

### Create Template
Manually input various parameters is boring. In such case, you can click `Create Template` to storage a note to the template. If non selected, hover note will be the template. If one selected, the selected note will be the template. `Left click` the button will restore the template to the UI panel.    
Renaming and deleting the template button is same as the bookmark, by `right clicking` and `middle clicking`, respectively.   
**Note**: Currently the storage is not persistent, which means it will get lost after exiting the map. I'm not sure where to store if we want it to be persistent.

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
