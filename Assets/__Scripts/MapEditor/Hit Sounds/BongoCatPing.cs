using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BongoCatPing : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Transform _transform;
    void Start()
    {
        _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        _transform = transform;
        _transform.position = new Vector3(Random.Range(-1f, 1f) - 1f,  Random.Range(-.25f, .25f) + 4f, 0);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 v = _transform.position;
        v.y += 0.025f; //todo might use lerp
        _transform.position = v;
        _canvasGroup.alpha -= 0.02f;
        if (_canvasGroup.alpha <= 0f) Destroy(gameObject);
        }
}
