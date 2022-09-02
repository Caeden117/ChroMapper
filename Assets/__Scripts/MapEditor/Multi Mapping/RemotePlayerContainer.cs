using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class RemotePlayerContainer : MonoBehaviour
{
    public Transform CameraTransform;
    public Transform GridTransform;

    [SerializeField] private TextMeshPro nameMesh;
    [SerializeField] private TextMeshPro gridNameMesh;
    [SerializeField] private SpriteRenderer gridSprite;

    private Transform lookAt;
    private MapperIdentityPacket identity;
    private int latency;

    public void AssignIdentity(MapperIdentityPacket identity)
    {
        this.identity = identity;

        nameMesh.text = identity.Name;

        gridSprite.color = gridNameMesh.color = identity.Color;

        UpdateGridText();
    }

    public void UpdateLatency(int latency)
    {
        this.latency = latency;

        UpdateGridText();
    }

    private void Start() => lookAt = Camera.main.transform;

    private void Update() => nameMesh.transform.LookAt(lookAt);

    private void UpdateGridText()
    {
        var stringBuilder = new StringBuilder();

        if (identity.ConnectionId == 0)
        {
            stringBuilder.Append("<b>[Host]</b> ");
        }

        gridNameMesh.text = stringBuilder
            .Append(latency)
            .Append("ms\n")
            .Append(identity.Name)
            .ToString();
    }
}
