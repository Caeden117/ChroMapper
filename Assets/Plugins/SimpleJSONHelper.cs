using System;
using System.Collections.Generic;

namespace SimpleJSON
{
    public static class SimpleJSONHelper
    {
        private const string v2CustomData = "_customData";
        private const string v3CustomData = "customData";
        
        public static JSONArray MapSequenceToJSONArray<T>(IEnumerable<T> source, Func<T, JSONNode> func)
        {
            var array = new JSONArray();

            foreach (var element in source)
            {
                array.Add(func(element));
            }
            
            return array;
        }
        

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
                    if (key == v2CustomData || key == v3CustomData) continue;

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