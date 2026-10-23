using System;
using System.Collections.Generic;
using SimpleJSON;

namespace Beatmap.Helper
{
    internal static class TrackDataReader
    {
        internal static string[] ReadNames(JSONNode data)
        {
            if (data is JSONString name)
                return new[] { name.Value };

            if (data is not JSONArray array)
                return Array.Empty<string>();

            var names = new List<string>(array.Count);
            foreach (var item in array.Children)
            {
                if (item is JSONString trackName)
                    names.Add(trackName.Value);

            }

            return names.ToArray();
        }
    }
}
