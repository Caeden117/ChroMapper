using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombPlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>
{
    public override BeatmapAction GenerateAction(BeatmapNoteContainer spawned, BeatmapObjectContainer container)
    {
        return new BeatmapObjectPlacementAction(spawned, objectContainerCollection, container);
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_BOMB, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.position.x + 1.5f);
        queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.position.y - 0.5f);
        foreach (MeshRenderer renderer in instantiatedContainer.GetComponentsInChildren<MeshRenderer>())
        {
            Color main = renderer.material.GetColor("_Color");
            if (renderer.transform.parent.name == "Direction") main = renderer.material.GetColor("_Color"); //but not for direction
            if (renderer.material.HasProperty("_Mode") && renderer.material.GetFloat("_Mode") == 2)
                continue; //Dont want to do this shit almost every frame.
            /*
             * Woah, this is some jank code. ChroMapper has jank code? Blasphemy!
             * 
             * This giant wall of crap is essentially how Unity changes materials from opaque to transparent.
             * If I want to have the hover note be transparent, this is what i gotta do.
             */
            renderer.material.SetFloat("_Mode", 2);
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.DisableKeyword("_ALPHATEST_ON");
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            renderer.material.renderQueue = 3001;
            renderer.material.SetColor("_Color", new Color(main.r, main.g, main.b, 0.75f));
        }
    }
}
