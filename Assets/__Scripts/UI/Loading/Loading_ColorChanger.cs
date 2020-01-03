using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Loading_ColorChanger : MonoBehaviour {

    public Color[] Colors;
    public float TimeBetweenColorChanges = 1;
    public float ColorFadeTime = 1;

    private Color oldColor;
    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
        oldColor = image.color;
        StartCoroutine(ChangeColors());
    }

    IEnumerator ChangeColors()
    {
        while (true)
        {
            yield return new WaitForSeconds(TimeBetweenColorChanges);
            yield return StartCoroutine(ChangeColor());
            oldColor = image.color;
        }
    }

    IEnumerator ChangeColor()
    {
        Color color = oldColor;
        while (color == oldColor)
            color = Colors[Random.Range(0, Colors.Length)]; //Prevent it from picking same color
        float t = 0;
        while (t < ColorFadeTime)
        {
            Color newColor = Color.Lerp(oldColor, color, t / ColorFadeTime);
            t += Time.deltaTime;
            image.color = newColor;
            yield return new WaitForEndOfFrame();
        }
        image.color = color;
    }
}
