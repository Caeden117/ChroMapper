namespace Beatmap.Info
{
    public class BaseContributor
    {
        public string LocalImageLocation { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }


        public BaseContributor() { }

        public BaseContributor(string localImageLocation, string name, string role)
        {
            LocalImageLocation = localImageLocation;
            Name = name;
            Role = role;
        }
    }
}
