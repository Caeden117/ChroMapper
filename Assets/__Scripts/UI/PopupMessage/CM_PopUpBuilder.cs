using System;
using System.Collections.Generic;
using UnityEngine;

public class CM_PopUpBuilder
{
    protected List<CM_Buildable> Buildables;

    private bool hasBeenBuilt;

    public void AddBuildable(CM_Buildable buildable)
    {
        if (!hasBeenBuilt) Buildables.Add(buildable);
        else throw new ArgumentException("Attempting to add a buildable to an already built pop up message.");
    }

    public void Build(GameObject parent)
    {
        //foreach (CM_Buildable buildable in Buildables) buildable.Build(); commented out because it wouldn't load in to unity with it here
    }
}
