public class CountersPlusSettings : JSONDictionarySetting
{
    public CountersPlusSettings() : base()
    {
        Add("enabled", false);
        Add("Notes", true);
        Add("Notes Per Second", true);
        Add("Swings Per Second", true);
        Add("Red/Blue Ratio", true);
        Add("Bombs", true);
        Add("Obstacles", true);
        Add("Events", true);
        Add("BPM Changes", true);
        Add("Current BPM", true);
        Add("Time Spent Mapping", true);
    }
}