using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RandomImage : MonoBehaviour {

    [SerializeField]
    private Image image;

    [SerializeField]
    private ImageList list;

    private void OnEnable() {
        image.sprite = list.GetRandomSprite();
    }

}
