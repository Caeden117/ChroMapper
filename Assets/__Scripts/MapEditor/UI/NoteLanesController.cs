using UnityEngine;
using UnityEngine.Serialization;

public class NoteLanesController : MonoBehaviour
{
    [FormerlySerializedAs("noteGrid")] public Transform NoteGrid;
    [SerializeField] private GridChild notePlacementGridChild;

    private void Start()
    {
        Settings.NotifyBySettingName("NoteLanes", UpdateNoteLanes);
        UpdateNoteLanes(4);
        if (Settings.NonPersistentSettings.ContainsKey("NoteLanes")) Settings.NonPersistentSettings["NoteLanes"] = 4;
    }

    private void OnDestroy() => Settings.ClearSettingNotifications("NoteLanes");

    public void UpdateNoteLanes(object value)
    {
        var noteLanesText = value.ToString();
        if (int.TryParse(noteLanesText, out var noteLanes))
        {
            if (noteLanes < 4) return;
            noteLanes -= noteLanes % 2; //Sticks to even numbers for note lanes.
            notePlacementGridChild.Size = noteLanes / 2;
            NoteGrid.localScale = new Vector3((float)noteLanes / 10, 1, NoteGrid.localScale.z);
        }
    }
}
