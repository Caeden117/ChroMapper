using TMPro;
using UnityEngine;

public class RotationDisplayController : MonoBehaviour
{
    [SerializeField] private RotationCallbackController rotationCallback;
    [SerializeField] private TextMeshProUGUI display;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(rotationCallback.IsActive);
        rotationCallback.RotationChangedEvent += RotationChanged;
    }

    private void RotationChanged(bool natural, int rotation)
    {
        if (Settings.Instance.Reset360DisplayOnCompleteTurn)
        {
            display.text = $"{betterModulo(rotation, 360)}°";
        }
        else
        {
            display.text = $"{rotation}°";
        }
    }

    private int betterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow

    private void OnDestroy()
    {
        rotationCallback.RotationChangedEvent -= RotationChanged;
    }
}
