using System.Collections;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class GlobalIntersectionCache
{
    internal static GameObject firstHit = null;
}

public class BeatmapInputController<T> : MonoBehaviour, CMInput.IBeatmapObjectsActions where T : BeatmapObjectContainer
{
    [SerializeField] protected CustomStandaloneInputModule customStandaloneInputModule;
    protected bool isSelecting;
    protected Vector2 mousePosition;

    private Camera mainCamera;
    private float timeWhenFirstSelecting = 0;
    private bool massSelect = false;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    protected virtual bool GetComponentFromTransform(GameObject t, out T obj)
    {
        return t.TryGetComponent(out obj);
    }

    // Update is called once per frame
    void Update()
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        GlobalIntersectionCache.firstHit = null;
        if (ObstaclePlacement.IsPlacing)
        {
            timeWhenFirstSelecting = Time.time;
            return;
        }
        if (!isSelecting || Time.time - timeWhenFirstSelecting < 0.5f) return;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        foreach (var hit in Intersections.RaycastAll(ray, 9))
        {
            if (GetComponentFromTransform(hit.GameObject, out T obj))
            {
                if (!SelectionController.IsObjectSelected(obj.objectData))
                {
                    SelectionController.Select(obj.objectData, true);
                    obj.SelectionStateChanged = true;
                }
            }
        }
    }

    protected void RaycastFirstObject(out T firstObject)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        if (GlobalIntersectionCache.firstHit == null)
        {
            if (Intersections.Raycast(ray, 9, out var hit))
            {
                GlobalIntersectionCache.firstHit = hit.GameObject;
            }
        }

        if (GlobalIntersectionCache.firstHit != null)
        {
            T obj = GlobalIntersectionCache.firstHit.GetComponentInParent<T>();
            if (obj != null)
            {
                firstObject = obj;
                return;
            }
        }
        firstObject = null;
    }

    public void OnDeleteTool(InputAction.CallbackContext context)
    {
        if (DeleteToolController.IsActive && context.performed) OnQuickDelete(context);
    }

    public void OnQuickDelete(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return; //Returns if the mouse is on top of UI
        RaycastFirstObject(out T obj);
        if (obj != null && !obj.dragging && context.performed)
        {
            StartCoroutine(CompleteDelete(obj));
        }
    }

    public IEnumerator CompleteDelete(T obj)
    {
        yield return null;
        BeatmapObjectContainerCollection.GetCollectionForType(obj.objectData.beatmapType)
            .DeleteObject(obj.objectData, true, true, "Deleted by the user.");
    }
    
    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) || ObstaclePlacement.IsPlacing) return;
        isSelecting = context.performed;
        if (context.performed)
        {
            timeWhenFirstSelecting = Time.time;
            RaycastFirstObject(out T firstObject);
            if (firstObject == null) return;
            BeatmapObject obj = firstObject.objectData;
            if (massSelect && SelectionController.SelectedObjects.Count() == 1 && SelectionController.SelectedObjects.First() != obj)
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

    public void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        mousePosition = context.ReadValue<Vector2>();
    }

    public void OnJumptoObjectTime(InputAction.CallbackContext context)
    {
        if (context.performed) // TODO: Find a way to detect if other keybinds are held
        {
            RaycastFirstObject(out T con);
            if (con != null)
            {
                // TODO make this use an AudioTimeSyncController reference when Zenject is added.
                BeatmapObjectContainerCollection.GetCollectionForType(con.objectData.beatmapType).AudioTimeSyncController.MoveToTimeInBeats(con.objectData._time);
            }
        }
    }

    public void OnMassSelectModifier(InputAction.CallbackContext context)
    {
        massSelect = context.performed;
    }
}
