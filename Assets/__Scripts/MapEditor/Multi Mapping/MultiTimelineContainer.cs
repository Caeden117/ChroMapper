using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiTimelineContainer : MonoBehaviour
{
    [SerializeField] private Graphic coloredGraphic;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Outline outline;

    private MultiTimelineController source;
    private MapperPosePacket pose;

    public void Init(MultiTimelineController source, MapperIdentityPacket identity)
    {
        this.source = source;
        coloredGraphic.color = identity.Color;
        nameText.text = identity.Name;

        var hsv = HSVUtil.ConvertRgbToHsv(identity.Color);
        hsv.V = 0.5;
        outline.effectColor = HSVUtil.ConvertHsvToRgb(hsv.H, hsv.S, hsv.V, 1);
    }

    public void RefreshPosition(MapperPosePacket pose, float width, float songLength)
    {
        this.pose = pose;
        var unitsPerBeat = width / songLength;
        var rectTransform = (RectTransform)transform;
        rectTransform.anchoredPosition = new Vector2(unitsPerBeat * pose.SongPosition, 50);
    }

    public void JumpToMapper() => source.JumpTo(pose);
}
