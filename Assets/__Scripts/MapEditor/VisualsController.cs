using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualsController : MonoBehaviour {

    [SerializeField]
    Renderer[] gridRenderers;

    [SerializeField]
    Renderer[] impedingTerrainRenderers;
	
	void Update () {
		if (Input.GetKey(KeyCode.LeftControl)) {
            if (Input.GetKeyDown(KeyCode.G)) {
                foreach (Renderer rend in gridRenderers) rend.enabled = !rend.enabled;
            }
            if (Input.GetKeyDown(KeyCode.T)) {
                foreach (Renderer rend in impedingTerrainRenderers) rend.enabled = !rend.enabled;
            }
        }
	}

}
