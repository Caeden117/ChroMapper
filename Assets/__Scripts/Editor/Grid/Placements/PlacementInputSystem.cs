using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class PlacementInputSystem : MonoBehaviour,
                                    CMInput.IPlacementControllersActions,
                                    CMInput.ICancelPlacementActions
{
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private PrecisionPlacementController precisionPlacementController;
    [SerializeField] private BoxSelectionPlacement boxSelectionPlacement;
    private bool applicationFocus;
    private bool applicationFocusChanged;

    private PlacementProvider currentProvider;
    private PlacementInputState inputState;
    private bool isOnGrid;
    private Vector2 mousePosition;

    // Retain the last grid surface so an active box selection can follow the cursor through gaps between tracks.
    private GameObject boxSelectionProjectionTarget;
    private Bounds boxSelectionProjectionBounds;
    private Plane boxSelectionProjectionPlane;
    private bool boxSelectionProjectionIsGround;
    private bool hasBoxSelectionProjection;
    private bool usingBoxSelectionProjection;
    // Cache XZ identities on grid refresh so the frame loop classifies hits in O(1).
    private readonly HashSet<GameObject> groundGridTargets = new();

    private bool CanInteract =>
        !Input.GetMouseButton((int)MouseButton.Right)
        && KeybindsController.IsMouseInWindow
        && !SongTimelineController.IsHovering
        && !SceneTransitionManager.IsLoading
        && !DeleteToolController.IsActive
        && !NodeEditorController.IsActive
        && applicationFocus
        && !UIMode.PreviewMode;

    private void Start()
    {
        gridViewController.OnGridViewUpdated += RefreshBound;
        // Grid initialization may precede this component's Start, so seed the identity index immediately.
        RefreshGroundGridTargets();
    }

    private void Update()
    {
        if (((inputState == PlacementInputState.Drag && !Input.GetMouseButton((int)MouseButton.Left))
                || (inputState == PlacementInputState.DragAtTime && !Input.GetMouseButton((int)MouseButton.Right)))
            && currentProvider != null)
        {
            currentProvider.Lane.MoveXYGridByZ(0f);
            HandleDragFinished();
        }

        if (Application.isFocused != applicationFocus)
        {
            applicationFocus = Application.isFocused;
            applicationFocusChanged = true;
            return;
        }

        if (applicationFocusChanged) applicationFocusChanged = false;
        if (PauseManager.IsPaused) return;

        if (!BoxSelectionOwnsProjection(boxSelectionPlacement.State))
        {
            usingBoxSelectionProjection = false;
        }

        var ray = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
        var hasHit = Intersections.Raycast(ray, 11, out var hit);
        // Treat the spectrogram as a non-editable visualization so it cannot establish or extend a selection endpoint.
        if (hasHit && IsSpectrogramGridHit(hit.GameObject))
        {
            hasHit = false;
        }

        // Keep placement-specific ground classification outside the shared geometric hit structure.
        var isGroundHit = hasHit && IsGroundGridHit(hit.GameObject);

        var provider = hasHit ? hit.GameObject.transform.parent.GetComponent<PlacementProvider>() : null;

        // Project gap frames onto the last real grid surface while the selection box owns the interaction.
        if ((!hasHit || provider == null)
            && BoxSelectionOwnsProjection(boxSelectionPlacement.State)
            && currentProvider != null
            && TryProjectBoxSelectionHit(ray, out hit))
        {
            hasHit = true;
            isGroundHit = true;
            provider = currentProvider;
            if (!usingBoxSelectionProjection)
            {
                usingBoxSelectionProjection = true;
            }
        }
        else if (hasHit && provider != null)
        {
            // Only real XZ hits replace the cached projection; XY hits never provide box-selection time projection.
            if (isGroundHit)
            {
                CacheBoxSelectionProjection(hit, isGroundHit: true);
            }

            if (usingBoxSelectionProjection)
            {
                usingBoxSelectionProjection = false;
            }
        }

        // Keep the originating provider active until its drag is finished; switching to a BPM/event lane otherwise leaves the source note removed but its visual alive.
        // This runs every frame, so scan the serialized array directly instead of allocating LINQ iterator/delegate state.
        if (currentProvider != null
            && HasDraggingPlacement(currentProvider.Placements)
            && (!hasHit || provider != currentProvider))
        {
            return;
        }

        var invalidPlacementHit = !hasHit || provider == null;
        // Hide a pending two-click preview across a grid gap without discarding its first endpoint.
        if (invalidPlacementHit
            && currentProvider != null
            && CanInteract
            && HasPendingPlacementThatRetainsInvalidHits(currentProvider.Placements))
        {
            foreach (var placement in currentProvider.Placements)
            {
                placement.HideVisual();
            }

            // Early return so we don't fully exit the provider, otherwise trying to place walls cancels randomly when you jump from vertical grid to horizontal grid
            return;
        }

        if (HandleExitWhen((!CanInteract && inputState == PlacementInputState.Hover) || invalidPlacementHit))
            return;

        if (currentProvider != provider && !boxSelectionPlacement.IsPlacing)
        {
            if (currentProvider != null) Exit(currentProvider);
            currentProvider = provider;
            // Seed the invariant once per provider so frame updates never discover scene dependencies or perform defensive null checks.
            CacheBoxSelectionGroundProjection(currentProvider);

            RefreshBound();
            foreach (var placement in currentProvider.Placements) placement.Initialize(currentProvider);
        }

        if (HandleExitWhen(PersistentUI.Instance.DialogBoxIsEnabled)
            || currentProvider == null
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
            return;

        isOnGrid = true;
        precisionPlacementController.UpdateMousePosition(hit.Point);
        // Pass the resolved surface role only to the placement that consumes it.
        boxSelectionPlacement.IsGroundHit = isGroundHit;
        foreach (var placement in currentProvider.Placements)
        {
            if (ShouldUpdatePlacementForBoxProjection(placement, boxSelectionPlacement, usingBoxSelectionProjection))
            {
                placement.UpdateState(hit, inputState);
            }
            else
            {
                // Synthetic ground projection must not create a Basic Event hover anchor for off-grid Ctrl+V.
                placement.Exit();
            }
        }

        if (boxSelectionPlacement.State == PlacementState.Idle) return;
        {
            foreach (var placement in currentProvider.Placements)
            {
                if (!ReferenceEquals(placement, boxSelectionPlacement)) placement.HideVisual();
            }
        }
    }

    private static bool HasDraggingPlacement(BasePlacement[] placements)
    {
        for (var i = 0; i < placements.Length; i++)
        {
            if (placements[i].IsDragging)
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        gridViewController.OnGridViewUpdated -= RefreshBound;
        Intersections.Clear();
    }

    public void OnCancelPlacement(InputAction.CallbackContext context)
    {
        if (!context.performed || currentProvider == null) return;
        foreach (var placement in currentProvider.Placements) placement.Cancel();
    }

    public void OnPlaceObject(InputAction.CallbackContext context)
    {
        // Cancel retained wall-style endpoints off-grid, but let box selection commit its live projected preview and logical selection.
        if (currentProvider != null
            && context.performed
            && !isOnGrid
            && !boxSelectionPlacement.IsPlacing)
        {
            foreach (var placement in currentProvider.Placements)
            {
                if (placement.IsPlacing)
                {
                    placement.Cancel();
                }
            }

            return;
        }

        if (currentProvider == null
            || !context.performed
            || !KeybindsController.IsMouseInWindow
            || inputState != PlacementInputState.Hover
            || applicationFocusChanged
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || PersistentUI.Instance.DialogBoxIsEnabled
            || !CanInteract)
            return;
        foreach (var placement in currentProvider
            .Placements
            .Where(p => p.AllowPlacement && p.CanPlace))
            placement.Apply();
    }

    public void OnInitiateClickandDrag(InputAction.CallbackContext context)
    {
        if (currentProvider == null || inputState != PlacementInputState.Hover) return;
        if (context.performed)
        {
            foreach (var p in currentProvider.Placements) p.HideVisual();

            var dragRay = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (!Intersections.Raycast(dragRay, 9, out var dragHit)) return;

            var container = currentProvider
                .Placements
                .Where(p => p.CanClickAndDrag)
                .Select(p => p.StartDrag(dragHit.GameObject))
                .FirstOrDefault(con => con != null);
            if (container == null) return;

            inputState = PlacementInputState.Drag;
        }
        else if (context.canceled && inputState == PlacementInputState.Drag) HandleDragFinished();
    }

    public void OnInitiateClickandDragatTime(InputAction.CallbackContext context)
    {
        if (currentProvider == null || inputState != PlacementInputState.Hover) return;
        if (context.performed)
        {
            var dragRay = cameraManager.SelectedCameraController.Camera.ScreenPointToRay(mousePosition);
            if (!Intersections.Raycast(dragRay, 9, out var dragHit)) return;

            var (placement, container) = currentProvider
                .Placements
                .Where(p => p.CanClickAndDrag)
                .Select(p => (p, container: p.StartDrag(dragHit.GameObject)))
                .FirstOrDefault(pair => pair.container != null);
            if (container == null) return;

            inputState = PlacementInputState.DragAtTime;
            var newZ = placement.GetContainerPosZ(container);
            currentProvider.Lane.MoveXYGridByZ(newZ);
        }
        else if (context.canceled && inputState == PlacementInputState.DragAtTime)
        {
            currentProvider.Lane.MoveXYGridByZ(0);
            HandleDragFinished();
        }
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context) =>
        mousePosition = Mouse.current.position.ReadValue();

    public void OnPrecisionPlacementToggle(InputAction.CallbackContext context)
    {
        switch (Settings.Instance.PrecisionPlacementMode)
        {
            case PrecisionPlacementMode.Off:
                precisionPlacementController.TogglePrecisionPlacement(false);
                break;
            case PrecisionPlacementMode.Hold:
                precisionPlacementController.TogglePrecisionPlacement(context.performed);
                break;
            case PrecisionPlacementMode.Toggle:
                if (context is { started: true, performed: false })
                    precisionPlacementController.TogglePrecisionPlacement(!PrecisionPlacementController.IsEnabled);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void RefreshBound()
    {
        RefreshGroundGridTargets();
        if (currentProvider == null) return;

        var boundLocal = currentProvider.Lane.XY.Grid.bounds;
        // Transform the bounds into the pseudo-world space we use for selection
        var localTransform = currentProvider.transform;
        var localScale = localTransform.localScale;
        var boundsNew = localTransform.InverseTransformBounds(boundLocal);
        boundsNew.center += new Vector3(
            localTransform.localPosition.x / localScale.x,
            localTransform.localPosition.y / localScale.y,
            localTransform.localPosition.z / localScale.z);
        boundsNew.extents = new Vector3(
            boundsNew.extents.x / localScale.x,
            boundsNew.extents.y / localScale.y,
            boundsNew.extents.z / localScale.z);

        foreach (var placement in currentProvider.Placements)
        {
            placement.Bounds = boundsNew;
            placement.BoundsPosition = localTransform.localPosition;
        }
    }

    // The caller already classified this hit, so carry that result into the cached plane without another lane scan.
    private void CacheBoxSelectionProjection(Intersections.IntersectionHit hit, bool isGroundHit)
    {
        CacheBoxSelectionProjection(hit.GameObject, hit.Bounds, hit.Point, isGroundHit);
    }

    // Plane construction receives the authoritative surface role so caching stays data-only and allocation-free.
    private void CacheBoxSelectionProjection(GameObject target, Bounds bounds, Vector3 point, bool isGroundHit)
    {
        var extents = bounds.extents;
        var localNormal = extents.x <= extents.y && extents.x <= extents.z
            ? Vector3.right
            : extents.y <= extents.z
                ? Vector3.up
                : Vector3.forward;
        var normal = target.transform.TransformDirection(localNormal).normalized;
        boxSelectionProjectionTarget = target;
        boxSelectionProjectionBounds = bounds;
        boxSelectionProjectionIsGround = isGroundHit;
        boxSelectionProjectionPlane = new Plane(normal, point);
        hasBoxSelectionProjection = true;
    }

    // Placement providers authoritatively own an XZ collider, so resolve it once when the provider becomes current.
    private void CacheBoxSelectionGroundProjection(PlacementProvider provider)
    {
        var groundCollider = provider.Lane.XZ.IntersectionCollider;
        var bounds = groundCollider.CollisionBounds;
        CacheBoxSelectionProjection(
            groundCollider.gameObject,
            bounds,
            groundCollider.transform.TransformPoint(bounds.center),
            isGroundHit: true);
    }

    // Produce a normal intersection hit at the cursor's unbounded position on the cached grid surface.
    private bool TryProjectBoxSelectionHit(Ray ray, out Intersections.IntersectionHit hit)
    {
        hit = default;
        // Only extend a missing hit across a previously hit XZ ground plane; projecting the XY lane plane turns skyward cursor movement into false vertical box growth.
        if (!hasBoxSelectionProjection
            || !boxSelectionProjectionIsGround
            || boxSelectionProjectionTarget == null
            || !boxSelectionProjectionPlane.Raycast(ray, out var distance))
            return false;

        hit = new Intersections.IntersectionHit(
            boxSelectionProjectionTarget,
            boxSelectionProjectionBounds,
            ray,
            distance);
        return true;
    }

    // Ctrl-active selection needs the same ground projection as an in-progress drag so its first click can occur outside the loaded zone.
    internal static bool BoxSelectionOwnsProjection(PlacementState state) =>
        state == PlacementState.Active || state == PlacementState.Placing;

    // Synthetic projection belongs exclusively to box selection; real grid hits continue updating every placement.
    internal static bool ShouldUpdatePlacementForBoxProjection(
        BasePlacement placement,
        BoxSelectionPlacement boxSelection,
        bool usingProjection) =>
        !usingProjection || ReferenceEquals(placement, boxSelection);

    // Identify XZ planes through the grid-refresh index instead of scanning all lanes for every pointer frame.
    private bool IsGroundGridHit(GameObject hitObject) => groundGridTargets.Contains(hitObject);

    // Rebuild only when grid visibility or membership changes, keeping scene discovery out of the frame loop.
    private void RefreshGroundGridTargets()
    {
        groundGridTargets.Clear();
        foreach (var gridChild in gridViewController)
        {
            if (gridChild is GridLane gridLane && gridLane.XZ != null)
            {
                groundGridTargets.Add(gridLane.XZ.gameObject);
            }
        }
    }

    // Keep the spectrogram's XY and XZ surfaces out of placement hit resolution because they are visual-only lanes.
    private static bool IsSpectrogramGridHit(GameObject hitObject)
    {
        var spectrogramLane = SpectrogramSideSwapper.SpectrogramGridLane;
        return spectrogramLane != null
            && (spectrogramLane.XY.gameObject == hitObject || spectrogramLane.XZ.gameObject == hitObject);
    }

    private void HandleDragFinished()
    {
        if (inputState == PlacementInputState.Hover) return;
        foreach (var placement in currentProvider.Placements.Where(p => p.IsDragging)) placement.FinishDrag();
        inputState = PlacementInputState.Hover;
    }

    // Restrict invalid-hit retention to placement types that explicitly preserve a first click.
    private static bool HasPendingPlacementThatRetainsInvalidHits(BasePlacement[] placements)
    {
        for (var i = 0; i < placements.Length; i++)
        {
            if (placements[i].IsPlacing && placements[i].RetainsPendingPlacementOnInvalidHit)
            {
                return true;
            }
        }

        return false;
    }

    private bool HandleExitWhen(bool shouldExit)
    {
        if (!shouldExit) return false;
        if (currentProvider == null) return true;
        Exit(currentProvider);
        isOnGrid = false;
        return true;
    }

    private void Exit(PlacementProvider provider)
    {
        if (!isOnGrid) return;
        foreach (var placement in provider.Placements) placement.Exit();
    }
}
