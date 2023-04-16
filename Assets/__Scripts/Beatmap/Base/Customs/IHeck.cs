using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public interface IHeckObject
    {
        JSONNode CustomTrack { get; set; }

        string CustomKeyTrack { get; }
    }
}
