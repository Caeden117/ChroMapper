using System.Collections.Generic;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class LightWithIdManagerData
{
    public Dictionary<int, LightId[]> Lights;

    public class LightId
    {
        public int InstanceId;
        public int? ArrayId;
    }
}
