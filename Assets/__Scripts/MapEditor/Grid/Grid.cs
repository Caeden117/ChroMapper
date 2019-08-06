using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {
    [SerializeField]
    private float gridLength = 32;

	// Use this for initialization
	void Start () {
        transform.localScale += new Vector3(0, 0, gridLength);
	}
}
