using System.Collections;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BookmarkTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void Cleanup()
        {
            CleanupUtils.CleanupBookmarks();
        }

        [Test]
        public void CheckOrder()
        {
            var bookmarkManager = Object.FindObjectOfType<BookmarkManager>();
            var atsc = Object.FindObjectOfType<AudioTimeSyncController>();

            atsc.MoveToSongBpmTime(1);
            bookmarkManager.CreateNewBookmark("1");

            atsc.MoveToSongBpmTime(3);
            bookmarkManager.CreateNewBookmark("3");

            atsc.MoveToSongBpmTime(2);
            bookmarkManager.CreateNewBookmark("2");

            bookmarkManager.OnPreviousBookmark();
            Assert.AreEqual(1, atsc.CurrentJsonTime);

            bookmarkManager.OnPreviousBookmark();
            Assert.AreEqual(1, atsc.CurrentJsonTime);

            bookmarkManager.OnNextBookmark();
            Assert.AreEqual(2, atsc.CurrentJsonTime);

            bookmarkManager.OnNextBookmark();
            Assert.AreEqual(3, atsc.CurrentJsonTime);

            bookmarkManager.OnNextBookmark();
            Assert.AreEqual(3, atsc.CurrentJsonTime);
        }
    }
}