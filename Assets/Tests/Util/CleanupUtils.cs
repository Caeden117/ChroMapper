using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
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

        public static void CleanupBookmarks()
        {
            BookmarkManager bookmarkManager = Object.FindObjectOfType<BookmarkManager>();
            foreach (BookmarkContainer bookmark in bookmarkManager.bookmarkContainers.ToArray())
            {
                bookmark.HandleDeleteBookmark(0);
            }
        }
        
        private static void CleanupType(ObjectType type)
        {
            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(type);

            foreach (BaseObject evt in eventsContainer.LoadedObjects.ToArray())
            {
                eventsContainer.DeleteObject(evt);
            }
        }
    }
}
