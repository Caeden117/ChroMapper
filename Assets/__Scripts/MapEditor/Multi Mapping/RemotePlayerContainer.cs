using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RemotePlayerContainer : MonoBehaviour
{
    public Transform CameraTransform;
    public Transform GridTransform;

    [SerializeField] private TextMeshPro nameMesh;
    [SerializeField] private TextMeshPro gridNameMesh;
    [SerializeField] private SpriteRenderer gridSprite;

    private Transform lookAt;
    private MultiNetListener source;
    private MapperIdentityPacket identity;
    private int latency;

    public void AssignIdentity(MultiNetListener source, MapperIdentityPacket identity)
    {
        this.identity = identity;
        this.source = source;

        nameMesh.text = identity.Name;

        gridSprite.color = gridNameMesh.color = identity.Color;

        UpdateGridText();
    }

    public void UpdateLatency(int latency)
    {
        this.latency = latency;

        UpdateGridText();
    }

    public void Kick()
    {
        if (source is INetAdmin admin) admin.Kick(identity);
    }

    public void Ban()
    {
        if (source is INetAdmin admin) admin.Ban(identity);
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
