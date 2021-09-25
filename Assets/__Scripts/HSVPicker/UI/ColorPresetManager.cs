using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.HSVPicker
{
    public static class ColorPresetManager
    {
        public static Dictionary<string, ColorPresetList> Presets = new Dictionary<string, ColorPresetList>();

        public static ColorPresetList Get(string listId = "default")
        {
            if (!Presets.TryGetValue(listId, out var preset))
            {
                preset = new ColorPresetList(listId);
                Presets.Add(listId, preset);
            }

            return preset;
        }
    }

    public class ColorPresetList
    {
        public ColorPresetList(string listId, List<Color> colors = null)
        {
            if (colors == null) colors = new List<Color>();

            Colors = colors;
            ListId = listId;
        }

        public string ListId { get; }
        public List<Color> Colors { get; }

        public event UnityAction<List<Color>> ColorsUpdated;

        public void AddColor(Color color)
        {
            Colors.Add(color);
            if (ColorsUpdated != null) ColorsUpdated.Invoke(Colors);
        }

        public void UpdateList(IEnumerable<Color> colors)
        {
            Colors.Clear();
            Colors.AddRange(colors);

            if (ColorsUpdated != null) ColorsUpdated.Invoke(Colors);
        }
    }
}
