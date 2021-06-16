using UnityEngine;

public class ToggleObjectsOnMouseClick : MonoBehaviour
{
    [SerializeField] private GameObject[] shitToToggle;

    private bool objectsEnabled = false;

    private void Start()
    {
        foreach (var go in shitToToggle) go.SetActive(false);
    }

    private void OnMouseDown()
    {
        objectsEnabled = !objectsEnabled;

        foreach (var go in shitToToggle) go.SetActive(objectsEnabled);
    }
}
