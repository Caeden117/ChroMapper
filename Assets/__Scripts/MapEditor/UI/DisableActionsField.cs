using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class DisableActionsField : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        OnSelect();
    }

    public void OnSelect()
    {
        StartCoroutine(WaitToEnable());
    }

    public void OnDeselect(BaseEventData eventData)
    {
        OnDeselect();
    }

    public void OnDeselect()
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(GetType(), typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
    }

    private IEnumerator WaitToEnable()
    {
        yield return new WaitForEndOfFrame();
        CMInputCallbackInstaller.DisableActionMaps(GetType(), typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
    }
}
