using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;

public class BeatmapContractResolver<TBeatmap> : DefaultContractResolver where TBeatmap : BaseBeatmapProperty
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
        var baseProperty = base.CreateProperty(member, memberSerialization);

        // Gather all constructor arguments for all beatmap attributes on each member
        var beatmapPropertyAttributes = member
            .GetCustomAttributesData()
            .Where(it => it.AttributeType == typeof(TBeatmap))
            .SelectMany(it => it.ConstructorArguments);

        // Select the first property name to use
        var propertyName = beatmapPropertyAttributes.FirstOrDefault();
        if (propertyName != default)
        {
            baseProperty.PropertyName = propertyName.Value.ToString();
        }

        return baseProperty;
    }
}
