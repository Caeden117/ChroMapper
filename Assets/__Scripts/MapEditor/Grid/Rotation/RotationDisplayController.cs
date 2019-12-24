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
        display.text = $"{betterModulo(rotation, 360)}°";
    }

    private int betterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow

    private void OnDestroy()
    {
        rotationCallback.RotationChangedEvent -= RotationChanged;
    }
}
