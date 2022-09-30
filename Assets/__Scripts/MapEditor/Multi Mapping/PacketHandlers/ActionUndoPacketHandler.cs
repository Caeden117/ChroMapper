using System;
using LiteNetLib.Utils;

public class ActionUndoPacketHandler : IPacketHandler
{
    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
    {
        var undoGuid = Guid.Parse(reader.GetString());
        BeatmapActionContainer.Undo(undoGuid);
    }
}
