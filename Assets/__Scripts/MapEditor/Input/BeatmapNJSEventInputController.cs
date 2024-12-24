﻿using Beatmap.Containers;
using Beatmap.Helper;
using UnityEngine.InputSystem;

public class BeatmapNJSEventInputController : BeatmapInputController<NJSEventContainer>
    , CMInput.INJSEventObjectsActions
{
    public void OnTweakNJSValue(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            RaycastFirstObject(out var containerToEdit);
            if (containerToEdit != null)
            {
                var original = BeatmapFactory.Clone(containerToEdit.ObjectData);

                // Think decimal NJS will be more common eventually. Can tweak this later.
                var modifier = context.ReadValue<float>() > 0 ? 0.5f : -0.5f;

                containerToEdit.NJSData.RelativeNJS += modifier;
                if (containerToEdit.NJSData.RelativeNJS <= -BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed)
                {
                    containerToEdit.NJSData.RelativeNJS = 0.5f - BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
                }
                
                containerToEdit.UpdateNJSText();

                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                    containerToEdit.ObjectData, original, "Tweaked NJS"));
            }
        }
    }
}
