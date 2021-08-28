using UnityEngine;

public class PlacementMessageSender : MonoBehaviour
{
    private void OnMouseExit() => SendMessageUpwards("ColliderExit");
}
