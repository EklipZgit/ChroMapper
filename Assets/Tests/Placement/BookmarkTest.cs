using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;

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
    }
}