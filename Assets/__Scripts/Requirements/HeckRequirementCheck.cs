using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;

public abstract class HeckRequirementCheck : RequirementCheck
{
    private static readonly HashSet<string> heckCustomEventTypes = new HashSet<string>
    {
        "AnimateTrack",
        "AssignPathAnimation",
        "AssignTrackParent",
        "AssignPlayerToTrack",
        "AnimateComponent"
    };

    protected bool HasAnimationsFromMod(BaseDifficulty map, ICollection<string> modSpecificTrackTypes,
        ICollection<string> modAnimationKeys)
    {
        var propertiesByHeckTrack = new Dictionary<string, HashSet<string>>();
        foreach (var customEvent in GetHeckCustomEventsFromMap(map))
        {
            // If you have Chroma/NE exclusive events, I'm going to assume you're using them.
            if (modSpecificTrackTypes.Contains(customEvent.Type))
            {
                return true;
            }

            // Can be Chroma, NE, or both. We check if they belong to the required mod later.
            if (customEvent.Type == "AnimateTrack" || customEvent.Type == "AssignPathAnimation")
            {
                if (propertiesByHeckTrack.TryGetValue(customEvent.Data[customEvent.CustomKeyTrack], out var tracks))
                {
                    tracks.UnionWith(customEvent.Data.Linq.Select(x => x.Key));
                }
                else
                {
                    propertiesByHeckTrack.Add(customEvent.Data[customEvent.CustomKeyTrack].Value,
                        new HashSet<string>(customEvent.Data.Linq.Select(x => x.Key)));
                }
            }
        }

        var heckData = GetHeckDataFromMap(map);

        // Any animation keys which require mod
        if (heckData.animationKeys.Intersect(modAnimationKeys).Any())
        {
            return true;
        }

        // Any used tracks which require mod
        foreach (var heckTrack in heckData.tracks)
        {
            if (propertiesByHeckTrack.TryGetValue(heckTrack, out var value))
            {
                if (value.Intersect(modAnimationKeys).Any())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<BaseCustomEvent> GetHeckCustomEventsFromMap(BaseDifficulty map) =>
        map.CustomEvents.Where(customEvent => heckCustomEventTypes.Contains(customEvent.Type) &&
                                              (customEvent.CustomTrack != null || customEvent.DataParentTrack != null ||
                                               customEvent.DataChildrenTracks != null));

    private static (IEnumerable<string> tracks, IEnumerable<string> animationKeys) GetHeckDataFromMap(BaseDifficulty map)
    {
        var tracks = new HashSet<string>();
        var animationKeys = new HashSet<string>();

        AddAnimationDataFromHeckGameplayObjects(animationKeys, tracks, map.Notes);
        AddAnimationDataFromHeckGameplayObjects(animationKeys, tracks, map.Obstacles);
        AddAnimationDataFromHeckGameplayObjects(animationKeys, tracks, map.Arcs);
        AddAnimationDataFromHeckGameplayObjects(animationKeys, tracks, map.Chains);
        // map.Events is handled by ChromaReq

        return (tracks, animationKeys);
    }

    private static void AddAnimationDataFromHeckGameplayObjects(ICollection<string> animations,
        ICollection<string> tracks, IEnumerable<BaseGrid> heckObjects)
    {
        foreach (var heckObject in heckObjects)
        {
            // Animations
            if (heckObject.CustomAnimation != null)
            {
                if (heckObject.CustomAnimation.IsObject)
                {
                    var animationKeys = heckObject.CustomAnimation.AsObject.Linq.Select(x => x.Key);
                    foreach (var animationKey in animationKeys)
                    {
                        animations.Add(animationKey);
                    }
                }
            }

            // Tracks
            if (heckObject.CustomTrack != null)
            {
                if (heckObject.CustomTrack.IsString)
                {
                    tracks.Add(heckObject.CustomTrack.Value);
                }
                else if (heckObject.CustomTrack.IsArray)
                {
                    foreach (var track in heckObject.CustomTrack.Children)
                    {
                        if (track.IsString)
                        {
                            tracks.Add(track);
                        }
                    }
                }
            }
        }
    }
}
