using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Info;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Localization.Settings;

internal interface ITextClipboard
{
    string Text { get; set; }
}

internal sealed class UnityTextClipboard : ITextClipboard
{
    public string Text
    {
        get => GUIUtility.systemCopyBuffer;
        set => GUIUtility.systemCopyBuffer = value;
    }
}

internal readonly struct ClipboardJsonObject
{
    internal ClipboardJsonObject(BaseObject source)
    {
        Source = source;
        OriginalJsonTime = source.JsonTime;
    }

    internal BaseObject Source { get; }
    internal float OriginalJsonTime { get; }
}

internal static class ClipboardJsonController
{
    private const string BpmInfoFileName = "BPMInfo.dat";
    private const string DifficultyFileName = "Difficulty.dat";
    private const string ClipboardJsonReadyMessageKey = "clipboard.json.ready";
    private const string PartialEventBoxCommentKey = "clipboard.partial.eventbox.comment";
    private const string PartialEventBoxMarker = "//+ ";

    private static ITextClipboard textClipboard = new UnityTextClipboard();
    private static IReadOnlyList<ClipboardJsonObject> pendingObjects;
    private static BaseEventBoxGroup pendingPartialGroup;
    private static List<BpmInfoBpmRegion> pendingBpmRegions;
    private static string lastExportedText;
    private static bool exportedSinceCapture;
    private static bool isChroMapperPresent = true;
    private static bool isPointerInside = true;
    private static bool hasApplicationFocus = true;

    internal static BaseEventBoxGroup CaptureCopiedObjects(IReadOnlyList<ClipboardJsonObject> copiedObjects)
    {
        pendingObjects = copiedObjects;
        pendingBpmRegions = CaptureBpmRegions(copiedObjects);
        TryCreatePartialGlsGroup(copiedObjects, out pendingPartialGroup);
        exportedSinceCapture = false;
        return pendingPartialGroup == null ? null : BeatmapFactory.Clone(pendingPartialGroup);
    }

    private static string SerializeCopiedObjects(IReadOnlyList<ClipboardJsonObject> copiedObjects)
    {
        var files = new Dictionary<string, Dictionary<string, JSONArray>>();
        var bpmRegionsByStartBeat = GetBpmRegionsByStartBeat(pendingBpmRegions);
        var copiedVfxGroups = copiedObjects
            .Select(copiedObject => copiedObject.Source)
            .OfType<BaseVfxEventEventBoxGroup>()
            .ToList();
        var floatFxEvents = copiedVfxGroups.Count > 0
            ? copiedVfxGroups.SelectMany(group => group.Boxes).SelectMany(box => box.Events).Distinct().ToList()
            : null;
        for (var i = 0; i < copiedObjects.Count; i++)
        {
            var copiedObject = copiedObjects[i];
            var fileName = GetFileName(copiedObject.Source);
            var collectionKey = GetDifficultyCollectionKey(copiedObject.Source);
            if (!files.TryGetValue(fileName, out var collections))
            {
                collections = new Dictionary<string, JSONArray>();
                files.Add(fileName, collections);
            }

            if (!collections.TryGetValue(collectionKey, out var collection))
            {
                collection = new JSONArray();
                collections.Add(collectionKey, collection);
            }

            if (copiedObject.Source is BaseBpmEvent)
            {
                collection.Add(bpmRegionsByStartBeat[copiedObject.OriginalJsonTime]);
            }
            else if (copiedObject.Source is BaseVfxEventEventBoxGroup vfxGroup)
            {
                collection.Add(V3VfxEventEventBoxGroup.ToJson(vfxGroup, floatFxEvents));
            }
            else
            {
                collection.Add(copiedObject.Source.ToJson());
            }
        }

        var collectionCount = files.Values.Sum(collections => collections.Count);
        if (collectionCount == 1 && floatFxEvents == null)
        {
            return files.Values.First().Values.First().ToString(2);
        }
        else if (files.Count == 1)
        {
            return CreateFileJson(files.Values.First(), floatFxEvents).ToString(2);
        }
        else
        {
            var json = new JSONObject();
            foreach (var fileName in GetOrderedFileNames(files.Keys))
            {
                json[fileName] = CreateFileJson(
                    files[fileName],
                    fileName == DifficultyFileName ? floatFxEvents : null);
            }

            return json.ToString(2);
        }
    }

    private static bool TryCreatePartialGlsGroup(
        IReadOnlyList<ClipboardJsonObject> copiedObjects,
        out BaseEventBoxGroup partialGroup)
    {
        partialGroup = null;
        if (copiedObjects.Count == 0
            || copiedObjects.Any(copiedObject => copiedObject.Source is not BaseGLSEvent))
        {
            return false;
        }

        var selectedEvents = copiedObjects
            .Select(copiedObject => (BaseGLSEvent)copiedObject.Source)
            .ToList();
        var sourceGroups = selectedEvents
            .Select(evt => evt.EventBoxGroupData)
            .Distinct()
            .ToList();
        if (sourceGroups.Count != 1
            || sourceGroups[0] == null
            || selectedEvents.Select(evt => evt.BoxIndex).Distinct().Count() <= 1)
        {
            return false;
        }

        partialGroup = BeatmapFactory.Clone(sourceGroups[0]);
        var selectedEventsByBox = selectedEvents
            .GroupBy(evt => evt.BoxIndex)
            .OrderBy(group => group.Key)
            .ToList();
        switch (partialGroup)
        {
            case BaseLightColorEventBoxGroup colorGroup:
                colorGroup.Boxes = selectedEventsByBox
                    .Select(group => CreatePartialBox(
                        colorGroup.Boxes[group.Key],
                        group.Cast<BaseLightColorBase>()))
                    .ToList();
                break;
            case BaseLightRotationEventBoxGroup rotationGroup:
                rotationGroup.Boxes = selectedEventsByBox
                    .Select(group => CreatePartialBox(
                        rotationGroup.Boxes[group.Key],
                        group.Cast<BaseLightRotationBase>()))
                    .ToList();
                break;
            case BaseLightTranslationEventBoxGroup translationGroup:
                translationGroup.Boxes = selectedEventsByBox
                    .Select(group => CreatePartialBox(
                        translationGroup.Boxes[group.Key],
                        group.Cast<BaseLightTranslationBase>()))
                    .ToList();
                break;
            case BaseVfxEventEventBoxGroup vfxGroup:
                vfxGroup.Boxes = selectedEventsByBox
                    .Select(group => CreatePartialBox(
                        vfxGroup.Boxes[group.Key],
                        group.Cast<BaseFxEventFloat>()))
                    .ToList();
                break;
            default:
                return false;
        }

        GLSCommonCommand.RebindGroup(partialGroup);
        return true;
    }

    private static string SerializePartialGlsGroup(BaseEventBoxGroup partialGroup)
    {
        if (partialGroup is BaseVfxEventEventBoxGroup partialVfxGroup)
        {
            var floatFxEvents = partialVfxGroup.Boxes
                .SelectMany(box => box.Events)
                .ToList();
            var root = new JSONObject();
            var groups = new JSONArray();
            groups.Add(V3VfxEventEventBoxGroup.ToJson(partialVfxGroup, floatFxEvents));
            root["vfxEventBoxGroups"] = groups;
            root["_fxEventsCollection"] = V3FxEventsCollection.ToJson(new BaseFxEventsCollection
            {
                FloatFxEvents = floatFxEvents.ToArray()
            });
            var formattedVfxJson = root.ToString(2);
            var groupsArrayIndex = formattedVfxJson.IndexOf('[', formattedVfxJson.IndexOf("\"vfxEventBoxGroups\""));
            var groupObjectIndex = formattedVfxJson.IndexOf('{', groupsArrayIndex);
            return formattedVfxJson.Insert(groupObjectIndex + 1, $"\n      {GetPartialEventBoxComment()}");
        }

        var formattedJson = partialGroup.ToJson().ToString(2);
        return formattedJson.Insert(1, $"\n  {GetPartialEventBoxComment()}");
    }

    internal static string GetPartialEventBoxComment()
    {
        return PartialEventBoxMarker
            + LocalizationSettings.StringDatabase.GetLocalizedString("Mapper", PartialEventBoxCommentKey);
    }

    private static BaseLightColorEventBox CreatePartialBox(
        BaseLightColorEventBox sourceBox,
        IEnumerable<BaseLightColorBase> selectedEvents)
    {
        var box = (BaseLightColorEventBox)sourceBox.Clone();
        box.Events = selectedEvents.Select(BeatmapFactory.Clone).ToArray();
        return box;
    }

    private static BaseLightRotationEventBox CreatePartialBox(
        BaseLightRotationEventBox sourceBox,
        IEnumerable<BaseLightRotationBase> selectedEvents)
    {
        var box = (BaseLightRotationEventBox)sourceBox.Clone();
        box.Events = selectedEvents.Select(BeatmapFactory.Clone).ToArray();
        return box;
    }

    private static BaseLightTranslationEventBox CreatePartialBox(
        BaseLightTranslationEventBox sourceBox,
        IEnumerable<BaseLightTranslationBase> selectedEvents)
    {
        var box = (BaseLightTranslationEventBox)sourceBox.Clone();
        box.Events = selectedEvents.Select(BeatmapFactory.Clone).ToArray();
        return box;
    }

    private static BaseVfxEventEventBox CreatePartialBox(
        BaseVfxEventEventBox sourceBox,
        IEnumerable<BaseFxEventFloat> selectedEvents)
    {
        var box = (BaseVfxEventEventBox)sourceBox.Clone();
        box.Events = selectedEvents.Select(BeatmapFactory.Clone).ToArray();
        return box;
    }

    internal static void HandleChroMapperPresenceChanged(bool isPresent)
    {
        if (isChroMapperPresent == isPresent)
        {
            return;
        }

        isChroMapperPresent = isPresent;
        if (!Settings.Instance.SyncCopiedObjectsWithSystemClipboard)
        {
            return;
        }

        if (!isPresent)
        {
            ExportPendingJson();
            return;
        }

        var clipboardText = textClipboard.Text;
        if (AreEquivalentClipboardText(clipboardText, lastExportedText))
        {
            return;
        }

        if (!TryImportCopiedObjects(clipboardText, out var importedObjects))
        {
            return;
        }

        SelectionController.CopiedObjects.Clear();
        for (var i = 0; i < importedObjects.Count; i++)
        {
            SelectionController.CopiedObjects.Add(importedObjects[i]);
        }

        lastExportedText = clipboardText;

        PersistentUI.Instance.DisplayMessage(
            "Mapper",
            ClipboardJsonReadyMessageKey,
            PersistentUI.DisplayMessageType.Bottom);
    }

    internal static void HandlePointerPresenceChanged(bool isInside)
    {
        isPointerInside = isInside;
        HandleChroMapperPresenceChanged(isPointerInside && hasApplicationFocus);
    }

    internal static void HandleApplicationFocusChanged(bool hasFocus)
    {
        hasApplicationFocus = hasFocus;
        HandleChroMapperPresenceChanged(isPointerInside && hasApplicationFocus);
    }

    internal static void SetTextClipboardForTests(ITextClipboard clipboard)
    {
        textClipboard = clipboard;
        pendingObjects = null;
        pendingPartialGroup = null;
        pendingBpmRegions = null;
        lastExportedText = null;
        exportedSinceCapture = false;
        isChroMapperPresent = true;
        isPointerInside = true;
        hasApplicationFocus = true;
    }

    internal static void ResetTextClipboardAfterTests()
    {
        textClipboard = new UnityTextClipboard();
        pendingObjects = null;
        pendingPartialGroup = null;
        pendingBpmRegions = null;
        lastExportedText = null;
        exportedSinceCapture = false;
        isChroMapperPresent = true;
        isPointerInside = true;
        hasApplicationFocus = true;
    }

    private static void ExportPendingJson()
    {
        if (exportedSinceCapture || pendingObjects == null || pendingObjects.Count == 0)
        {
            return;
        }

        var pendingExportText = pendingPartialGroup == null
            ? SerializeCopiedObjects(pendingObjects)
            : SerializePartialGlsGroup(pendingPartialGroup);
        textClipboard.Text = pendingExportText;
        lastExportedText = pendingExportText;
        exportedSinceCapture = true;
    }

    private static bool AreEquivalentClipboardText(string clipboardText, string exportedText)
    {
        if (clipboardText == exportedText)
        {
            return true;
        }

        if (clipboardText == null || exportedText == null)
        {
            return false;
        }

        return NormalizeNewlines(clipboardText) == NormalizeNewlines(exportedText);
    }

    private static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    private static bool TryImportCopiedObjects(string clipboardText, out List<BaseObject> importedObjects)
    {
        importedObjects = new List<BaseObject>();
        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            return false;
        }

        try
        {
            var trimmedText = clipboardText.Trim();
            var isPartialGlsGroup = TryStripPartialEventBoxComment(trimmedText, out var jsonText);
            if (!isPartialGlsGroup
                && trimmedText.IndexOf(PartialEventBoxMarker, System.StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            trimmedText = jsonText;

            JSONNode json;
            if (trimmedText[0] == '{' && trimmedText.EndsWith(","))
            {
                json = JSON.Parse($"[{trimmedText.TrimEnd().TrimEnd(',')}]");
            }
            else
            {
                json = JSON.Parse(trimmedText);
            }

            if (json == null)
            {
                return false;
            }

            if (isPartialGlsGroup && json.IsObject)
            {
                if (!TryImportPartialGlsGroup(json.AsObject, importedObjects))
                {
                    return false;
                }
            }
            else if (json.IsArray)
            {
                if (!TryImportRawArray(json.AsArray, importedObjects))
                {
                    return false;
                }
            }
            else if (json.IsObject)
            {
                if (!TryImportDifficultyObject(json.AsObject, importedObjects))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            var firstJsonTime = importedObjects.Min(importedObject => importedObject.JsonTime);
            for (var i = 0; i < importedObjects.Count; i++)
            {
                var importedObject = importedObjects[i];
                importedObject.JsonTime -= firstJsonTime;
                if (importedObject is BaseSlider slider)
                {
                    slider.TailJsonTime -= firstJsonTime;
                }
            }

            return true;
        }
        catch
        {
            importedObjects.Clear();
            return false;
        }
    }

    private static bool TryStripPartialEventBoxComment(string text, out string jsonText)
    {
        jsonText = text;
        if (text.Length == 0 || text[0] != '{')
        {
            return false;
        }

        var markerIndex = 1;
        while (markerIndex < text.Length && char.IsWhiteSpace(text[markerIndex]))
        {
            markerIndex++;
        }

        if (TryStripPartialEventBoxCommentAt(text, markerIndex, out jsonText))
        {
            return true;
        }

        const string vfxGroupsKey = "\"vfxEventBoxGroups\"";
        if (text.IndexOf(vfxGroupsKey, markerIndex, System.StringComparison.Ordinal) != markerIndex)
        {
            return false;
        }

        var groupsArrayIndex = text.IndexOf('[', markerIndex + vfxGroupsKey.Length);
        var groupObjectIndex = groupsArrayIndex >= 0 ? text.IndexOf('{', groupsArrayIndex + 1) : -1;
        if (groupObjectIndex < 0)
        {
            return false;
        }

        var nestedMarkerIndex = groupObjectIndex + 1;
        while (nestedMarkerIndex < text.Length && char.IsWhiteSpace(text[nestedMarkerIndex]))
        {
            nestedMarkerIndex++;
        }

        return TryStripPartialEventBoxCommentAt(text, nestedMarkerIndex, out jsonText);
    }

    private static bool TryStripPartialEventBoxCommentAt(string text, int markerIndex, out string jsonText)
    {
        jsonText = text;
        if (text.IndexOf(PartialEventBoxMarker, markerIndex, System.StringComparison.Ordinal) != markerIndex)
        {
            return false;
        }

        var carriageReturnIndex = text.IndexOf('\r', markerIndex + PartialEventBoxMarker.Length);
        var lineFeedIndex = text.IndexOf('\n', markerIndex + PartialEventBoxMarker.Length);
        var lineEndIndex = carriageReturnIndex < 0
            ? lineFeedIndex
            : lineFeedIndex < 0
                ? carriageReturnIndex
                : System.Math.Min(carriageReturnIndex, lineFeedIndex);
        if (lineEndIndex < 0)
        {
            return false;
        }

        jsonText = text.Remove(markerIndex, lineEndIndex - markerIndex);
        return true;
    }

    private static bool TryImportPartialGlsGroup(JSONObject json, List<BaseObject> importedObjects)
    {
        if (json["vfxEventBoxGroups"].IsArray
            && json["vfxEventBoxGroups"].Count == 1
            && json["_fxEventsCollection"].IsObject)
        {
            var fxEventsCollection = V3FxEventsCollection.GetFromJson(json["_fxEventsCollection"]);
            if (fxEventsCollection.FloatFxEvents.Length == 0)
            {
                return false;
            }

            var vfxGroup = V3VfxEventEventBoxGroup.GetFromJson(
                json["vfxEventBoxGroups"][0],
                fxEventsCollection.FloatFxEvents);
            return AddPartialGlsGroup(vfxGroup, importedObjects);
        }

        if (!json.HasKey("b") || !json.HasKey("g") || !json["e"].IsArray || json["e"].Count == 0)
        {
            return false;
        }

        var firstBox = json["e"][0];
        BaseEventBoxGroup group;
        if (firstBox["e"].IsArray)
        {
            group = V3LightColorEventBoxGroup.GetFromJson(json);
        }
        else if (!firstBox["l"].IsArray || firstBox["l"].Count == 0 || !firstBox["l"][0].IsObject)
        {
            return false;
        }
        else if (firstBox["l"][0].HasKey("r"))
        {
            group = V3LightRotationEventBoxGroup.GetFromJson(json);
        }
        else if (firstBox["l"][0].HasKey("t"))
        {
            group = V3LightTranslationEventBoxGroup.GetFromJson(json);
        }
        else
        {
            return false;
        }

        return AddPartialGlsGroup(group, importedObjects);
    }

    private static bool AddPartialGlsGroup(BaseEventBoxGroup group, List<BaseObject> importedObjects)
    {
        importedObjects.Add(group);
        for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
        {
            foreach (var evt in group.ReadOnlyBoxes[boxIndex].ReadOnlyEvents)
            {
                importedObjects.Add(evt);
            }
        }

        return importedObjects.Count > 1;
    }


    private static bool TryImportRawArray(JSONArray jsonArray, List<BaseObject> importedObjects)
    {
        if (jsonArray.Count == 0)
        {
            return false;
        }

        var collectionKey = InferCollectionKey(jsonArray[0]);
        if (collectionKey == null)
        {
            return false;
        }

        for (var i = 1; i < jsonArray.Count; i++)
        {
            if (InferCollectionKey(jsonArray[i]) != collectionKey)
            {
                return false;
            }
        }

        if (!TryImportCollection(collectionKey, jsonArray, importedObjects))
        {
            return false;
        }

        return collectionKey.StartsWith("gls")
            ? TryBindRawGlsEvents(importedObjects)
            : true;
    }

    private static bool TryBindRawGlsEvents(IReadOnlyList<BaseObject> importedObjects)
    {
        var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
        var context = provider != null ? provider.GroupContext : null;
        if (context == null || context.ReadOnlyBoxes.Count == 0)
        {
            return false;
        }

        var firstEvent = importedObjects[0] as BaseGLSEvent;
        if ((firstEvent is BaseLightColorBase && context is not BaseLightColorEventBoxGroup)
            || (firstEvent is BaseLightRotationBase && context is not BaseLightRotationEventBoxGroup)
            || (firstEvent is BaseLightTranslationBase && context is not BaseLightTranslationEventBoxGroup)
            || (firstEvent is BaseFxEventFloat && context is not BaseVfxEventEventBoxGroup))
        {
            return false;
        }

        var sourceBox = context.ReadOnlyBoxes[0];
        for (var i = 0; i < importedObjects.Count; i++)
        {
            var evt = importedObjects[i] as BaseGLSEvent;
            if (evt == null)
            {
                return false;
            }

            evt.EventBoxGroupData = context;
            evt.EventBoxData = sourceBox;
            evt.BoxIndex = 0;
        }

        return true;
    }

    private static bool TryImportDifficultyObject(JSONObject jsonObject, List<BaseObject> importedObjects)
    {
        if (jsonObject.Count == 0)
        {
            return false;
        }

        var fxEventsCollection = jsonObject["_fxEventsCollection"].IsObject
            ? V3FxEventsCollection.GetFromJson(jsonObject["_fxEventsCollection"])
            : null;
        var foundCollection = false;
        foreach (var pair in jsonObject)
        {
            if (IsRecognizedFileName(pair.Key) && pair.Value.IsObject)
            {
                var objectCountBeforeImport = importedObjects.Count;
                if (!TryImportDifficultyObject(pair.Value.AsObject, importedObjects))
                {
                    return false;
                }

                foundCollection |= importedObjects.Count > objectCountBeforeImport;
                continue;
            }

            if (pair.Key == "vfxEventBoxGroups")
            {
                if (!pair.Value.IsArray
                    || pair.Value.Count == 0
                    || fxEventsCollection == null
                    || fxEventsCollection.FloatFxEvents.Length == 0)
                {
                    return false;
                }

                foreach (JSONNode groupJson in pair.Value)
                {
                    importedObjects.Add(V3VfxEventEventBoxGroup.GetFromJson(
                        groupJson,
                        fxEventsCollection.FloatFxEvents));
                }

                foundCollection = true;
                continue;
            }

            if (!IsRecognizedCollectionKey(pair.Key) || !pair.Value.IsArray || pair.Value.Count == 0)
            {
                continue;
            }

            foundCollection = true;
            if (!TryImportCollection(pair.Key, pair.Value.AsArray, importedObjects))
            {
                return false;
            }
        }

        return foundCollection && importedObjects.Count > 0;
    }

    private static bool TryImportCollection(
        string collectionKey,
        JSONArray jsonArray,
        List<BaseObject> importedObjects)
    {
        for (var i = 0; i < jsonArray.Count; i++)
        {
            var importedObject = CreateObject(collectionKey, jsonArray[i]);
            if (importedObject == null)
            {
                return false;
            }

            importedObjects.Add(importedObject);
        }

        return true;
    }

    private static BaseObject CreateObject(string collectionKey, JSONNode json)
    {
        return collectionKey switch
        {
            "colorNotes" or "bombNotes" or "_notes" => BeatmapFactory.Note(json),
            "obstacles" or "_obstacles" => BeatmapFactory.Obstacle(json),
            "sliders" or "_sliders" => BeatmapFactory.Arc(json),
            "burstSliders" => BeatmapFactory.Chain(json),
            "waypoints" or "_waypoints" => BeatmapFactory.Waypoint(json),
            "basicBeatmapEvents" or "colorBoostBeatmapEvents" => BeatmapFactory.Event(json),
            "_events" => CreateV2Event(json),
            "rotationEvents" => BeatmapFactory.RotationEvent(json),
            "bpmEvents" => BeatmapFactory.BpmEvent(json),
            "_regions" => CreateBpmEventFromRegion(json),
            "lightColorEventBoxGroups" => BeatmapFactory.LightColorEventBoxGroups(json),
            "lightRotationEventBoxGroups" => BeatmapFactory.LightRotationEventBoxGroups(json),
            "lightTranslationEventBoxGroups" => BeatmapFactory.LightTranslationEventBoxGroups(json),
            "glsColorEvents" => V3LightColorBase.GetFromJson(json),
            "glsRotationEvents" => V3LightRotationBase.GetFromJson(json),
            "glsTranslationEvents" => V3LightTranslationBase.GetFromJson(json),
            "glsFloatEvents" => V3FloatFxEvent.GetFromJson(json),
            "customEvents" or "_customEvents" => BeatmapFactory.CustomEvent(json),
            "bookmarks" => BeatmapFactory.Bookmark(json),
            "njsEvents" => new BaseNJSEvent(json),
            _ => null
        };
    }

    private static string InferCollectionKey(JSONNode json)
    {
        if (Settings.Instance.MapVersion == 2)
        {
            if (json.HasKey("_startSampleIndex")
                && json.HasKey("_endSampleIndex")
                && json.HasKey("_startBeat")
                && json.HasKey("_endBeat"))
            {
                return "_regions";
            }

            if (json.HasKey("_headTime")
                && json.HasKey("_headLineIndex")
                && json.HasKey("_tailTime")
                && json.HasKey("_tailLineIndex"))
            {
                return "_sliders";
            }

            if (json.HasKey("_time")
                && json.HasKey("_lineIndex")
                && json.HasKey("_duration")
                && json.HasKey("_width"))
            {
                return "_obstacles";
            }

            if (json.HasKey("_time")
                && json.HasKey("_lineIndex")
                && json.HasKey("_lineLayer")
                && json.HasKey("_offsetDirection"))
            {
                return "_waypoints";
            }

            if (json.HasKey("_time")
                && json.HasKey("_lineIndex")
                && json.HasKey("_lineLayer")
                && json.HasKey("_type")
                && json.HasKey("_cutDirection"))
            {
                return "_notes";
            }

            if (json.HasKey("_time") && json.HasKey("_type") && json.HasKey("_value"))
            {
                return "_events";
            }

            return null;
        }

        if (json.HasKey("_startSampleIndex")
            && json.HasKey("_endSampleIndex")
            && json.HasKey("_startBeat")
            && json.HasKey("_endBeat"))
        {
            return "_regions";
        }

        if (json.HasKey("beat")
            && json.HasKey("usePrevious")
            && json.HasKey("easing")
            && json.HasKey("relative-njs"))
        {
            return "njsEvents";
        }

        if (json.HasKey("tb") && json.HasKey("tx") && json.HasKey("ty"))
        {
            return json.HasKey("sc") ? "burstSliders" : "sliders";
        }

        if (json.HasKey("b") && json.HasKey("g") && json["e"].IsArray && json["e"].Count > 0)
        {
            var firstEventBox = json["e"][0];
            if (firstEventBox["e"].IsArray && firstEventBox["e"].Count > 0)
            {
                return "lightColorEventBoxGroups";
            }

            if (firstEventBox["l"].IsArray && firstEventBox["l"].Count > 0)
            {
                var firstEvent = firstEventBox["l"][0];
                if (firstEvent.HasKey("r") && firstEvent.HasKey("o"))
                {
                    return "lightRotationEventBoxGroups";
                }

                if (firstEvent.HasKey("t") && firstEvent.HasKey("p"))
                {
                    return "lightTranslationEventBoxGroups";
                }
            }
        }

        if (json.HasKey("x") && json.HasKey("y") && json.HasKey("w") && json.HasKey("h"))
        {
            return "obstacles";
        }

        if (json.HasKey("x") && json.HasKey("y"))
        {
            if (json.HasKey("c") && json.HasKey("d"))
            {
                return "colorNotes";
            }

            return json.HasKey("d") ? "waypoints" : "bombNotes";
        }

        if (json.HasKey("b") && json.HasKey("et") && json.HasKey("i") && json.HasKey("f"))
        {
            return "basicBeatmapEvents";
        }

        if (json.HasKey("b") && json.HasKey("o") && json["o"].IsBoolean)
        {
            return "colorBoostBeatmapEvents";
        }

        if (json.HasKey("b") && json.HasKey("sb") && json.HasKey("sf") && json.HasKey("s"))
        {
            return "glsColorEvents";
        }

        if (json.HasKey("b")
            && json.HasKey("r")
            && json.HasKey("o")
            && json.HasKey("e")
            && json.HasKey("l")
            && json.HasKey("p"))
        {
            return "glsRotationEvents";
        }

        if (json.HasKey("b") && json.HasKey("p") && json.HasKey("e") && json.HasKey("t"))
        {
            return "glsTranslationEvents";
        }

        if (json.HasKey("b") && json.HasKey("p") && json.HasKey("v") && json.HasKey("i"))
        {
            return "glsFloatEvents";
        }

        if (json.HasKey("b") && json.HasKey("e") && json.HasKey("r"))
        {
            return "rotationEvents";
        }

        if (json.HasKey("b") && json.HasKey("m"))
        {
            return "bpmEvents";
        }

        if (json.HasKey("b") && json.HasKey("t") && json["d"].IsObject)
        {
            return "customEvents";
        }

        if (json.HasKey("b") && json.HasKey("n") && json["c"].IsArray)
        {
            return "bookmarks";
        }

        return null;
    }

    private static bool IsRecognizedCollectionKey(string collectionKey)
    {
        return collectionKey is "colorNotes"
            or "bombNotes"
            or "_notes"
            or "obstacles"
            or "_obstacles"
            or "sliders"
            or "_sliders"
            or "burstSliders"
            or "waypoints"
            or "_waypoints"
            or "basicBeatmapEvents"
            or "colorBoostBeatmapEvents"
            or "_events"
            or "rotationEvents"
            or "bpmEvents"
            or "_regions"
            or "lightColorEventBoxGroups"
            or "lightRotationEventBoxGroups"
            or "lightTranslationEventBoxGroups"
            or "customEvents"
            or "_customEvents"
            or "bookmarks"
            or "njsEvents";
    }

    private static BaseObject CreateV2Event(JSONNode json)
    {
        var eventType = json["_type"].AsInt;
        if (eventType == 100)
        {
            return V2BpmEvent.GetFromJson(json);
        }

        return eventType is 14 or 15
            ? V2RotationEvent.GetFromJson(json)
            : V2Event.GetFromJson(json);
    }

    private static bool IsRecognizedFileName(string fileName) =>
        fileName is BpmInfoFileName or DifficultyFileName;

    private static BaseBpmEvent CreateBpmEventFromRegion(JSONNode json)
    {
        var sampleCount = json["_endSampleIndex"].AsInt - json["_startSampleIndex"].AsInt;
        var beatCount = json["_endBeat"].AsFloat - json["_startBeat"].AsFloat;
        var frequency = BeatSaberSongContainer.Instance.LoadedSongFrequency;
        if (sampleCount <= 0 || beatCount <= 0 || frequency <= 0)
        {
            return null;
        }

        var bpm = beatCount / sampleCount * frequency * 60;
        if (float.IsNaN(bpm) || float.IsInfinity(bpm) || bpm <= 0)
        {
            return null;
        }

        return new BaseBpmEvent
        {
            JsonTime = json["_startBeat"].AsFloat,
            Bpm = bpm
        };
    }

    private static JSONObject CreateFileJson(
        Dictionary<string, JSONArray> collections,
        IList<BaseFxEventFloat> floatFxEvents = null)
    {
        var json = new JSONObject();
        foreach (var collectionKey in GetOrderedCollectionKeys(collections.Keys))
        {
            json[collectionKey] = collections[collectionKey];
        }

        if (floatFxEvents != null)
        {
            json["_fxEventsCollection"] = V3FxEventsCollection.ToJson(new BaseFxEventsCollection
            {
                FloatFxEvents = floatFxEvents.ToArray()
            });
        }

        return json;
    }

    private static string GetFileName(BaseObject copiedObject) =>
        copiedObject is BaseBpmEvent ? BpmInfoFileName : DifficultyFileName;

    private static IEnumerable<string> GetOrderedFileNames(IEnumerable<string> fileNames) =>
        fileNames.OrderBy(fileName => fileName == BpmInfoFileName ? 0 : 1).ThenBy(fileName => fileName);

    private static List<BpmInfoBpmRegion> CaptureBpmRegions(
        IReadOnlyList<ClipboardJsonObject> copiedObjects)
    {
        if (!copiedObjects.Any(copiedObject => copiedObject.Source is BaseBpmEvent))
        {
            return null;
        }

        var songContainer = BeatSaberSongContainer.Instance;
        var bpmEvents = BaseObject.InFileOrder(songContainer.Map.BpmEvents).ToList();
        return BaseBpmInfo.GetBpmInfoRegions(
            bpmEvents,
            songContainer.Info.BeatsPerMinute,
            songContainer.LoadedSongSamples - 1,
            songContainer.LoadedSongFrequency);
    }

    private static Dictionary<float, JSONNode> GetBpmRegionsByStartBeat(List<BpmInfoBpmRegion> regions)
    {
        if (regions == null)
            return null;

        var regionsByStartBeat = new Dictionary<float, JSONNode>(regions.Count);
        for (var i = 0; i < regions.Count; i++)
        {
            var region = regions[i];
            regionsByStartBeat.Add(region.StartBeat, new JSONObject
            {
                ["_startSampleIndex"] = region.StartSampleIndex,
                ["_endSampleIndex"] = region.EndSampleIndex,
                ["_startBeat"] = region.StartBeat,
                ["_endBeat"] = region.EndBeat
            });
        }

        return regionsByStartBeat;
    }

    private static string GetDifficultyCollectionKey(BaseObject copiedObject)
    {
        if (copiedObject is BaseBpmEvent)
        {
            return "_regions";
        }

        if (Settings.Instance.MapVersion == 2)
        {
            return copiedObject.ObjectType switch
            {
                ObjectType.Note => "_notes",
                ObjectType.Event or ObjectType.BpmChange or ObjectType.RotationEvent => "_events",
                ObjectType.Obstacle => "_obstacles",
                ObjectType.Waypoint => "_waypoints",
                ObjectType.Arc => "_sliders",
                ObjectType.CustomEvent => "_customEvents",
                _ => copiedObject.ObjectType.ToString()
            };
        }

        return copiedObject.ObjectType switch
        {
            ObjectType.Note when ((BaseNote)copiedObject).Type == (int)NoteType.Bomb => "bombNotes",
            ObjectType.Note => "colorNotes",
            ObjectType.Event when ((BaseEvent)copiedObject).IsColorBoostEvent() => "colorBoostBeatmapEvents",
            ObjectType.Event => "basicBeatmapEvents",
            ObjectType.BpmChange => "_regions",
            ObjectType.RotationEvent => "rotationEvents",
            ObjectType.Obstacle => "obstacles",
            ObjectType.Arc => "sliders",
            ObjectType.Chain => "burstSliders",
            ObjectType.Waypoint => "waypoints",
            ObjectType.GLSColor => "lightColorEventBoxGroups",
            ObjectType.GLSRotation => "lightRotationEventBoxGroups",
            ObjectType.GLSTranslation => "lightTranslationEventBoxGroups",
            ObjectType.GLSFloatFx => "vfxEventBoxGroups",
            ObjectType.CustomEvent => "customEvents",
            ObjectType.Bookmark => "bookmarks",
            ObjectType.NJSEvent => "njsEvents",
            _ => copiedObject.ObjectType.ToString()
        };
    }

    private static IEnumerable<string> GetOrderedCollectionKeys(IEnumerable<string> collectionKeys)
    {
        var collectionOrder = Settings.Instance.MapVersion == 2
            ? new[] { "_events", "_notes", "_obstacles", "_waypoints", "_sliders", "_customEvents" }
            : new[]
            {
                "bpmEvents",
                "_regions",
                "rotationEvents",
                "basicBeatmapEvents",
                "colorBoostBeatmapEvents",
                "colorNotes",
                "bombNotes",
                "obstacles",
                "sliders",
                "burstSliders",
                "waypoints",
                "lightColorEventBoxGroups",
                "lightRotationEventBoxGroups",
                "lightTranslationEventBoxGroups",
                "vfxEventBoxGroups",
                "njsEvents",
                "bookmarks",
                "customEvents"
            };

        return collectionKeys.OrderBy(key =>
        {
            var index = System.Array.IndexOf(collectionOrder, key);
            return index >= 0 ? index : int.MaxValue;
        }).ThenBy(key => key);
    }
}
