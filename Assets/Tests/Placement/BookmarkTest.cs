using System.Collections;
using System.Linq;
using Beatmap.Base;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    public class BookmarkTest : TestBase
    {
        [Test]
        public void CheckOrder()
        {
            var bookmarkManager = Object.FindAnyObjectByType<BookmarkManager>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

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

        // Stable failed clipboard operations while rendering the reported unsupported Japanese glyph; preserve that
        // unsupported input and verify the complete copy/paste operation still produces the note on dev.
        [UnityTest]
        public IEnumerator UnsupportedUnicodeBookmarkDoesNotPreventNoteCopyPaste()
        {
            var bookmarkManager = Object.FindAnyObjectByType<BookmarkManager>();
            var selectionController = Object.FindAnyObjectByType<SelectionController>();
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            atsc.MoveToJsonTime(1);
            bookmarkManager.CreateNewBookmark("ず");
            yield return null;

            var source = PlaceUtils.Place(new BaseNote { JsonTime = 2 });
            SelectionController.Select(source);
            selectionController.Copy();

            atsc.MoveToJsonTime(4);
            selectionController.Paste();
            yield return null;

            var pasted = SelectionController.SelectedObjects.OfType<BaseNote>().Single();
            Assert.That(pasted.JsonTime, Is.EqualTo(4));
        }
    }
}
