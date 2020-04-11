using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindUpdateUIController : MonoBehaviour, CMInput.IWorkflowsActions
{
    [SerializeField] private UIWorkflowToggle workflowToggle;
    [SerializeField] private NotePlacementUI notePlacementUI;
    [SerializeField] private EventPlacementUI eventPlacementUI;

    public void OnChangeWorkflows(InputAction.CallbackContext context)
    {
        if (context.performed) workflowToggle.UpdateWorkflowGroup();
    }

    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context)
    {
        notePlacementUI.BlueNote(true);
        eventPlacementUI.Blue(true);
    }

    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        notePlacementUI.Bomb(true);
    }

    public void OnPlaceObstacle(InputAction.CallbackContext context)
    {
        notePlacementUI.Wall(true);
    }

    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context)
    {
        notePlacementUI.RedNote(true);
        eventPlacementUI.Red(true);
    }

    public void OnToggleDeleteTool(InputAction.CallbackContext context)
    {
        notePlacementUI.Delete(true);
        eventPlacementUI.Delete(true);
    }

    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context)
    {
        (BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE) as NotesContainer)
                   .UpdateSwingArcVisualizer();
    }
}
