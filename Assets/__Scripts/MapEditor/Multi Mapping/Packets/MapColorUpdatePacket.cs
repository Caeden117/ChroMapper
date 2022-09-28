using LiteNetLib.Utils;

public class MapColorUpdatePacket : INetSerializable
{
    public ColorSerializable NoteLeft;
    public ColorSerializable NoteRight;
    public ColorSerializable LightLeft;
    public ColorSerializable LightRight;
    public ColorSerializable Obstacle;
    public ColorSerializable BoostLeft;
    public ColorSerializable BoostRight;


    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NoteLeft);
        writer.Put(NoteRight);
        writer.Put(LightLeft);
        writer.Put(LightRight);
        writer.Put(Obstacle);
        writer.Put(BoostLeft);
        writer.Put(BoostRight);
    }

    public void Deserialize(NetDataReader reader)
    {
        NoteLeft = reader.Get<ColorSerializable>();
        NoteRight = reader.Get<ColorSerializable>();
        LightLeft = reader.Get<ColorSerializable>();
        LightRight = reader.Get<ColorSerializable>();
        Obstacle = reader.Get<ColorSerializable>();
        BoostLeft = reader.Get<ColorSerializable>();
        BoostRight = reader.Get<ColorSerializable>();
    }
}
