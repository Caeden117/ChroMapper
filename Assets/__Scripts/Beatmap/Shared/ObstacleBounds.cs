namespace Beatmap.Shared
{
    public class ObstacleBounds
    {
        public ObstacleBounds(float width, float height, float position, float startHeight)
        {
            Width = width;
            Height = height;
            Position = position;
            StartHeight = startHeight;
        }

        public float Width { get; }
        public float Height { get; }
        public float Position { get; }
        public float StartHeight { get; }
    }
}