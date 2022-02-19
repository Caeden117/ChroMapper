using System;

public class PluginAttribute : Attribute
{
    public string Name;

    public PluginAttribute(string name) => Name = name;
}
