using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class KeybindUpdateUIController : MonoBehaviour, CMInput.IWorkflowsActions
{
    [SerializeField] private UIWorkflowToggle workflowToggle;
    [SerializeField] private NotePlacementUI notePlacementUI;
    [SerializeField] private EventPlacementUI eventPlacementUI;

    [SerializeField] private Toggle redNoteToggle;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private Toggle blueNoteToggle;
    [SerializeField] private Toggle blueEventToggle;
    [SerializeField] private Toggle bombToggle;
    [SerializeField] private Toggle wallToggle;
    [SerializeField] private Toggle deleteToggle;

    public void OnChangeWorkflows(InputAction.CallbackContext context)
    {
        if (context.performed) workflowToggle.UpdateWorkflowGroup();
    }

    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context)
    {
        blueNoteToggle.isOn = true;
        blueEventToggle.isOn = true;
        deleteToggle.isOn = false;
    }

    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        bombToggle.isOn = true;
        deleteToggle.isOn = false;
    }

    public void OnPlaceObstacle(InputAction.CallbackContext context)
    {
        wallToggle.isOn = true;
        deleteToggle.isOn = false;
    }

    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context)
    {
        redNoteToggle.isOn = true;
        redEventToggle.isOn = true;
        deleteToggle.isOn = false;
        eventPlacementUI.Red(true);
    }

    public void OnToggleDeleteTool(InputAction.CallbackContext context)
    {
        notePlacementUI.Delete(true);
        eventPlacementUI.Delete(true);
        deleteToggle.isOn = true;
    }

    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context)
    {
        (BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE) as NotesContainer)
                   .UpdateSwingArcVisualizer();
    }
}
