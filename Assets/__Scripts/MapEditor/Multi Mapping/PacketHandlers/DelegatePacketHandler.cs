using System;
using LiteNetLib.Utils;

public class DelegatePacketHandler : IPacketHandler
{
    private Action<MultiNetListener, MapperIdentityPacket, NetDataReader> onHandlePacket;

    public DelegatePacketHandler(Action<MultiNetListener, MapperIdentityPacket, NetDataReader> onHandlePacket)
        => this.onHandlePacket = onHandlePacket;

    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
        => onHandlePacket?.Invoke(client, identity, reader);
}
