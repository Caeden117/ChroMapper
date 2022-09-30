using System;
using LiteNetLib.Utils;

public class ActionRedoPacketHandler : IPacketHandler
{
    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
    {
        var redoGuid = Guid.Parse(reader.GetString());
        BeatmapActionContainer.Redo(redoGuid);
    }
}
