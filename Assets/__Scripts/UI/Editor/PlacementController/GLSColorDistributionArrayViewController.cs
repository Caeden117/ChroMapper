using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GLSColorDistributionArrayViewController : MonoBehaviour
{
    [SerializeField] private GLSColorDistributionRowView rowTemplate;
    [SerializeField] private Transform listRoot;
    [SerializeField] private ButtonComponent addButton;

    private Action<string[]> onColorDistributionsChanged;
    private bool suppressNotify;
    private readonly List<GLSColorDistributionRowView> rows = new();

    private void Awake()
    {
        if (addButton != null)
        {
            addButton.OnClick(HandleAddClicked);
        }
        if (rowTemplate != null)
        {
            rowTemplate.gameObject.SetActive(false);
        }
    }

    public void Initialize(Action<string[]> onChanged)
    {
        onColorDistributionsChanged = onChanged;
    }

    public void SetColorDistributions(string[] colorDistributions)
    {
        suppressNotify = true;
        try
        {
            var count = colorDistributions?.Length ?? 0;
            for (var i = 0; i < count; i++)
            {
                if (i < rows.Count)
                {
                    var row = rows[i];
                    if (row != null && row.GetDistribution() != colorDistributions[i])
                    {
                        row.SetDistribution(colorDistributions[i]);
                    }
                }
                else
                {
                    CreateRow(colorDistributions[i]);
                }
            }

            for (var i = rows.Count - 1; i >= count; i--)
            {
                var row = rows[i];
                rows.RemoveAt(i);
                if (row != null) Destroy(row.gameObject);
            }
        }
        finally
        {
            suppressNotify = false;
        }
    }

    public string[] GetColorDistributions() => rows.Select(r => r.GetDistribution()).ToArray();

    private void HandleAddClicked()
    {
        CreateRow("h,0,l");
        NotifyChanged();
    }

    private void CreateRow(string distribution)
    {
        if (rowTemplate == null || listRoot == null)
        {
            Debug.LogError("[GLSColorDistributionArrayView] rowTemplate/listRoot not wired. No auto-creation.");
            return;
        }

        var row = Instantiate(rowTemplate, listRoot);
        row.gameObject.SetActive(true);
        row.SetDistribution(distribution);
        row.OnDistributionChanged += _ => { if (!suppressNotify) NotifyChanged(); };
        row.OnRemove += r =>
        {
            rows.Remove(r);
            Destroy(r.gameObject);
            if (!suppressNotify) NotifyChanged();
        };
        rows.Add(row);
    }

    private void NotifyChanged()
    {
        if (suppressNotify) return;
        var colorDistributions = GetColorDistributions();
        onColorDistributionsChanged?.Invoke(colorDistributions);
    }
}
