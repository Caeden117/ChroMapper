﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridChild : MonoBehaviour
{
    #region GridChild Properties
    /// <summary>
    /// Order that determines its original position. Each child with the same Order will be at the same position
    /// </summary>
    public int Order { get => order; set
        {
            order = value;
            GridOrderController.MarkDirty();
        }
    }
    [SerializeField] private int order = 0;
    /// <summary>
    /// Local offset that each individual child will be offset by.
    /// </summary>
    public Vector3 LocalOffset
    {
        get => localOffset; set
        {
            localOffset = value;
            GridOrderController.MarkDirty();
        }
    }
    [SerializeField] private Vector3 localOffset = Vector3.zero;
    /// <summary>
    /// How large this object is, to the largest integer.
    /// </summary>
    public int Size
    {
        get => size; set
        {
            size = value;
            GridOrderController.MarkDirty();
        }
    }
    [SerializeField] private int size = 0;
    #endregion

    public bool RegisterChildOnStart = true;

    public IEnumerable<Renderer> GridRenderers;

    private GridOrderController gridOrderController;

    // Start is called before the first frame update
    void Start()
    {
        if (!RegisterChildOnStart) return;
        GridRenderers = GetComponentsInChildren<Renderer>().Where(x => x.material.shader.name.Contains("Grid"));
        GridOrderController.RegisterChild(this);
    }
}
