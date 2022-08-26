using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class LightV3Buttons : MonoBehaviour
{
    [SerializeField] private LightV3ColorBinder colorBinder;
    [SerializeField] private LightV3RotationBinder rotationBinder;

    [SerializeField] private Button applyButton;
    private void Awake()
    {
        applyButton.onClick.AddListener(Apply);
    }

    private void OnDestroy()
    {
        applyButton.onClick.RemoveListener(Apply);
    }


    public void Apply()
    {
        if (SelectionController.SelectedObjects.Count == 1)
        {
            var obj = SelectionController.SelectedObjects.First();
            if (obj is BeatmapLightColorEvent color)
            {
                colorBinder.Load(color);
                if (color.HasAttachedContainer)
                {
                    var col = BeatmapObjectContainerCollection.GetCollectionForType<LightColorEventsContainer>(BeatmapObject.ObjectType.LightColorEvent);
                    col.RefreshPool(true);
                }
            }
            if (obj is BeatmapLightRotationEvent rot)
            {
                rotationBinder.Load(rot);
                if (rot.HasAttachedContainer)
                {
                    var col = BeatmapObjectContainerCollection.GetCollectionForType<LightRotationEventsContainer>(BeatmapObject.ObjectType.LightRotationEvent);
                    col.RefreshPool(true);
                }
            }
        }
    }
}
