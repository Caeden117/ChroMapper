using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class OSCMessageSender : MonoBehaviour
{
    [SerializeField] private OSC osc;
    [SerializeField] private BeatmapObjectCallbackController visualGridCallback;

    private bool readyToGo = false;
    public static bool IsActive { get; private set; } = false;

    private void Start()
    {
        readyToGo = ValidateOSCStatus(osc, out _, out _);
        visualGridCallback.EventPassedThreshold += EventPassed;
    }

    public bool ValidateOSCStatus(OSC osc, out bool ipMatches, out bool portsMatches)
    {
        //Create two regex expressions to make sure OSC is ready to use.
        Regex ipRegex = new Regex(@"\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b");
        Regex tcpIpPortRegex = new Regex(@"/^([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$/");
        //Probably dont need these checks, but its safe to have 'em.
        ipMatches = ipRegex.IsMatch(osc.outIP);
        portsMatches = tcpIpPortRegex.IsMatch(osc.inPort.ToString()) && tcpIpPortRegex.IsMatch(osc.outPort.ToString());
        return ipMatches && portsMatches;
    }

    public void UpdateActive(bool enabled)
    {
        IsActive = enabled;
    }

    private void EventPassed(bool initial, int index, BeatmapObject data)
    {
        if (!readyToGo || !IsActive) return;
        MapEvent e = data as MapEvent;
        if (!e.IsUtilityEvent())
        {
            OscMessage message = new OscMessage();
            message.address = $"/CM_EventType{e._type}";
            if (e._value >= ColourManager.RGB_INT_OFFSET)
            {
                Color color = ColourManager.ColourFromInt(e._value);
                message.values.Add(color.r);
                message.values.Add(color.g);
                message.values.Add(color.b);
            }
            else message.values.Add(e._value);
            osc?.Send(message);
        }
    }

    private void OnDestroy()
    {
        visualGridCallback.EventPassedThreshold -= EventPassed;
    }
}
