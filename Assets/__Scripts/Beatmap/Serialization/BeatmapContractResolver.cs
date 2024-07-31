using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class BeatmapContractResolver<T> : DefaultContractResolver
{
    private static readonly BindingFlags searchFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private Dictionary<string, IEnumerable<string>> propertyMappings = new();

    public BeatmapContractResolver()
    {
        var reflectionType = typeof(T);

        // Through LINQ, gather all fields and properties that have our BeatmapProperty attribute
        var allMembers = reflectionType
            .GetFields(searchFlags)
            .Cast<MemberInfo>()
            .Union(reflectionType
                .GetProperties(searchFlags)
                .Cast<MemberInfo>())
            .Where(it => Attribute.IsDefined(it, typeof(BeatmapProperty)));
    
        // Construct our dictionary of beatmap properties to .NET property name
        foreach (var member in allMembers)
        {
            // Gather all constructor arguments for all BeatmapProperty attributes on each member
            var beatmapPropertyAttributes = member
                .GetCustomAttributesData()
                .Where(it => it.AttributeType == typeof(BeatmapProperty))
                .SelectMany(it => it.ConstructorArguments);

            // Add these BeatmapProperty arguments to the property map dictionary
            propertyMappings.Add(member.Name, beatmapPropertyAttributes.Select(it => it.Value.ToString()));
        }
    }

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization) {
        // Generate our initial list through Newtonsoft.JSON
        var initialList = base.CreateProperties(type, memberSerialization);

        var count = initialList.Count;
        for (var i = 0; i < count; i++)
        {
            var property = initialList[i];
            if (!propertyMappings.TryGetValue(property.PropertyName, out var mappings)) continue;
        
            // For each BeatmapProperty mapping, generate a duplicate of the property that uses our new mapping
            foreach (var mapping in mappings)
            {
                var duplicate = new JsonProperty
                {
                    AttributeProvider = property.AttributeProvider,
                    Converter = property.Converter,
                    DeclaringType = property.DeclaringType,
                    DefaultValue = property.DefaultValue,
                    DefaultValueHandling = property.DefaultValueHandling,
                    GetIsSpecified = property.GetIsSpecified,
                    HasMemberAttribute = property.HasMemberAttribute,
                    Ignored = property.Ignored,
                    IsReference = property.IsReference,
                    ItemConverter = property.ItemConverter,
                    ItemIsReference = property.ItemIsReference,
                    ItemReferenceLoopHandling = property.ItemReferenceLoopHandling,
                    ItemTypeNameHandling = property.ItemTypeNameHandling,
                    NullValueHandling = property.NullValueHandling,
                    ObjectCreationHandling = property.ObjectCreationHandling,
                    Order = property.Order,
                    PropertyName = property.PropertyName,
                    PropertyType = property.PropertyType,
                    Readable = property.Readable,
                    ReferenceLoopHandling = property.ReferenceLoopHandling,
                    Required = property.Required,
                    SetIsSpecified = property.SetIsSpecified,
                    ShouldDeserialize = property.ShouldDeserialize,
                    ShouldSerialize = property.ShouldSerialize,
                    TypeNameHandling = property.TypeNameHandling,
                    UnderlyingName = property.UnderlyingName,
                    ValueProvider = property.ValueProvider,
                    Writable = property.Writable,
                };

                // Overwrite our property name with the beatmap property mapping
                duplicate.PropertyName = mapping;

                initialList.Add(duplicate);
            }
        }

        return initialList;
    }
}
