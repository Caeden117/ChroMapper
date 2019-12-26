using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualsController : MonoBehaviour {

    [SerializeField] GameObject[] togglableGameObjects;

    void Update() {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
            foreach (GameObject go in togglableGameObjects) go.SetActive(!go.activeSelf);
    }

}
