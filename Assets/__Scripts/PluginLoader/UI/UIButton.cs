using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    public RectTransform Transform;
    public Button Button;
    public TextMeshProUGUI Text;
    public Image Image;

    private void SetType(bool text)
    {
        Text.gameObject.SetActive(text);
        Image.gameObject.SetActive(!text);
    }
    
    public void SetText(string txt)
    {
        SetType(true);
        Text.text = txt;
    }

    public void SetImage(Sprite img)
    {
        SetType(false);
        Image.sprite = img;
    }
}