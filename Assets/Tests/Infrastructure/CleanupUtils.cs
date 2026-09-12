using System;
using System.Linq;
using Beatmap.Enums;
using Object = UnityEngine.Object;

namespace Tests.Infrastructure
{
    internal class CleanupUtils
    {
        private static readonly ObjectType[] objectTypes =
            Enum.GetValues(typeof(ObjectType)).Cast<ObjectType>().ToArray();

        public static void CleanupObjects()
        {
            // Clear GLS child nodes before parent groups so deferred group-context removal cannot leave teardown ghosts.
            CleanupType(ObjectType.GLSEvent);
            foreach (var objectType in objectTypes)
                CleanupType(objectType);
        }

        private static void CleanupBookmarks()
        {
            var bookmarkManager = Object.FindAnyObjectByType<BookmarkManager>();
            if (bookmarkManager == null) return;

            foreach (var bookmark in bookmarkManager.bookmarkContainers.ToArray()) bookmark.HandleDeleteBookmark(0);
        }

        private static void CleanupType(ObjectType type)
        {
            if (type == ObjectType.Bookmark)
            {
                CleanupBookmarks();
                return;
            }

            var container = BeatmapObjectContainerCollection.GetCollectionForType(type);
            if (container == null) return;

            // GLS children were already cleared first; avoid running their cleanup a second time through the enum order.
            if (type == ObjectType.GLSEvent)
                return;

            // RotationCallbackProperties and NJSEventsStats require the original deletion actions to invalidate
            // their state providers, while indexed tail removal still avoids stale-key searches and list shifts.
            container.DeleteAllObjectsFromEnd();
        }
    }
}
