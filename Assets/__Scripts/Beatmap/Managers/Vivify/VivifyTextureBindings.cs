using System.Collections.Generic;
using UnityEngine;

/// <summary>Restores global shader textures when overlapping map resources are released in any order.</summary>
public sealed class VivifyTextureBindings
{
    private readonly Dictionary<string, Binding> bindings = new();

    /// <summary>Publishes an owner's texture. The latest call for a name takes precedence.</summary>
    public void Set(object owner, string name, Texture texture)
    {
        if (owner == null || string.IsNullOrEmpty(name)) return;
        if (!bindings.TryGetValue(name, out var binding))
        {
            binding = new Binding(Shader.GetGlobalTexture(name));
            bindings.Add(name, binding);
        }
        for (var i = 0; i < binding.Owners.Count; i++)
        {
            if (!ReferenceEquals(binding.Owners[i].Owner, owner)) continue;
            binding.Owners.RemoveAt(i);
            break;
        }
        binding.Owners.Add(new Entry(owner, texture));
        Shader.SetGlobalTexture(name, texture);
    }

    /// <summary>Releases a binding before its texture is destroyed, restoring the next owner or original texture.</summary>
    public void Remove(object owner, string name)
    {
        if (string.IsNullOrEmpty(name) || !bindings.TryGetValue(name, out var binding)) return;
        var previous = binding.Owners[binding.Owners.Count - 1].Texture;
        for (var i = binding.Owners.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(binding.Owners[i].Owner, owner)) continue;
            binding.Owners.RemoveAt(i);
            break;
        }
        // Preserve assignments made outside this registry.
        if (Shader.GetGlobalTexture(name) == previous)
        {
            var texture = binding.Owners.Count > 0 ? binding.Owners[binding.Owners.Count - 1].Texture : binding.Previous;
            Shader.SetGlobalTexture(name, texture != null ? texture : null);
        }
        if (binding.Owners.Count == 0) bindings.Remove(name);
    }

    public void Reset()
    {
        foreach (var entry in bindings)
        {
            var binding = entry.Value;
            if (Shader.GetGlobalTexture(entry.Key) == binding.Owners[binding.Owners.Count - 1].Texture)
                Shader.SetGlobalTexture(entry.Key, binding.Previous != null ? binding.Previous : null);
        }
        bindings.Clear();
    }

    private sealed class Binding
    {
        public readonly Texture Previous;
        public readonly List<Entry> Owners = new();
        public Binding(Texture previous) => Previous = previous;
    }

    private readonly struct Entry
    {
        public readonly object Owner;
        public readonly Texture Texture;
        public Entry(object owner, Texture texture)
        {
            Owner = owner;
            Texture = texture;
        }
    }
}
