using System;

public class PluginAttribute : Attribute
{
    public string name;

    public PluginAttribute(string name)
    {
        this.name = name;
    }
}
