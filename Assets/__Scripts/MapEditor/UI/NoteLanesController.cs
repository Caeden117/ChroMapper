using UnityEngine;

public class NoteLanesController : MonoBehaviour {

    public Transform noteGrid;

    public float NoteLanes
    {
        get
        {
            return (noteGrid.localScale.x - 0.01f) * 10;
        }
    }

    public void UpdateNoteLanes(string noteLanesText)
    {
        if (int.TryParse(noteLanesText, out int noteLanes))
        {
            if (noteLanes < 4) return;
            noteLanes = noteLanes - (noteLanes % 2); //Sticks to even numbers for note lanes.
            noteGrid.localScale = new Vector3((float)noteLanes / 10 + 0.01f, 1, noteGrid.localScale.z);
            foreach(BoxCollider boxCollider in noteGrid.GetComponentsInChildren<BoxCollider>())
            {
                //Here's a sort of hacky fix to get Notes not snapping outside the visual grid.
                //For the interface grids (vertical), we want the entire box collider to shrink.
                //For the horizontal grids, we can only scale down the sides, not forward/backward.
                if (boxCollider.transform.GetComponent<PlacementMessageSender>() == null) continue;
                float scaleX = 10 / noteGrid.localScale.x;
                Vector3 newSize = new Vector3(scaleX * (noteGrid.localScale.x - 0.02f), boxCollider.size.y, 0);
                if (boxCollider.transform.parent.name.Contains("Interface"))
                {
                    float scaleZ = 10 / boxCollider.transform.parent.localScale.z;
                    newSize += new Vector3(0, 0, scaleZ * (boxCollider.transform.parent.localScale.z - 0.2f));
                }
                else
                {
                    newSize += new Vector3(0, 0, boxCollider.size.z);
                }
                boxCollider.size = newSize;
            }
        }
    }
}
