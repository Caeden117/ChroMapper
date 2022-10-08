using Beatmap.Base;
using UnityEngine;

// TODO remove
public class SelectObjectOnGrid : MonoBehaviour
{
    private void Start() => SelectionController.ObjectWasSelectedEvent += ObjectSelected;

    private void OnDestroy() => SelectionController.ObjectWasSelectedEvent -= ObjectSelected;

    private void ObjectSelected(IObject container)
    {
    }
}
