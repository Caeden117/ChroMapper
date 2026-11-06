using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class BeatmapRaycastCache
{
    public static GameObject FirstHit;
    public static Vector3? FirstHitPoint;
    public static bool HasHit;
    public static bool HasRaycastThisFrame;

    // Clear the physical hit because pooled containers can change identity after group replacement.
    public static void Invalidate()
    {
        FirstHit = null;
        FirstHitPoint = null;
        HasHit = false;
        HasRaycastThisFrame = false;
    }
}

public class BeatmapInputController<TContainer> : MonoBehaviour, CMInput.IBeatmapObjectsActions
    where TContainer : ObjectContainer
{
    [Header("State")] public bool IsSelecting;
    public bool IsHovering;
    public TContainer HoveredObject;

    [Header("Dependencies")] [SerializeField]
    protected CustomStandaloneInputModule CustomStandaloneInputModule;

    [SerializeField] private CameraManager cameraManager;
    [SerializeField] protected EditModeContext EditContext;
    [SerializeField] protected EditingMode editMode;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private bool ignoreBaseInput;

    protected bool MassSelect;
    private Vector2 mousePosition;
    private float timeWhenFirstSelecting;
    private List<Intersections.IntersectionHit> preAllocIntersections = new();

    private void Start() => DeleteToolController.OnDeleteToolActivated += HandleDeleteToolActivated;

    private void OnDestroy() => DeleteToolController.OnDeleteToolActivated -= HandleDeleteToolActivated;

    private void HandleDeleteToolActivated()
    {
        if (IsHovering) HoveredObject.RefreshOutlineColor();
    }

    // Update is called once per frame
    private void Update()
    {
        if (PlaybackHoverSuppressed)
        {
            if (IsHovering)
            {
                HoveredObject.Highlighted = false;
                IsHovering = false;
                HandleHoverChanged(null);
            }
            return;
        }

        if ((EditContext.EditingMode & editMode) == 0)
        {
            if (IsHovering) HoveredObject.Highlighted = false;
            IsHovering = false;
            HandleHoverChanged(null);
            return;
        }

        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (obstaclePlacement.IsPlacing)
        {
            timeWhenFirstSelecting = Time.time;
            return;
        }

        if (Application.isFocused && RaycastFirstObject(out var first) && SpecialCaseContainer(first))
        {
            if (HoveredObject != first && IsHovering) HoveredObject.Highlighted = false;
            HoveredObject = first;
            HoveredObject.Highlighted = ShouldHighlightHoveredObject(HoveredObject);
            IsHovering = true;
            HandleHoverChanged(HoveredObject);
        }
        else if (IsHovering)
        {
            if (!HoveredObject.Dragged)
            {
                // Objects like ArcIndicator and ChainIndicators are offset from the cursor while dragging so only
                // stop highlighting and hovering when the dragging has finished
                HoveredObject.Highlighted = false;
                IsHovering = false;
                HandleHoverChanged(null);
            }
        }
        else
            IsHovering = false;

        if (!IsSelecting || Time.time - timeWhenFirstSelecting < 0.5f) return;
        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
        Intersections.RaycastAllNoAlloc(ray, 9, ref preAllocIntersections);
        // Treat the nearest ribbon as a blocker during held selection so nodes behind it are not selected.
        var nearestHit = default(GameObject);
        var nearestDistance = float.PositiveInfinity;
        foreach (var hit in preAllocIntersections)
        {
            if (hit.Distance < nearestDistance)
            {
                nearestHit = hit.GameObject;
                nearestDistance = hit.Distance;
            }
        }

        if (IsGradientHit(nearestHit))
            return;

        foreach (var hit in preAllocIntersections)
        {
            if (!GetComponentFromTransform(hit.GameObject, out var obj) || obj.IsPlacementVisual)
                continue;
            if (!SelectionController.IsObjectSelected(obj.ObjectData))
                SelectionController.Select(obj.ObjectData, true);
        }
    }

    protected virtual void LateUpdate()
    {
        // End the shared frame cache as one operation so its collider and resolved owner cannot get out of sync.
        BeatmapRaycastCache.Invalidate();
    }

    // because abstract object container can be used to handle multitype,
    // we do want to only handle specific type and ignore already existing input
    protected virtual bool SpecialCaseContainer(ObjectContainer con) => true;

    internal static bool PlaybackHoverSuppressed =>
        UIMode.PreviewMode
        && AudioTimeSyncController.Instance != null
        && AudioTimeSyncController.Instance.IsPlaying;

    // Notify specialized controllers when their hover target changes without adding per-frame polling.
    protected virtual void HandleHoverChanged(TContainer container) { }

    protected virtual bool ShouldHighlightHoveredObject(TContainer container) =>
        !IsGradientHit(BeatmapRaycastCache.FirstHit);

    protected static bool IsGradientHit(GameObject hit) =>
        hit != null && hit.GetComponentInParent<LightGradientController>() != null;

    public void OnDeleteTool(InputAction.CallbackContext context)
    {
        if (ignoreBaseInput || (DeleteToolController.IsActive && context.performed)) OnQuickDelete(context);
    }

    public void OnQuickDelete(InputAction.CallbackContext context)
    {
        if (ignoreBaseInput || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return; //Returns if the mouse is on top of UI

        if (!Application.isFocused) return;
        // Shift+middle on a Basic Event ribbon raycasts its preceding owner, so reject the physical ribbon hit
        // before the shared quick-delete path can delete that source node; this also protects any future interactive GLS ribbon.
        if (RaycastFirstObject(out var obj) && SpecialCaseContainer(obj) && !obj.Dragged && context.performed)
        {
            if (IsGradientHit(BeatmapRaycastCache.FirstHit))
            {
                return;
            }

            CompleteDelete(obj);
        }
    }

    public void OnSelectObjects(InputAction.CallbackContext context)
    {
        if (ignoreBaseInput
            || CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || obstaclePlacement.IsPlacing)
            return;

        IsSelecting = context.performed;
        if (!context.performed) return;
        timeWhenFirstSelecting = Time.time;
        if (!RaycastFirstObject(out var firstObject) || !SpecialCaseContainer(firstObject)) return;
        if (IsGradientHit(BeatmapRaycastCache.FirstHit)) return;
        var obj = firstObject.ObjectData;
        if (MassSelect
            && SelectionController.SelectedObjects.Count == 1
            && SelectionController.SelectedObjects.First() != obj)
            SelectionController.SelectBetween(SelectionController.SelectedObjects.First(), obj, true);
        else if (SelectionController.IsObjectSelected(obj))
            SelectionController.Deselect(obj);
        else if (!SelectionController.IsObjectSelected(obj)) SelectionController.Select(obj, true);
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context) =>
        mousePosition = context.ReadValue<Vector2>();

    public void OnJumptoObjectTime(InputAction.CallbackContext context)
    {
        if (ignoreBaseInput || !context.performed) return; // TODO: Find a way to detect if other keybinds are held
        if (RaycastFirstObject(out var con) && SpecialCaseContainer(con))
        {
            // TODO make this use an AudioTimeSyncController reference when Zenject is added.
            BeatmapObjectContainerCollection
                .GetCollectionForType(con.ObjectData.ObjectType)
                .BeatmapContext.Atsc.MoveToSongBpmTime(con.ObjectData.SongBpmTime);
        }
    }

    public void OnMassSelectModifier(InputAction.CallbackContext context) => MassSelect = context.performed;

    protected virtual bool GetComponentFromTransform(GameObject t, out TContainer obj) => t.TryGetComponent(out obj);

    protected virtual bool RaycastFirstObject(out TContainer firstObject)
    {
        if (!BeatmapRaycastCache.HasRaycastThisFrame)
        {
            var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (Intersections.Raycast(ray, 9, out var hit))
            {
                BeatmapRaycastCache.FirstHit = hit.GameObject;
                BeatmapRaycastCache.FirstHitPoint = hit.Point;
                BeatmapRaycastCache.HasHit = hit.GameObject != null;
            }

            BeatmapRaycastCache.HasRaycastThisFrame = true;
        }

        if (!BeatmapRaycastCache.HasHit)
        {
            firstObject = null;
            return false;
        }

        // Resolve the requested generic owner from the hit so child indicator containers reach their owning arc.
        // Without this you can't shift+click arcs. Should be performant?
        var container = BeatmapRaycastCache.FirstHit.GetComponentInParent<TContainer>();
        if (container != null && !container.IsPlacementVisual && ValidObject(container))
        {
            firstObject = container;
            return true;
        }

        firstObject = null;
        return false;
    }

    protected virtual bool ValidObject(TContainer container) => true;

    public void CompleteDelete(TContainer obj)
    {
        BeatmapObjectContainerCollection
            .GetCollectionForType(obj.ObjectData.ObjectType)
            .DeleteObject(obj.ObjectData, true, true, "Deleted by the user.");
    }
}
