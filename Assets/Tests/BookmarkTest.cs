using NUnit.Framework;
using System.Collections;
using Tests.Util;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests
{
    public class BookmarkTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void Cleanup()
        {
            var bookmarkManager = Object.FindObjectOfType<BookmarkManager>();

            foreach (BookmarkContainer bookmark in bookmarkManager.bookmarkContainers.ToArray())
            {
                bookmark.HandleDeleteBookmark(0);
            }
        }

        [Test]
        public void CheckOrder()
        {
            var bookmarkManager = Object.FindObjectOfType<BookmarkManager>();
            var atsc = Object.FindObjectOfType<AudioTimeSyncController>();

            atsc.MoveToTimeInBeats(1);
            bookmarkManager.HandleNewBookmarkName("1");

            atsc.MoveToTimeInBeats(3);
            bookmarkManager.HandleNewBookmarkName("3");

            atsc.MoveToTimeInBeats(2);
            bookmarkManager.HandleNewBookmarkName("2");

            bookmarkManager.OnPreviousBookmark();
            Assert.AreEqual(1, atsc.CurrentBeat);

            bookmarkManager.OnPreviousBookmark();
            Assert.AreEqual(1, atsc.CurrentBeat);

            bookmarkManager.OnNextBookmark();
            Assert.AreEqual(2, atsc.CurrentBeat);

            bookmarkManager.OnNextBookmark();
            Assert.AreEqual(3, atsc.CurrentBeat);

            bookmarkManager.OnNextBookmark();
            Assert.AreEqual(3, atsc.CurrentBeat);
        }
    }
}
