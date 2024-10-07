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

        /// <summary>
        ///     Loops through all children of a JSON object, and remove any that are null or empty.
        ///     This help makes _customData objects compliant with BeatSaver schema in a reusable and smart way.
        /// </summary>
        /// <param name="obj">Object of which to loop through and remove all empty children from.</param>
        public static JSONNode CleanObject(JSONNode obj)
        {
            if (obj is null) return null;
            var clone = obj.Clone();
            foreach (var key in clone.Keys)
            {
                if (obj.HasKey(key) && (obj[key].IsNull || obj[key].AsArray?.Count <= 0 ||
                                        (!obj.IsArray && !obj.IsObject && string.IsNullOrEmpty(obj[key].Value))))
                {
                    obj.Remove(key);
                }
            }

            return obj;
        }
    }
}