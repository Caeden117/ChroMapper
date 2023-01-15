using LiteNetLib.Utils;

public class ActionCreatedPacketHandler : IPacketHandler
{
    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
    {
        var action = reader.GetBeatmapAction(identity);
        BeatmapActionContainer.AddAction(action, true);
    }
}
