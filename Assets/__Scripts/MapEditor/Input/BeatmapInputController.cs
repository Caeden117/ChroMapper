using System.Collections;
using System.Linq;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GlobalIntersectionCache
{
    internal static GameObject firstHit;
}

public class BeatmapInputController<T> : MonoBehaviour, CMInput.IBeatmapObjectsActions where T : ObjectContainer
{
    [FormerlySerializedAs("customStandaloneInputModule")][SerializeField] protected CustomStandaloneInputModule CustomStandaloneInputModule;
    protected bool IsSelecting;

    [SerializeField] private CameraManager cameraManager;
    private bool massSelect;
    protected Vector2 MousePosition;
    private float timeWhenFirstSelecting;

    // Update is called once per frame
    private void Update()
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        GlobalIntersectionCache.firstHit = null;
        if (ObstaclePlacement.IsPlacing)
        {
            timeWhenFirstSelecting = Time.time;
            return;
        }

        if (!IsSelecting || Time.time - timeWhenFirstSelecting < 0.5f) return;
        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(MousePosition);
        foreach (var hit in Intersections.RaycastAll(ray, 9))
        {
            if (GetComponentFromTransform(hit.GameObject, out var obj))
            {
                if (!SelectionController.IsObjectSelected(obj.ObjectData))
                {
                    SelectionController.Select(obj.ObjectData, true);
                    obj.selectionStateChanged = true;
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
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return; //Returns if the mouse is on top of UI

        if (!Application.isFocused) return;
        
        RaycastFirstObject(out var obj);
        if (obj != null && !obj.Dragging && context.performed) StartCoroutine(CompleteDelete(obj));
    }

    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true) ||
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
                firstObject.selectionStateChanged = true;
            }
            else if (!SelectionController.IsObjectSelected(obj))
            {
                SelectionController.Select(obj, true);
                firstObject.selectionStateChanged = true;
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
                BeatmapObjectContainerCollection.GetCollectionForType(con.ObjectData.ObjectType)
                    .AudioTimeSyncController.MoveToSongBpmTime(con.ObjectData.SongBpmTime);
            }
        }
    }

    public void OnMassSelectModifier(InputAction.CallbackContext context) => massSelect = context.performed;

    protected virtual bool GetComponentFromTransform(GameObject t, out T obj) => t.TryGetComponent(out obj);

    protected void RaycastFirstObject(out T firstObject)
    {
        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(MousePosition);
        if (GlobalIntersectionCache.firstHit == null)
        {
            if (Intersections.Raycast(ray, 9, out var hit))
                GlobalIntersectionCache.firstHit = hit.GameObject;
        }

        if (GlobalIntersectionCache.firstHit != null)
        {
            var obj = GlobalIntersectionCache.firstHit.GetComponentInParent<T>();
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
        BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectData.ObjectType)
            .DeleteObject(obj.ObjectData, true, true, "Deleted by the user.");
    }
}
