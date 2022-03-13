using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapChainContainer : BeatmapObjectContainer
{
    [FormerlySerializedAs("chainData")] public BeatmapChain ChainData;
    public override BeatmapObject ObjectData { get => ChainData; set => ChainData = (BeatmapChain)value; }

    [SerializeField] private GameObject headNode;
    [SerializeField] private GameObject tailNode;
    private List<GameObject> nodes = new List<GameObject>();

    public static BeatmapChainContainer SpawnChain(BeatmapChain data, ref GameObject prefab)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapChainContainer>();
        container.ChainData = data;
        return container;
    }

    public override void Setup()
    {
        base.Setup();
        MaterialPropertyBlock.SetFloat("_Lit", 1);
        UpdateMaterials();
    }
    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(-1.5f, 0.5f, ChainData.B * EditorScaleController.EditorScale);
        GenerateChain();
        UpdateCollisionGroups();
    }

    public void GenerateChain(BeatmapChain chainData = null)
    {
        if (chainData != null) ChainData = chainData;
        headNode.transform.localPosition = new Vector3(ChainData.X, ChainData.Y, 0);
        headNode.transform.localRotation = Quaternion.Euler(BeatmapNoteContainer.Directionalize(ChainData.D));
        tailNode.transform.localPosition = new Vector3(ChainData.Tx, ChainData.Ty, (ChainData.Tb - ChainData.B) * EditorScaleController.EditorScale);
        tailNode.transform.localRotation = Quaternion.Euler(BeatmapNoteContainer.Directionalize(ChainData.D));
        int i = 0;
        for (; i < ChainData.Sc - 1; ++i)
        {
            if (i >= nodes.Count) break;
            nodes[i].SetActive(true);
            Interpolate(ChainData.Sc - 1, i + 1, headNode, tailNode, nodes[i]);
        }
        for (; i < nodes.Count; ++i)
        {
            nodes[i].SetActive(false);
        }
        for (; i < ChainData.Sc - 1; ++i)
        {
            var newNode = Instantiate(tailNode, transform);
            newNode.SetActive(true);
            newNode.GetComponent<MeshRenderer>().material.CopyPropertiesFromMaterial(tailNode.GetComponent<MeshRenderer>().material);
            Interpolate(ChainData.Sc - 1, i + 1, headNode, tailNode, newNode);
            nodes.Add(newNode);
        }
    }

    private void Interpolate(int n, int i, in GameObject t0, in GameObject t1, in GameObject t)
    {
        float p0 = (float)i / n;
        t.transform.localPosition = Vector3.Slerp(t0.transform.localPosition, t1.transform.localPosition, p0);
        t.transform.localRotation = Quaternion.Slerp(t0.transform.localRotation, t1.transform.localRotation, p0);
    }

    public void SetColor(Color color)
    {
        MaterialPropertyBlock.SetColor(BeatmapObjectContainer.color, color);
        UpdateMaterials();
    }

    internal override void UpdateMaterials()
    {
        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.SetPropertyBlock(MaterialPropertyBlock);
        }
    }
}
