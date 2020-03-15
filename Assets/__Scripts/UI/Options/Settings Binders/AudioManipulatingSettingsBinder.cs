using System;
using UnityEngine;

public class AudioManipulatingSettingsBinder : SimpleSettingsBinder
{
    protected override object UIValueToSettings(object input)
    {
        AudioListener.volume = Convert.ToSingle(input);
        return Convert.ChangeType(input, Settings.AllFieldInfos[BindedSetting].FieldType);
    }
}
