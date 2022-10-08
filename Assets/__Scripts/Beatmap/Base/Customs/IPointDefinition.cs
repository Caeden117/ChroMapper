using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public interface IPointDefinitionAry
    {
        string Name { get; set; }
        JSONNode[][] Point { get; set; }
    }

    public interface IPointDefinition
    {
        IDictionary<string, object[][]> PointDefinitionDict { get; set; }
        IPointDefinitionAry PointDefinitionAry { get; set; }
    }
}
