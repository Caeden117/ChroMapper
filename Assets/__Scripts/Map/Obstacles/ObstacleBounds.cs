public class ObstacleBounds
{
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float Position { get; private set; }
    public float StartHeight { get; private set; }

    public ObstacleBounds(float width, float height, float position, float startHeight)
    {
        Width = width;
        Height = height;
        Position = position;
        StartHeight = startHeight;
    }
}