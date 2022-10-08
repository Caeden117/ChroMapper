namespace Beatmap.Base.Customs
{
    public abstract class BaseContributor : BaseItem
    {
        string LocalImageLocation { get; set; }
        string Name { get; set; }
        string Role { get; set; }
    }
}
