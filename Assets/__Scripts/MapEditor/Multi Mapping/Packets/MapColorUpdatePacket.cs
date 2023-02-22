using LiteNetLib.Utils;

public class MapColorUpdatePacket : INetSerializable
{
    public ColorSerializable NoteLeft;
    public ColorSerializable NoteRight;
    public ColorSerializable LightLeft;
    public ColorSerializable LightRight;
    public ColorSerializable LightWhite;
    public ColorSerializable Obstacle;
    public ColorSerializable BoostLeft;
    public ColorSerializable BoostRight;
    public ColorSerializable BoostWhite;


    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NoteLeft);
        writer.Put(NoteRight);
        writer.Put(LightLeft);
        writer.Put(LightRight);
        writer.Put(LightWhite);
        writer.Put(Obstacle);
        writer.Put(BoostLeft);
        writer.Put(BoostRight);
        writer.Put(BoostWhite);
    }

    public void Deserialize(NetDataReader reader)
    {
        NoteLeft = reader.Get<ColorSerializable>();
        NoteRight = reader.Get<ColorSerializable>();
        LightLeft = reader.Get<ColorSerializable>();
        LightRight = reader.Get<ColorSerializable>();
        LightWhite = reader.Get<ColorSerializable>();
        Obstacle = reader.Get<ColorSerializable>();
        BoostLeft = reader.Get<ColorSerializable>();
        BoostRight = reader.Get<ColorSerializable>();
        BoostWhite = reader.Get<ColorSerializable>();
    }
}
