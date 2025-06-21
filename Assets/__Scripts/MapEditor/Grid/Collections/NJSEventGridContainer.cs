using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class NJSEventGridContainer : BeatmapObjectContainerCollection<BaseNJSEvent>
{
    [SerializeField] private GameObject njsEventPrefab;
    // [SerializeField] private NJSEventAppearanceSO njsEventAppearanceSo;

    [SerializeField] private CountersPlusController countersPlus;
    
    private static readonly int currentHJDShaderID = Shader.PropertyToID("_CurrentHJD");
    private static readonly int DisplayHJDLine = Shader.PropertyToID("_DisplayHJDLine");
    
    public override ObjectType ContainerType => ObjectType.NJSEvent;

    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
        AudioTimeSyncController.TimeChanged += UpdateHJDLine;
        UIMode.PreviewModeSwitched += OnUIPreviewModeSwitch;
        
        Settings.NotifyBySettingName("DisplayHJDLine", UpdateDisplayHJDLine);
        UpdateHJDLine();
        UpdateDisplayHJDLine(Settings.Instance.DisplayHJDLine);
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
        AudioTimeSyncController.TimeChanged -= UpdateHJDLine;
        UIMode.PreviewModeSwitched -= OnUIPreviewModeSwitch;
        
        Settings.ClearSettingNotifications("DisplayHJDLine");
    }

    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    public override ObjectContainer CreateContainer() => NJSEventContainer.SpawnNJSEvent(null, ref njsEventPrefab);

    // protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    // {
    //     var njsEvent = con as NJSEventContainer;
    //     var njsEventData = obj as BaseNJSEvent;
    //     NJSEventAppearanceSo.SetNJSEventAppearance(njsEvent);
    //     njsEvent.Setup();
    // }

    protected override void OnObjectSpawned(BaseObject _, bool __ = false) => UpdateHJDLine();

    protected override void OnObjectDelete(BaseObject _, bool __ = false) => UpdateHJDLine();

    private float currentNJS;

    public float CurrentNJS
    {
        get => currentNJS;
        private set
        {
            if (currentNJS != value)
            {
                currentNJS = value;
                countersPlus.UpdateStatistic(CountersPlusStatistic.NJSEvents);
            }
        }
    }

    public void UpdateHJDLine()
    {
        var baseNJS = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        var baseHJD = SpawnParameterHelper.CalculateHalfJumpDuration(baseNJS,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteStartBeatOffset, 
            BeatSaberSongContainer.Instance.Info.BeatsPerMinute);
        
        // No NJS Events => static values for NJS and HJD 
        if (MapObjects.Count == 0)
        {
            if (CurrentNJS == baseNJS) return;
            
            CurrentNJS = baseNJS;
            Shader.SetGlobalFloat(currentHJDShaderID, baseHJD);
            return;
        }
        
        // Grab NJS events
        var previousNJSEvent = MapObjects.FindLast(x => x.JsonTime <= AudioTimeSyncController.CurrentJsonTime + 0.01f);
        var nextNJSEvent = MapObjects.Find(x => x.JsonTime >= AudioTimeSyncController.CurrentJsonTime - 0.01f);
        
        var previousNJS = (previousNJSEvent?.RelativeNJS ?? 0) + baseNJS;
        var nextNJS = (nextNJSEvent?.RelativeNJS ?? previousNJSEvent?.RelativeNJS ?? 0) + baseNJS;
        
        var previousJsonTime = previousNJSEvent?.JsonTime ?? 0;
        var nextJsonTime = nextNJSEvent?.JsonTime ?? previousJsonTime;
        
        var easingPoint = Mathf.Approximately(previousJsonTime, nextJsonTime)
            ? 0 // Calculation below gets wacky if they're very close together - just use the previous event
            : (AudioTimeSyncController.CurrentJsonTime - previousJsonTime) / (nextJsonTime - previousJsonTime); 
        var lerpPoint = Easing.BeatSaber.EaseVNJS(nextNJSEvent?.Easing, easingPoint);

        
        var currentNJS = Mathf.Lerp(previousNJS, nextNJS, lerpPoint);
        if (currentNJS > baseNJS)
        {
            Shader.SetGlobalFloat(currentHJDShaderID, baseHJD);
        }
        else
        {
            var increasedHJDFactor = baseNJS / currentNJS;
            Shader.SetGlobalFloat(currentHJDShaderID, baseHJD * increasedHJDFactor);
        }
        
        CurrentNJS = currentNJS;
        
        countersPlus.UpdateStatistic(CountersPlusStatistic.NJSEvents);
    }
    
    private void UpdateDisplayHJDLine(object value) => Shader.SetGlobalInt(DisplayHJDLine, (bool)value ? 1 : 0);
}
