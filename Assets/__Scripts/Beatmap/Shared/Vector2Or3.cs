namespace Beatmap.Shared
{
    public struct Vector2Or3
    {
        public Vector2Or3(float? x, float? y, float? z = null)
        {
            this.x = x ?? 0f;
            this.y = y ?? 0f;
            this.z = z;
        }

        public float x { get; }
        public float y { get; }
        public float? z { get; }
    }
}