namespace Beatmap.Base.Customs
{
    public abstract class IContributor : IItem
    {
        string LocalImageLocation { get; set; }
        string Name { get; set; }
        string Role { get; set; }
    }
}
