using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
public static class GLSEventInputHoverTracker
{
    private static int hoveredControllerCount;

    // Both inner and outer GLS controllers claim scroll precision only while their own hover target is active.
    public static bool IsHovering => hoveredControllerCount > 0;

    public static void SetHovering(bool isHovering) => hoveredControllerCount += isHovering ? 1 : -1;
}

public abstract class BeatmapGLSEventInputController<TData> : BeatmapInputController<GLSEventContainer>
    where TData : BaseGLSEvent
{
    [SerializeField] protected ScrollPrecisionController ScrollPrecisionController;
    [SerializeField] protected BeatmapEasingsSelectionInputController EasingInputController;

    private bool wasHovering;

    protected override void LateUpdate()
    {
        if (wasHovering != IsHovering)
        {
            wasHovering = IsHovering;
            GLSEventInputHoverTracker.SetHovering(wasHovering);
        }

        base.LateUpdate();
    }

    protected virtual void OnDisable()
    {
        if (!wasHovering) return;
        wasHovering = false;
        GLSEventInputHoverTracker.SetHovering(false);
    }

    // Group actions recycle and rebind inner containers synchronously, so wheel callbacks must resolve the physical target again.
    protected bool TryGetHoveredEvent(InputAction.CallbackContext context, out TData evt)
    {
        evt = null;
        if (!context.performed || !IsHovering)
        {
            return false;
        }

        var cachedContainer = HoveredObject;
        var cachedEvent = cachedContainer != null
            ? cachedContainer.EventData
            : null;
        BeatmapRaycastCache.Invalidate();
        if (!RaycastFirstObject(out var currentContainer))
        {
            return false;
        }

        evt = currentContainer.EventData as TData;
        if (evt == null
            || evt.EventBoxData == null
            || evt.EventBoxGroupData == null
            || evt.BoxIndex < 0
            || evt.BoxIndex >= evt.EventBoxGroupData.ReadOnlyBoxes.Count
            || !ReferenceEquals(evt.EventBoxGroupData.ReadOnlyBoxes[evt.BoxIndex], evt.EventBoxData))
        {
            evt = null;
            return false;
        }

        SetHoveredContainer(currentContainer);
        return true;
    }

    // Clone-producing commands synchronously rebuild the pool, so reacquire and highlight the physical target after that rebuild too.
    protected void RefreshHoveredVisualAfterMutation()
    {
        BeatmapRaycastCache.Invalidate();
        if (RaycastFirstObject(out var currentContainer))
        {
            SetHoveredContainer(currentContainer);
        }
    }

    private void SetHoveredContainer(GLSEventContainer currentContainer)
    {
        if (HoveredObject != currentContainer)
        {
            // Pool refresh can rebind the highlighted container to another node, so transfer hover visuals before rendering.
            if (HoveredObject != null)
            {
                HoveredObject.Highlighted = false;
            }

        }

        // The pool may have reset this same container during rebinding, so reassert the target outline even when its identity is unchanged.
        currentContainer.Highlighted = true;
        HoveredObject = currentContainer;
        IsHovering = true;
    }

    protected override bool ValidObject(GLSEventContainer container) => container.ObjectData is TData;
}
