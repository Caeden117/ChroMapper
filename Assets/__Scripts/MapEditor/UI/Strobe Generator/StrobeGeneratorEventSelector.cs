using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrobeGeneratorEventSelector : MonoBehaviour
{
    [SerializeField] private int startingNum = 1;

    public int SelectedNum { get; private set; } = 1;

    private void Start()
    {
        SelectedNum = startingNum;
    }

    public void UpdateValue(int v)
    {
        SelectedNum = v;
    }
}
