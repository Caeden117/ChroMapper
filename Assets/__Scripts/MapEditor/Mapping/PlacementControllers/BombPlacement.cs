﻿using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

public class BombPlacement : PlacementController<BaseNote, NoteContainer, NoteGridContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;
    [SerializeField] private ColorPicker colorPicker;

    [SerializeField] private ToggleColourDropdown dropdown;

    private static readonly int alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");

    // Chroma Color Check
    public static bool CanPlaceChromaObjects
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            return false;
        }
    }

    public override int PlacementXMin => base.PlacementXMax * -1;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a Bomb.");

    public override BaseNote GenerateOriginalData() => new BaseNote { Type = (int)NoteType.Bomb };

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 roundedHit)
    {
        // Check if Chroma Color notes button is active and apply _color
        queuedData.CustomColor = (CanPlaceChromaObjects && dropdown.Visible)
            ? colorPicker.CurrentColor
            : null;

        var posX = (int)roundedHit.x;
        var posY = (int)roundedHit.y;

        var vanillaX = Mathf.Clamp(posX, 0, 3);
        var vanillaY = Mathf.Clamp(posY, 0, 2);

        var vanillaBounds = vanillaX == posX && vanillaY == posY;

        queuedData.PosX = vanillaX;
        queuedData.PosY = vanillaY;

        if (UsePrecisionPlacement)
        {
            var rawHit = ParentTrack.InverseTransformPoint(hit.Point);
            rawHit.z = SongBpmTime * EditorScaleController.EditorScale;

            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedHit = ((Vector2)Vector2Int.RoundToInt((precisionOffset + (Vector2)rawHit) * precision)) / precision;
            instantiatedContainer.transform.localPosition = roundedHit;

            queuedData.CustomCoordinate = (Vector2)roundedHit;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);

            queuedData.CustomCoordinate = !vanillaBounds
                ? (Vector2)roundedHit - vanillaOffset + precisionOffset
                : null;
        }

        instantiatedContainer.MaterialPropertyBlock.SetFloat(alwaysTranslucent, 1);
        instantiatedContainer.UpdateMaterials();

        instantiatedContainer.NoteData = queuedData;
        instantiatedContainer.UpdateGridPosition();
    }

    public override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
    }
}
