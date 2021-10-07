using TMPro;
using UnityEngine;

public class DifficultyInfo : MonoBehaviour
{
    [SerializeField] private TMP_InputField bpmField;

    [SerializeField] private TMP_InputField halfJumpDurationField;
    [SerializeField] private TMP_InputField jumpDistanceField;

    [SerializeField] private TMP_InputField njsField;
    [SerializeField] private TMP_InputField songBeatOffsetField;

    public void Start()
    {
        njsField.onValueChanged.AddListener(v => UpdateValues());
        songBeatOffsetField.onValueChanged.AddListener(v => UpdateValues());
        bpmField.onValueChanged.AddListener(v => UpdateValues());
    }

    private void UpdateValues()
    {
        float.TryParse(bpmField.text, out var bpm);
        var num = 60f / bpm;
        float halfJumpDuration = 4;
        float.TryParse(njsField.text, out var songNoteJumpSpeed);
        float.TryParse(songBeatOffsetField.text, out var songStartBeatOffset);

        while (songNoteJumpSpeed * num * halfJumpDuration > 18)
            halfJumpDuration /= 2;

        halfJumpDuration += songStartBeatOffset;

        if (halfJumpDuration < 0.25f) halfJumpDuration = 0.25f;
        var jumpDistance = songNoteJumpSpeed * num * halfJumpDuration * 2;

        halfJumpDurationField.text = halfJumpDuration.ToString();
        jumpDistanceField.text = jumpDistance.ToString("0.00");
    }
}
