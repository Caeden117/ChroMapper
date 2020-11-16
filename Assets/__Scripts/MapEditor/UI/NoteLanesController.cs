using UnityEngine;

public class NoteLanesController : MonoBehaviour {

    public Transform noteGrid;
    [SerializeField] private GridChild notePlacementGridChild;

    private void Start()
    {
        Settings.NotifyBySettingName("NoteLanes", UpdateNoteLanes);
        UpdateNoteLanes(4);
    }

    private void OnDestroy()
    {
        Settings.ClearSettingNotifications("NoteLanes");
    }

    public void UpdateNoteLanes(object value)
    {
        string noteLanesText = value.ToString();
        if (int.TryParse(noteLanesText, out int noteLanes))
        {
            if (noteLanes < 4) return;
            noteLanes = noteLanes - (noteLanes % 2); //Sticks to even numbers for note lanes.
            notePlacementGridChild.Size = noteLanes / 2;
            noteGrid.localScale = new Vector3((float)noteLanes / 10, 1, noteGrid.localScale.z);
        }
    }
}
