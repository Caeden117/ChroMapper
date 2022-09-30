using System;
using LiteNetLib.Utils;

[Obsolete("TODO: Refactor bookmarks to use the same systems as every other BeatmapObject")]
public class BookmarkCreatePacketHandler : IPacketHandler
{
    private BookmarkManager bookmarkManager;

    public BookmarkCreatePacketHandler(BookmarkManager bookmarkManager)
        => this.bookmarkManager = bookmarkManager;

    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
    {
        var creationObject = reader.GetBeatmapObject();

        if (creationObject is BeatmapBookmark creationBookmark)
        {
            bookmarkManager.AddBookmark(creationBookmark, false);
        }
    }
}
