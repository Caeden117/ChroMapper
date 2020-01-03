using UnityEngine;

public class RefreshRotationsButtonController : MonoBehaviour
{

    [SerializeField] private RotationCallbackController rotationCallbackController;
    [SerializeField] private TracksManager tracksManager;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(rotationCallbackController.IsActive);   
    }

    public void RefreshRotations()
    {
        tracksManager.RefreshTracks();
    }
}
