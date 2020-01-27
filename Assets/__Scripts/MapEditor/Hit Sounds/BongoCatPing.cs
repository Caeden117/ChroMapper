using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BongoCatPing : MonoBehaviour
{
    private CanvasGroup _canvasGroup;

    void Start()
    {
        _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        transform.localPosition = new Vector3(Random.Range(-1f, 1f) * (1f / transform.lossyScale.x), 0, Random.Range(-.25f, .25f) + 1);
        transform.localEulerAngles = Vector3.right * 90;
        transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 v = transform.localPosition;
        v.z += Time.deltaTime; //todo might use lerp
        transform.localPosition = v;
        _canvasGroup.alpha -= Time.deltaTime;
        if (_canvasGroup.alpha <= 0f) Destroy(gameObject);
    }
}
