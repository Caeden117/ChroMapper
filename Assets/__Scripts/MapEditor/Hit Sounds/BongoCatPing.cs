using UnityEngine;

public class BongoCatPing : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        transform.localPosition = new Vector3(Random.Range(-1f, 1f) * (1f / transform.lossyScale.x), 0,
            Random.Range(-.25f, .25f) + 1);
        transform.localEulerAngles = Vector3.right * 90;
        transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z);
    }

    // Update is called once per frame
    private void Update()
    {
        var v = transform.localPosition;
        v.z += Time.deltaTime; //todo might use lerp
        transform.localPosition = v;
        canvasGroup.alpha -= Time.deltaTime;
        if (canvasGroup.alpha <= 0f) Destroy(gameObject);
    }
}
