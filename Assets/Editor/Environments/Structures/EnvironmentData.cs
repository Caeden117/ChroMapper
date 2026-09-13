using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// Main class for EnvData information. This class, and all its children, is structured according to the JSON schema used by EnvironmentData:
/// https://ugecko.github.io/chroodleWeb/data/envdata/#__tabbed_1_5
/// </summary>
public class EnvironmentData
{
    [JsonProperty("environmentData")] public EnvironmentDataInfo Data;
    public List<EnvironmentDataObject> Objects = new();
}
