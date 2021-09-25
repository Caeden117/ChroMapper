using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingColorChanger : MonoBehaviour
{
    public Color[] Colors;
    public float TimeBetweenColorChanges = 1;
    public float ColorFadeTime = 1;
    private Image image;

    private Color oldColor;

    private void Start()
    {
        image = GetComponent<Image>();
        oldColor = image.color;
        StartCoroutine(ChangeColors());
    }

    private IEnumerator ChangeColors()
    {
        while (true)
        {
            yield return new WaitForSeconds(TimeBetweenColorChanges);
            yield return StartCoroutine(ChangeColor());
            oldColor = image.color;
        }
    }

    private IEnumerator ChangeColor()
    {
        var color = oldColor;
        while (color == oldColor)
            color = Colors[Random.Range(0, Colors.Length)]; //Prevent it from picking same color
        float t = 0;
        while (t < ColorFadeTime)
        {
            var newColor = Color.Lerp(oldColor, color, t / ColorFadeTime);
            t += Time.deltaTime;
            image.color = newColor;
            yield return new WaitForEndOfFrame();
        }

        image.color = color;
    }
}
