using TMPro;
using UnityEngine;

public class DifficultyInfo : MonoBehaviour
{
    [SerializeField] private TMP_InputField bpmField;

    [SerializeField] private TMP_InputField reactionTimeField;

    [SerializeField] private TMP_InputField halfJumpDurationField;
    [SerializeField] private TMP_InputField jumpDistanceField;

    [SerializeField] private TMP_InputField njsField;
    [SerializeField] private TMP_InputField songBeatOffsetField;

    public void Start()
    {
        njsField.onValueChanged.AddListener(v => UpdateValues());
        songBeatOffsetField.onValueChanged.AddListener(v => UpdateValues());
        bpmField.onValueChanged.AddListener(v => UpdateValues());

        // Reasonable to assume mappers want to keep NJS constant and adjust offset
        jumpDistanceField.onValueChanged.AddListener(v => UpdateValuesFromJumpDistance());
        halfJumpDurationField.onValueChanged.AddListener(v => UpdateValuesFromHalfJumpDuration());
        reactionTimeField.onValueChanged.AddListener(v => UpdateValuesFromReactionTime());

        reactionTimeField.onSelect.AddListener(v => RemoveMsFromText());

        jumpDistanceField.onDeselect.AddListener(v => UpdateValues());
        halfJumpDurationField.onDeselect.AddListener(v => UpdateValues());
        reactionTimeField.onDeselect.AddListener(v => UpdateValues());
    }

    private void UpdateValues()
    {
        float.TryParse(bpmField.text, out var bpm);
        float.TryParse(njsField.text, out var songNoteJumpSpeed);
        float.TryParse(songBeatOffsetField.text, out var songStartBeatOffset);

        var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(songNoteJumpSpeed, songStartBeatOffset, bpm);

        var num = 60 / bpm;
        var jumpDistance = songNoteJumpSpeed * num * halfJumpDuration * 2;

        var beatms = 60000 / bpm;
        var reactionTime = beatms * halfJumpDuration;

        if (!halfJumpDurationField.isFocused) halfJumpDurationField.SetTextWithoutNotify(halfJumpDuration.ToString());
        if (!jumpDistanceField.isFocused) jumpDistanceField.SetTextWithoutNotify(jumpDistance.ToString("0.00"));
        if (!reactionTimeField.isFocused) reactionTimeField.SetTextWithoutNotify(reactionTime.ToString("N0") + " ms");
    }

    private void RemoveMsFromText() => reactionTimeField.text = reactionTimeField.text.Split()[0];

    private void UpdateValuesFromReactionTime()
    {
        float.TryParse(bpmField.text, out var bpm);
        float.TryParse(reactionTimeField.text, out var reactionTime);

        SetSongBeatOffset(Mathf.Max(0.25f, reactionTime / ((60000 / bpm))));
    }

    private void UpdateValuesFromJumpDistance()
    {
        float.TryParse(bpmField.text, out var bpm);
        float.TryParse(njsField.text, out var songNoteJumpSpeed);
        float.TryParse(jumpDistanceField.text, out var jumpDistance);

        SetSongBeatOffset(Mathf.Max(0.25f, jumpDistance / ((60 / bpm) * songNoteJumpSpeed * 2)));
    }

    private void UpdateValuesFromHalfJumpDuration()
    {
        float.TryParse(halfJumpDurationField.text, out var halfJumpDuration);

        SetSongBeatOffset(Mathf.Max(0.25f, halfJumpDuration));
    }

    private void SetSongBeatOffset(float hjdAfterOffset)
    {
        float.TryParse(bpmField.text, out var bpm);
        float.TryParse(njsField.text, out var songNoteJumpSpeed);

        var hjdBeforeOffset = SpawnParameterHelper.CalculateHalfJumpDuration(songNoteJumpSpeed, 0, bpm);
        songBeatOffsetField.text = (hjdAfterOffset - hjdBeforeOffset).ToString();
    }
}
