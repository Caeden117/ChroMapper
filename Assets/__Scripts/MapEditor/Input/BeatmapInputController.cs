using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapInputController<T> : MonoBehaviour, CMInput.IBeatmapObjectsActions where T : BeatmapObjectContainer
{
    protected bool isSelecting;
    protected HashSet<T> hoveredObjects = new HashSet<T>();
    protected Vector2 mousePosition;

    private Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 999, 1 << 9))
        {
            if (hit.transform.TryGetComponent(out T obj))
            {
                hoveredObjects.Add(obj);
                if (isSelecting && !obj.SelectionStateChanged && !SelectionController.IsObjectSelected(obj))
                {
                    SelectionController.Select(obj, true);
                    obj.SelectionStateChanged = true;
                }
            }
        }
    }

    public void OnDeleteTool(InputAction.CallbackContext context)
    {
        if (NotePlacementUI.delete) OnQuickDelete(context);
    }

    public void OnQuickDelete(InputAction.CallbackContext context)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform.TryGetComponent(out T obj))
            {
                BeatmapObjectContainer.FlaggedForDeletionEvent?.Invoke(obj, true, "Deleted by the user.");
            }
        }
    }

    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        isSelecting = context.performed;
        if (context.performed)
        {
            if (hoveredObjects.Any())
            {
                foreach (T obj in hoveredObjects)
                {
                    obj.SelectionStateChanged = false;
                }
                hoveredObjects.Clear();
            }
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 999, 1 << 9))
            {
                if (hit.transform.TryGetComponent(out T obj))
                {
                    if (!obj.SelectionStateChanged && SelectionController.IsObjectSelected(obj))
                    {
                        SelectionController.Deselect(obj);
                        obj.SelectionStateChanged = true;
                        break;
                    }
                }
            }
        }
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        mousePosition = context.ReadValue<Vector2>();
    }
}
