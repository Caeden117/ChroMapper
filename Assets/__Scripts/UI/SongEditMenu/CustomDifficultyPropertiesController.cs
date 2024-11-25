using UnityEngine;

public class CustomDifficultyPropertiesController : MonoBehaviour
{
    // TODO: Add other custom properties
    //  * ForceOneSaber
    //  * ShowRotationNoteSpawnLines
    //  * Warnings
    //  * Information
    
    [SerializeField] private GameObject EditDialog;
    [SerializeField] private EnvRemoval EnvRemoval;
    
    public void ToggleEditDialog() => EditDialog.SetActive(!EditDialog.activeSelf);
}

