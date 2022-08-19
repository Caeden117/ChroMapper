using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColorEventCallbackController : MetaBeatmapObjectCallbackController<BeatmapLightColorEvent, LightColorEventsContainer>
{
    protected override bool CheckLoadingPrerequisite()
    {
        return Settings.Instance.Load_MapV3 && ObjectContainers != null;
    }
}
