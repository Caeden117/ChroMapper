using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingEvent : MonoBehaviour {

    [HideInInspector] public Material LightMaterial;

	// Use this for initialization
	void Start () {
        LightMaterial = GetComponent<Renderer>().material;
	}
}
