using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightV3Generator : MonoBehaviour
{
    [SerializeField] private LightV3GeneratorAppearance ui;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnToggle() => ui.ToggleDropdown(!ui.IsActive);
}
