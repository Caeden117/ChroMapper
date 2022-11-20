using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRotationEventCallbackController : MetaBeatmapObjectCallbackController<BeatmapLightRotationEvent, LightRotationEventsContainer>
{
    protected override bool CheckLoadingPrerequisite()
    {
        return Settings.Instance.Load_MapV3 && ObjectContainers != null;
    }
}
