using LiteNetLib.Utils;

public interface IPacketHandler
{
    void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader);
}
