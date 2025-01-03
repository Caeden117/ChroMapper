﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrecisionPlacementGridController : MonoBehaviour
{
    [SerializeField] private GameObject expandedGridParent;

    [SerializeField] private IntersectionCollider expandedGridBoxCollider;

    //I would like the grid itself to remain visible, but its box collider disabled. So we going BoxCollider boyes
    [SerializeField] private IntersectionCollider regularGridBoxCollider;
    private List<Material> allMaterialsInExpandedGrid = new List<Material>();

    private bool isEnabled = true;
    private Vector3 mousePosition;
    private static readonly int position = Shader.PropertyToID("_MousePosition");

    private void Start()
    {
        allMaterialsInExpandedGrid =
            expandedGridParent.GetComponentsInChildren<Renderer>().Select(x => x.material).ToList();
        TogglePrecisionPlacement(false);
    }

    private void LateUpdate()
    {
        if (!isEnabled) return;
        foreach (var material in allMaterialsInExpandedGrid) material.SetVector(position, mousePosition);
    }

    public void TogglePrecisionPlacement(bool isVisible)
    {
        if (isEnabled == isVisible) return;
        isEnabled = isVisible;
        if (isVisible && Settings.Instance.PrecisionPlacementMode != PrecisionPlacementMode.Off)
        {
            expandedGridParent.SetActive(true);
            expandedGridBoxCollider.enabled = true;
            regularGridBoxCollider.enabled = false;
        }
        else
        {
            expandedGridParent.SetActive(false);
            expandedGridBoxCollider.enabled = false;
            regularGridBoxCollider.enabled = true;
        }
    }

    public void UpdateMousePosition(Vector3 mousePosition) => this.mousePosition = mousePosition;
}
