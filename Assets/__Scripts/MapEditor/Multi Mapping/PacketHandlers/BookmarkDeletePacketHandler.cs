using System;
using LiteNetLib.Utils;
using Beatmap.Base.Customs;

[Obsolete("TODO: Refactor bookmarks to use the same systems as every other BeatmapObject")]
public class BookmarkDeletePacketHandler : IPacketHandler
{
    private BookmarkManager bookmarkManager;

    public BookmarkDeletePacketHandler(BookmarkManager bookmarkManager)
        => this.bookmarkManager = bookmarkManager;

    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
    {
        var deletionObject = reader.GetBeatmapObject();

        if (deletionObject is BaseBookmark deletionBookmark)
        {
            bookmarkManager.DeleteBookmarkAtTime(deletionBookmark.JsonTime, false);
        }
    }
}
