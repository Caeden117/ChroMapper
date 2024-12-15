using Beatmap.Containers;
using Beatmap.Helper;
using UnityEngine.InputSystem;

// TODO: This is a stub
public class BeatmapNJSEventInputController : BeatmapInputController<NJSEventContainer>
    // , CMInput.INJSEventObjectsActions
{
    public void OnTweakNJSEventValue(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            RaycastFirstObject(out var containerToEdit);
            if (containerToEdit != null)
            {
                var original = BeatmapFactory.Clone(containerToEdit.ObjectData);

                var modifier = context.ReadValue<float>() > 0 ? 1 : -1;

                containerToEdit.NJSData.RelativeNJS += modifier;
                containerToEdit.UpdateNJSText();

                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                    containerToEdit.ObjectData, original, "Tweaked NJS"));
            }
        }
    }
}
