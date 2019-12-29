using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualsController : MonoBehaviour {

    [SerializeField] GameObject[] gameObjectsWithRenderersToToggle;

    private List<Renderer> renderers = new List<Renderer>();

    private void Start()
    {
        foreach (GameObject go in gameObjectsWithRenderersToToggle)
            renderers.AddRange(go.GetComponentsInChildren<Renderer>());
    }

    void Update() {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
            foreach (Renderer renderer in renderers) renderer.enabled = !renderer.enabled;
    }

}
