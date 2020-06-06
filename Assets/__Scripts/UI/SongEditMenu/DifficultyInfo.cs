using SimpleJSON;
using System.IO;
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
        njsField.onValueChanged.AddListener((v) => UpdateValues());
        songBeatOffsetField.onValueChanged.AddListener((v) => UpdateValues());
        bpmField.onValueChanged.AddListener((v) => UpdateValues());
    }
    
    private void UpdateValues()
    {
        float.TryParse(bpmField.text, out float bpm);
        float num = 60f / bpm;
        float halfJumpDuration = 4;
        float.TryParse(njsField.text, out float songNoteJumpSpeed);
        float.TryParse(songBeatOffsetField.text, out float songStartBeatOffset);

        while (songNoteJumpSpeed * num * halfJumpDuration > 18)
            halfJumpDuration /= 2;

        halfJumpDuration += songStartBeatOffset;

        if (halfJumpDuration < 1) halfJumpDuration = 1;
        float jumpDistance = songNoteJumpSpeed * num * halfJumpDuration * 2;

        halfJumpDurationField.text = halfJumpDuration.ToString();
        jumpDistanceField.text = jumpDistance.ToString("0.00");
    }
}