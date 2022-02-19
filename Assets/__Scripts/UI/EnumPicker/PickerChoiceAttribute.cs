using System;

public class PickerChoiceAttribute : Attribute
{
    public string Table { get; private set; }
    public string Entry { get; private set; }
    public PickerChoiceAttribute(string table, string entry)
    {
        Table = table;
        Entry = entry;
    }
}