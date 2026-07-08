using System;
using System.Linq;
using Beatmap.Enums;
using Object = UnityEngine.Object;

namespace Tests.Util
{
    internal class CleanupUtils
    {
        private static readonly ObjectType[] objectTypes =
            Enum.GetValues(typeof(ObjectType)).Cast<ObjectType>().ToArray();

        public static void CleanupObjects()
        {
            foreach (var objectType in objectTypes) CleanupType(objectType);
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

            foreach (var evt in container.LoadedObjects.ToArray()) container.DeleteObject(evt);
        }
    }
}