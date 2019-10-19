using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementMessageSender : MonoBehaviour
{
    private void OnMouseOver()
    {
        //SendMessageUpwards("ColliderHit");
    }

    private void OnMouseExit()
    {
        SendMessageUpwards("ColliderExit");
    }
}
