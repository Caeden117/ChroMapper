using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightV3ToggleCheckmark : MonoBehaviour
{
    [SerializeField] private Image checkmark;

    public void SetSprite(Sprite s)
    {
        checkmark.sprite = s;
    }

    public Sprite GetSprite() => checkmark.sprite;
}
