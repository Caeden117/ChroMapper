using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// TODO remove
public class SelectObjectOnGrid : MonoBehaviour {

    void Start()
    {
        SelectionController.ObjectWasSelectedEvent += ObjectSelected;
    }

    void ObjectSelected(BeatmapObject container)
    {
        return;
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= ObjectSelected;
    }
}
