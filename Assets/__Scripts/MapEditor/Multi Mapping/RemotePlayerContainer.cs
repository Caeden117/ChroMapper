using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RemotePlayerContainer : MonoBehaviour
{
    public Transform CameraTransform;

    [SerializeField] private TextMeshPro nameMesh;
    [SerializeField] private TextMeshPro gridNameMesh;
    [SerializeField] private SpriteRenderer gridSprite;

    private Transform lookAt;

    public void AssignIdentity(MapperIdentityPacket identity)
    {
        nameMesh.text = gridNameMesh.text = identity.Name;

        gridSprite.color = gridNameMesh.color = Random.ColorHSV(0, 1, 1, 1, 1, 1);
    }

    private void Start() => lookAt = Camera.main.transform;

    private void Update() => nameMesh.transform.LookAt(lookAt);
}
