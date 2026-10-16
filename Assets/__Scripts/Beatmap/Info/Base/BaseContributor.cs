namespace Beatmap.Info
{
    public class BaseContributor
    {
        private string localImageLocation;

        public string LocalImageLocation
        {
            get => localImageLocation;
            set => localImageLocation = PathUtils.RequireFileName(value);
        }

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
