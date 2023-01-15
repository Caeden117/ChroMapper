using LiteNetLib.Utils;

public class MapColorUpdatePacketHandler : IPacketHandler
{
    private CustomColorsUIController customColors;

    public MapColorUpdatePacketHandler(CustomColorsUIController customColors)
        => this.customColors = customColors;

    public void HandlePacket(MultiNetListener client, MapperIdentityPacket identity, NetDataReader reader)
        => customColors.UpdateCustomColorsFromPacket(reader.Get<MapColorUpdatePacket>());
}
