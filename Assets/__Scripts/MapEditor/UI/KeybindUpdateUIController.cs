using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class KeybindUpdateUIController : MonoBehaviour, CMInput.IWorkflowsActions
{
    [SerializeField] private UIWorkflowToggle workflowToggle;
    [SerializeField] private NotePlacementUI notePlacementUI;
    [SerializeField] private EventPlacementUI eventPlacementUI;
    [SerializeField] private DeleteToolController deleteToolController;

    [SerializeField] private Toggle redNoteToggle;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private Toggle blueNoteToggle;
    [SerializeField] private Toggle blueEventToggle;
    [SerializeField] private Toggle bombToggle;
    [SerializeField] private Toggle wallToggle;

    public void OnChangeWorkflows(InputAction.CallbackContext context)
    {
        if (context.performed) workflowToggle.UpdateWorkflowGroup();
    }

    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        blueNoteToggle.isOn = true;
        blueEventToggle.isOn = true;
        deleteToolController.UpdateDeletion(false);
    }

    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        bombToggle.isOn = true;
        deleteToolController.UpdateDeletion(false);
    }

    public void OnPlaceObstacle(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        wallToggle.isOn = true;
        deleteToolController.UpdateDeletion(false);
    }

    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        redNoteToggle.isOn = true;
        redEventToggle.isOn = true;
        eventPlacementUI.Red(true);
        deleteToolController.UpdateDeletion(false);
    }

    public void OnToggleDeleteTool(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        deleteToolController.UpdateDeletion(true);
    }

    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context)
    {
        if (KeybindsController.AnyCriticalKeys && context.performed) return;
        (BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE) as NotesContainer)
                   .UpdateSwingArcVisualizer();
    }
}
