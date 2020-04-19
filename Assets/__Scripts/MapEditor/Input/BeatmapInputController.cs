using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapInputController<T> : MonoBehaviour, CMInput.IBeatmapObjectsActions where T : BeatmapObjectContainer
{
    [SerializeField] protected CustomStandaloneInputModule customStandaloneInputModule;
    protected bool isSelecting;
    protected Vector2 mousePosition;

    private Camera mainCamera;
    private float timeWhenFirstSelecting = 0;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        if (!isSelecting || Time.time - timeWhenFirstSelecting < 0.5f) return;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        foreach (RaycastHit hit in Physics.RaycastAll(ray, 999, 1 << 9))
        {
            if (hit.transform.TryGetComponent(out T obj))
            {
                if (!SelectionController.IsObjectSelected(obj))
                {
                    SelectionController.Select(obj, true);
                    obj.SelectionStateChanged = true;
                }
            }
        }
    }

    protected void RaycastFirstObject(out T firstObject)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 99, 1 << 9))
        {
            if (hit.transform.TryGetComponent(out T obj))
            {
                firstObject = obj;
                return;
            }
        }
        firstObject = null;
    }

    public void OnDeleteTool(InputAction.CallbackContext context)
    {
        if (NotePlacementUI.delete && context.performed) OnQuickDelete(context);
    }

    public void OnQuickDelete(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        RaycastFirstObject(out T obj);
        if (obj != null && context.performed)
        {
            BeatmapObjectContainer.FlaggedForDeletionEvent?.Invoke(obj, true, "Deleted by the user.");
        }
    }

    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        isSelecting = context.performed;
        if (context.performed)
        {
            RaycastFirstObject(out T firstObject);
            if (firstObject != null && SelectionController.IsObjectSelected(firstObject))
            {
                SelectionController.Deselect(firstObject);
                firstObject.SelectionStateChanged = true;
            }
            else if (firstObject != null && !SelectionController.IsObjectSelected(firstObject))
            {
                SelectionController.Select(firstObject, true);
                firstObject.SelectionStateChanged = true;
            }
            timeWhenFirstSelecting = Time.time;
        }
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        mousePosition = context.ReadValue<Vector2>();
    }
}
