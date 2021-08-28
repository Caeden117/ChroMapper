using UnityEngine;

public class OptionsLauncher : MonoBehaviour
{
    public void OpenOptions(int startingGroupID) => OptionsController.ShowOptions(startingGroupID);
}
