using System.Collections.Generic;

namespace SimpleJSON
{
    public static class SimpleJSONHelper
    {
        public static void RemovePropertiesWithDefaultValues(JSONNode node)
        {
            if (node.IsArray)
            {
                foreach (var child in node.AsArray.Values)
                {
                    RemovePropertiesWithDefaultValues(child);
                }
            }
            else if (node.IsObject)
            {
                var keysToRemove = new List<string>();
                foreach (var key in node.Keys)
                {
                    var value = node[key];
                    if (value.IsObject || value.IsArray)
                    {
                        RemovePropertiesWithDefaultValues(value);
                    }
                    else if ((value.IsBoolean && value.AsBool == default)
                        || (value.IsNumber && value.AsFloat == default)
                        || (value.IsNull))
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    node.Remove(key);
                };
                keysToRemove.Clear();
            }
        }
    }
}