public interface INetAdmin
{
    void Kick(MapperIdentityPacket identity);

    void Ban(MapperIdentityPacket identity);
}
