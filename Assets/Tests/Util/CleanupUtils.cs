using System.Linq;
using Beatmap.Enums;
using UnityEngine;

namespace Tests.Util
{
    internal class CleanupUtils
    {
        public static void CleanupNotes()
        {
            CleanupType(ObjectType.Note);
        }

        public static void CleanupEvents()
        {
            CleanupType(ObjectType.Event);
        }

        public static void CleanupObstacles()
        {
            CleanupType(ObjectType.Obstacle);
        }

        public static void CleanupArcs()
        {
            CleanupType(ObjectType.Arc);
        }

        public static void CleanupChains()
        {
            CleanupType(ObjectType.Chain);
        }

        public static void CleanupBPMChanges()
        {
            CleanupType(ObjectType.BpmChange);
        }

        public static void CleanupNJSEvents()
        {
            CleanupType(ObjectType.NJSEvent);
        }

        public static void CleanupBookmarks()
        {
            var bookmarkManager = Object.FindObjectOfType<BookmarkManager>();
            foreach (var bookmark in bookmarkManager.bookmarkContainers.ToArray()) bookmark.HandleDeleteBookmark(0);
        }

        private static void CleanupType(ObjectType type)
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(type);

            foreach (var evt in eventsContainer.LoadedObjects.ToArray()) eventsContainer.DeleteObject(evt);
        }
    }
}