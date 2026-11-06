using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class RotationCommand
{
    public static BaseRotationEvent PlaceEventInPlace(float time, bool clockwise, float prec)
    {
        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        var regc = BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(
            ObjectType.RotationEvent);
        var evt = regc.MapObjects.Find(e =>
            e.SongBpmTime - epsilon < time
            && e.SongBpmTime + epsilon > time
            && e.ExecutionTime == ExecutionTime.Early);

        var modifier = clockwise ? 1 : -1;
        if (evt != null)
        {
            var newEvt = BeatmapFactory.Clone(evt);
            newEvt.Rotation = CMMath.RoundToDecimals(newEvt.Rotation + (modifier * prec)) % 360f;
            if (Mathf.Approximately(newEvt.Rotation, 0f))
            {
                regc.DeleteObject(evt, true, true, "Deleted a rotation event for 0 rotation.");
                return null;
            }

            BeatmapActionContainer.AddAction(
                new BeatmapObjectUpdatedAction(newEvt, evt, "Updated a rotation event."),
                true);
            return newEvt;
        }
        else
        {
            var newEvt = new BaseRotationEvent
            {
                JsonTime = time,
                ExecutionTime = ExecutionTime.Early,
                Rotation = CMMath.RoundToDecimals(modifier * prec)
            };
            regc.SpawnObject(newEvt, out var conflicting);
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(newEvt, conflicting, "Placed a rotation event."));
            return newEvt;
        }
    }

    public static BaseObject RotateObject(BaseObject originalObject, bool clockwise, float prec)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        var modifier = clockwise ? 1 : -1;
        switch (newObject)
        {
            case BaseRotationEvent evt:
                {
                    evt.Rotation = CMMath.RoundToDecimals(evt.Rotation + (modifier * prec)) % 360f;
                    break;
                }
            case BaseGrid grid:
                {
                    grid.Rotation = (int)Mathf.Repeat(grid.Rotation + Mathf.RoundToInt(modifier * prec), 360);
                    break;
                }
        }

        if (newObject.HasSameContent(originalObject)) return null;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue,
                preserveSelection: true),
            true);
        return newObject;
    }

    public static BaseObject SetRotationInfer(BaseObject originalObject, float rotate)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        switch (newObject)
        {
            case BaseRotationEvent evt:
                {
                    var sign = Mathf.Sign(evt.Rotation);
                    evt.Rotation = rotate * sign % 360f;
                    break;
                }
            case BaseGrid grid:
                {
                    var sign = Mathf.Sign(grid.Rotation);
                    grid.Rotation = (int)Mathf.Repeat(rotate * sign, 360f);
                    break;
                }
        }

        // Content equality detects a no-op without relying on the collection ordering keys.
        if (newObject.HasSameContent(originalObject)) return null;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue,
                preserveSelection: true),
            true);
        return newObject;
    }

    public static BaseObject SetRotation(BaseObject originalObject, float rotate)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        switch (newObject)
        {
            case BaseRotationEvent evt:
                {
                    evt.Rotation = rotate % 360f;
                    break;
                }
            case BaseGrid grid:
                {
                    grid.Rotation = (int)Mathf.Repeat(rotate, 360);
                    break;
                }
        }

        // Content equality detects a no-op without relying on the collection ordering keys.
        if (newObject.HasSameContent(originalObject)) return null;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue,
                preserveSelection: true),
            true);
        return newObject;
    }

    public static BaseRotationEvent Invert(BaseRotationEvent originalObject)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        newObject.Rotation *= -1;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue,
                preserveSelection: true),
            true);
        return newObject;
    }

    public static BaseRotationEvent ModifyHover(BaseRotationEvent originalObject, int modifier, float prec)
    {
        if (HoverSelectionMutation.TryApply(
                originalObject,
                data => data.Rotation = CMMath.RoundToDecimals(data.Rotation + (modifier * prec)) % 360f,
                out var edited))
        {
            return edited.Count > 0
                ? (BaseRotationEvent)edited[0]
                : null;
        }


        var newObject = BeatmapFactory.Clone(originalObject);

        newObject.Rotation = CMMath.RoundToDecimals(newObject.Rotation + (modifier * prec)) % 360f;

        // Content equality detects a no-op without relying on the collection ordering keys.
        if (newObject.HasSameContent(originalObject)) return null;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue,
                preserveSelection: true),
            true);
        return newObject;
    }
}
