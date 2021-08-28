using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GlobalIntersectionCache
{
    internal static GameObject FirstHit;
}

public class BeatmapInputController<T> : MonoBehaviour, CMInput.IBeatmapObjectsActions where T : BeatmapObjectContainer
{
    [FormerlySerializedAs("customStandaloneInputModule")] [SerializeField] protected CustomStandaloneInputModule CustomStandaloneInputModule;
    protected bool IsSelecting;

    private Camera mainCamera;
    private bool massSelect;
    protected Vector2 MousePosition;
    private float timeWhenFirstSelecting;

    // Start is called before the first frame update
    private void Start() => mainCamera = Camera.main;

    // Update is called once per frame
    private void Update()
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        GlobalIntersectionCache.FirstHit = null;
        if (ObstaclePlacement.IsPlacing)
        {
            timeWhenFirstSelecting = Time.time;
            return;
        }

        if (!IsSelecting || Time.time - timeWhenFirstSelecting < 0.5f) return;
        var ray = mainCamera.ScreenPointToRay(MousePosition);
        foreach (var hit in Intersections.RaycastAll(ray, 9))
        {
            if (GetComponentFromTransform(hit.GameObject, out var obj))
            {
                if (!SelectionController.IsObjectSelected(obj.ObjectData))
                {
                    SelectionController.Select(obj.ObjectData, true);
                    obj.SelectionStateChanged = true;
                }
            }
        }
    }

    public void OnDeleteTool(InputAction.CallbackContext context)
    {
        if (DeleteToolController.IsActive && context.performed) OnQuickDelete(context);
    }

    public void OnQuickDelete(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true))
            return; //Returns if the mouse is on top of UI
        RaycastFirstObject(out var obj);
        if (obj != null && !obj.Dragging && context.performed) StartCoroutine(CompleteDelete(obj));
    }

    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) ||
            ObstaclePlacement.IsPlacing)
        {
            return;
        }

        IsSelecting = context.performed;
        if (context.performed)
        {
            timeWhenFirstSelecting = Time.time;
            RaycastFirstObject(out var firstObject);
            if (firstObject == null) return;
            var obj = firstObject.ObjectData;
            if (massSelect && SelectionController.SelectedObjects.Count() == 1 &&
                SelectionController.SelectedObjects.First() != obj)
            {
                SelectionController.SelectBetween(SelectionController.SelectedObjects.First(), obj, true);
            }
            else if (SelectionController.IsObjectSelected(obj))
            {
                SelectionController.Deselect(obj);
                firstObject.SelectionStateChanged = true;
            }
            else if (!SelectionController.IsObjectSelected(obj))
            {
                SelectionController.Select(obj, true);
                firstObject.SelectionStateChanged = true;
            }
        }
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context) =>
        MousePosition = context.ReadValue<Vector2>();

    public void OnJumptoObjectTime(InputAction.CallbackContext context)
    {
        if (context.performed) // TODO: Find a way to detect if other keybinds are held
        {
            RaycastFirstObject(out var con);
            if (con != null)
            {
                // TODO make this use an AudioTimeSyncController reference when Zenject is added.
                BeatmapObjectContainerCollection.GetCollectionForType(con.ObjectData.BeatmapType)
                    .AudioTimeSyncController.MoveToTimeInBeats(con.ObjectData.Time);
            }
        }
    }

    public void OnMassSelectModifier(InputAction.CallbackContext context) => massSelect = context.performed;

    protected virtual bool GetComponentFromTransform(GameObject t, out T obj) => t.TryGetComponent(out obj);

    protected void RaycastFirstObject(out T firstObject)
    {
        var ray = mainCamera.ScreenPointToRay(MousePosition);
        if (GlobalIntersectionCache.FirstHit == null)
        {
            if (Intersections.Raycast(ray, 9, out var hit))
                GlobalIntersectionCache.FirstHit = hit.GameObject;
        }

        if (GlobalIntersectionCache.FirstHit != null)
        {
            var obj = GlobalIntersectionCache.FirstHit.GetComponentInParent<T>();
            if (obj != null)
            {
                firstObject = obj;
                return;
            }
        }

        firstObject = null;
    }

    public IEnumerator CompleteDelete(T obj)
    {
        yield return null;
        BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectData.BeatmapType)
            .DeleteObject(obj.ObjectData, true, true, "Deleted by the user.");
    }
}
