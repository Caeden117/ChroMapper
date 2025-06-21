public class CountersPlusSettings : JSONDictionarySetting
{
    public CountersPlusSettings()
    {
        Add("enabled", false);
        Add("Notes", true);
        Add("Notes Per Second", true);
        Add("Swings Per Second", true);
        Add("Red/Blue Ratio", true);
        Add("Bombs", true);
        Add("Arcs", true);
        Add("Chains", true);
        Add("Obstacles", true);
        Add("Events", true);
        Add("BPM Changes", true); // Bpm Events. Name not changed to preserve settings.
        Add("Current BPM", true);
        
        // For NJS Events, I feel the user should decide which of the other stats they want to enable
        Add("NJS Events", true);
        Add("Current NJS", true);
        Add("Current HJD", false);
        Add("Current JD", false);
        Add("Current RT", false);
        
        Add("Time Spent Mapping", true);
    }
}
