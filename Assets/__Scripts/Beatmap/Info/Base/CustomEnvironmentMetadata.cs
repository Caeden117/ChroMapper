namespace Beatmap.Info
{
    public struct CustomEnvironmentMetadata
    {
        public string Name { get; set; }
        
        // This is calculated on save - except if the user loads a map and doesn't have the custom environment
        public string Hash { get; set; }
    }
}